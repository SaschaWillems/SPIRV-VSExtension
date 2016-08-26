/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SPIRVExtension
{
    /// <summary>
    /// Helper class for displaying messages in a custom output window pane
    /// </summary>
    public static class OutputWindow
    {
        static private IVsOutputWindowPane customPane()
        {
            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Guid customGuid = new Guid("6AFC25E1-1622-4589-82D7-136503A69B2F");
            string customTitle = "SPIRV Commands Extension";
            outputWindow.CreatePane(ref customGuid, customTitle, 1, 1);
            IVsOutputWindowPane pane;
            outputWindow.GetPane(ref customGuid, out pane);
            return pane;
        }

        static public void Add(string text)
        {
            customPane().OutputString(text);
        }

        public static void Clear()
        {
            customPane().Clear();
        }

        public static void Show()
        {
            customPane().Activate();
        }
    }
}
