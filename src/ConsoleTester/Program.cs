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
        private const string DirectoryPath = @"C:\Stage\git\RoslynTextUtilities\src\Roslyn.Utilities\Syntax\";

        private static ConsoleWorkspace Workspace = new ConsoleWorkspace();
        public static void Main()
        {
            ModeConCols();
            FileInfo info = new FileInfo(@"C:\Stage\git\PowerShellModules\src\Infrastructure\Resources\Class1.cs");

            Document document = Workspace.CreateDocument(info.Name, File.ReadAllText(info.FullName), info.FullName);

            JoinableTaskContext context = new JoinableTaskContext(Thread.CurrentThread);
            JoinableTaskFactory factory = new JoinableTaskFactory(context);

            document = factory.Run(async () => await Refactory.FixAsync(document));

            SourceText text = factory.Run(async () => await document.GetTextAsync(CancellationToken.None));

            Console.WriteLine(text.ToString());

            ViewFile(text);

            // PressAnyKey();
        }

        private static void ViewFile(SyntaxNode newRoot)
        {
            string text = newRoot.ToFullString();

            var path = Path.GetTempFileName();
            File.WriteAllText(path, text);

            Process.Start("notepad.exe", path).WaitForExit();

            File.Delete(path);
        }
        private static void ViewFile(SourceText sourceText)
        {
            string text = sourceText.ToString();

            var path = Path.GetTempFileName();
            File.WriteAllText(path, text);

            Process.Start("notepad.exe", path).WaitForExit();

            File.Delete(path);
        }

        private static ImmutableArray<Document> GetFilesAndContent()
        {
            ImmutableArray<Document>.Builder builder = ImmutableArray.CreateBuilder<Document>();

            foreach (FileInfo info in GetFiles())
            {
                Document document = Workspace.CreateDocument(info.Name, File.ReadAllText(info.FullName), info.FullName);
                
                builder.Add(document);
            }

            return builder.ToImmutable();
        }

        private static IEnumerable<FileInfo> GetFiles()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(DirectoryPath);

            return directoryInfo.GetFiles("*.cs", SearchOption.AllDirectories);
        }

        private static void PressAnyKey()
        {
            Console.WriteLine();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ModeConCols()
        {
            Console.SetWindowSize(160, 50);
        }
    }
}
