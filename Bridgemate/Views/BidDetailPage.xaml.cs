using Bridgemate.Models;
using Bridgemate.Services;
using Microsoft.Maui.Controls.Shapes;
using Model.Bidding.Bids;
using Model.Enums;

namespace Bridgemate.Views;

public partial class BidDetailPage : ContentPage {

    private readonly NavigationStore _navigationStore;

    public BidDetailPage(NavigationStore navigationStore) {
        _navigationStore = navigationStore;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args) {
        base.OnNavigatedTo(args);
        BuildPage(_navigationStore.SelectedEntry);
    }

    private void BuildPage(AuctionEntry? entry) {
        DetailsStack.Children.Clear();
        BidPathRow.Children.Clear();
        if (entry == null) {
            Title = "Bid Details";
            BidBadgeLabel.Text = "?";
            BidderLabel.Text = string.Empty;
            DescriptionBox.IsVisible = false;
            StackTraceDivider.IsVisible = false;
            StackTraceSection.IsVisible = false;
            DetailsStack.Children.Add(new Label { Text = "No entry selected.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
            return;
        }
        Title = $"Bid: {entry.DisplayText}";
        var bidColor = GetBidColor(entry);
        BidBadgeLabel.Text = entry.DisplayText;
        BidBadgeLabel.TextColor = bidColor;
        BidBadge.Stroke = new SolidColorBrush(bidColor);
        BidderLabel.Text = $"Bidder: {entry.Position}";
        var node = entry.Node;
        if (node != null && !string.IsNullOrEmpty(node.Description)) {
            DescriptionLabel.Text = node.Description;
            DescriptionBox.IsVisible = true;
        } else { DescriptionBox.IsVisible = false; }
        if (node == null || !HasMeaningfulInfo(node)) {
            DetailsStack.Children.Add(new Label { Text = "No system information available for this bid.\n(Natural / freestyle bid)", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center, FontSize = 14, Margin = new Thickness(0, 8) });
        } else {
            if (!string.IsNullOrEmpty(node.Convention)) AddRow("Convention", node.Convention!);
            if (!string.IsNullOrEmpty(node.Condition))  AddRow("Condition",  node.Condition!);
            bool hasReqs = node.PointsRange != null || node.SpadesCardRange != null || node.HeartsCardRange != null || node.DiamondsCardRange != null || node.ClubsCardRange != null || node.Aces.HasValue || node.Kings.HasValue;
            if (hasReqs) {
                AddSectionHeader("Requirements");
                if (node.PointsRange != null)       AddRow("Points",     node.PointsRange.ToString()!);
                if (node.SpadesCardRange != null)   AddRow("S Spades",   node.SpadesCardRange.ToString()!);
                if (node.HeartsCardRange != null)   AddRow("H Hearts",   node.HeartsCardRange.ToString()!);
                if (node.DiamondsCardRange != null) AddRow("D Diamonds", node.DiamondsCardRange.ToString()!);
                if (node.ClubsCardRange != null)    AddRow("C Clubs",    node.ClubsCardRange.ToString()!);
                if (node.Aces.HasValue)             AddRow("Aces",       node.Aces.Value.ToString());
                if (node.Kings.HasValue)            AddRow("Kings",      node.Kings.Value.ToString());
            }
            AddSectionHeader("Flags");
            AddFlag("Opener Bid",         node.OpenerBid);
            AddFlag("Sign Off",           node.SignOff);
            AddFlag("One-Round Forcing",  node.OneRoundForcing);
            AddFlag("Game Forcing",       node.GameForcing);
            AddFlag("Automatic Response", node.AutomaticResponse);
            AddFlag("Go To Openings",     node.GoToOpenings);
            if (!string.IsNullOrEmpty(node.AiSource)) { AddSectionHeader("AI"); AddRow("Source", node.AiSource!); }
        }
        BuildStackTrace(entry);
    }

    private void BuildStackTrace(AuctionEntry entry) {
        var bidPath  = entry.Node?.Path ?? string.Empty;
        var rawTrace = entry.Bid.StackTrace ?? string.Empty;
        bool hasPath  = !string.IsNullOrWhiteSpace(bidPath);
        bool hasTrace = !string.IsNullOrWhiteSpace(rawTrace);
        if (!hasPath && !hasTrace) { StackTraceDivider.IsVisible = false; StackTraceSection.IsVisible = false; return; }
        StackTraceDivider.IsVisible = true;
        StackTraceSection.IsVisible = true;
        BidPathRow.Children.Clear();
        if (hasPath) {
            var steps = bidPath.Split(" > ", StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < steps.Length; i++) {
                bool isLast   = i == steps.Length - 1;
                var  stepColor = isLast ? Color.FromArgb("#1565C0") : Color.FromArgb("#555555");
                BidPathRow.Children.Add(new Border { StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) }, Stroke = new SolidColorBrush(stepColor), StrokeThickness = isLast ? 2 : 1, BackgroundColor = isLast ? Color.FromArgb("#E3F0FF") : Colors.White, Padding = new Thickness(8, 3), Content = new Label { Text = steps[i].Trim(), TextColor = stepColor, FontSize = 13, FontAttributes = isLast ? FontAttributes.Bold : FontAttributes.None } });
                if (!isLast) BidPathRow.Children.Add(new Label { Text = " > ", FontSize = 13, TextColor = Color.FromArgb("#AAAAAA"), VerticalOptions = LayoutOptions.Center });
            }
        }
        RawStackTraceLabel.Text      = hasTrace ? rawTrace : string.Empty;
        RawStackTraceLabel.IsVisible = hasTrace;
    }

