using System.Threading.Tasks;
using Xunit;

namespace GodotCompletionProviders.Test
{
    [Collection("Sequential")]
    public class ScenePathTests : TestsBase
    {
        private const string StubCode = @"
namespace Godot
{
    public class SceneTree
    {
        public Error ChangeScene(string path) => throw new NotImplementedException();
    }
}
";

        public ScenePathTests() : base(new ScenePathCompletionProvider())
        {
        }

        private Task<BaseCompletionProvider.CheckResult> ProvidesForFull(string statements)
        {
            string testCode = $@"
using Godot;
{statements}";
            string code = Utils.ReadSingleCaretTestCode(testCode, out int caretPosition);
            return ShouldProvideCompletion(StubCode, code, caretPosition);
        }

        private async Task<bool> ProvidesFor(string statements) =>
            (await ProvidesForFull(statements)).ShouldProvideCompletion;

        [Fact]
        public void TestNotSomethingElse()
        {
            Assert.False(ProvidesFor("((SceneTree)null).Foo(⛶)").Result);
            Assert.False(ProvidesFor("((SceneTree)null).Foo(, ⛶)").Result);
            Assert.False(ProvidesFor("((SceneTree)null).Foo(, , ⛶)").Result);
        }

        [Fact]
        public void TestChangeScene()
        {
            Assert.True(ProvidesFor("((SceneTree)null).ChangeScene(⛶").Result);
            Assert.True(ProvidesFor("((SceneTree)null).ChangeScene(⛶)").Result);
            Assert.False(ProvidesFor("((SceneTree)null).ChangeScene(, ⛶").Result);
            Assert.False(ProvidesFor("((SceneTree)null).ChangeScene(, ⛶)").Result);
        }
    }
}
