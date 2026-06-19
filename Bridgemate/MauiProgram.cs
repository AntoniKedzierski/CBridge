using Bridgemate.Services;
using Bridgemate.ViewModels;
using Bridgemate.Views;
using Microsoft.Extensions.Logging;

namespace Bridgemate {
    public static class MauiProgram {
        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<NavigationStore>();
            builder.Services.AddSingleton<SimulationService>();

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<DealPage>();
            builder.Services.AddTransient<BidDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
