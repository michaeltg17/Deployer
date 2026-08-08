using System.Text.RegularExpressions;

namespace Core.Testing.Validators
{
    public static partial class ExceptionValidator
    {
        public static bool IsValid(string exceptionText)
        {
            if (string.IsNullOrWhiteSpace(exceptionText))
                return false;

            if (!TypeMessageRegex().IsMatch(exceptionText))
                return false;

            var lines = exceptionText.Split('\n');
            bool hasStackFrame = false;
            bool hasSourceOrLambda = false;

            for (int i = 1; i < lines.Length; i++)
            {
                if (StackFrameRegex().IsMatch(lines[i]))
                {
                    hasStackFrame = true;
                    if (SourceLocationRegex().IsMatch(lines[i]) || CompilerGeneratedRegex().IsMatch(lines[i]))
                        hasSourceOrLambda = true;
                }
            }

            return hasStackFrame && hasSourceOrLambda;
        }

        [GeneratedRegex(@"^(?<type>[a-zA-Z][\w.]+):\s+(?<message>.+)$", RegexOptions.Multiline)]
        private static partial Regex TypeMessageRegex();

        [GeneratedRegex(@"^\s+at\s+")]
        private static partial Regex StackFrameRegex();

        [GeneratedRegex(@"in\s+\S+\.cs:line\s+\d+")]
        private static partial Regex SourceLocationRegex();

        [GeneratedRegex(@"(<|lambda_)")]
        private static partial Regex CompilerGeneratedRegex();
    }
}