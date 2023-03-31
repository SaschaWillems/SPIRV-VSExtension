﻿/*
* SPIR-V Visual Studio Extensiondeleted
*
* Copyright (C) 2016-2022 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using System.ComponentModel.Design;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;

namespace SPIRVExtension
{
    internal sealed class CommandCompileHLSL : ShaderCommandBase
    {
        public const int CommandId = 0x0140;
        public static readonly Guid CommandSet = new Guid("c25a4989-8e55-4457-822d-1e690eb23169");
        private readonly Package package;

        private CommandCompileHLSL(Package package) : base(package, "Compile HLSL to SPIR-V (Vulkan only)")
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

        public static CommandCompileHLSL Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new CommandCompileHLSL(package);
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            List<ShaderFile> selectedShaderFiles = new List<ShaderFile>();
            if (GetSelectedShaderFiles(selectedShaderFiles))
            {
                CompileShaders(selectedShaderFiles, DxcCompiler.CompileToVulkan);
            }
        }

        void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            var item = (OleMenuCommand)sender;
            if (item != null)
            {
                int count = GetSelectedShaderFileCount();
                item.Visible = (count > 0);
                item.Text = (count == 1) ? "Compile HLSL to SPIR-V (DXC)" : "Compile HLSL shaders to SPIR-V (DXC)";
            }
        }

        public override void ParseErrors(List<string> validatorOutput, ShaderFile shaderFile)
        {
            foreach (string line in validatorOutput)
            {
                // Example:
                // V:\Vulkan\Vulkan_mstr\data\shaders\hlsl\raytracingbasic\raygen.rgen:38:2: error: use of undeclared identifier 'imxage'; did you mean 'image'?
                MatchCollection matches = Regex.Matches(line, @":\d+:\d+", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                if (matches.Count > 0)
                {
                    // Line is the first number after :
                    MatchCollection lineMatches = Regex.Matches(line, @":\d+:", RegexOptions.IgnoreCase);
                    int errorLine = Convert.ToInt32(lineMatches[0].Value.Replace(":", ""));
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
    }
}
