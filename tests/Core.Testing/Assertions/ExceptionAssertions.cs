using System.Text.RegularExpressions;

namespace Core.Testing
{
    public static partial class ExceptionAssertions
    {
        static readonly Regex TypeMessageRegex = TypeMessageRegexValidator();
        static readonly Regex StackFrameRegex = StackFrameRegexValidator();
        static readonly Regex SourceLocationRegex = SourceLocationRegexValidator();
        static readonly Regex CompilerGeneratedRegex = CompilerGeneratedRegexValidator();

        public static bool Assert(string exceptionText)
        {
            if (string.IsNullOrWhiteSpace(exceptionText))
                return false;

            if (!TypeMessageRegex.IsMatch(exceptionText))
                return false;

            var lines = exceptionText.Split('\n');
            bool hasStackFrame = false;
            bool hasSourceOrLambda = false;

            for (int i = 1; i < lines.Length; i++)
            {
                if (StackFrameRegex.IsMatch(lines[i]))
                {
                    hasStackFrame = true;
                    if (SourceLocationRegex.IsMatch(lines[i]) || CompilerGeneratedRegex.IsMatch(lines[i]))
                        hasSourceOrLambda = true;
                }
            }

            return hasStackFrame && hasSourceOrLambda;
        }

        [GeneratedRegex(
            @"^(?<type>[a-zA-Z][\w.]+):\s+(?<message>.+)$",
            RegexOptions.Multiline | RegexOptions.Compiled,
            "en-US")]
        private static partial Regex TypeMessageRegexValidator();

        [GeneratedRegex(
            @"^\s+at\s+",
            RegexOptions.Compiled,
            "en-US")]
        private static partial Regex StackFrameRegexValidator();

        [GeneratedRegex(
            @"in\s+\S+\.cs:line\s+\d+",
            RegexOptions.Compiled,
            "en-US")]
        private static partial Regex SourceLocationRegexValidator();

        [GeneratedRegex(
            @"(<|lambda_)",
            RegexOptions.Compiled,
            "en-US")]
        private static partial Regex CompilerGeneratedRegexValidator();
    }
}