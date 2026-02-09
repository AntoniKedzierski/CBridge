using BiddingBrowser.BiddingTree.Bids;
using BiddingBrowser.BiddingTree.Validation;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BiddingBrowser.BiddingTree;
/// <summary>
/// Interaction logic for BiddingTree.xaml
/// </summary>
public partial class BiddingTree : UserControl {

    private BiddingTreeViewModel _viewModel;
    private const string ClipboardFormat = "BIDDING_TREE_BID";

    public BiddingTree() {
        InitializeComponent();
        DataContext = _viewModel = new("NewSystem");
    }


    private void BiddingTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
        _viewModel.SelectedItem = e.NewValue;
    }


    private void NewElementButton_Click(object sender, RoutedEventArgs e) {
        if (DataTree.SelectedItem is IBidsContainer bidsContainer) {
            bidsContainer.AddBid(new Bid() { Identifier = "<empty>" });
        }
    }

    private void DeleteElementButton_Click(object sender, RoutedEventArgs e) {
        if (DataTree.SelectedItem is Bid bid) {
            bid.RemoveSelf();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
        var dlg = new SaveFileDialog {
            FileName = _viewModel.SystemName, // Default file name
            DefaultExt = ".json", // Default file extension
            Filter = "Json files (.json)|*.json" // Filter files by extension
        };

        // Process save file dialog box results
        if (dlg.ShowDialog() == true) {
            var serializedModel = JsonConvert.SerializeObject(_viewModel, Formatting.Indented);
            File.WriteAllText(dlg.FileName, serializedModel);
        }
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e) {
        if (DataContext is not BiddingTreeViewModel vm)
        {
            MessageBox.Show("Brak DataContext.");
            return;
        }

        var validator = new TreeValidator();
        var issues = validator.Validate(vm.Roots);

        if (issues.Count == 0)
        {
            MessageBox.Show("Brak problemów.");
            return;
        }

        var text = string.Join("\n", issues.Select(i => i.ToString()));
        MessageBox.Show(text, "Wynik walidacji");
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) {
        var dlg = new OpenFileDialog {
            DefaultExt = ".json", // Default file extension
            Filter = "Json files (.json)|*.json" // Filter files by extension
        };

        if (dlg.ShowDialog() == true) {
            using (var file = File.OpenText(dlg.FileName)) {
                using (JsonTextReader reader = new JsonTextReader(file)) {
                    DataContext = _viewModel = new JsonSerializer().Deserialize<BiddingTreeViewModel>(reader)!;
                }
            }

            foreach (var root in _viewModel.Roots) {
                foreach (var bid in root.Bids) {
                    bid.Parent = root;
                    bid.AssignParent();
                }
            }
        }
    }

    #region COPY

    private void CopyCanExecute(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = _viewModel?.SelectedItem is Bid;
    }

    private void CopyExecuted(object sender, ExecutedRoutedEventArgs e) {
        if (_viewModel?.SelectedItem is not Bid bid) {
            return;
        }

        var json = JsonConvert.SerializeObject(
            bid,
            Formatting.None,
            new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }
        );

        Clipboard.SetData(ClipboardFormat, json);
    }

    #endregion

    #region PASTE

    private void PasteCanExecute(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = Clipboard.ContainsData(ClipboardFormat) && _viewModel?.SelectedItem is IBidsContainer;
    }

    private void PasteExecuted(object sender, ExecutedRoutedEventArgs e) {
        if (_viewModel?.SelectedItem is not IBidsContainer container) {
            return;
        }

        var json = Clipboard.GetData(ClipboardFormat) as string;
        if (string.IsNullOrWhiteSpace(json)) {
            return;
        }

        var clonedBid = JsonConvert.DeserializeObject<Bid>(json);
        if (clonedBid == null) {
            return;
        }

        // ważne!
        clonedBid.AssignParent();
        container.AddBid(clonedBid);
    }

    #endregion
}
