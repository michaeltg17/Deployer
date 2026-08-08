using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Core.Testing.Validators
{
    public static partial class TraceIdValidator
    {
        [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Clearer")]
        public static bool IsValid(string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
                return false;

            var match = TraceIdRegex().Match(traceId);
            if (!match.Success)
                return false;

            var version = match.Groups["version"].Value;
            var traceIdValue = match.Groups["traceId"].Value;
            var parentId = match.Groups["parentId"].Value;

            // Ensure version is valid (currently only "00" is supported)
            if (!string.Equals(version, "00", StringComparison.OrdinalIgnoreCase))
                return false;

            // Trace ID must not be all zeros
            if (traceIdValue == new string('0', 32))
                return false;

            // Parent ID must not be all zeros
            if (parentId == new string('0', 16))
                return false;

            return true;
        }

        [GeneratedRegex(@"^(?<version>[0-9a-f]{2})-(?<traceId>[0-9a-f]{32})-(?<parentId>[0-9a-f]{16})-(?<flags>[0-9a-f]{2})$")]
        private static partial Regex TraceIdRegex();
    }
}
