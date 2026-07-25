using System.Buffers.Text;
using System.Globalization;
using System.Windows.Input;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Views;
using LatincitaAndroid.Services;

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

    //public ICommand MediaPlayerLoaded { get; }
    //public ICommand MediaOpenedCommand { get; }

    [ObservableProperty]
    private string mediaButton1Text = "Play";

    [ObservableProperty]
    private string mediaButton2Text = "";

    [ObservableProperty]
    private bool playlistHasTimestamps = true;

    public AllLatincitaService AllLatincitaService { get; }
    public RadioProgramsService RadioProgramsService { get; }
    public RandomService RandomService { get; }
    public ProgramListService ProgramListService { get; }
    public AudioPlaybackService AudioPlaybackService { get; }

    [ObservableProperty]
    bool isRefreshing;

    IConnectivity connectivity;

    public event Action<string> Mp3UrlChanged;

    public RadioProgramDetailsViewModel(AllLatincitaService AllLatincitaService, RadioProgramsService RadioProgramsService, RandomService RandomService, ProgramListService ProgramListService, AudioPlaybackService AudioPlaybackService, IConnectivity connectivity)
    {
        //  this.Title = this.radio_program.ArticleTitle;
        this.connectivity = connectivity;
        this.AllLatincitaService = AllLatincitaService;
        this.RadioProgramsService = RadioProgramsService;
        this.RandomService = RandomService;
        this.ProgramListService = ProgramListService;
        this.AudioPlaybackService = AudioPlaybackService;

        //MediaPlayerLoaded = new Command(MediaPlayer_Loaded);
        //MediaOpenedCommand = new Command(MediaPlayer_MediaOpened);

        ProgramListService.PropertyChanged += ProgramListService_PropertyChanged;

        //RadioPrograms = ProgramListService.RadioPrograms;
        //CurrentRadioProgram = ProgramListService.CurrentRadioProgram;
        //CurrentTrack = ProgramListService.CurrentTrack;
    }

    public void Initialize()  //   <<< has to be explicity called from the DetailsPage when it appears ;-(
    {
        PublishCurrentUrl();

        // we are being called when DetailsPage has been loaded

        // this happens after RadioProgramsViewModel has passed the selected RadioProgram
        // to ProgramListService

        RadioProgramType type = this.ProgramListService.CurrentType;
        RadioProgram program = this.ProgramListService.CurrentRadioProgram;
        List<VisibleTrackObject> track_list = this.ProgramListService.CurrentVisibleTrackList;
        TrackObject track = this.ProgramListService.CurrentTrack;
        PlayListItem play_list_item = this.ProgramListService.CurrentPlayListItem;

    }

    public async Task<TrackObject> GoTo_Next_Track()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::GoTo_Next_Track");

        var track = await this.ProgramListService.Goto_NextTrack();
        return track;
    }
    public async Task<TrackObject> GoTo_Prev_Track()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::GoTo_Prev_Track");

        var track = await this.ProgramListService.Goto_PrevTrack();
        return track;
    }


    public void MediaPlayer_Register(MediaElement _mediaPlayer)
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::MediaPlayer_Register");

        this.ProgramListService.isPlaying = false;

        this.AudioPlaybackService.MediaPlayer_Register(_mediaPlayer);
    }
    public void MediaPlayer_Unregister()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::MediaPlayer_Unregister");

        this.ProgramListService.isPlaying = false;

        this.AudioPlaybackService.MediaPlayer_Unregister();
    }

    public async void MediaPlayer_MediaOpened(MediaElement mediaElement) // (object sender, EventArgs args)
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::MediaPlayer_MediaOpened");

        Debug.WriteLine("| The track '" + mediaElement.MetadataTitle + "' has been loaded");
        //  await Shell.Current.DisplayAlert("Latincita Android", "The track '" + mediaElement.MetadataTitle + "' has been loaded", "OK");

        // when MediaPlayer tells us it loaded a MP3, it is actually telling us that
        // AudioPlaybackService read a track from the queue, and pushed it to the MediaPlayer
        // this track had to come from the ProgramListService.CurrentTrackList
        // what we need to do here is identify which track it was that was loaded

        // ProgramListService takes each track from CurrentTrackList
        // and adds them to the Queue.  AudioPlaybackService.CurrentPlayListItem
        // points to the entry on CurrentTrackList that was read from the Queue

        // note:  addToPlaylist takes a TrackObject from the CurrentTrackListItem
        //        and converts it into a PlayListItem which is added to the queue
        //        after converting the TrackObject to a PlayListItem
        //        track.cached_playlist_item = playListItem;

        PlayListItem playlist_item = this.AudioPlaybackService.CurrentPlayListItem;  // for a RADIO, this points to entire show

        if (playlist_item != null) {
            // ok - MediaSource == what Load_Track last loaded
            // we have a PlayListItem ... now we turn this into a TrackObject
            TrackObject track = await this.ProgramListService.PlayListItemToTrackObject(playlist_item, 0);
            //                                                                                         ^--- media opened, find first entry
            if (track != null) {
                // mark this as the current item in CurrentTrackList
                this.ProgramListService.SetTrack(track);
            }
        }

        this.AudioPlaybackService.MediaPlayer_MediaOpened(mediaElement);
    }
    public async Task MediaPlayer_MediaEnded()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::MediaPlayer_MediaEnded");

        // song ended ... tell audio-service to read next item from the queue
        // if queue is empty & we are in RANDOM mode
        // need to fetch a new RANDOM, add it to playlist
        // and push it onto the Queue

        bool queue_is_empty = this.AudioPlaybackService.Queue_Is_Empty();

        this.AudioPlaybackService.MediaPlayer_MediaEnded();

        if (queue_is_empty) {
            RadioProgramType type = this.ProgramListService.CurrentType;
            if (type == RadioProgramType.RANDOM) {
                await GetRandomAsync();
            }
        }
    }
    public async void MediaPlayer_TrackEnded()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::MediaPlayer_TrackEnded");

    //  play head reached end of current track, advance pointer to next track

        this.AudioPlaybackService.MediaPlayer_TrackEnded();  // will advance to next track in the show

        if (this.AudioPlaybackService.CurrentTrackListIndex < 0 ||
            this.AudioPlaybackService.CurrentTrackListItem == null) {

            // no next track ... end of show

            this.AudioPlaybackService.MediaPlayer_MediaEnded();

        } else {
            TrackListItem track_item = this.AudioPlaybackService.CurrentTrackListItem;

            TrackObject track = await this.ProgramListService.TrackListItem_to_TrackObject(track_item);

            if (track != null) {
                // mark this as the current item in CurrentTrackList
                this.ProgramListService.SetTrack(track);
            }
        }
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
        if (e.PropertyName == nameof(ProgramListService.Mp3Url)) {

            Debug.WriteLine($"| RadioProgramDetailsViewModel::ProgramListService_PropertyChanged(Mp3Url)");

            PublishCurrentUrl();

            return;
        }

        if (e.PropertyName != nameof(ProgramListService.CurrentTrack)) {
            return;
        }
        Debug.WriteLine($"| RadioProgramDetailsViewModel::ProgramListService_PropertyChanged(CurrentTrack)");

        RadioProgram radioProgram = ProgramListService.CurrentRadioProgram;
        TrackObject trackObject = ProgramListService.CurrentTrack;
        PlayListItem playlistitem = ProgramListService.CurrentPlayListItem;

        if (ProgramListService.CurrentType == RadioProgramType.RADIO ||
            ProgramListService.CurrentType == RadioProgramType.CD) {
            PlaylistHasTimestamps = true;
        } else {
            PlaylistHasTimestamps = false;
        }

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

    void PublishCurrentUrl()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::PublishCurrentUrl");

        if (ProgramListService == null) return;

        Mp3UrlChanged?.Invoke(ProgramListService.Mp3Url);  // note Mp3Url may be empty ... hopefully this will clear media on MediaElement
    }

    [RelayCommand]
    async Task GetRandomAsync()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::GetRandomAsync");

        if (IsBusy)
            return;

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet) {
                await Shell.Current.DisplayAlert("No connectivity!",
                    $"Please check internet and try again.", "OK");
                return;
            }

            ///////////////////////////// CODE IS DUPLICATED IN RadioProgramsViewModel

            bool have_radio = false;
            have_radio = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.RADIO);

            if (have_radio) {
                ProgramListService.ClearList();
            }

            IsBusy = true;
            var _Random = await RandomService.GetRandom();

            if (_Random == null) {
                return;
            }

            ProgramListService.AddRandom(_Random);  // append Random to main-list

        } catch (Exception ex) {
            Debug.WriteLine($"| Unable to get Random Track: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        } finally {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    async Task GoBack()
    {
        Debug.WriteLine($"| RadioProgramDetailsViewModel::GoBack");

        //await Shell.Current.GoToAsync(nameof(MainPage), true, new Dictionary<string, object>
        //{
        //    //  ["RadioProgram"] = RadioProgram //,
        //    //  ["TrackObject"] = _track
        //});
        await Shell.Current.GoToAsync("..");
    }

}
