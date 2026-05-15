using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Validation {
    internal sealed class ValidationIssue {
        private const string EmptyIdentifier = "<empty>";

        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
        public string? BidIdentifier { get; }
        public string? ConventionContext { get; }

        public ValidationIssue(ValidationSeverity severity, string message, string path, string? bidIdentifier = null, string? conventionContext = null) {
            Severity = severity;
            Message = message;
            Path = path;
            BidIdentifier = NormalizeIdentifier(bidIdentifier);
            ConventionContext = NormalizeConventionContext(conventionContext);
        }

        public override string ToString() {
            var suffix = BidIdentifier is null ? string.Empty : $", Id: {BidIdentifier}";
            var conventionSuffix = ConventionContext is null ? string.Empty : $", Convention: {ConventionContext}";
            return $"{Severity}: {Message} (Path: {Path}{suffix}{conventionSuffix})";
        }

        private static string? NormalizeIdentifier(string? bidIdentifier) {
            if (string.IsNullOrWhiteSpace(bidIdentifier)) {
                return null;
            }

            return string.Equals(bidIdentifier, EmptyIdentifier, StringComparison.Ordinal)
                ? null
                : bidIdentifier;
        }

        private static string? NormalizeConventionContext(string? conventionContext) {
            return string.IsNullOrWhiteSpace(conventionContext) ? null : conventionContext;
        }
    }
}
