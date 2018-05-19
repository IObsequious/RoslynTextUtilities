using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using SourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace ConsoleTester.Scratch
{
    internal partial class ConsoleWorkspace : Workspace
    {
        private ImmutableArray<MetadataReference> _references = ImmutableArray<MetadataReference>.Empty;

        public ConsoleWorkspace(params string[] directorySearchPaths)
            : base(CreateMefHostServices(), ServiceLayer.Desktop)
        {
            foreach (string path in directorySearchPaths)
            {
                _references = _references.AddRange(GetMetadataReferences(path));
            }

            _references = _references.AddRange(GetMetadataReferences(
                Path.GetDirectoryName(typeof(ConsoleWorkspace).Assembly.Location)));
        }

        public Document CreateDocument(string name, string text, string path)
        {
            Solution solution = CreateSolution(SolutionId.CreateNewId());
            Project project = solution.AddProject("X", "X", LanguageNames.CSharp);
            solution = project.Solution;
            DocumentId documentId = DocumentId.CreateNewId(project.Id);
            Document document = solution.AddDocument(documentId, name, text, null, path).GetDocument(documentId);
            return document;
        }

        public Solution CreateSolution(string solutionDirectory, string solutionName, bool saveToDisk)
        {
            Solution solution = CreateSolution(CreateSolutionInfo(solutionDirectory, solutionName));
            return SetCurrentSolution(solution);
        }

        private void SaveSolutionFile(SolutionFile solutionFile, string filePath)
        {
            SolutionWriter writer = new SolutionWriter(new FileInfo(filePath).OpenWrite());

            writer.WriteSolutionFile(solutionFile);
        }

        private DocumentInfo CreateDocumentInfo(ProjectInfo projectInfo, string documentName, string documentText, params string[] folders)
        {
            string Merge(IEnumerable<string> strings)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string str in strings)
                {
                    sb.Append(str);
                    sb.Append('\\');
                }

                return sb.ToString();
            }

            TextLoader documentTextLoader = TextLoader.From(TextAndVersion.Create(SourceText.From(documentText), VersionStamp.Create()));
            DocumentId documentId = DocumentId.CreateNewId(projectInfo.Id);
            DocumentInfo documentInfo = DocumentInfo.Create(
                documentId,
                documentName,
                folders,
                SourceCodeKind.Regular,
                documentTextLoader,
                Path.Combine(Path.GetDirectoryName(projectInfo.FilePath), Merge(folders), documentName));
            return documentInfo;
        }

        private ProjectInfo CreateProjectInfo(string solutionDirectoryName, string projectName, params DocumentInfo[] documents)
        {
            DirectoryInfo solutionDirectory = new DirectoryInfo(solutionDirectoryName);
            DirectoryInfo repositoryDirectory = solutionDirectory.Parent;
            DirectoryInfo projectDirectory = new DirectoryInfo(Path.Combine(solutionDirectory.FullName, projectName));
            string solutionDirectoryNameWithSlash = projectDirectory.FullName;
            ProjectId projectId = ProjectId.CreateNewId();
            ProjectInfo projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectName,
                projectName,
                LanguageNames.CSharp,
                $"{solutionDirectoryNameWithSlash}{projectName}.csproj",
                $"{solutionDirectoryNameWithSlash}\\..\\bin\\Debug\\{projectName}",
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    true,
                    projectName,
                    null,
                    null,
                    null,
                    OptimizationLevel.Debug,
                    false,
                    true,
                    null,
                    null).WithStrongNameProvider(new DesktopStrongNameProvider()),
                new CSharpParseOptions(
                    LanguageVersion.CSharp7,
                    DocumentationMode.None,
                    SourceCodeKind.Regular),
                documents,
                null,
                _references,
                null,
                null,
                false);
            return projectInfo;
        }

        private SolutionInfo CreateSolutionInfo(string solutionDirectory, string solutionName, params ProjectInfo[] projects)
        {
            string filePath = Path.Combine(solutionDirectory, solutionName + ".sln");
            SolutionInfo solutionInfo = SolutionInfo.Create(
                SolutionId.CreateNewId(),
                VersionStamp.Create(),
                filePath,
                projects);
            return solutionInfo;
        }

        private static MefHostServices CreateMefHostServices()
        {
            CompositionContext CreateContext()
            {
                ContainerConfiguration container = new ContainerConfiguration();
                ImmutableArray<Assembly> immutableArray = MefHostServices.DefaultAssemblies.Add(typeof(ConsoleWorkspace).Assembly);
                container = container.WithAssemblies(immutableArray);
                CompositionHost context = container.CreateContainer();
                return context;
            }

            return MefHostServices.Create(CreateContext());
        }

        private static IEnumerable<MetadataReference> GetMetadataReferences(string directory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            IEnumerable<FileInfo> libraries =
                directoryInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly);
            foreach (FileInfo file in libraries)
            {
                if (TryCreateMetadataReference(file, out MetadataReference reference))
                {
                    yield return reference;
                }
            }
        }

        private static bool TryCreateMetadataReference(FileInfo file, out MetadataReference reference)
        {
            try
            {
                reference = MetadataReference.CreateFromFile(file.FullName, new MetadataReferenceProperties(MetadataImageKind.Assembly));
                return true;
            }
            catch (Exception)
            {
                reference = null;
                return false;
            }
        }

        public Solution CreateConsoleSolution(string solutionDirectoryName, string solutionName)
        {
            Solution currentSolution = CurrentSolution;



            ProjectInfo projectInfo1 = CreateProjectInfo(solutionDirectoryName, "Project1");
            DocumentInfo document1 = CreateDocumentInfo(
                projectInfo1,
                "Document1.cs",
                string.Empty);
            projectInfo1 = projectInfo1.WithDocuments(new[] {document1});
            ProjectInfo projectInfo2 = CreateProjectInfo(solutionDirectoryName, "Project2");
            DocumentInfo document2 = CreateDocumentInfo(projectInfo2, "Document2.cs", string.Empty);
            projectInfo2 = projectInfo2.WithDocuments(new[] {document2});
            ProjectInfo projectInfo3 = CreateProjectInfo(solutionDirectoryName, "Project3");
            DocumentInfo document3 = CreateDocumentInfo(projectInfo1, "Document3.cs", string.Empty);
            projectInfo3 = projectInfo3.WithDocuments(new[] {document3});
            SolutionInfo solution = CreateSolutionInfo(solutionDirectoryName, solutionName, projectInfo1, projectInfo2, projectInfo3);
            currentSolution = currentSolution.AddProject(projectInfo1);
            currentSolution = currentSolution.AddProject(projectInfo2);
            currentSolution = currentSolution.AddProject(projectInfo3);
            return SetCurrentSolution(currentSolution);
        }
    }
}
