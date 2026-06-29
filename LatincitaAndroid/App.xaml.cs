namespace LatincitaAndroid;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            Debug.WriteLine("UnhandledException: " + e.ExceptionObject?.ToString());
        };
        //TaskScheduler.UnobservedTaskException += (s, e) => {
        //    Debug.WriteLine("UnobservedTaskException: " + e.Exception?.ToString());
        //};

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
        {
            Debug.WriteLine("Android UnhandledExceptionRaiser: " + e.Exception?.ToString());
        };
#endif

    //  MainPage = new AppShell(); // <<< enabling this produces "this property is deprecated, override CreateWindow instead"
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        // here is where the Android-App starts...

        return new Window(new AppShell());
    }
}
