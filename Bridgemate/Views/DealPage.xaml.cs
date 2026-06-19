using Bridgemate.Models;
using Bridgemate.Services;
using Model;
using Model.Enums;
using Model.Helpers;

namespace Bridgemate.Views;

[QueryProperty(nameof(DealNumber), "deal")]
public partial class DealPage : ContentPage {

    private readonly NavigationStore _navigationStore;
    private int _dealNumber;

    public int DealNumber { set => _dealNumber = value; }

    public DealPage(NavigationStore navigationStore) {
        _navigationStore = navigationStore;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args) {
        base.OnNavigatedTo(args);
        if (_dealNumber > 0) BuildPage(_dealNumber);
    }

    private void BuildPage(int dealNumber) {
        var deal = _navigationStore.Deals.FirstOrDefault(d => d.DealNumber == dealNumber);
        if (deal == null) return;
        Title = $"Deal #{dealNumber}";
        InfoLabel.Text = $"Dealer: {deal.Dealer}  -  NS: {deal.NsPoints} PC  |  EW: {deal.EwPoints} PC";
        BuildCompassContainer(deal);
        BuildAuctionContainer(deal);
        ContractLabel.Text = DescribeContract(deal);
    }

    private void BuildCompassContainer(DealResult deal) {
        CompassContainer.RowDefinitions.Clear();
        CompassContainer.ColumnDefinitions.Clear();
        CompassContainer.Children.Clear();
        CompassContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        CompassContainer.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
        CompassContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        CompassContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        CompassContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        CompassContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        CompassContainer.RowSpacing = 12;
        CompassContainer.ColumnSpacing = 8;
        var north = HandView("N", deal.Hands[PlayerPosition.North], LayoutOptions.Center);
        Grid.SetRow(north, 0); Grid.SetColumn(north, 1); CompassContainer.Children.Add(north);
        var west = HandView("W", deal.Hands[PlayerPosition.West], LayoutOptions.Start);
        Grid.SetRow(west, 1); Grid.SetColumn(west, 0); CompassContainer.Children.Add(west);
        var center = new VerticalStackLayout { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Spacing = 2, Children = { new Label { Text = $"NS {deal.NsPoints}", HorizontalOptions = LayoutOptions.Center, FontSize = 12, TextColor = Colors.Gray }, new Label { Text = "vs", HorizontalOptions = LayoutOptions.Center, FontSize = 11, TextColor = Colors.Gray }, new Label { Text = $"EW {deal.EwPoints}", HorizontalOptions = LayoutOptions.Center, FontSize = 12, TextColor = Colors.Gray } } };
        Grid.SetRow(center, 1); Grid.SetColumn(center, 1); CompassContainer.Children.Add(center);
        var east = HandView("E", deal.Hands[PlayerPosition.East], LayoutOptions.End);
        Grid.SetRow(east, 1); Grid.SetColumn(east, 2); CompassContainer.Children.Add(east);
        var south = HandView("S", deal.Hands[PlayerPosition.South], LayoutOptions.Center);
        Grid.SetRow(south, 2); Grid.SetColumn(south, 1); CompassContainer.Children.Add(south);
    }

