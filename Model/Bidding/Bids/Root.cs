using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Bids; 
public class Root {

    public string? Name { get; set; }

    public List<BidNode> Bids { get; set; } = new List<BidNode>();

    public void AssignParnt() {
        foreach (var bid in Bids) {
            bid.AssignParent(null);
        }
    }


}
