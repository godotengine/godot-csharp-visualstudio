using System.Threading.Tasks;
using Xunit;

namespace GodotCompletionProviders.Test
{
    [Collection("Sequential")]
    public class NodePathTests : TestsBase
    {
        private const string StubCode = @"
namespace Godot
{
    public class NodePath
    {
        public NodePath() { }
        public NodePath(string from) { }
        public static implicit operator NodePath(string from) => throw new NotImplementedException();
        public static implicit operator string(NodePath from) => throw new NotImplementedException();
    }

    public class Object { }
    public class Node : Godot.Object { }
}
";

        public NodePathTests() : base(new NodePathCompletionProvider())
        {
        }

        private Task<BaseCompletionProvider.CheckResult> ProvidesForFull(string classMemberDeclaration)
        {
            string testCode = $@"
using Godot;
{classMemberDeclaration}";
            string code = Utils.ReadSingleCaretTestCode(testCode, out int caretPosition);
            return ShouldProvideCompletion(StubCode, code, caretPosition);
        }

        private async Task<bool> ProvidesFor(string classMemberDeclaration) =>
            (await ProvidesForFull(classMemberDeclaration)).ShouldProvideCompletion;

        [Fact]
        public void TestFieldDeclarations()
        {
            Assert.True(ProvidesFor("NodePath npField = ⛶;").Result);
        }

        [Fact]
        public void TestPropertyDeclarations()
        {
            Assert.True(ProvidesFor("NodePath npProp1 => ⛶;").Result);
            Assert.True(ProvidesFor("NodePath npProp2 { get => ⛶; }").Result);
            Assert.True(ProvidesFor("NodePath npProp3 { get { return ⛶; } }").Result);
            Assert.True(ProvidesFor("NodePath npProp4 { get; set; } = ⛶;").Result);
        }

