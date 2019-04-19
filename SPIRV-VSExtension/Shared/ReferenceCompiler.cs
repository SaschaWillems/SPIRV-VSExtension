/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

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
    public static class ReferenceCompiler
    {
        /// <summary>
        /// Searches the PATH environment and common SDK path environment variables for the glslangvalidator
        /// </summary>
        public static string Locate()
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            List<string> paths = new List<string>(pathEnv.Split(';'));
            // Also add Vulkan SDK env vars (if present)
            // todo: Add an option to add user paths?
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

        private static bool Run(string fileName, string commandLine, out List<string> validatorOutput)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = Locate();
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
            catch (Exception)
            {
                //todo: Display error message
                throw;
            }

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
        public static bool CompileToVulkan(string fileName, out List<string> validatorOutput)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-V \"{0}\" -o \"{1}\"", fileName, fileName + ".spv");
            return Run(fileName, commandLine, out validatorOutput);
        }

        /// <summary>
        /// Compile the shader using OpenGL semantics and output to a binary file (.spv)
        /// </summary>
        public static bool CompileToOpenGL(string fileName, out List<string> validatorOutput)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-G \"{0}\" -o \"{1}\"", fileName, fileName + ".spv");
            return Run(fileName, commandLine, out validatorOutput);
        }

        /// <summary>
        /// Converts the shader to human readable SPIR-V and returns the reference compiler output
        /// </summary>
        public static bool GetHumanReadableSPIRV(string fileName, out List<string> validatorOutput)
        {
            string commandLine = string.Format(CultureInfo.CurrentCulture, "-H \"{0}\"", fileName);
            return Run(fileName, commandLine, out validatorOutput);
        }
    }
}
