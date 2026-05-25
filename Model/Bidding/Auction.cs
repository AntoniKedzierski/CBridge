using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding; 
public class Auction {
    public PlayerPosition CurrentBidder { get; private set; }

    public bool IsCompleted() {
        return true;
    }

    public void Submit() { 
        
    }
}
