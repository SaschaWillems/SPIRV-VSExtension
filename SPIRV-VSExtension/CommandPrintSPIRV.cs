/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;

namespace SPIRVExtension
{
    internal sealed class CommandPrintSPIRV : ShaderCommandBase
    {
        public const int CommandId = 0x0130;
        public static readonly Guid CommandSet = new Guid("c25a4989-8e55-4457-822d-1e690eb23169");

        private CommandPrintSPIRV(AsyncPackage package, OleMenuCommandService commandService) : base(package, "Print human readable SPIR-V")
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (commandService == null)
            {
                throw new ArgumentNullException(nameof(commandService));
            }

            CommandID menuCommandID = new CommandID(CommandSet, (int)CommandId);
            OleMenuCommand oleMenuItem = new OleMenuCommand(new EventHandler(MenuItemCallback), menuCommandID);
            oleMenuItem.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
            commandService.AddCommand(oleMenuItem);
        }

        public static CommandPrintSPIRV Instance
        {
            get;
            private set;
        }

        public static void Initialize(AsyncPackage package, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Instance = new CommandPrintSPIRV(package, commandService);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<ShaderFile> selectedShaderFiles = new List<ShaderFile>();
            if (GetSelectedShaderFiles(selectedShaderFiles))
            {
                ErrorList.Clear();
                foreach (var shaderFile in selectedShaderFiles)
                {
                    List<string> spirvOutput = new List<string>();
                    if (GetReadableSPIRV(shaderFile, out spirvOutput))
                    {
                        string fileName = Path.GetTempPath() + Path.GetFileName(shaderFile.fileName) + ".spirv.readable";
                        File.WriteAllLines(fileName, spirvOutput);
                        VsShellUtilities.OpenDocument(ServiceProvider, fileName);
                    }
                }
                if (ErrorList.ErrorCount() > 0)
                {
                    ErrorList.Show();
                }
            }
        }

        void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var item = (OleMenuCommand)sender;
            if (item != null)
            {
                int count = GetSelectedShaderFileCount();
                item.Visible = (count > 0);
                item.Text = "Print human-readable SPIR-V";
            }
        }

    }

}
