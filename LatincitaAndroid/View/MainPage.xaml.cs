namespace LatincitaAndroid.View;

public partial class MainPage : ContentPage
{
	public MainPage(RadioProgramsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
        //ImageContainer.SizeChanged += OnContainerSizeChanged;
    }

    void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject bo)
        {
            var item = bo.BindingContext;

            if (BindingContext is LatincitaAndroid.ViewModel.RadioProgramsViewModel vm)
            {
                var cmd = vm.GoToDetailsCommand;
                if (cmd != null && cmd.CanExecute(item))
                {
                    cmd.Execute(item);
                }
            }
        }
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

