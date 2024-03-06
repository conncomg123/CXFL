using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MauiIcons.Material;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CXFLGUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiApp<App>().UseMaterialMauiIcons()
                .ConfigureFonts(fonts =>
                {   
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Continue initializing your .NET MAUI App here
            builder.Logging.AddDebug();
            return builder.Build();
        }
    }
}