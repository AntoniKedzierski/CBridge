using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids;

public class Bid {
    public BidType BidType { get; set; }
    public BidColor Color { get; set; }
    public int? Value { get; set; }
}
