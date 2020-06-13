using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace GodotCompletionProviders.Test
{
    public abstract class TestsBase
    {
        private readonly BaseCompletionProvider _completionProvider;

        protected TestsBase(BaseCompletionProvider completionProvider)
        {
            _completionProvider = completionProvider;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Global
        protected Task<BaseCompletionProvider.CheckResult> ShouldProvideCompletion(string stubCode, string testCode, int caretPosition)
        {
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
            Assert.NotNull(host);
            var workspace = new AdhocWorkspace(host);

            var projectId = ProjectId.CreateNewId();

            var stubDocumentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId), "Stub.cs", sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(stubCode), VersionStamp.Create())));

            var testDocumentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId), "TestFile.cs", sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(testCode), VersionStamp.Create())));

            var projectInfo = ProjectInfo
                .Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
                .WithMetadataReferences(new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)})
                .WithDocuments(new[] {stubDocumentInfo, testDocumentInfo});
            var project = workspace.AddProject(projectInfo);

            var testDocument = project.GetDocument(testDocumentInfo.Id);

            return _completionProvider.ShouldProvideCompletion(testDocument, caretPosition);
        }
    }
}
