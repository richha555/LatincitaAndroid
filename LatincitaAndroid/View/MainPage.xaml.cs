namespace LatincitaAndroid.View;

public partial class MainPage : ContentPage
{
	public MainPage(RadioProgramsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

