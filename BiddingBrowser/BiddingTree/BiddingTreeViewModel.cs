using BiddingBrowser.BiddingTree.Bids;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Serialization;

namespace BiddingBrowser.BiddingTree;

public class BiddingTreeViewModel : BindableBase {

    public string SystemName { get; set => SetProperty(ref field, value); }

    public ObservableCollection<Root> Roots { get; set => SetProperty(ref field, value); }

    public object? SelectedItem { get; set => SetProperty(ref field, value); }

    public BiddingTreeViewModel(string systemName) {
        SystemName = systemName;
        Roots = [
            new() { Name = "Otwarcia" },
            new() { Name = "Obrona" },
            new() { Name = "Konwencje" },
            new() { Name = "Reguły" }
        ];
    }


    [JsonConstructor]
    public BiddingTreeViewModel() {

    }

}

