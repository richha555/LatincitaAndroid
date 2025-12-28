using CommunityToolkit.Maui;
using LatincitaAndroid.Services;
using LatincitaAndroid.View;

namespace LatincitaAndroid;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
		    .UseMauiApp<App>()
                    .UseMauiCommunityToolkitMediaElement()
                    .ConfigureFonts(fonts =>
		    {
			    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
		    });
#if DEBUG
//		builder.Logging.AddDebug();
#endif
    	        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

	//	builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
	//	builder.Services.AddSingleton<IMap>(Map.Default);

                builder.Services.AddSingleton<MainPage>();
                builder.Services.AddTransient<DetailsPage>();

                builder.Services.AddSingleton<AllLatincitaService>();
                builder.Services.AddSingleton<RadioProgramsService>();
                builder.Services.AddSingleton<RandomService>();
                builder.Services.AddSingleton<ProgramListService>();

                builder.Services.AddSingleton<RadioProgramsViewModel>();
                builder.Services.AddTransient<RadioProgramDetailsViewModel>();

                return builder.Build();
	}
}
