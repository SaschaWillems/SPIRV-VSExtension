/*
* SPIR-V Visual Studio Extension
*
* Copyright (C) 2016 by Sascha Willems - www.saschawillems.de
*
* This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
*/

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SPIRVExtension
{
    /// <summary>
    /// Helper class for displaying documents and navigating inside of them
    /// </summary>
    public static class DocumentHelper
    {
        /// <summary>
        /// Open the file and jump to a line (and optional column)
        /// </summary>
        public static void OpenAndNavigateTo(string fileName, int line, int column = 0)
        {
            IVsUIShellOpenDocument uishellOpenDocument = Package.GetGlobalService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (uishellOpenDocument != null)
            {
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
                IVsWindowFrame frame;
                IVsUIHierarchy hierarchy;
                uint itemId;
                Guid logicalView = VSConstants.LOGVIEWID_Code;
                if (ErrorHandler.Succeeded(uishellOpenDocument.OpenDocumentViaProject(fileName, ref logicalView, out serviceProvider, out hierarchy, out itemId, out frame)))
                {
                    object document;
                    frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out document);
                    VsTextBuffer buffer = document as VsTextBuffer;
                    if (buffer == null)
                    {
                        IVsTextBufferProvider bufferProvider = document as IVsTextBufferProvider;
                        if (bufferProvider != null)
                        {
                            IVsTextLines lines;
                            ErrorHandler.ThrowOnFailure(bufferProvider.GetTextBuffer(out lines));
                            buffer = lines as VsTextBuffer;
                        }
                    }
                    if (buffer != null)
                    {
                        IVsTextManager textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
                        textManager.NavigateToLineAndColumn(buffer, ref logicalView, line, column, line, column);
                    }
                }
            }
        }

        /// <summary>
        /// Callback to be assigned to a task
        /// </summary>
        public static void NavigateDocument(object sender, EventArgs e)
        {
            ErrorTask task = sender as ErrorTask;
            OpenAndNavigateTo(task.Document, task.Line, task.Column);
        }
    }
}
