namespace LatincitaAndroid.View;

public partial class MainPage : ContentPage
{
	public MainPage(RadioProgramsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        //ImageContainer.SizeChanged += OnContainerSizeChanged;
    }
    //private void OnContainerSizeChanged(object sender, EventArgs e)
    //{
    //    var width = ImageContainer.Width;
    //    var height = ImageContainer.Height;

    //    if (width > height) {
    //        // Landscape → align top
    //        MainImage.VerticalOptions = LayoutOptions.Start;
    //    } else {
    //        // Portrait → center vertically
    //        MainImage.VerticalOptions = LayoutOptions.Center;
    //    }
    //}
}

