using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTester.Refactoring;
using ConsoleTester.Scratch;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Threading;

namespace ConsoleTester
{
    internal static class Program
    {
        private static readonly ConsoleWorkspace Workspace = new ConsoleWorkspace();

        public static void Main()
        {
            ModeConCols();
            FileInfo info = new FileInfo(@"C:\Stage\git\PowerShellModules\src\Infrastructure\Resources\Class1.cs");

            Document document = Workspace.CreateDocument(info.Name, File.ReadAllText(info.FullName), info.FullName);

            JoinableTaskContext context = new JoinableTaskContext(Thread.CurrentThread);
            JoinableTaskFactory factory = new JoinableTaskFactory(context);

            document = factory.Run(() => Refactory.FixAsync(document));

            SourceText text = factory.Run(() => document.GetTextAsync(CancellationToken.None));

            Console.WriteLine(text.ToString());

            ViewFile(text);

            // PressAnyKey();
        }

        private static void ViewFile(SourceText sourceText)
        {
            string text = sourceText.ToString();

            string path = Path.GetTempFileName();
            File.WriteAllText(path, text);

            Process.Start("notepad.exe", path).WaitForExit();

            File.Delete(path);
        }

        private static void ModeConCols()
        {
            Console.SetWindowSize(160, 50);
        }
    }
}
