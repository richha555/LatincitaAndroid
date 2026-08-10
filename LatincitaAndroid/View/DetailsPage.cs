using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web;

#if ANDROID
using AndroidX.Lifecycle;
#endif
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
//using Java.Net;

//using Java.Net;
//using CommunityToolkit.Maui.Sample.Constants;
//using CommunityToolkit.Maui.Sample.ViewModels.Views;
using LatincitaAndroid.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;

namespace LatincitaAndroid;

public partial class DetailsPage : ContentPage
{
    private RadioProgramDetailsViewModel? viewModel = null;

    readonly ILogger logger;
    readonly IDeviceInfo deviceInfo;

    private bool page_ready = false;
    private bool media_player_ready = false;

    private MediaSource current_media_source = null;

    private MediaSource auto_play_source;

    public DetailsPage(IDeviceInfo deviceInfo, ILogger<DetailsPage> logger)
    {
        Debug.WriteLine("| DetailsPage Initializer called");

        InitializeComponent();

        // don't rely on BindingContext being set here (may be set later by Shell/DI)

        this.logger = logger;
        this.deviceInfo = deviceInfo;

        this.mediaPlayer.PropertyChanged += MediaElement_PropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is RadioProgramDetailsViewModel vm) {

            Debug.WriteLine("| DetailsPage: PAGE LOADED");

            viewModel = vm;

            if (media_player_ready)
                viewModel.MediaPlayer_Register(this.mediaPlayer);  // we hope MediaPlayer is ready to start playing

            //if (!page_ready) {
            //    Debug.WriteLine("| sleeping for 500 secs");
            //    await Task.Delay(500);
            //    page_ready = true;
            //}

            //      vm.Mp3UrlChanged += OnMp3UrlChanged;

            //vm.Initialize(); // <<< stupid ... have to call Initialize in RadioProgramDetailsViewModel
            //                 //     to get it to invoke the Mp3UrlChanged event for mp3url in ProgramListService
            //                 //
            //                 //     the viewmodel listens for this event and then calls our Load_URL
            //                 //
            //                 //     all this because the Mp3UrlChanged event is getting raised
            //                 //     before the viewmodel has been initialized
        }
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is RadioProgramDetailsViewModel vm) {
            //vm.Mp3UrlChanged -= OnMp3UrlChanged;
        }
        if (viewModel != null) {
            viewModel.MediaPlayer_Unregister();
        }
        base.OnDisappearing();

        media_player_ready = false;  // *** we hope next time we appear MediaPlayer_Loaded will get called!
    }


    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"| DetailsPage::OnBindingContextChanged BindingContext={BindingContext?.GetType().FullName ?? "null"}");

        Reset_Play_Buttons();

        if (BindingContext is RadioProgramDetailsViewModel vm) {
            viewModel = vm;

            if (media_player_ready)
                viewModel.MediaPlayer_Register(this.mediaPlayer);  // we hope MediaPlayer is ready to start playing

            //vm.Mp3UrlChanged += OnMp3UrlChanged;

        } else {
            viewModel?.MediaPlayer_Unregister();
            viewModel = null;
        }
    }

    //private void OnMp3UrlChanged(string url)
    //{
    //    bool auto_play = ((viewModel != null) && (viewModel.ProgramListService != null)) ? viewModel.ProgramListService.Auto_play : false;

    //    Load_URL(url, auto_play);
    //}
    private void MediaPlayer_Loaded(object sender, EventArgs e)
    {
        Debug.WriteLine("| DetailsPage: MEDIA-PLAYER: LOADED");

        if (viewModel != null) {
            viewModel.MediaPlayer_Register(this.mediaPlayer);
        }
        media_player_ready = true;
    }

    private async void MediaPlayer_MediaOpened(object sender, EventArgs args)
    {
        Debug.WriteLine("| DetailsPage: MEDIA-PLAYER: MEDIA OPENED");

        if (viewModel != null) {
            viewModel.MediaPlayer_MediaOpened(this.mediaPlayer); // seek needs to run on "main" thread
        } 
    }

    void MediaElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(this.mediaPlayer.DurationProperty))
        {
            double max_secs = (int)this.mediaPlayer.Duration.TotalSeconds;
            Debug.WriteLine($"| Duration: {this.mediaPlayer.Duration}");

            if ((viewModel != null) && (viewModel.ProgramListService != null)) {
                int beg_offset = viewModel.ProgramListService.StartPosition;
                int nxt_offset = viewModel.ProgramListService.EndPosition;

                if ((double)beg_offset > max_secs) {
                    // invalid start_position or media is corrupted
                    viewModel.ProgramListService.StartPosition = -1;
                    viewModel.ProgramListService.EndPosition = -1;
                } else if ((double)nxt_offset > max_secs) {
                    // invalid end_position or media is corrupted
                    viewModel.ProgramListService.EndPosition = (int)max_secs;
                    if (beg_offset >= 0)
                        max_secs = max_secs - (double)beg_offset;
                } else {
                    if ((beg_offset >= 0) && (nxt_offset >= 0) && (nxt_offset >= beg_offset))
                        max_secs = (double)(nxt_offset - beg_offset);
                }
            }
            PositionSlider.Maximum = max_secs;
        }
    }

    void OnMediaOpened(object? sender, EventArgs? e)
    {
        Debug.WriteLine("| DetailsPage: Media opened.");
        Reset_Play_Buttons();
    }

    void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {   Debug.WriteLine($"| DetailsPage: Media State Changed. Old State: {e.PreviousState}, New State: {e.NewState}");
        if (viewModel != null) {
            bool is_playing = false;
            switch(e.NewState) {
                case MediaElementState.Playing:
                case MediaElementState.Paused:
                    is_playing = true;
                    break;
                default:
                    is_playing = false;
                    break;
            }
            viewModel.ProgramListService.isPlaying = is_playing;
        }
        Manage_Play_Buttons();
    }

    void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {   Debug.WriteLine($"| DetailsPage: Media failed. Error: {e.ErrorMessage}");
        Reset_Play_Buttons();
    //  viewModel.AudioPlaybackService.ResumeAfterSongEnds();
    }

    void OnMediaEnded(object? sender, EventArgs? e)
    {
        Debug.WriteLine("| DetailsPage: Media ended.");
        Reset_Play_Buttons();

        // tell AudioPlaybackService to read next track from Queue and play it
        //
        // if there is no track waiting in the queue, and we are in RANDOM mode,
        // we need to fetch a new RANDOM, add it to the CurrentTrackList
        // and push it on to the QUEUE

        viewModel.MediaPlayer_MediaEnded();
    }

    async void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        MediaElement mediaElement = (MediaElement)sender;
        int curr_offset = (int)e.Position.TotalSeconds;

        // for radio's we need to use offset to find item in CurrentTrackList
        // for other types, we just use URL to find item
        if (viewModel.ProgramListService.CurrentType == RadioProgramType.RADIO ||
            ! mediaElement.Source.Equals(this.current_media_source)) {

            TrackObject _track = await viewModel.ProgramListService.MediaToPlaylist(mediaElement.Source, curr_offset);

            if (_track != null) {

                if (String.IsNullOrEmpty(viewModel.ProgramListService.CurrentTrack.id) || 
                    (_track.id != viewModel.ProgramListService.CurrentTrack.id)) {

                    viewModel.ProgramListService.SetTrack(_track);

                    Debug.WriteLine($"| Selected {_track.article_title} - begin {_track.offset}  end {_track.nxtoffset}");
                }

                //viewModel.ProgramListService.CurrentTrack = _track;
                //viewModel.ProgramListService.StartPosition = _track.offset;
                //viewModel.ProgramListService.EndPosition = _track.nxtoffset;
            }
            this.current_media_source = mediaElement.Source;
        }

        if ((viewModel != null) && (viewModel.ProgramListService != null)) {
            int tbeg_offset = viewModel.ProgramListService.StartPosition;
            int tnxt_offset = viewModel.ProgramListService.EndPosition;
            int tcurr_offset = curr_offset;
            if ((tnxt_offset > 0) && (tcurr_offset >= tnxt_offset)) {
                if ((tbeg_offset >= 0) && (tcurr_offset >= tbeg_offset) && (tnxt_offset >= tbeg_offset)) {
                    Debug.WriteLine($"| Position {tcurr_offset} reached EndPosition {tnxt_offset}, stopping playback.");
                    // *** here is where we need to handle auto-play next track INSTEAD of just stopping

                    if (viewModel.ProgramListService.CurrentType == RadioProgramType.RADIO) {
                        // if track is an item from a RADIO
                        // all we do is change the CurrentTrack so the next item is highlighted

                        // actually we do nothing, because MP3 will continue to play and then code
                        // up above will see that offset belongs to a new track and will
                        // call SetTrack which highlights the new row in CurrentTrackList

                        // however ... if this is the last track on the RADIO
                        //             then we need to stop playing

                        TrackObject curr_track = viewModel.ProgramListService.CurrentTrack;

                        Boolean is_last = await viewModel.ProgramListService.IsLastTrack(curr_track);

                        if (is_last) {

                            OnStopClicked(this.mediaPlayer, null);

                            Reset_Play_Buttons();

                            await viewModel.MediaPlayer_MediaEnded();

                        } else {

                            viewModel.MediaPlayer_TrackEnded();
                        }

                    } else {
                        // For all other types we pretend OnMediaEnded event was thrown

                        OnStopClicked(this.mediaPlayer, null);

                        Reset_Play_Buttons();

                        await viewModel.MediaPlayer_MediaEnded();
                    }
                    return;
                }
            }
        }

        double max_secs = (int)this.mediaPlayer.Duration.TotalSeconds;

        if (max_secs < 2.0) {
            return;  // don't trust it
        }
        int beg_offset = 0;
        int nxt_offset = (int)max_secs;
        if ((viewModel != null) && (viewModel.ProgramListService != null)) {
            beg_offset = viewModel.ProgramListService.StartPosition;
            nxt_offset = viewModel.ProgramListService.EndPosition;
            if ((double)beg_offset > max_secs) {
                // invalid start_position or media is corrupted
                beg_offset = viewModel.ProgramListService.StartPosition = -1;
                nxt_offset = viewModel.ProgramListService.EndPosition = -1;
            } else if ((double)nxt_offset > max_secs) {
                // invalid end_position or media is corrupted
                nxt_offset = viewModel.ProgramListService.EndPosition = (int)max_secs;
                if (beg_offset >= 0)
                    max_secs = max_secs - (double)beg_offset;
            } else {
                if ((beg_offset >= 0) && (nxt_offset >= 0) && (nxt_offset >= beg_offset))
                    max_secs = (double)(nxt_offset - beg_offset);
            }
        }
        double pos_secs = curr_offset;
        if ((beg_offset >= 0) && (nxt_offset >= 0) && (nxt_offset >= beg_offset)) {
            // slider runs from beg_offset to nxt_offset
            if (pos_secs < (double)beg_offset) {
                pos_secs = 0;
            } else if (pos_secs > (double)nxt_offset) { 
                pos_secs = (double)(nxt_offset - beg_offset);
            } else {
                pos_secs = pos_secs - (double)beg_offset;
            }
        } else if (beg_offset >= 0) {
            // slider runs from beg_offset to max_secs
            pos_secs = pos_secs - (double)beg_offset;
        } else {
            // slider runs from 0 to max_secs
        }
        if (pos_secs < 0)
            pos_secs = 0;
        if (pos_secs > max_secs)
            pos_secs = max_secs;
        // slider runs from beg_offset to nxt_offset
    //  Debug.WriteLine($"| DetailsPage: Position changed to {curr_offset}");
        PositionSlider.Maximum = max_secs;
        PositionSlider.Value = pos_secs;
    }

    void OnSeekCompleted(object? sender, EventArgs? e) {
        Debug.WriteLine("| DetailsPage: Seek completed.");
        if (auto_play_source != null) {
            if (auto_play_source.ToString() == this.mediaPlayer.Source.ToString()) {
                auto_play_source = null;
                OnPlayClicked(this.mediaPlayer, null);
            }
        }
    }

    void OnSpeedMinusClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer.Speed >= 1)
        {
            this.mediaPlayer.Speed -= 1;
        }
    }

    void OnSpeedPlusClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer.Speed < 10)
        {
            this.mediaPlayer.Speed += 1;
        }
    }

    void OnVolumeMinusClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer.Volume >= 0)
        {
            if (this.mediaPlayer.Volume < .1)
            {
                this.mediaPlayer.Volume = 0;

                return;
            }

            this.mediaPlayer.Volume -= .1;
        }
    }

    void OnVolumePlusClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer.Volume < 1)
        {
            if (this.mediaPlayer.Volume > .9)
            {
                this.mediaPlayer.Volume = 1;

                return;
            }

            this.mediaPlayer.Volume += .1;
        }
    }

        //private async void Load_URL(string url, bool auto_play)
        //{
        //    Debug.WriteLine($"| >>> DetailsPage:Load_URL({url},{auto_play})");

        //    if (String.IsNullOrWhiteSpace(url)) {
        //        return;
        //    }

        //    if (!page_ready) { // && media_player_ready <<< media-player not calling MediaPlayer_Loaded on Android
        //        Debug.WriteLine($"| >>> page/player not ready - regretfully ignoring laod command");
        //        return;
        //    }
        //    url = HttpUtility.UrlDecode(url);  // ChatGPT says undo url encoding

        //    bool url_unchanged = IsSameMediaUrlIgnoringDomain(this.mediaPlayer.Source, url);

        //    Debug.WriteLine($"| >>> => {url}");

        //    if (!url_unchanged) {
        //        Reset_Play_Buttons();  // would have to first call w/ empty URL to get us to reset play buttons
        //    }

        //    auto_play_source = null;
        //    try {
        //        MainThread.BeginInvokeOnMainThread(async () => {
        //            try {
        //            //    UriMediaSource _src = new UriMediaSource {
        //            //                                 Uri = new Uri(url)
        //            //                              };
        //                if (auto_play) {
        //                //  OnPlayClicked(this.mediaPlayer,null);
        //                    auto_play_source = MediaSource.FromUri(url);
        //                }
        //                if (!url_unchanged) {
        //                    this.mediaPlayer.Stop();
        //                    this.mediaPlayer.Source = null;
        //                //  this.mediaPlayer.Source = _src;
        //                    this.mediaPlayer.Source = MediaSource.FromUri(url); 
        //                    Debug.WriteLine($"| >>> DetailsPage:Load_URL - Loading {url} succeeded");
        //                } else {
        //                    Debug.WriteLine($"| >>> DetailsPage:Load_URL - {url} already loaded");
        //                }
        //            } catch (COMException comEx) {
        //                Debug.WriteLine($"| >>> DetailsPage:Load_URL - COMException loading media: {comEx.HResult:X} {comEx.Message}");
        //                await Shell.Current.DisplayAlert("Media Load error", "Unable to load {url} (platform error).", "OK");
        //            } catch (Exception exInner) {
        //                Debug.WriteLine($"| >>> DetailsPage:Load_URL - Exception loading media: {exInner.GetType().FullName}: {exInner.Message}");
        //                await Shell.Current.DisplayAlert("Media Load error", exInner.Message, "OK");
        //            }
        //        });
        //    } catch (Exception ex) {
        //        // Last-resort catch. Note: corrupted-state exceptions may still escape.
        //        Debug.WriteLine($"| >>> DetailsPage:Load_URL - Media-Load top-level exception: {ex.GetType().FullName}: {ex.Message}");
        //        try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        //    }
        //}

    private bool IsSameMediaUrlIgnoringDomain(MediaSource source, string newUrl)
    {
        if (source is not UriMediaSource uriSource ||
            uriSource.Uri == null) {
            return false;
        }

        Uri currentUri = uriSource.Uri;
        Uri compareUri = new Uri(newUrl);

        return string.Equals(
            currentUri.LocalPath,
            compareUri.LocalPath,
            StringComparison.OrdinalIgnoreCase);
    }

    private void OnButton1Clicked(object? sender, EventArgs? e)
    { 
        if (this.mediaPlayer == null) {
            return;
        }
        var curr_state = this.mediaPlayer.CurrentState;
        switch (curr_state) {
            case MediaElementState.Playing:
                OnPauseClicked(sender, e);
                break;
            default:
                OnPlayClicked(sender, e);
                break;
        }
    //  Manage_Play_Buttons();
    }

    private void OnButton2Clicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null) {
            return;
        }
        var curr_state = this.mediaPlayer.CurrentState;
        switch (curr_state) {
            case MediaElementState.Playing:
            case MediaElementState.Paused:
                OnStopClicked(sender, e);
                break;
            default:
                // do nothing
                break;
        }
    }
    private async void OnNextTrackClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null) {
            return;
        }
        if (viewModel.ProgramListService.CurrentType == RadioProgramType.RADIO) {
            await viewModel.GoTo_Next_Track();
            return;
        }

        var curr_state = this.mediaPlayer.CurrentState;
        switch (curr_state) {
            case MediaElementState.Playing:
            case MediaElementState.Paused:
                    //  playing radio tracks:  don't stop playing, just move play-head
                OnStopClicked(sender, e);
                break;
            default:
                // do nothing
                break;
        }
        // act as if media ended
        await viewModel.MediaPlayer_MediaEnded();
    }

    private async void OnPrevTrackClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null) {
            return;
        }
        if (viewModel.ProgramListService.CurrentType == RadioProgramType.RADIO) {
            await viewModel.GoTo_Prev_Track();
            return;
        }
        var curr_state = this.mediaPlayer.CurrentState;
        switch (curr_state) {
            case MediaElementState.Playing:
            case MediaElementState.Paused:
                if (viewModel.ProgramListService.CurrentType == RadioProgramType.RADIO) {
                    //  playing radio tracks:  don't stop playing, just move play-head
                } else {
                    OnStopClicked(sender, e);
                }
                break;
            default:
                // do nothing
                break;
        }
        // *** now what ??
    }


    private async void OnPlayClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null) {
            return;
        }
        if (this.mediaPlayer.Source == null) {
            //if (viewModel != null && viewModel.ProgramListService != null && !string.IsNullOrWhiteSpace(this.viewModel.ProgramListService.Mp3Url))
            //    Load_URL(this.viewModel.ProgramListService.Mp3Url, true);

            // *** not sure what to do if nothing is loaded

            return;
        }
        try
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    this.mediaPlayer.Play();
                    Debug.WriteLine($"| Play succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"| COMException during Play: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Play the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"| Exception during Play: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"| PLAY top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }
    }

    private async void OnPauseClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null || this.mediaPlayer.Source == null) {
            return;
        }
        try {
            await MainThread.InvokeOnMainThreadAsync(async () => {
                try
                {
                    this.mediaPlayer.Pause();
                    Debug.WriteLine($"| Pause succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"| COMException during Pause: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Pause the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"| Exception during Pause: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"| PAUSE top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }
    }

    private async void OnStopClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null || this.mediaPlayer.Source == null) {
            return;
        }
        try {
            await MainThread.InvokeOnMainThreadAsync(async () => {
                try
                {
                    this.mediaPlayer.Stop();
                    Debug.WriteLine($"| Stop succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"| COMException during Stop: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Stop the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"| Exception during Stop: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"| STOP top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }
    }

    void OnMuteClicked(object? sender, EventArgs? e)
    {
        this.mediaPlayer.ShouldMute = !this.mediaPlayer.ShouldMute;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        this.mediaPlayer.Stop();
        this.mediaPlayer.Handler?.DisconnectHandler();
    }

    async void Slider_DragCompleted(object? sender, EventArgs? e)
    {
        ArgumentNullException.ThrowIfNull(sender);

        var newValue = ((Slider)sender).Value;

        double max_secs = (int)this.mediaPlayer.Duration.TotalSeconds;

        int beg_offset = 0;
        int nxt_offset = (int)max_secs;
        if ((viewModel != null) && (viewModel.ProgramListService != null)) {
            beg_offset = viewModel.ProgramListService.StartPosition;
            nxt_offset = viewModel.ProgramListService.EndPosition;
            if ((double)beg_offset > max_secs) {
                // invalid start_position or media is corrupted
                beg_offset = viewModel.ProgramListService.StartPosition = -1;
                nxt_offset = viewModel.ProgramListService.EndPosition = -1;
            } else if ((double)nxt_offset > max_secs) {
                // invalid end_position or media is corrupted
                nxt_offset = viewModel.ProgramListService.EndPosition = (int)max_secs;
                if (beg_offset >= 0)
                    max_secs = max_secs - (double)beg_offset;
            } else {
                if ((beg_offset >= 0) && (nxt_offset >= 0) && (nxt_offset >= beg_offset))
                    max_secs = (double)(nxt_offset - beg_offset);
            }
        }
        if ((beg_offset >= 0) && (nxt_offset >= 0) && (nxt_offset >= beg_offset)) {
            // slider runs from beg_offset to nxt_offset
            newValue = beg_offset + newValue * (nxt_offset - beg_offset);
        } else if (beg_offset >= 0) {
            // slider runs from beg_offset to max_secs
            newValue = beg_offset + newValue * (max_secs - beg_offset);
        } else {
            // slider runs from 0 to max_secs
        }
        await this.mediaPlayer.SeekTo(TimeSpan.FromSeconds(newValue), CancellationToken.None);

        this.mediaPlayer.Play();
    }

    void Slider_DragStarted(object? sender, EventArgs? e)
    {
        this.mediaPlayer.Pause();
    }

    private void Manage_Play_Buttons ()
    {
        var curr_state = this.mediaPlayer.CurrentState;

        string button1 = "Play";
        string button2 = "";

        switch (curr_state) {
            case MediaElementState.Playing:
                button1 = "Pause";
                button2 = "Stop";
                break;
            case MediaElementState.Paused:
                //---------------------- when media is only cued up, reports state "Paused"
                if (this.mediaPlayer.Source != null) {
                    var position = this.mediaPlayer.Position;
                    if (position.TotalSeconds > 1) {
                        button1 = "Resume";
                        button2 = "Stop";
                    }
                }
                break;
        }

        if (viewModel != null) {
            viewModel.MediaButton1Text = button1;
            viewModel.MediaButton2Text = button2;
        }
    }
    private void Reset_Play_Buttons()
    {
        string button1 = "-";
        string button2 = "";
        if ((this.mediaPlayer != null) && (this.mediaPlayer.Source != null))
            button1 = "Play";

        if (viewModel != null) {
            viewModel.MediaButton1Text = button1;
            viewModel.MediaButton2Text = button2;
        }
    }
 
    //async void Button_Clicked(object? sender, EventArgs? e)
    //{
    //    if (string.IsNullOrWhiteSpace(CustomSourceEntry.Text))
    //    {
    //        await DisplayAlertAsync("Error Loading URL Source", "No value was found to load as a media source. " +
    //            "When you do enter a value, make sure it's a valid URL. No additional validation is done.",
    //            "OK");

    //        return;
    //    }

    //    this.mediaPlayer.Source = MediaSource.FromUri(CustomSourceEntry.Text);
    //}

    //async void ChangeAspectClicked(object? sender, EventArgs? e)
    //{
    //    const string cancel = "Cancel";

    //    var resultAspect = await DisplayActionSheetAsync(
    //        "Choose aspect ratio",
    //        cancel,
    //        null,
    //        Aspect.AspectFit.ToString(),
    //        Aspect.AspectFill.ToString(),
    //        Aspect.Fill.ToString());

    //    if (resultAspect is null or cancel)
    //    {
    //        return;
    //    }

    //    if (!Enum.TryParse(typeof(Aspect), resultAspect, true, out var aspectEnum))
    //    {
    //        await DisplayAlertAsync("Error", "There was an error determining the selected aspect", "OK");

    //        return;
    //    }

    //    this.mediaPlayer.Aspect = (Aspect)aspectEnum;
    //}

    //async void DisplayPopup(object? sender, EventArgs? e)
    //{
    //    this.mediaPlayer.Pause();

    //    MediaSource source;

    //    if (deviceInfo.Platform == DevicePlatform.Android)
    //    {
    //        source = MediaSource.FromResource("AndroidVideo.mp4");
    //    }
    //    else if (deviceInfo.Platform == DevicePlatform.MacCatalyst
    //             || deviceInfo.Platform == DevicePlatform.iOS
    //             || deviceInfo.Platform == DevicePlatform.macOS)
    //    {
    //        source = MediaSource.FromResource("AppleVideo.mp4");
    //    }
    //    else
    //    {
    //        source = MediaSource.FromResource("WindowsVideo.mp4");
    //    }

    //    var popupMediaElement = new MediaElement
    //    {
    //        WidthRequest = 600,
    //        HeightRequest = 400,
    //        AndroidViewType = AndroidViewType.SurfaceView,
    //        Source = source,
    //        MetadataArtworkUrl = botImageUrl,
    //        ShouldAutoPlay = true,
    //        ShouldShowPlaybackControls = true,
    //    };

    //    await this.ShowPopupAsync(popupMediaElement);

    //    popupthis.mediaPlayer.Stop();
    //    popupthis.mediaPlayer.Source = null;
    //}
}
