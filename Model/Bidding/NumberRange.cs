using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding;

public class NumberRange {
     
    public int? Lower { get; set; }

    public int? Upper { get; set; }

    public NumberRange(int? lower, int? upper) {
        Lower = lower;
        Upper = upper;
    }

    public void Narrow(NumberRange? newValue) {
        if (newValue.Lower != null && (Lower == null || newValue.Lower > Lower)) {
            Lower = newValue.Lower;
        }
        if (newValue.Upper != null && (Upper == null || newValue.Upper < Upper)) {
            Upper = newValue.Upper;
        }
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
