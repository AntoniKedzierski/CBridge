using Bridgemate.Views;

namespace Bridgemate {
    public partial class AppShell : Shell {
        public AppShell() {
            InitializeComponent();
            Routing.RegisterRoute("dealpage", typeof(DealPage));
            Routing.RegisterRoute("biddetail", typeof(BidDetailPage));
        }
    }
}
