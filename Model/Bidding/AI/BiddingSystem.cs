using Model.Bidding.Bids;
using Newtonsoft.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI;

public class BiddingSystem {

    public string SystemName { get; set; } = "";

    public List<Root> Roots { get; set; } = new();

    [JsonConstructor]
    public BiddingSystem() {
    }

    public BiddingSystem(string filePath) {
        LoadSystem(filePath);
        AssignParent();
    }

    public void LoadSystem(string filePath) {
        using var file = File.OpenText(filePath);
        using var reader = new JsonTextReader(file);

        var loadedSystem = new JsonSerializer().Deserialize<BiddingSystem>(reader)!;

        SystemName = loadedSystem.SystemName;
        Roots = loadedSystem.Roots;
    }


    public void AssignParent() {
        foreach (var root in Roots) {
            root.AssignParnt();
        }
    }


    public IEnumerable<BidNode> GetDescendants(BidNode parent, Bid bid) {
        foreach (var child in parent.NextBids) {
            if (child.Matches(bid)) {
                yield return child;
            }
        }
    }


    public IEnumerable<BidNode> GetOpenings(Bid bid) {
        var bids = Openings().Bids;
        foreach (var child in bids) {
            if (child.Matches(bid)) {
                yield return child;
            }
        }
    }


    public Root? Openings() {
        return Roots.FirstOrDefault(e => e.Name == "Otwarcia");
    }


    public Root? Defences() {
        return Roots.FirstOrDefault(e => e.Name == "Obrona");
    }
}
