/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016-2023 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using Microsoft.VisualStudio.Shell;
using SPIRVExtension.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SPIRVExtension
{
    /// <summary>
    /// Helper class for locating and invocating the DXC compiler
    /// </summary>
    public class DxcCompiler
    {
        /// <summary>
        /// Searches the PATH environment and common SDK path environment variables for the glslangvalidator
        /// </summary>
        public static string Locate(SPIRVExtensionPackage package)
        {
            if (package.OptionGlslangValidatorBinaryPath != "")
            {
                OutputWindow.Add("Using glslangvalidator from options path: " + Path.Combine(package.OptionGlslangValidatorBinaryPath, "glslangvalidator.exe"));
                return Path.Combine(package.OptionGlslangValidatorBinaryPath, "glslangvalidator.exe");
            }

            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            List<string> paths = new List<string>(pathEnv.Split(';'));
            var additionalPaths = new[] { "VK_SDK_PATH", "VULKAN_SDK" };
            foreach (string path in additionalPaths)
            {
                if (Environment.GetEnvironmentVariable(path) != null)
                {
                    paths.Add(Environment.GetEnvironmentVariable(path));
                }
            }

            foreach (var path in paths)
            {
                var filePath = Path.Combine(path, "dxc.exe");
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            return null;
        }

        private static bool Run(string fileName, string commandLine, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Locate(package);
            startInfo.Arguments = commandLine;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            var process = new Process();
            process.StartInfo = startInfo;

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                //todo: Display error message
                OutputWindow.Add("Exception while running: " + e.Message);
                throw;
            }

            OutputWindow.Add(Locate(package));
            OutputWindow.Add(commandLine);

            List<string> output = new List<string>();
            while (!process.StandardOutput.EndOfStream)
            {
                output.Add(process.StandardOutput.ReadLine());
            }
            while (!process.StandardError.EndOfStream)
            {
                output.Add(process.StandardError.ReadLine());
            }

            validatorOutput = output;

            return (process.ExitCode == 0);
        }

        /// <summary>
        /// Compile the shader using Vulkan semantics and output to a binary file (.spv)
        /// </summary>
        public static bool CompileToVulkan(string fileName, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            // Get profile and additional options based on file extension
            var profileDictionary = new Dictionary<string, string>
            {
                { ".vert", "vs_6_1" },
                { ".frag", "ps_6_1" },
                { ".comp", "cs_6_1" },
                { ".geom", "gs_6_1" },
                { ".tesc", "hs_6_1" },
                { ".tese", "ds_6_1" },
                { ".rgen", "lib_6_3" },
                { ".rchit", "lib_6_3" },
                { ".rmiss", "lib_6_3" },
                { ".rahit", "lib_6_3" },
                { ".mesh", "ms_6_6" },
                { ".task", "as_6_6" },
            };
            string fileExt = Path.GetExtension(fileName).ToLower();
            if (!profileDictionary.ContainsKey(fileExt))
            {
                List<string> output = new List<string>();
                output.Add("Could not match file extension to HLSL shader profile");
                validatorOutput = output;
                return false;
            }
            string profile = profileDictionary[fileExt];

            List<string> commandLineArgs = new List<string>
            {
                "-spirv",
                "-T " + profile,
                "-E main"
            };
            if (package.OptionTargetEnv != "")
            {
                commandLineArgs.Add("-fspv-target-env=" + package.OptionTargetEnv);
            }
            else
            {
                // Ray tracing shaders require at least SPIR-V 1.4
                if (Utils.IsRayTracingShaderFile(fileName))
                {
                    commandLineArgs.Add("-fspv-target-env=vulkan1.2");
                }
            }
            commandLineArgs.Add(fileName);
            commandLineArgs.Add("-Fo");
            commandLineArgs.Add(fileName + ".spv");

            string commandLine = string.Join(" ", commandLineArgs);
            return Run(fileName, commandLine, out validatorOutput, package);
        }
    }
}
