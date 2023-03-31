/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016-2023 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;

public class OptionPageGrid : DialogPage
{
    [Category("GLSL")]
    [DisplayName("Target Environment")]
    [Description("Select the target environment for Vulkan shader compilation")]
    public string OptionTargetEnv { get; set; } = "";

    [Category("GLSL")]
    [DisplayName("glslangvalidator path")]
    [Description("Manually specify a path to the glslangvalidator binary to override the default one from PATH")]
    public string OptionGlslangValidatorBinaryPath { get; set; } = "";

    [Category("HLSL")]
    [DisplayName("dxc path")]
    [Description("Manually specify a path to the dxc binary to override the default one from PATH")]
    public string OptionDxcBinaryPath { get; set; } = "";
}

namespace SPIRVExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(SPIRVExtensionPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82", PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionPageGrid),
    "SPIRV Extension", "General", 0, 0, true)]
    public sealed class SPIRVExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// SPIRVExtension GUID string.
        /// </summary>
        public const string PackageGuidString = "1ec0f76d-4687-49ea-a037-76a4ab592f51";

        /// <summary>
        /// Initializes a new instance of the <see cref="SPIRVExtension"/> class.
        /// </summary>
        public SPIRVExtensionPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            CommandCompileVulkan.Initialize(this);
            CommandCompileHLSL.Initialize(this);
            CommandCompileOpenGL.Initialize(this);
            CommandPrintSPIRV.Initialize(this);

            //base.Initialize();
            ErrorList.Initialize(this);

            // When initialized asynchronously, we *may* be on a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            // Otherwise, remove the switch to the UI thread if you don't need it.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        public string OptionTargetEnv
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.OptionTargetEnv;
            }
        }

        public string OptionGlslangValidatorBinaryPath
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.OptionGlslangValidatorBinaryPath;
            }
        }

        public string OptionDxcBinaryPath
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.OptionDxcBinaryPath;
            }
        }
        #endregion
    }
}
