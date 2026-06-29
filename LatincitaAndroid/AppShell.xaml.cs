namespace LatincitaAndroid;

public partial class AppShell : Shell
{
    public AppShell()
    {
	    InitializeComponent();  // only creates MainPage.xaml

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(DetailsPage), typeof(DetailsPage));
    }
}