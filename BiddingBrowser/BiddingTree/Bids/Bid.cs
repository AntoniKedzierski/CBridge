using Model.Bidding;
using Model.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiddingBrowser.BiddingTree.Bids;

public class Bid : BindableBase {

    public required string Identifier { get; set => SetProperty(ref field, value); }

    public int Value {
        get;
        set {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(Code));
        }
    }

    public BidColor Color {
        get;
        set {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(Code));
        }
    }

    public BidType Type {
        get;
        set {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(Code));
        }
    }

    public string? Description { get; set => SetProperty(ref field, value); }

    public string? Condition { get; set => SetProperty(ref field, value); }

    public NumberRange PointsRange { get; set => SetProperty(ref field, value); } = new(null, null);

    public NumberRange SpadesCardRange { get; set => SetProperty(ref field, value); } = new(null, null);

    public NumberRange HeartsCardRange { get; set => SetProperty(ref field, value); } = new(null, null);

    public NumberRange DiamondsCardRange { get; set => SetProperty(ref field, value); } = new(null, null);

    public NumberRange ClubsCardRange { get; set => SetProperty(ref field, value); } = new(null, null);

    public bool OpenerBid { get; set => SetProperty(ref field, value); }

    public ObservableCollection<Bid> NextBids { get; set => SetProperty(ref field, value); } = new();

    public string Code {
        get {
            if (Type == BidType.Pass) {
                return "Pass";
            }
            else if (Type == BidType.Double) {
                return "X";
            }
            else if (Type == BidType.Redouble) {
                return "XX";
            }

            return Value.ToString() + Color switch {
                BidColor.Clubs => "♣",
                BidColor.Diamonds => "♢",
                BidColor.Hearts => "♡",
                BidColor.Spades => "♠",
                BidColor.NoTrump => "NT",
                _ => ""
            };
        }
    }
}
