using BiddingBrowser.BiddingTree.Bids;
using BiddingBrowser.BiddingTree.Validation.Constraints;
using Model.Bidding;
using System;
using System.Collections.Generic;
using System.Text;

using DomainBidColor = Model.Enums.BidColor;


namespace BiddingBrowser.BiddingTree.Validation {
    internal enum ValidationSeverity {
        Info,
        Warning,
        Error
    }

    internal sealed class TreeValidator
    {
        private const int MinPC = 0;
        private const int MaxPC = 40;

        private const int MinCards = 0;
        private const int MaxCards = 13;

        // Feature 1: Structural validation only (speaker alternation, basic sanity checks).
        public List<ValidationIssue> Validate(IEnumerable<Root> roots) {
            var issues = new List<ValidationIssue>();

            foreach (var root in roots)
            {
                ValidateRoot(root, issues);
            }

            return issues;
        }

        private void ValidateRoot(Root root, List<ValidationIssue> issues) {
            var rootName = string.IsNullOrWhiteSpace(root.Name) ? "<root>" : root.Name;
            var con = BiddingCon.CreateInitial(MinPC, MaxPC, MinCards, MaxCards);

            foreach (var bid in root.Bids)
            {
                ValidateBidSubtree(parent: null, current: bid, path: rootName, issues: issues, con: con);
            }
        }

        private void ValidateBidSubtree(Bid? parent, Bid current, string path, List<ValidationIssue> issues, BiddingCon con) {
            var currentPath = $"{path} > {FormatBid(current)}";

            ValidateSpeakerAlternation(parent, current, currentPath, issues);
            ValidateBidRanges(current, currentPath, issues);

            if (!TryApplyPcConstraint(current, currentPath, issues, con, out var nextCon))
                return;

            if (!TryApplySuitLengthConstraints(current, currentPath, issues, nextCon, out nextCon))
                return;

            if (!ValidatePartnershipPc(nextCon, current, currentPath, issues))
                return;
            if (!ValidatePartnershipSuitTotals(nextCon, current, currentPath, issues))
                return;

            // Recurse into children.
            foreach (var child in current.NextBids) {
                ValidateBidSubtree(parent: current, current: child, path: currentPath, issues: issues, con: nextCon);
            }
        }

        private static string FormatBid(Bid bid) {
            var label = $"{bid.Value?.ToString() ?? ""}{bid.Code}".Trim();
            if (string.IsNullOrWhiteSpace(label))
                label = "<bid>";

            if (!string.IsNullOrWhiteSpace(bid.Identifier)) {
                return $"{label} ({bid.Identifier})";
            }

            return label;
        }

