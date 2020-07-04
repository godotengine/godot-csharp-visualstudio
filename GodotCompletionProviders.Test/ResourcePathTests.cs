using System.Threading.Tasks;
using Xunit;

namespace GodotCompletionProviders.Test
{
    [Collection("Sequential")]
    public class ResourcePathTests : TestsBase
    {
        private const string StubCode = @"
namespace Godot
{
    public static class GD
    {
        public static Resource Load(string path) => throw new NotImplementedException();
        public static T Load<T>(string path) where T : class => throw new NotImplementedException();
    }

    public static class ResourceLoader
    {
        public static Resource Load(string path, string typeHint = "", bool noCache = false) => throw new NotImplementedException();
        public static T Load<T>(string path, string typeHint = null, bool noCache = false) where T : class => throw new NotImplementedException();
    }
}
";

        public ResourcePathTests() : base(new ResourcePathCompletionProvider())
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
            Assert.False(ProvidesFor("ResourceLoader.Foo(⛶)").Result);
            Assert.False(ProvidesFor("ResourceLoader.Foo(, ⛶)").Result);
            Assert.False(ProvidesFor("ResourceLoader.Foo(, , ⛶)").Result);
        }

        [Fact]
        public void TestResourceLoaderLoad()
        {
            Assert.True(ProvidesFor("ResourceLoader.Load(⛶").Result);
            Assert.True(ProvidesFor("ResourceLoader.Load(⛶)").Result);
            Assert.False(ProvidesFor("ResourceLoader.Load(, ⛶").Result);
            Assert.False(ProvidesFor("ResourceLoader.Load(, ⛶)").Result);

            // Generic
            Assert.True(ProvidesFor("ResourceLoader.Load<PackedScene>(⛶)").Result);
        }

        [Fact]
        public void TestGdLoad()
        {
            Assert.True(ProvidesFor("GD.Load(⛶").Result);
            Assert.True(ProvidesFor("GD.Load(⛶)").Result);
            Assert.False(ProvidesFor("GD.Load(, ⛶").Result);
            Assert.False(ProvidesFor("GD.Load(, ⛶)").Result);

            // Generic
            Assert.True(ProvidesFor("GD.Load<PackedScene>(⛶)").Result);
        }
    }
}