    private static VerticalStackLayout HandView(string pos, Hand hand, LayoutOptions alignment) {
        var layout = new VerticalStackLayout { Spacing = 1, HorizontalOptions = alignment };
        layout.Children.Add(new Label { Text = pos, FontAttributes = FontAttributes.Bold, FontSize = 13, HorizontalOptions = LayoutOptions.Center });
        layout.Children.Add(new Label { Text = $"{hand.PointsNt} PC", FontSize = 11, TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
        layout.Children.Add(SuitLabel("S", hand.OfColor(CardColor.Spades),   Colors.Black));
        layout.Children.Add(SuitLabel("H", hand.OfColor(CardColor.Hearts),   Color.FromArgb("#C0392B")));
        layout.Children.Add(SuitLabel("D", hand.OfColor(CardColor.Diamonds), Color.FromArgb("#C0392B")));
        layout.Children.Add(SuitLabel("C", hand.OfColor(CardColor.Clubs),    Colors.Black));
        return layout;
    }

    private static Label SuitLabel(string sym, IEnumerable<Card> cards, Color color) =>
        new Label { Text = $"{sym}: {string.Join(" ", cards.Select(CardVal))}", TextColor = color, FontSize = 13 };

    private static string CardVal(Card c) => c.Value switch {
        CardValue.Ace => "A", CardValue.King => "K", CardValue.Queen => "Q", CardValue.Jack => "J",
        CardValue.Ten => "10", CardValue.Nine => "9", CardValue.Eight => "8", CardValue.Seven => "7",
        CardValue.Six => "6", CardValue.Five => "5", CardValue.Four => "4", CardValue.Three => "3",
        CardValue.Two => "2", _ => "?"
    };

    private void BuildAuctionContainer(DealResult deal) {
        AuctionContainer.RowDefinitions.Clear();
        AuctionContainer.ColumnDefinitions.Clear();
        AuctionContainer.Children.Clear();
        AuctionContainer.RowSpacing = 2;
        AuctionContainer.ColumnSpacing = 4;
        for (int i = 0; i < 4; i++) AuctionContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        AuctionContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var headers = new[] { "N", "E", "S", "W" };
        for (int col = 0; col < 4; col++) {
            var hdr = new Label { Text = headers[col], FontAttributes = FontAttributes.Bold, FontSize = 13, TextColor = Color.FromArgb("#888888"), HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center };
            Grid.SetRow(hdr, 0); Grid.SetColumn(hdr, col); AuctionContainer.Children.Add(hdr);
        }
        var padded = new List<AuctionEntry?>();
        for (int i = 0; i < (int)deal.Dealer; i++) padded.Add(null);
        padded.AddRange(deal.AuctionHistory.Cast<AuctionEntry?>());
        while (padded.Count % 4 != 0) padded.Add(null);
        int rowCount = padded.Count / 4;
        for (int row = 0; row < rowCount; row++) {
            AuctionContainer.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (int col = 0; col < 4; col++) {
                var entry = padded[row * 4 + col];
                var cell  = entry == null ? (View)new Label { Text = "" } : CreateBidCell(entry);
                Grid.SetRow(cell, row + 1); Grid.SetColumn(cell, col); AuctionContainer.Children.Add(cell);
            }
        }
    }

    private View CreateBidCell(AuctionEntry entry) {
        var textColor = BidTextColor(entry);
        if (entry.Node != null) {
            var btn = new Button { Text = entry.DisplayText, TextColor = textColor, BackgroundColor = Colors.Transparent, BorderColor = textColor, BorderWidth = 1, CornerRadius = 4, FontSize = 13, Padding = new Thickness(6, 2), Margin = new Thickness(2), HeightRequest = 32 };
            btn.Clicked += async (_, _) => { _navigationStore.SelectedEntry = entry; await Shell.Current.GoToAsync("biddetail"); };
            return btn;
        }
        return new Label { Text = entry.DisplayText, TextColor = textColor, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, FontSize = 13, Padding = new Thickness(6, 2), Margin = new Thickness(2) };
    }

    private static Color BidTextColor(AuctionEntry entry) => entry.Bid.Type switch {
        BidType.Pass => Colors.Gray,
        BidType.Double or BidType.Redouble => Color.FromArgb("#C0392B"),
        BidType.Submit => entry.Bid.Color switch {
            BidColor.Hearts or BidColor.Diamonds => Color.FromArgb("#C0392B"),
            BidColor.NoTrump => Color.FromArgb("#1565C0"),
            _ => Colors.Black
        },
        _ => Colors.Black
    };

    private static string DescribeContract(DealResult deal) {
        if (deal.Contract.Passed) return "Passed out - no contract.";
        var c         = deal.Contract;
        var playerHand = deal.Hands[c.Player];
        var dummyHand  = deal.Hands[c.Player.GetPartner()];
        var totalPts   = playerHand.PointsNt + dummyHand.PointsNt;
        var dist       = $"{playerHand.SpadesCount}-{playerHand.HeartsCount}-{playerHand.DiamondsCount}-{playerHand.ClubsCount}";
        var dummyDist  = $"{dummyHand.SpadesCount}-{dummyHand.HeartsCount}-{dummyHand.DiamondsCount}-{dummyHand.ClubsCount}";
        return $"{c}  on {totalPts} PC  ({dist} / dummy {dummyDist})";
    }
}