        private void ValidateSpeakerAlternation(Bid? parent, Bid current, string currentPath, List<ValidationIssue> issues) {
            if (parent == null)
                return;

            if (parent.OpenerBid == current.OpenerBid) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Invalid speaker alternation: child has the same OpenerBid as parent.",
                    currentPath,
                    string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                ));
            }
        }

        private void ValidateBidRanges(Bid bid, string path, List<ValidationIssue> issues) {
            ValidateRange("PointsRange", bid.PointsRange, MinPC, MaxPC, bid, path, issues);

            ValidateRange("ClubsCardRange", bid.ClubsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("DiamondsCardRange", bid.DiamondsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("HeartsCardRange", bid.HeartsCardRange, MinCards, MaxCards, bid, path, issues);
            ValidateRange("SpadesCardRange", bid.SpadesCardRange, MinCards, MaxCards, bid, path, issues);
        }

        private void ValidateRange(string rangeName, NumberRange range, int minDomain, int maxDomain,
                                        Bid bid, string path, List<ValidationIssue> issues) {
            var lower = range.Lower;
            var upper = range.Upper;

            if (lower.HasValue && upper.HasValue && lower.Value > upper.Value) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName} is invalid: Lower ({lower}) > Upper ({upper}).",
                    path
                ));
            }

            if (lower.HasValue && (lower.Value < minDomain || lower.Value > maxDomain)) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName}.Lower ({lower}) is outside domain [{minDomain}, {maxDomain}].",
                    path
                ));
            }

            if (upper.HasValue && (upper.Value < minDomain || upper.Value > maxDomain)) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"{rangeName}.Upper ({upper}) is outside domain [{minDomain}, {maxDomain}].",
                    path
                ));
            }
        }

        private bool TryApplyPcConstraint(Bid current, string currentPath, List<ValidationIssue> issues, BiddingCon con, out BiddingCon nextCon) {
            nextCon = con;

            var constraint = current.PointsRange;
            if (constraint.Lower == null && constraint.Upper == null) {
                return true;
            }

            if (current.OpenerBid) {
                if (!RangeCon.TryIntersect(con.Opener.Pc, constraint, out var narrowed)) {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        $"Bid is impossible due to PC constraints (opener). Constraint: {constraint}, Current: {con.Opener.Pc}.",
                        currentPath,
                        string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                    ));
                    return false;
                }

                nextCon = con.WithOpenerPc(narrowed);
                return true;
            }
            else {
                if (!RangeCon.TryIntersect(con.Responder.Pc, constraint, out var narrowed)) {
                    issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        $"Bid is impossible due to PC constraints (responder). Constraint: {constraint}, Current: {con.Responder.Pc}.",
                        currentPath,
                        string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                    ));
                    return false;
                }

                nextCon = con.WithResponderPc(narrowed);
                return true;
            }
        }

        private bool TryApplySuitLengthConstraints(Bid current, string currentPath, List<ValidationIssue> issues, BiddingCon con, out BiddingCon nextCon) {
            nextCon = con;

            if (!TryApplyOneSuitLength(current, currentPath, issues, nextCon, DomainBidColor.Clubs, current.ClubsCardRange, out nextCon))
                return false;
            if (!TryApplyOneSuitLength(current, currentPath, issues, nextCon, DomainBidColor.Diamonds, current.DiamondsCardRange, out nextCon))
                return false;
            if (!TryApplyOneSuitLength(current, currentPath, issues, nextCon, DomainBidColor.Hearts, current.HeartsCardRange, out nextCon))
                return false;
            if (!TryApplyOneSuitLength(current, currentPath, issues, nextCon, DomainBidColor.Spades, current.SpadesCardRange, out nextCon))
                return false;

            return true;
        }

        private bool TryApplyOneSuitLength(Bid current, string currentPath, List<ValidationIssue> issues, BiddingCon con, DomainBidColor suit, NumberRange constraint, out BiddingCon nextCon) {

            nextCon = con;

            if (constraint.Lower == null && constraint.Upper == null) {
                return true;
            }

            var isOpener = current.OpenerBid;

            var currentRange = con.GetSuitLength(isOpener, suit);

            if (!RangeCon.TryIntersect(currentRange, constraint, out var narrowed)) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Bid is impossible due to suit length constraints ({(isOpener ? "opener" : "responder")}). {suit}: Constraint: {constraint}, Current: {currentRange}.",
                    currentPath,
                    string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                ));
                return false;
            }

            nextCon = con.WithSuitLength(isOpener, suit, narrowed);
            return true;
        }

        private bool ValidatePartnershipPc(BiddingCon con, Bid current, string currentPath, List<ValidationIssue> issues) {
            var oMin = con.Opener.Pc.Lower ?? MinPC;
            var oMax = con.Opener.Pc.Upper ?? MaxPC;

            var rMin = con.Responder.Pc.Lower ?? MinPC;
            var rMax = con.Responder.Pc.Upper ?? MaxPC;

            var minSum = oMin + rMin;
            var maxSum = oMax + rMax;

            if (minSum > MaxPC) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Partnership PC impossible: min sum {minSum} > {MaxPC}. Opener {con.Opener.Pc}, Responder {con.Responder.Pc}.",
                    currentPath,
                    string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                ));
                return false;
            }

            // Optional: you can keep this for debugging as Info/Warning later.
            // var effectiveMaxSum = Math.Min(MaxPC, maxSum);

            return true;
        }

        private bool ValidatePartnershipSuitTotals(BiddingCon con, Bid current, string currentPath, List<ValidationIssue> issues) {
            if (!ValidateOneSuitTotal(con, current, currentPath, issues, Model.Enums.BidColor.Clubs, "Clubs"))
                return false;
            if (!ValidateOneSuitTotal(con, current, currentPath, issues, Model.Enums.BidColor.Diamonds, "Diamonds"))
                return false;
            if (!ValidateOneSuitTotal(con, current, currentPath, issues, Model.Enums.BidColor.Hearts, "Hearts"))
                return false;
            if (!ValidateOneSuitTotal(con, current, currentPath, issues, Model.Enums.BidColor.Spades, "Spades"))
                return false;

            return true;
        }

        private bool ValidateOneSuitTotal(BiddingCon con, Bid current, string currentPath, List<ValidationIssue> issues, DomainBidColor suit, string suitName) {

            var o = con.GetSuitLength(opener: true, suit);
            var r = con.GetSuitLength(opener: false, suit);

            var oMin = o.Lower ?? MinCards;
            var rMin = r.Lower ?? MinCards;

            var minSum = oMin + rMin;

            if (minSum > MaxCards) {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    $"Partnership {suitName} length impossible: min sum {minSum} > {MaxCards}. Opener {o}, Responder {r}.",
                    currentPath,
                    string.IsNullOrWhiteSpace(current.Identifier) ? null : current.Identifier
                ));
                return false;
            }

            return true;
        }











    }
}
