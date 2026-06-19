using Bridgemate.Services;
using Bridgemate.ViewModels;

namespace Bridgemate {
    public partial class MainPage : ContentPage {

        public MainPage() {
            InitializeComponent();
            BindingContext = ServiceHelper.GetService<MainViewModel>();
        }
    }
}
