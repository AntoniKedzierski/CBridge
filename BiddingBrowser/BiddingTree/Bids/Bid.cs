using Model.Bidding;
using Model.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiddingBrowser.BiddingTree.Bids;

public class Bid : BindableBase, IBidsContainer {

    public static List<BidColor> BidColorValues { get; } = Enum.GetValues<BidColor>().ToList();

    public static List<BidType> BidTypeValues { get; } = Enum.GetValues<BidType>().ToList();

    public required string Identifier { get; set => SetProperty(ref field, value); }

    public int? Value {
        get;
        set {
            SetProperty(ref field, value);
            RaisePropertyChanged(nameof(Code));
        }
    } = null;

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

    public bool SignOff { get; set => SetProperty(ref field, value); }

    public ObservableCollection<Bid> NextBids { get; set => SetProperty(ref field, value); } = new();


    [JsonIgnore]
    public IBidsContainer? Parent { get; set; }

    [JsonIgnore]
    public DelegateCommand MoveUpCommand { get; set; }

    [JsonIgnore]
    public DelegateCommand MoveDownCommand { get; set; }


    public Bid() {
        MoveUpCommand = new DelegateCommand(() => MoveUp());
        MoveDownCommand = new DelegateCommand(() => MoveDown());
    }


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

            return Color switch {
                BidColor.Clubs => "♣",
                BidColor.Diamonds => "♦",
                BidColor.Hearts => "♥",
                BidColor.Spades => "♠",
                BidColor.NoTrump => "NT",
                _ => ""
            };
        }
    }


    public void AddBid(Bid bid) {
        NextBids.Add(bid);
        bid.Parent = this;
    }


    public void RemoveBid(Bid bid) {
        NextBids.Remove(bid);
    }


    public void MoveUp(Bid bid) {
        var index = NextBids.IndexOf(bid);
        if (index < 1) {
            return;
        }

        NextBids.Remove(bid);
        NextBids.Insert(index - 1, bid);
    }


    public void MoveDown(Bid bid) {
        var index = NextBids.IndexOf(bid);
        if (index > NextBids.Count - 1) {
            return;
        }

        NextBids.Remove(bid);
        NextBids.Insert(index + 1, bid);
    }


    public void RemoveSelf() {
        Parent?.RemoveBid(this);
    }


    public void AssignParent() {
        foreach (var child in NextBids) {
            child.Parent = this;
            child.AssignParent();
        }
    }


    public void MoveUp() {
        Parent?.MoveUp(this);
    }


    public void MoveDown() {
        Parent?.MoveDown(this);
    }
}
