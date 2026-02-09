using Model.Bidding;
using System;
using System.Collections.Generic;
using System.Text;

namespace BiddingBrowser.BiddingTree.Validation.Constraints {
    internal static class RangeCon {

        public static bool TryIntersect(NumberRange current, NumberRange constraint, out NumberRange result) {
            int? lower = MaxNullable(current.Lower, constraint.Lower);
            int? upper = MinNullable(current.Upper, constraint.Upper);

            result = new NumberRange(lower, upper);
            return !IsEmpty(result);
        }

        public static bool IsEmpty(NumberRange r) {
            return r.Lower.HasValue && r.Upper.HasValue && r.Lower.Value > r.Upper.Value;
        }

        private static int? MaxNullable(int? x, int? y) {
            if (!x.HasValue)
                return y;
            if (!y.HasValue)
                return x;
            return x.Value > y.Value ? x.Value : y.Value;
        }

        private static int? MinNullable(int? x, int? y) {
            if (!x.HasValue)
                return y;
            if (!y.HasValue)
                return x;
            return x.Value < y.Value ? x.Value : y.Value;
        }
    }
}
