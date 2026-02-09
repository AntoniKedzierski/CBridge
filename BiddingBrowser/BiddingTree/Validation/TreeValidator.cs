using BiddingBrowser.BiddingTree.Bids;
using Model.Bidding;
using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Validation
{
    internal enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    internal sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public string Path { get; }
        public string? BidIdentifier { get; }

        public ValidationIssue(ValidationSeverity severity, string message, string path, string? bidIdentifier = null)
        {
            Severity = severity;
            Message = message;
            Path = path;
            BidIdentifier = bidIdentifier;
        }

        public override string ToString()
        {
            return $"{Severity}: {Message} (Path: {Path}, Id: {BidIdentifier ?? "n/a"})";
        }
    }

    internal sealed class TreeValidator
    {
        private const int MinPoints = 0;
        private const int MaxPoints = 40;

        private const int MinCards = 0;
        private const int MaxCards = 13;

        // Feature 1: Structural validation only (speaker alternation, basic sanity checks).
        public List<ValidationIssue> Validate(IEnumerable<Root> roots)
        {
            var issues = new List<ValidationIssue>();

            foreach (var root in roots)
            {
                ValidateRoot(root, issues);
            }

            return issues;
        }

        private void ValidateRoot(Root root, List<ValidationIssue> issues)
        {
            var rootName = string.IsNullOrWhiteSpace(root.Name) ? "<root>" : root.Name;

            foreach (var bid in root.Bids)
            {
                ValidateBidSubtree(parent: null, current: bid, path: rootName, issues: issues);
            }
        }

        private void ValidateBidSubtree(Bid? parent, Bid current, string path, List<ValidationIssue> issues)
        {
            var currentPath = $"{path} > {FormatBid(current)}";

            ValidateBidRanges(current, currentPath, issues);

            // Speaker alternation: parent and child must not have the same OpenerBid.
            if (parent != null && parent.OpenerBid == current.OpenerBid)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Invalid speaker alternation: child has the same OpenerBid as parent.",
                    currentPath,
                    string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                ));
            }

            // Recurse into children.
            foreach (var child in current.NextBids)
            {
                ValidateBidSubtree(parent: current, current: child, path: currentPath, issues: issues);
            }
        }

        private static string FormatBid(Bid bid)
        {
            var label = $"{bid.Value?.ToString() ?? ""}{bid.Code}".Trim();
            if (string.IsNullOrWhiteSpace(label)) label = "<bid>";

            if (!string.IsNullOrWhiteSpace(bid.Identifier))
            {
                return $"{label} ({bid.Identifier})";
            }

            return label;
        }

        private void ValidateBidRanges(Bid bid, string path, List<ValidationIssue> issues)
        {
            ValidateRange("PointsRange", bid.PointsRange, MinPoints, MaxPoints, bid, path, issues);

            ValidateRange("ClubsCardRange", bid.ClubsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("DiamondsCardRange", bid.DiamondsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("HeartsCardRange", bid.HeartsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("SpadesCardRange", bid.SpadesCardRange, MinCards, MaxCards, bid, path, issues);
        }

        private void ValidateRange(string rangeName, NumberRange range, int minDomain, int maxDomain,
                                        Bid bid, string path, List<ValidationIssue> issues) {
            var lower = range.Lower;
            var upper = range.Upper;

            if (lower.HasValue && upper.HasValue && lower.Value > upper.Value)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName} is invalid: Lower ({lower}) > Upper ({upper}).",
                    path
                ));
            }

            if (lower.HasValue && (lower.Value < minDomain || lower.Value > maxDomain))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName}.Lower ({lower}) is outside domain [{minDomain}, {maxDomain}].",
                    path
                ));
            }

            if (upper.HasValue && (upper.Value < minDomain || upper.Value > maxDomain))
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName}.Upper ({upper}) is outside domain [{minDomain}, {maxDomain}].",
                    path
                ));
            }
        }


    }
}
