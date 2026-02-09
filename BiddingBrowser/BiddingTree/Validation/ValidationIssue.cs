using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Validation {
    internal sealed class ValidationIssue {
        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
        public string? BidIdentifier { get; }

        public ValidationIssue(ValidationSeverity severity, string message, string path, string? bidIdentifier = null) {
            Severity = severity;
            Message = message;
            Path = path;
            BidIdentifier = bidIdentifier;
        }

        public override string ToString() {
            return $"{Severity}: {Message} (Path: {Path}, Id: {BidIdentifier ?? "n/a"})";
        }
    }
}
