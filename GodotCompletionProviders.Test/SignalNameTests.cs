using System.Threading.Tasks;
using Xunit;

namespace GodotCompletionProviders.Test
{
    [Collection("Sequential")]
    public class SignalNameTests : TestsBase
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

    public class Object
    {
        public Error Connect(StringName signal, Callable callable, Godot.Collections.Array binds = null, uint flags = 0) =>
            throw new NotImplementedException();
        public void Disconnect(StringName signal, Callable callable) => throw new NotImplementedException();
        public bool IsConnected(StringName signal, Callable callable) => throw new NotImplementedException();
        public void EmitSignal(StringName signal, params object[] @args) => throw new NotImplementedException();
        public SignalAwaiter ToSignal(Object source, StringName signal) => throw new NotImplementedException();
    }
}
";

        public SignalNameTests() : base(new SignalNameCompletionProvider())
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
            Assert.False(ProvidesFor("((Object)null).Foo(⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).Foo(, ⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).Foo(, , ⛶)").Result);
        }

        [Fact]
        public void TestConnect()
        {
            Assert.True(ProvidesFor("((Object)null).Connect(⛶").Result);
            Assert.True(ProvidesFor("((Object)null).Connect(⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).Connect(, ⛶").Result);
            Assert.False(ProvidesFor("((Object)null).Connect(, ⛶)").Result);
        }

        [Fact]
        public void TestDisconnect()
        {
            Assert.True(ProvidesFor("((Object)null).Disconnect(⛶").Result);
            Assert.True(ProvidesFor("((Object)null).Disconnect(⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).Disconnect(, ⛶").Result);
            Assert.False(ProvidesFor("((Object)null).Disconnect(, ⛶)").Result);
        }

        [Fact]
        public void TestIsConnected()
        {
            Assert.True(ProvidesFor("((Object)null).IsConnected(⛶").Result);
            Assert.True(ProvidesFor("((Object)null).IsConnected(⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).IsConnected(, ⛶").Result);
            Assert.False(ProvidesFor("((Object)null).IsConnected(, ⛶)").Result);
        }

        [Fact]
        public void TestEmitSignal()
        {
            Assert.True(ProvidesFor("((Object)null).EmitSignal(⛶").Result);
            Assert.True(ProvidesFor("((Object)null).EmitSignal(⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).EmitSignal(, ⛶").Result);
            Assert.False(ProvidesFor("((Object)null).EmitSignal(, ⛶)").Result);
        }

        [Fact]
        public void TestToSignal()
        {
            Assert.True(ProvidesFor("((Object)null).ToSignal(, ⛶").Result);
            Assert.True(ProvidesFor("((Object)null).ToSignal(, ⛶)").Result);
            Assert.False(ProvidesFor("((Object)null).ToSignal(⛶").Result);
            Assert.False(ProvidesFor("((Object)null).ToSignal(⛶)").Result);
        }
    }
}
