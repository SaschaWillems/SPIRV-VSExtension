using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SPIRVExtension
{
    public delegate bool CompileFunc(string a, out List<string> b);

    /// <summary>
    /// Base class for commands that compile to SPIR-V
    /// </summary>
    public class ShaderCommandBase
    {
        private readonly Package package;
        public string name;

        public ShaderCommandBase(Package package, string name)
        {
            this.package = package;
            this.name = name;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Read hierarchy of an item recursively to also include files in folders
        /// </summary>
        public void ReadItemHierarchy(uint itemid, IVsHierarchy hierarchy, List<uint> itemids)
        {
            itemids.Add(itemid);

            object value = null;
            int res = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_FirstVisibleChild, out value);

            if (res != VSConstants.S_OK || value == null)
            {
                // Single item, add and return                
                if (value is int)
                {
                    itemids.Add(Convert.ToUInt32(value));
                    return;
                }
            }
            else
            {
                // Item has siblings (e.g. folder)
                while (res == VSConstants.S_OK && value != null)
                {
                    if (value is int && (uint)(int)value == VSConstants.VSITEMID_NIL)
                    {
                        break;
                    }
                    uint childNode = Convert.ToUInt32(value);
                    ReadItemHierarchy(childNode, hierarchy, itemids);
                    res = hierarchy.GetProperty(childNode, (int)__VSHPROPID.VSHPROPID_NextVisibleSibling, out value);
                }
            }
        }

        /// <summary>
        /// Returns a list of valid shader files from the current file selection
        /// </summary>
        /// <param name="shaderFileNames">List to be filled with all valid shader files from the current selection</param>
        /// <returns>True if at least one shader has been selected</returns>
        public bool GetSelectedShaderFiles(List<ShaderFile> shaderFiles)
        {
            IVsHierarchy hierarchy = null;
            uint itemid = VSConstants.VSITEMID_NIL;
            int hr = VSConstants.S_OK;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            IVsMultiItemSelect multiItemSelect = null;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;            

            try
            {
                hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    return false;
                }

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                {
                    return false;
                }

                List<uint> itemids = new List<uint>();

                if (multiItemSelect == null)
                {
                    ReadItemHierarchy(itemid, hierarchy, itemids);
                }
                else
                {
                    // todo: Read hierarchy for multi selects
                    uint itemCount = 0;
                    int fSingleHierarchy = 0;
                    hr = multiItemSelect.GetSelectionInfo(out itemCount, out fSingleHierarchy);

                    VSITEMSELECTION[] items = new VSITEMSELECTION[itemCount];
                    hr = multiItemSelect.GetSelectedItems(0, itemCount, items);

                    foreach (var item in items)
                    {
                        itemids.Add(item.itemid);
                    }

                }

                foreach (var id in itemids)
                {
                    string filepath = null;
                    ((IVsProject)hierarchy).GetMkDocument(id, out filepath);
                    if (filepath != null && ReferenceCompiler.IsShaderFile(filepath))
                    {
                        var transformFileInfo = new FileInfo(filepath);
                        shaderFiles.Add(new ShaderFile(id, hierarchy, filepath));
                    }
                }

                // todo: hierarchy node
                if (itemid == VSConstants.VSITEMID_ROOT)
                {
                    return false;
                }

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                {
                    return false;
                }

                return (shaderFiles.Count > 0);
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                    Marshal.Release(selectionContainerPtr);
                if (hierarchyPtr != IntPtr.Zero)
                    Marshal.Release(hierarchyPtr);
            }
        }

        /// <summary>
        /// Gets the selected shader file count
        /// </summary>
        /// <returns>Count of valid selected shader files</returns>
        public int GetSelectedShaderFileCount()
        {
            List<ShaderFile> selectedShaderFiles = new List<ShaderFile>();
            if (GetSelectedShaderFiles(selectedShaderFiles))
            {
                return selectedShaderFiles.Count;
            }
            return 0;
        }

        /// <summary>
        /// Parse error output from the reference compiler and add it to the error list
        /// </summary>
        /// <param name="validatorOutput">Output of the reference compiler</param>
        /// <param name="shaderFile">Shader file info for which the validator output has been generated</param>
        public void ParseErrors(List<string> validatorOutput, ShaderFile shaderFile)
        {
            foreach (string line in validatorOutput)
            {
                MatchCollection matches = Regex.Matches(line, @"\d+:", RegexOptions.IgnoreCase);
                // Example: ERROR: 0:26: 'aaa' : undeclared identifier 
                if (matches.Count > 1)
                {
                    // Line
                    int errorLine = Convert.ToInt32(matches[1].Value.Replace(":", ""));
                    // Error message
                    string msg = line;
                    Match match = Regex.Match(line, @"ERROR:\s.*\d+:(.*)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        msg = match.Groups[1].Value;
                    }
                    ErrorList.Add(msg, shaderFile.fileName, errorLine, 0, shaderFile.hierarchy);
                }
            }
        }

        /// <summary>
        /// Compile the shader file using the given compilation function
        /// </summary>
        public void CompileShaders(List<ShaderFile> shaderFiles, CompileFunc compileFunc)
        {
            string title = name;
            string msg;

            if (ReferenceCompiler.Locate() == null)
            {
                msg = "Could not locate the glslang reference compiler (glslangvalidator.exe) in system path!";
                VsShellUtilities.ShowMessageBox(ServiceProvider, msg, title, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                OutputWindow.Add(msg);
                return;
            }
            
            ErrorList.Clear();

            bool success = true;
            msg = "";
            foreach (var shaderFile in shaderFiles)
            {
                List<string> validatorOutput;
                bool compiled = compileFunc(shaderFile.fileName, out validatorOutput);
                if (compiled)
                {
                    OutputWindow.Add(string.Join("\n", validatorOutput));
                }
                else
                {
                    msg += string.Format(CultureInfo.CurrentCulture, "Shader \"{0}\" could not be compiled to SPIR-V!", shaderFile.fileName) + "\n";
                    Debug.Write(msg);
                    ParseErrors(validatorOutput, shaderFile);
                    OutputWindow.Add(shaderFile.fileName + "\n" + string.Join("\n", validatorOutput));
                    success = false;
                }
            }

            if (success)
            {
                if (shaderFiles.Count == 1)
                {
                    msg = string.Format(CultureInfo.CurrentCulture, "Shader successfully compiled to \"{0}\"", shaderFiles[0].fileName + ".spv");
                }
                else
                {
                    msg = "All shaders successfully compiled to SPIR-V";
                }
                VsShellUtilities.ShowMessageBox(ServiceProvider, msg, title, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            else
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, msg, title, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }

            if (ErrorList.ErrorCount() > 0)
            {
                ErrorList.Show();
            }
        }

        /// <summary>
        /// Returns the human readble SPIR-V representation of the shader
        /// </summary>
        public bool GetReadableSPIRV(ShaderFile shaderFile, out List<string> spirvOutput)
        {
            List<string> output = new List<string>();
            spirvOutput = output;
            string title = name;
            string msg;

            if (ReferenceCompiler.Locate() == null)
            {
                msg = "Could not locate the glslang reference compiler (glslangvalidator.exe) in system path!";
                VsShellUtilities.ShowMessageBox(ServiceProvider, msg, title, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                OutputWindow.Add(msg);
                return false;
            }

            List<string> validatorOutput;
            bool res = ReferenceCompiler.GetHumanReadableSPIRV(shaderFile.fileName, out validatorOutput);
            if (res)
            {
                spirvOutput = validatorOutput;
            }
            else
            {
                msg = string.Format(CultureInfo.CurrentCulture, "Could not get human readable SPIR-V for shader \"{0}\" ", shaderFile.fileName) + "\n";
                Debug.Write(msg);
                VsShellUtilities.ShowMessageBox(ServiceProvider, msg, title, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                ParseErrors(validatorOutput, shaderFile);
                msg += string.Join("\n", validatorOutput);
                OutputWindow.Add(msg);
            }

            return res;
        }
    }
}
