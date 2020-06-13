using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace GodotCompletionProviders
{
    public abstract class BaseCompletionProvider : CompletionProvider
    {
        private readonly CompletionKind _kind;
        private readonly string _inlineDescription;

        // No idea how else to pass this as we don't create the provider instance
        // ReSharper disable once UnassignedField.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public static IProviderContext Context;

        protected BaseCompletionProvider(CompletionKind kind, string inlineDescription)
        {
            _kind = kind;
            _inlineDescription = inlineDescription;
        }

        public struct CheckResult
        {
            public bool ShouldProvideCompletion;
            public SyntaxNode StringSyntax;

            public object StringSyntaxValue => RoslynUtils.GetStringSyntaxValue(StringSyntax);

            public static CheckResult False() => new CheckResult {ShouldProvideCompletion = false};

            public static CheckResult True(SyntaxNode stringSyntax) =>
                new CheckResult {ShouldProvideCompletion = true, StringSyntax = stringSyntax};
        }

        public abstract Task<CheckResult> ShouldProvideCompletion(Document document, int position);

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual async Task<CheckResult> ShouldProvideCompletion(CompletionContext context)
        {
            var document = context.Document;

            if (document == null)
                return CheckResult.False();

            return await ShouldProvideCompletion(document, context.Position);
        }

        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual bool ShouldTriggerCompletion()
        {
            if (Context == null)
                return false;

            return Context.AreCompletionsEnabledFor(_kind) && Context.CanRequestCompletionsFromServer();
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options) =>
            ShouldTriggerCompletion();

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (!ShouldTriggerCompletion())
                return;

            var checkResult = await ShouldProvideCompletion(context);

            if (!checkResult.ShouldProvideCompletion)
                return;

            string scriptFile = Path.GetFullPath(context.Document.FilePath);

            var suggestions = await Context.RequestCompletion(_kind, scriptFile);

            if (suggestions.Length == 0)
                return;

            ImmutableDictionary<string, string> properties = null;

            if (checkResult.StringSyntax != null)
            {
                var propertiesBuilder = ImmutableDictionary.CreateBuilder<string, string>();
                propertiesBuilder.Add("GodotSpan.Start", checkResult.StringSyntax.Span.Start.ToString());

                // TODO:
                // This is commented out because of cases like the following: `Foo("Bar$$, 10, "Baz");`
                // Instead of replacing only `"Bar` it would replace `"Bar, 10, "`. It gets even worse
                // with verbatim string literals which can be multiline. Unless we can find a way to
                // avoid this, it's better to only replace up to the caret position, even if that means
                // something like `Foo("Bar$$Baz")` will result in `Foo("BarINSERTED"Baz").
                //
                // propertiesBuilder.Add("GodotSpan.Length", checkResult.StringSyntax.Span.Length.ToString());

                propertiesBuilder.Add("GodotSpan.Length", (context.Position - checkResult.StringSyntax.Span.Start).ToString());

                properties = propertiesBuilder.ToImmutable();
            }

            foreach (string suggestion in suggestions)
            {
                var completionItem = CompletionItem.Create(
                    displayText: suggestion,
                    filterText: null,
                    sortText: null,
                    properties: properties,
                    tags: ImmutableArray<string>.Empty,
                    rules: null,
                    displayTextPrefix: null,
                    displayTextSuffix: null,
                    inlineDescription: _inlineDescription
                );
                context.AddItem(completionItem);
            }
        }

        public override Task<CompletionChange> GetChangeAsync(
            Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            int? spanStart = null;
            int? spanLength = null;

            if (item.Properties.TryGetValue("GodotSpan.Start", out string spanStartStr))
            {
                if (int.TryParse(spanStartStr, out int startResult))
                {
                    spanStart = startResult;
                }

                if (item.Properties.TryGetValue("GodotSpan.Length", out string spanLengthStr))
                {
                    if (int.TryParse(spanLengthStr, out int lengthResult))
                    {
                        spanLength = lengthResult;
                    }
                }
            }

            var span = spanStart.HasValue && spanLength.HasValue ?
                new TextSpan(spanStart.Value, spanLength.Value) :
                item.Span;

            return Task.FromResult(CompletionChange.Create(new TextChange(span, item.DisplayText)));
        }
    }
}
