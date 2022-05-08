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
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SPIRVExtension
{
    /// <summary>
    /// Helper class for locating and invocating the glslang reference compiler
    /// </summary>
    public class ReferenceCompiler
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
                var filePath = Path.Combine(path, "glslangvalidator.exe");
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the file has a valid shader extension that can be used as an input for the reference compiler
        /// </summary>
        public static bool IsShaderFile(string fileName)
        {
            var shaderExtensions = new[] { ".vert", ".tesc", ".tese", ".geom", ".frag", ".comp", ".mesh", ".task", ".rgen", ".rint", ".rahit", ".rchit", ".rmiss", ".rcall" };
            foreach (string ext in shaderExtensions)
            {
                if (string.Compare(ext, Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool Run(string fileName, string commandLine, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Locate(package);
            startInfo.Arguments = commandLine;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
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

            validatorOutput = output;

            return (process.ExitCode == 0);
        }

        /// <summary>
        /// Compile the shader using Vulkan semantics and output to a binary file (.spv)
        /// </summary>
        public static bool CompileToVulkan(string fileName, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-V \"{0}\" -o \"{1}\"", fileName, fileName + ".spv");
            if (package.OptionTargetEnv != "")
            {
                commandLine += " --target-env " + package.OptionTargetEnv;
            } else
            {
                // Ray tracing shaders require at least SPIR-V 1.4
                var rayTracingshaderExtensions = new[] { ".rgen", ".rint", ".rahit", ".rchit", ".rmiss", ".rcall" };
                foreach (string ext in rayTracingshaderExtensions)
                {
                    if (string.Compare(ext, Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        commandLine += " --target-env spirv1.4";
                    }
                }
            }
            return Run(fileName, commandLine, out validatorOutput, package);
        }

        /// <summary>
        /// Compile the shader using OpenGL semantics and output to a binary file (.spv)
        /// </summary>
        public static bool CompileToOpenGL(string fileName, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-G \"{0}\" -o \"{1}\"", fileName, fileName + ".spv");
            return Run(fileName, commandLine, out validatorOutput, package);
        }

        /// <summary>
        /// Converts the shader to human readable SPIR-V and returns the reference compiler output
        /// </summary>
        public static bool GetHumanReadableSPIRV(string fileName, out List<string> validatorOutput, SPIRVExtensionPackage package)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-H \"{0}\"", fileName);
            if (package.OptionTargetEnv != "")
            {
                commandLine += " --target-env " + package.OptionTargetEnv;
            }
            return Run(fileName, commandLine, out validatorOutput, package);
        }
    }
}
