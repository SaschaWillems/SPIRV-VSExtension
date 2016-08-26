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
    /// Helper class for the adding items to the Visual Studio error list
    /// </summary>
    public static class ErrorList
    {
        private static ErrorListProvider errorListProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            errorListProvider = new ErrorListProvider(serviceProvider);
        }

        public static void Add(string text, string fileName, int line, int column, IVsHierarchy hierarchy)
        {
            ErrorTask errorTask = new ErrorTask();
            errorTask.Text = text;
            errorTask.Line = line - 1;
            errorTask.Column = column;
            errorTask.Category = TaskCategory.User;
            errorTask.ErrorCategory = TaskErrorCategory.Error;
            errorTask.Document = fileName;
            errorTask.HierarchyItem = hierarchy;
            errorTask.Navigate += DocumentHelper.NavigateDocument;
            errorListProvider.Tasks.Add(errorTask);
        }

        public static void Clear()
        {
            errorListProvider.Tasks.Clear();
        }

        public static void Show()
        {
            errorListProvider.Show();
        }

        public static int ErrorCount()
        {
            return errorListProvider.Tasks.Count;
        }
    }
}
