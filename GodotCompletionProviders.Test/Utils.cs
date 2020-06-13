using System.Collections.Generic;

namespace GodotCompletionProviders.Test
{
    public static class Utils
    {
        private static int IndexOfAny(this string str, char[] anyOf, out char which)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                foreach (char charInAnyOf in anyOf)
                {
                    if (c == charInAnyOf)
                    {
                        which = c;
                        return i;
                    }
                }
            }

            which = default;
            return -1;
        }

        public static string ReadMultiCaretTestCode(string testCode, ICollection<int> mustPassCaretPositions, ICollection<int> mustNotPassCaretPositions)
        {
            string code = testCode;

            const char mustPassChar = '✔';
            const char mustNotPassChar = '✘';

            int indexOfCaret;
            while ((indexOfCaret = code.IndexOfAny(new[] {mustPassChar, mustNotPassChar}, out char which)) >= 0)
            {
                (which == mustPassChar ? mustPassCaretPositions : mustNotPassCaretPositions).Add(indexOfCaret);
                code = code.Remove(indexOfCaret, 1);
            }

            return code;
        }

        public static string ReadSingleCaretTestCode(string testCode, out int caretPosition)
        {
            const char caretChar = '⛶';

            string code = testCode;

            caretPosition = code.IndexOf(caretChar);

            if (caretPosition >= 0)
                code = code.Remove(caretPosition, 1);

            return code;
        }

        public static bool CheckLiteralResult(this BaseCompletionProvider.CheckResult result, string expected)
        {
            if (!result.ShouldProvideCompletion)
                return false;

            if (result.StringSyntax == null)
                return false;

            return result.StringSyntaxValue is string strValue && strValue == expected;
        }
    }
}
