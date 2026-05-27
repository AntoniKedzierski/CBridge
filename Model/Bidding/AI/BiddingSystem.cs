using Model.Bidding.Bids;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.AI; 
public class BiddingSystem {
    public string SystemName { get; set; } = "";

    public ObservableCollection<Root> Roots { get; set; } = new();

    [JsonConstructor]
    public BiddingSystem() {
    }
    public BiddingSystem(string filePath) {
        LoadSystem(filePath);
    }

    public void LoadSystem(string filePath) {
        using var file = File.OpenText(filePath);
        using var reader = new JsonTextReader(file);

        var loadedSystem = new JsonSerializer().Deserialize<BiddingSystem>(reader)!;

        SystemName = loadedSystem.SystemName;
        Roots = loadedSystem.Roots;
    }


}
