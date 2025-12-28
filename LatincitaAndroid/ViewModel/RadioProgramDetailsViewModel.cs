using LatincitaAndroid.Services;
using System.Buffers.Text;
using System.Globalization;

namespace LatincitaAndroid.ViewModel;

// copy to this variable vvvvvv  parm to copy vvvv
//[QueryProperty(nameof(RadioProgram), "RadioProgram")]
//[QueryProperty(nameof(TrackObject),  "TrackObject")]
public partial class RadioProgramDetailsViewModel : BaseViewModel
{
    public ObservableCollection<RadioProgram> RadioPrograms { get; } = new();

    //[ObservableProperty]
    //private RadioProgram currentRadioProgram;

    //[ObservableProperty]
    //private TrackObject currentTrack;

    public AllLatincitaService AllLatincitaService { get; }
    public RadioProgramsService RadioProgramsService { get; }
    public RandomService RandomService { get; }
    public ProgramListService ProgramListService { get; }
    IConnectivity connectivity;

    public RadioProgramDetailsViewModel(AllLatincitaService AllLatincitaService, RadioProgramsService RadioProgramsService, RandomService RandomService, ProgramListService ProgramListService, IConnectivity connectivity)
    {
        //  this.Title = this.radio_program.ArticleTitle;
        this.connectivity = connectivity;
        this.AllLatincitaService = AllLatincitaService;
        this.RadioProgramsService = RadioProgramsService;
        this.RandomService = RandomService;
        this.ProgramListService = ProgramListService;

        ProgramListService.PropertyChanged += ProgramListService_PropertyChanged;

        //RadioPrograms = ProgramListService.RadioPrograms;
        //CurrentRadioProgram = ProgramListService.CurrentRadioProgram;
        //CurrentTrack = ProgramListService.CurrentTrack;
    }

    //[ObservableProperty]
    //private RadioProgram radioProgram;  // << backing-field

    //private RadioProgram _radioProgram;  // << backing-field
    //public RadioProgram RadioProgram
    //{
    //    get => _radioProgram;
    //    set
    //    {
    //        _radioProgram = value;
    //        OnPropertyChanged();
    //    }
    //}

    //[ObservableProperty]
    //private TrackObject trackObject;    // << backing-field
    //private TrackObject _trackObject;  // << backing-field
    //public TrackObject TrackObject
    //{
    //    get => _trackObject;
    //    set
    //    {
    //        _trackObject = value;
    //        OnPropertyChanged();
    //    }
    //}

    //[ObservableProperty]
    //private string? mp3;

    //[ObservableProperty]
    //private string? detail_line_1;

    //[ObservableProperty]
    //private string? detail_line_2;

    //[ObservableProperty]
    //private string? detail_line_3;

    // optional: react to the generated property change to set the page title
    //partial void OnRadioProgramChanged(RadioProgram value)
    private void ProgramListService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProgramListService.CurrentTrack)) {
            return;
        }
        RadioProgram radioProgram = ProgramListService.CurrentRadioProgram;
        TrackObject trackObject = ProgramListService.CurrentTrack;

        if (radioProgram is null) {
            Title = "Latincita Radio Programs";
            //Mp3 = "https://www.latincita.com" + "/best_of_latincita/Latincita%20Opener.mp3";

            //Detail_line_1 = "LATINCITA";
            //Detail_line_2 = "";
            //Detail_line_3 = "";

            //if (trackObject.article_title == "Best of Latincita")
            //    return;  // no infinite loop

            //trackObject = new();
            //trackObject.article_title = "Best of Latincita";
            //trackObject.artist = "Latincita";
            //trackObject.poster_url = "";

            //ProgramListService.SetTrack(trackObject);

            return;
        }
        Title = radioProgram?.ArticleTitle ?? this.Title;
        //Mp3 = radioProgram?.MP3URL;
        //var p = Mp3.IndexOf("#");
        //if (p > 0)
        //    Mp3 = Mp3.Substring(0, p);
        //DateTime min_date = new DateTime(2000, 1, 1);
        //if ((trackObject == null) || (trackObject.SongID <= 0)) {
        //    // assume this is an entire radio-program (tracks exclude entire shows ... no track probably means it's a show)
        //    Detail_line_1 = radioProgram.RecordedOn <= min_date ? "Entire Radio Program" : string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
        //    Detail_line_2 = "";
        //    Detail_line_3 = "";
        //} else {
        //    // assume this is a single track or song
        //    if (radioProgram.RecordedOn <= min_date) {
        //        Detail_line_1 = "" + trackObject.song_title;
        //        Detail_line_2 = "/ " + trackObject.artist;
        //        Detail_line_3 = "";
        //    } else {
        //        Detail_line_1 = "" + trackObject.song_title;
        //        Detail_line_2 = "/ " + trackObject.artist;
        //        Detail_line_3 = string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
        //    }
        //}
    }

    [ObservableProperty]
    bool isRefreshing;

    [RelayCommand]
    async Task GetRandomAsync()
    {
        if (IsBusy)
            return;

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet) {
                await Shell.Current.DisplayAlert("No connectivity!",
                    $"Please check internet and try again.", "OK");
                return;
            }

///////////////////////////// CODE IS DUPLICATED IN RadioProgramsViewModel

            IsBusy = true;
            var _Random = await RandomService.GetRandom();

            if (_Random == null) {
                return;
            }

            RadioProgram radioProgram = new();

            radioProgram.ID = _Random.id;
            radioProgram.ArticleTitle = _Random.title;
            radioProgram.MP3URL = _Random.m4v;
            radioProgram.PictureURL = _Random.image.ImageFullURL;
            //  radioProgram.RecordedOn .... Random does not provide date !

            TrackObject _track = await AllLatincitaService.get_track(radioProgram);

            DateTime recorded_on = DateTime.MinValue;

            if ((_track != null) && !string.IsNullOrWhiteSpace(_track.recorded_on)) {
                recorded_on = DateTime.ParseExact(
                                _track.recorded_on,
                                "MMMM yyyy",
                                CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None);
            }

            radioProgram.RecordedOn = recorded_on;

            radioProgram.Type = RadioProgramType.TRACK;

            // The following lines expect a collection, but GetRandom returns a single PlayListItem.
            // Adjust as needed. For now, clear and add the single item.
            if (_Random != null)
                //  this.RadioPrograms.Add(radioProgram);
                ProgramListService.AddProgram(radioProgram);
            else ProgramListService.SetProgram(null);

            //ProgramListService.SetTrack(_track);

        } catch (Exception ex) {
            Debug.WriteLine($"Unable to get Random Track: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        } finally {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

}
