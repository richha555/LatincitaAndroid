namespace LatincitaAndroid;

public partial class DetailsPage : ContentPage
{
	public DetailsPage(RadioProgramDetailsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}