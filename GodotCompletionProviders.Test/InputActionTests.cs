using System.Threading.Tasks;
using Xunit;

namespace GodotCompletionProviders.Test
{
    [Collection("Sequential")]
    public class InputActionTests : TestsBase
    {
        private const string StubCode = @"
namespace Godot
{
    public class StringName
    {
        public StringName() { }
        public StringName(string from) { }
        public static implicit operator StringName(string from) => throw new NotImplementedException();
        public static implicit operator string(StringName from) => throw new NotImplementedException();
    }

    public static class Input
    {
        public static bool IsActionPressed(StringName action) => throw new NotImplementedException();
        public static bool IsActionJustPressed(StringName action) => throw new NotImplementedException();
        public static bool IsActionJustReleased(StringName action) => throw new NotImplementedException();
        public static float GetActionStrength(StringName action) => throw new NotImplementedException();
        public static void ActionPress(StringName action, float strength = 1f) => throw new NotImplementedException();
        public static void ActionRelease(StringName action) => throw new NotImplementedException();
    }
}
";

        public InputActionTests() : base(new InputActionCompletionProvider())
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
        public void TestTestNotSomethingElse()
        {
            Assert.False(ProvidesFor("Input.Foo(⛶)").Result);
            Assert.False(ProvidesFor("Input.Foo(, ⛶)").Result);
            Assert.False(ProvidesFor("Input.Foo(, , ⛶)").Result);
        }

        [Fact]
        public void TestIsActionPressed()
        {
            Assert.True(ProvidesFor("Input.IsActionPressed(⛶").Result);
            Assert.True(ProvidesFor("Input.IsActionPressed(⛶)").Result);
            Assert.False(ProvidesFor("Input.IsActionPressed(, ⛶").Result);
            Assert.False(ProvidesFor("Input.IsActionPressed(, ⛶)").Result);
        }

        [Fact]
        public void TestIsActionJustPressed()
        {
            Assert.True(ProvidesFor("Input.IsActionJustPressed(⛶").Result);
            Assert.True(ProvidesFor("Input.IsActionJustPressed(⛶)").Result);
            Assert.False(ProvidesFor("Input.IsActionJustPressed(, ⛶").Result);
            Assert.False(ProvidesFor("Input.IsActionJustPressed(, ⛶)").Result);
        }

        [Fact]
        public void TestIsActionJustReleased()
        {
            Assert.True(ProvidesFor("Input.IsActionJustReleased(⛶").Result);
            Assert.True(ProvidesFor("Input.IsActionJustReleased(⛶)").Result);
            Assert.False(ProvidesFor("Input.IsActionJustReleased(, ⛶").Result);
            Assert.False(ProvidesFor("Input.IsActionJustReleased(, ⛶)").Result);
        }

        [Fact]
        public void TestGetActionStrength()
        {
            Assert.True(ProvidesFor("Input.GetActionStrength(⛶").Result);
            Assert.True(ProvidesFor("Input.GetActionStrength(⛶)").Result);
            Assert.False(ProvidesFor("Input.GetActionStrength(, ⛶").Result);
            Assert.False(ProvidesFor("Input.GetActionStrength(, ⛶)").Result);
        }

        [Fact]
        public void TestActionPress()
        {
            Assert.True(ProvidesFor("Input.ActionPress(⛶").Result);
            Assert.True(ProvidesFor("Input.ActionPress(⛶)").Result);
            Assert.False(ProvidesFor("Input.ActionPress(, ⛶").Result);
            Assert.False(ProvidesFor("Input.ActionPress(, ⛶)").Result);
        }

        [Fact]
        public void TestActionRelease()
        {
            Assert.True(ProvidesFor("Input.ActionRelease(⛶").Result);
            Assert.True(ProvidesFor("Input.ActionRelease(⛶)").Result);
            Assert.False(ProvidesFor("Input.ActionRelease(, ⛶").Result);
            Assert.False(ProvidesFor("Input.ActionRelease(, ⛶)").Result);
        }
    }
}
