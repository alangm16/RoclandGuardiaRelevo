using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using RoclandGuardiaRelevo.Mobile.Services;
using RoclandGuardiaRelevo.Mobile.ViewModels;
using RoclandGuardiaRelevo.Mobile.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace RoclandGuardiaRelevo.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddSingleton<AuthStateService>();

            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<RondinViewModel>();
            builder.Services.AddTransient<DetalleRondinViewModel>();
            //builder.Services.AddTransient<IncidenciasViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<RondinPage>();
            builder.Services.AddTransient<DetalleRondinPage>();
            //builder.Services.AddTransient<IncidenciasPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
