using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;

namespace ConsoleTester.Scratch
{
    internal partial class ConsoleWorkspace : Workspace
    {
        private class SolutionWriter : TextWriter
        {
            private readonly StreamWriter _innerWriter;

            public SolutionWriter(FileStream stream)
            {
                _innerWriter = new StreamWriter(stream);
                _innerWriter.Write("\xfeff");
            }

            public override Encoding Encoding
            {
                get
                {
                    return new UnicodeEncoding(true, true);
                }
            }

            public override void Close()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _innerWriter?.Close();
                }
            }

            public void WriteSolutionFile(SolutionFile solution)
            {
                WriteSolutionDeclaration(Version.Parse("12.00"),
                    Version.Parse("15.0.26430.15"),
                    Version.Parse("10.0.40219.1"));
                foreach (ProjectInSolution projectInSolution in solution.ProjectsInOrder)
                {
                    WriteProjectInSolution(projectInSolution);
                }

                WriteGlobalStart();
                WriteGlobalEnd();
            }

            /// <summary>
            ///  Parse the first line of a Project section of a solution file. This line should look like:
            ///   Project("{Project type GUID}") = "Project name", "Relative path to project file", "{Project GUID}"
            /// </summary>
            /// <param name="firstLine"></param>
            /// <param name="proj"></param>
            /// <param name="projectInSolution"></param>
            public void WriteProjectInSolution(ProjectInSolution projectInSolution)
            {
                WriteLine(
                    "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"",
                    projectInSolution.ParentProjectGuid,
                    projectInSolution.ProjectName,
                    projectInSolution.RelativePath,
                    projectInSolution.ProjectGuid);
                WriteLine("EndProject");
            }

            public void WriteSolutionDeclaration(Version formatVersion, Version visualStudioVersion, Version minimumVisualStudioVersion)
            {
                Write("Visual Studio Solution File, File Format ");
                WriteVersion(formatVersion, true);
                WriteComment("Visual Studio {0}", visualStudioVersion.Major);
                WriteCurrentVisualStudioVersion(visualStudioVersion);
                WriteMinimumVisualStudioVersion(minimumVisualStudioVersion);
            }

            public void WriteCurrentVisualStudioVersion(Version version)
            {
                Write("VisualStudioVersion = ");
                WriteVersion(version, true);
            }

            public void WriteMinimumVisualStudioVersion(Version version)
            {
                Write("MinimumVisualStudioVersion = ");
                WriteVersion(version, true);
            }

            public void WriteGlobalStart()
            {
                WriteLine("Global");
            }

            public void WriteGlobalEnd()
            {
                WriteLine("EndGlobal");
            }

            public void WriteVersion(Version version, bool newLine = false)
            {
                if (newLine)
                {
                    WriteLine(version.ToString());
                }
                else
                {
                    Write(version.ToString());
                }
            }

            public void WriteComment(string comment, params object[] args)
            {
                Write("# ");
                WriteLine(comment, args);
            }

            public override void WriteLine(string format, params object[] arg)
            {
                _innerWriter.WriteLine(format, arg);
            }

            public override void Write(string value)
            {
                _innerWriter.Write(value);
            }

            public override void WriteLine(string value)
            {
                _innerWriter.WriteLine(value);
            }
        }
    }
}
