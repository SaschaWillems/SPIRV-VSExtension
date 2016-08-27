/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using System.ComponentModel.Design;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;

namespace SPIRVExtension
{    
    internal sealed class CommandCompileVulkan : ShaderCommandBase
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("c25a4989-8e55-4457-822d-1e690eb23169");
        private readonly Package package;

        private CommandCompileVulkan(Package package) : base(package, "Compile to SPIR-V (Vulkan semantics)")
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService mcs = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID menuCommandID = new CommandID(CommandSet, (int)CommandId);
                OleMenuCommand oleMenuItem = new OleMenuCommand(new EventHandler(MenuItemCallback), menuCommandID);
                oleMenuItem.BeforeQueryStatus += new EventHandler(OnBeforeQueryStatus);
                mcs.AddCommand(oleMenuItem);
            }
        }

        public static CommandCompileVulkan Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new CommandCompileVulkan(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            List<ShaderFile> selectedShaderFiles = new List<ShaderFile>();
            if (GetSelectedShaderFiles(selectedShaderFiles))
            {
                CompileShaders(selectedShaderFiles, ReferenceCompiler.CompileToVulkan);
            }
        }

        void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var item = (OleMenuCommand)sender;
            if (item != null)
            {
                item.Visible = (GetSelectedShaderFileCount() > 0);
            }
        }

    }
}
