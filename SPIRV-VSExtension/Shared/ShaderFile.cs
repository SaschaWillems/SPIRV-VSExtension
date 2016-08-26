/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace SPIRVExtension
{
    /// <summary>
    /// Encaspulates details of a shader file
    /// </summary>
    public class ShaderFile
    {
        public uint itemid;
        public IVsHierarchy hierarchy;
        public string fileName;
        public string fileExt;

        public ShaderFile(uint id, IVsHierarchy hr, string file)
        {
            itemid = id;
            hierarchy = hr;
            fileName = file;
            fileExt = Path.GetExtension(file);
        }
    }
}