    private void AddSectionHeader(string text) {
        DetailsStack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#E8E8E8"), Margin = new Thickness(0, 8, 0, 4) });
        DetailsStack.Children.Add(new Label { Text = text.ToUpperInvariant(), FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#888888"), CharacterSpacing = 0.8, Margin = new Thickness(0, 0, 0, 4) });
    }

    private void AddRow(string label, string value) {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(140, GridUnitType.Absolute)), new ColumnDefinition(GridLength.Star) }, Margin = new Thickness(0, 1) };
        grid.Children.Add(new Label { Text = label, FontSize = 13, TextColor = Color.FromArgb("#555555"), VerticalOptions = LayoutOptions.Center });
        var val = new Label { Text = value, FontSize = 13, TextColor = Color.FromArgb("#1A1A1A"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);
        DetailsStack.Children.Add(grid);
    }

    private void AddFlag(string name, bool value) {
        DetailsStack.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { new Label { Text = value ? "v" : "x", TextColor = value ? Color.FromArgb("#27AE60") : Color.FromArgb("#BBBBBB"), FontSize = 13, WidthRequest = 18 }, new Label { Text = name, TextColor = value ? Color.FromArgb("#1A1A1A") : Color.FromArgb("#AAAAAA"), FontSize = 13 } } });
    }

    private static Color GetBidColor(AuctionEntry entry) => entry.Bid.Type switch {
        BidType.Pass => Color.FromArgb("#757575"),
        BidType.Double or BidType.Redouble => Color.FromArgb("#C0392B"),
        BidType.Submit => entry.Bid.Color switch {
            BidColor.Hearts or BidColor.Diamonds => Color.FromArgb("#C0392B"),
            BidColor.NoTrump => Color.FromArgb("#1565C0"),
            _ => Color.FromArgb("#1A1A1A")
        },
        _ => Color.FromArgb("#1A1A1A")
    };

    private static bool HasMeaningfulInfo(BidNode node) =>
        !string.IsNullOrEmpty(node.Convention) || !string.IsNullOrEmpty(node.Condition)
        || node.PointsRange != null || node.SpadesCardRange != null || node.HeartsCardRange != null
        || node.DiamondsCardRange != null || node.ClubsCardRange != null
        || node.Aces.HasValue || node.Kings.HasValue
        || node.OpenerBid || node.SignOff || node.OneRoundForcing || node.GameForcing
        || node.AutomaticResponse || node.GoToOpenings;
}
