namespace LatincitaAndroid;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            Debug.WriteLine("UnhandledException: " + e.ExceptionObject?.ToString());
        };
        TaskScheduler.UnobservedTaskException += (s, e) => {
            Debug.WriteLine("UnobservedTaskException: " + e.Exception?.ToString());
        };

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            Debug.WriteLine("Android UnhandledExceptionRaiser: " + e.Exception?.ToString());
        };
#endif

    //  MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(new AppShell());
    }
}