        [Fact]
        public void TestInvocationArgument()
        {
            // First argument
            Assert.True(ProvidesFor(@"
void FirstParam(NodePath path) { }
FirstParam(⛶);
").Result);

            // Second argument
            Assert.True(ProvidesFor(@"
void SecondParam(object nothing, NodePath path) { }
SecondParam(null, ⛶);
").Result);

            // First argument of generic method invocation
            Assert.True(ProvidesFor(@"
void FirstParamGeneric<T>(NodePath path) { }
FirstParamGeneric<char>(⛶);
").Result);
        }

        [Fact]
        public void TestNodePathCreationExpression()
        {
            Assert.True(ProvidesFor(@"_ = new NodePath(⛶);").Result);
        }

        [Fact]
        public void TestExplicitCast()
        {
            Assert.True(ProvidesFor(@"_ = (NodePath)⛶").Result);
            Assert.True(ProvidesFor(@"_ = (NodePath)(⛶)").Result);
            Assert.True(ProvidesFor(@"_ = (NodePath)⛶;").Result);
            Assert.True(ProvidesFor(@"_ = (NodePath)(⛶);").Result);

            Assert.False(ProvidesFor(@"_ = ((NodePath))⛶").Result);
            Assert.False(ProvidesFor(@"_ = ((NodePath))⛶;").Result);
        }

        [Fact]
        public void TestBinaryOperation()
        {
            Assert.True(ProvidesFor(@"_ = new NodePath() == ⛶;").Result);
            Assert.True(ProvidesFor(@"_ = new NodePath() != ⛶;").Result);

            // TODO: Not supported by the type inference service.
            //Assert.True(ProvidesFor(@"_ = ⛶ == new NodePath();").Result);
            //Assert.True(ProvidesFor(@"_ = ⛶ != new NodePath();").Result);
        }

        [Fact]
        public void TestAssignment()
        {
            // Assignment in local declaration
            Assert.True(ProvidesFor(@"NodePath npLocal = ⛶;").Result);

            // Assignment to a previously declared local
            Assert.True(ProvidesFor(@"
NodePath npLocal;
npLocal = ⛶;").Result);

            // Assignment to a previously declared field
            Assert.True(ProvidesFor(@"
NodePath npField;
void Foo() { npField = ⛶; }
").Result);

            // Assignment to a previously declared property
            Assert.True(ProvidesFor(@"
NodePath npProp { get; }
void Foo() { npProp = ⛶; }
").Result);

            // Assignment to ref parameter
            Assert.True(ProvidesFor(@"void Foo(ref NodePath npRefParam) { npRefParam = ⛶; }").Result);

            // Assignment to out parameter
            Assert.True(ProvidesFor(@"void Foo(out NodePath npOutParam) { npOutParam = ⛶; }").Result);
        }

        [Fact]
        public void TestElementAccessArgument()
        {
            Assert.True(ProvidesFor(@"
System.Collections.Generic.Dictionary<NodePath, object> npDictLocal = default;
_ = npDictLocal[⛶];
").Result);
        }

        [Fact]
        public void TestParenthesizedExpression()
        {
            Assert.True(ProvidesFor(@"NodePath _ = (⛶);").Result);
            Assert.True(ProvidesFor(@"NodePath _ = ((⛶));").Result);
            Assert.True(ProvidesFor(@"NodePath _ = (((⛶)));").Result);

            Assert.True(ProvidesFor(@"
void FirstParam(NodePath path) { }
FirstParam((⛶));
").Result);
        }

        [Fact]
        public void TestNullCoalescing()
        {
            // TODO: Not supported by the type inference service.
            // Null-coalescing operator
            //Assert.True(ProvidesFor(@"NodePath _ = null ?? ⛶;").Result);
            // Null-coalescing assignment
            //Assert.True(ProvidesFor(@"NodePath _ ??= ⛶;").Result);

            // Null-coalescing assignment in expression
            Assert.True(ProvidesFor(@"
NodePath _;
_ = _ ??= ⛶;
").Result);
        }

        [Fact]
        public void TestConditionalOperator()
        {
            Assert.True(ProvidesFor(@"NodePath _ = false ? ⛶ : default;").Result);
            Assert.True(ProvidesFor(@"NodePath _ = false ? default : ⛶;").Result);
        }

        [Fact]
        public void TestByRefParametersAssignment()
        {
            // Return statement expression
            Assert.True(ProvidesFor(@"NodePath Foo() { return ⛶; }").Result);

            // Return expression of expression-bodied method
            Assert.True(ProvidesFor(@"NodePath Foo() => ⛶;").Result);

            // Yield return expression
            Assert.True(ProvidesFor(@"IEnumerable<NodePath> Foo() { yield return ⛶; }").Result);
        }

        [Fact]
        public void TestStringLiteral()
        {
            // Empty string literal
            Assert.True(ProvidesForFull(@"NodePath _ = ""⛶").Result.CheckLiteralResult(""));
            Assert.True(ProvidesForFull(@"NodePath _ = ""⛶;").Result.CheckLiteralResult(";"));

            // Inside string literal (not closed, at end)
            Assert.True(ProvidesForFull(@"NodePath _ = ""Foo⛶").Result.CheckLiteralResult("Foo"));
            // Inside string literal (not closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = ""Foo⛶Bar").Result.CheckLiteralResult("FooBar"));
            // At end of string literal (closed)
            Assert.True(ProvidesForFull(@"NodePath _ = ""Foo⛶"";").Result.CheckLiteralResult("Foo"));
            // Inside string literal (closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = ""Foo⛶Bar"";").Result.CheckLiteralResult("FooBar"));
        }

        [Fact]
        public void TestVerbatimStringLiteral()
        {
            // Empty verbatim string literal
            Assert.True(ProvidesForFull(@"NodePath _ = @""⛶").Result.CheckLiteralResult(""));
            Assert.True(ProvidesForFull(@"NodePath _ = @""⛶;").Result.CheckLiteralResult(";"));

            // Inside verbatim string literal (not closed, at end)
            Assert.True(ProvidesForFull(@"NodePath _ = @""Foo⛶").Result.CheckLiteralResult("Foo"));
            // Inside verbatim string literal (not closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = @""Foo⛶Bar").Result.CheckLiteralResult("FooBar"));
            // Inside verbatim string literal (closed, at end)
            Assert.True(ProvidesForFull(@"NodePath _ = @""Foo⛶"";").Result.CheckLiteralResult("Foo"));
            // Inside verbatim string literal (closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = @""Foo⛶Bar"";").Result.CheckLiteralResult("FooBar"));
        }

        [Fact]
        public void TestInterpolatedStringLiteral()
        {
            // Empty interpolated string
            Assert.True(ProvidesForFull(@"NodePath _ = $""⛶").Result.CheckLiteralResult(""));
            Assert.True(ProvidesForFull(@"NodePath _ = $""⛶;").Result.CheckLiteralResult(";"));

            // Interpolated string literal without interpolations are supported
            // Inside interpolated string literal (not closed, at end)
            Assert.True(ProvidesForFull(@"NodePath _ = $""Foo⛶").Result.CheckLiteralResult("Foo"));
            // Inside interpolated string literal (not closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = $""Foo⛶Bar").Result.CheckLiteralResult("FooBar"));
            // Inside interpolated string literal (closed, at end)
            Assert.True(ProvidesForFull(@"NodePath _ = $""Foo⛶"";").Result.CheckLiteralResult("Foo"));
            // Inside interpolated string literal (closed, in between)
            Assert.True(ProvidesForFull(@"NodePath _ = $""Foo⛶Bar"";").Result.CheckLiteralResult("FooBar"));

            // Interpolated string literal with interpolations are not supported and must not provide completion
            // Inside interpolated string literal (not closed, at end)
            Assert.False(ProvidesFor(@"string aux = ""; NodePath _ = $""Foo{aux}⛶").Result);
            // Inside interpolated string literal (not closed, in between)
            Assert.False(ProvidesFor(@"string aux = ""; NodePath _ = $""Foo{aux}⛶Bar").Result);
            // Inside interpolated string literal (closed, at end)
            Assert.False(ProvidesFor(@"string aux = ""; NodePath _ = $""Foo{aux}⛶"";").Result);
            // Inside interpolated string literal (closed, in between)
            Assert.False(ProvidesFor(@"string aux = ""; NodePath _ = $""Foo{aux}⛶Bar"";").Result);
        }
    }
}
