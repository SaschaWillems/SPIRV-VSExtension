/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016-2023 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using System.IO;

namespace SPIRVExtension.Shared
{
    class Utils
    {
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

        /// <summary>
        /// Returns true if the file has a ray tracing related file extension
        /// </summary>
        public static bool IsRayTracingShaderFile(string fileName)
        {
            var shaderExtensions = new[] { ".rgen", ".rint", ".rahit", ".rchit", ".rmiss", ".rcall" };
            foreach (string ext in shaderExtensions)
            {
                if (string.Compare(ext, Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }
            return false;
        }


    }
}
