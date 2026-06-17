using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding;

public class NumberRange {
     
    public int? Lower { get; set; }

    public int? Upper { get; set; }


    public NumberRange() {
        Lower = null;
        Upper = null;
    }


    public NumberRange(int? lower, int? upper) {
        Lower = lower;
        Upper = upper;
    }

    public void Narrow(NumberRange? newValue) {
        if (newValue == null) {
            return;
        }

        if (newValue.Lower != null && (Lower == null || newValue.Lower > Lower)) {
            Lower = newValue.Lower;
        }
        if (newValue.Upper != null && (Upper == null || newValue.Upper < Upper)) {
            Upper = newValue.Upper;
        }
    }


    public static NumberRange operator+(NumberRange? left, NumberRange? right) {
        return new NumberRange(
            (left?.Lower ?? 0) + (right?.Lower ?? 0),
            (left?.Upper ?? 0) + (right?.Upper ?? 0)
        );
    }


    public static bool operator<(NumberRange? left, int? right) {
        if (left == null || right == null) {
            return false;
        }
        return left.Upper < right;
    }


    public static bool operator>(NumberRange? left, int? right) {
        if (left == null || right == null) {
            return false;
        }
        return left.Lower > right;
    }


    public static bool operator <=(NumberRange? left, int? right) {
        if (left == null || right == null) {
            return false;
        }
        return left.Upper <= right;
    }


    public static bool operator >=(NumberRange? left, int? right) {
        if (left == null || right == null) {
            return false;
        }
        return left.Lower >= right;
    }


    public override string ToString() {
        if (Lower == null) {
            return $"<{Upper}";
        }
        if (Upper == null) {
            return $"{Lower}+";
        }
        return $"{Lower}-{Upper}";
    }
}
