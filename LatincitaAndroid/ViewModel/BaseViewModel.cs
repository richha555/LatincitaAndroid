using LatincitaAndroid.Services;

namespace LatincitaAndroid.ViewModel;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    bool isBusy;

    [ObservableProperty]
    string title;

    //public string Title
    //{
    //    get => title;
    //    set 
    //    {
    //        if (title == value)
    //            return;
    //        title = value;
    //        OnPropertyChanged(nameof(Title));
    //    }
    //}

    public bool IsNotBusy => !IsBusy;
}
