using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if ANDROID
using AndroidX.Lifecycle;
#endif
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
//using Java.Net;
//using CommunityToolkit.Maui.Sample.Constants;
//using CommunityToolkit.Maui.Sample.ViewModels.Views;
using LatincitaAndroid.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;

namespace LatincitaAndroid;

public partial class DetailsPage : ContentPage
{
    private RadioProgramDetailsViewModel? viewModel;
    readonly ILogger logger;
    readonly IDeviceInfo deviceInfo;

    private MediaSource auto_play_source;

    public DetailsPage(IDeviceInfo deviceInfo, ILogger<DetailsPage> logger)
    {
        Debug.WriteLine("DetailsPage Initializer called");

        InitializeComponent();

        // don't rely on BindingContext being set here (may be set later by Shell/DI)

        this.logger = logger;
        this.deviceInfo = deviceInfo;

        this.mediaPlayer.PropertyChanged += MediaElement_PropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is RadioProgramDetailsViewModel vm) {

            vm.Mp3UrlChanged += OnMp3UrlChanged;

            vm.Initialize(); // <<< stupid ... have to call Initialize in RadioProgramDetailsViewModel
                             //     to get it to invoke the Mp3UrlChanged event for mp3url in ProgramListService
                             //
                             //     the viewmodel listens for this event and then calls our Load_URL
                             //
                             //     all this because the Mp3UrlChanged event is getting raised
                             //     before the viewmodel has been initialized
        }
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is RadioProgramDetailsViewModel vm) {
            vm.Mp3UrlChanged -= OnMp3UrlChanged;
        }
        base.OnDisappearing();
    }


    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"DetailsPage.OnBindingContextChanged BindingContext={BindingContext?.GetType().FullName ?? "null"}");

        Reset_Play_Buttons();

        if (BindingContext is RadioProgramDetailsViewModel vm) {
            viewModel = vm;

            vm.Mp3UrlChanged += OnMp3UrlChanged;

        } else {
            viewModel = null;
        }
    }

    private void OnMp3UrlChanged(string url)
    {
        bool auto_play = ((viewModel != null) && (viewModel.ProgramListService != null)) ? viewModel.ProgramListService.Auto_play : false;

        Load_URL(url, auto_play);
    }

    private async void MediaPlayer_MediaOpened(object sender, EventArgs args)
    {
        MediaElement mediaElement = (MediaElement)sender;
        int start_pos = (viewModel != null) && (viewModel.ProgramListService != null) ? viewModel.ProgramListService.StartPosition : 0;
        // *** need to find a way to determine if this start_pos is for currently loaded Source
        try {
            if (start_pos > 0) {
                Debug.WriteLine($"Attempting Seek to {start_pos} secs for '{this.mediaPlayer.MetadataTitle}'");

                // Guard: ensure media supports seeking and duration is known
                bool canSeek = true;
                TimeSpan duration = TimeSpan.Zero;
                try {
                    // Access Duration/CanSeek on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        duration = this.mediaPlayer.Duration;
                        // if Duration == TimeSpan.Zero many implementations report unknown duration
                    });

                    // If Duration is zero or unknown, we should avoid seeking to an out-of-range position
                    if (duration == TimeSpan.Zero) {
                        Debug.WriteLine("Media duration unknown; skipping SeekTo to avoid native crash.");
                        canSeek = false;
                    } else if (start_pos > duration.TotalSeconds) {
                        Debug.WriteLine($"Requested start_pos {start_pos} > duration {duration.TotalSeconds}; skipping SeekTo.");
                        canSeek = false;
                    }
                } catch (Exception ex) {
                    Debug.WriteLine($"Failed to query Duration/CanSeek: {ex.GetType().FullName}: {ex.Message}");
                    // If querying Duration itself fails, avoid calling SeekTo
                    canSeek = false;
                }

                if (canSeek) {
                    // Execute the Seek on the UI thread and await it so exceptions surface on this Task
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        try {
                            await this.mediaPlayer.SeekTo(TimeSpan.FromSeconds(start_pos), CancellationToken.None);
                            Debug.WriteLine($"SeekTo succeeded to {start_pos} secs");
                        } catch (COMException comEx) {
                            Debug.WriteLine($"COMException during SeekTo: {comEx.HResult:X} {comEx.Message}");
                            await Shell.Current.DisplayAlert("Playback error", "Unable to seek in the media (platform error).", "OK");
                        } catch (Exception exInner) {
                            Debug.WriteLine($"Exception during SeekTo: {exInner.GetType().FullName}: {exInner.Message}");
                            await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                        }
                    });
                }
            } else {
                Debug.WriteLine("The track '" + this.mediaPlayer.MetadataTitle + "' has been loaded");
            }
        } catch (Exception ex) {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"MediaOpened top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }

        ////////////////////////////// *** HAVE TO MOVE THIS TO SEEK FINISHED !!
        //
        //if (auto_play_source != null) {
        //    if (auto_play_source.ToString() == mediaElement.Source.ToString()) {
        //        auto_play_source = null;
        //        OnPlayClicked(this.mediaPlayer, null);
        //    }
        //}
    }

    void MediaElement_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(this.mediaPlayer.DurationProperty))
        {
            double max_secs = (int)this.mediaPlayer.Duration.TotalSeconds;
            Debug.WriteLine($"Duration: {this.mediaPlayer.Duration}");

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
        Debug.WriteLine("Media opened.");
        Reset_Play_Buttons();
    }

    void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {   Debug.WriteLine($"Media State Changed. Old State: {e.PreviousState}, New State: {e.NewState}");
        Manage_Play_Buttons();
    }

    void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {   Debug.WriteLine($"Media failed. Error: {e.ErrorMessage}");
        Reset_Play_Buttons();
    }

    void OnMediaEnded(object? sender, EventArgs? e)
    { 
        Debug.WriteLine("Media ended.");
        Reset_Play_Buttons();
    }

    void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        if ((viewModel != null) && (viewModel.ProgramListService != null)) {
            int tbeg_offset = viewModel.ProgramListService.StartPosition;
            int tnxt_offset = viewModel.ProgramListService.EndPosition;
            int tcurr_offset = (int)e.Position.TotalSeconds;
            if ((tnxt_offset > 0) && (tcurr_offset >= tnxt_offset)) {
                if ((tbeg_offset >= 0) && (tcurr_offset >= tbeg_offset) && (tnxt_offset >= tbeg_offset)) {
                    Debug.WriteLine($"Position {tcurr_offset} reached EndPosition {tnxt_offset}, stopping playback.");
                    // *** here is where we need to handle auto-play next track INSTEAD of just stopping
                    OnStopClicked(this.mediaPlayer, null);
                    return;
                }
            }
        }

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
        double pos_secs = e.Position.TotalSeconds;
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
        Debug.WriteLine("Position changed to {e.Position}");
        PositionSlider.Maximum = max_secs;
        PositionSlider.Value = pos_secs;
    }

    void OnSeekCompleted(object? sender, EventArgs? e) => Debug.WriteLine("Seek completed.");

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

    private async void Load_URL(string url, bool auto_play)
    {
        Reset_Play_Buttons();

        auto_play_source = null;
        try {
            MainThread.BeginInvokeOnMainThread(async () => {
                try {
                    this.mediaPlayer.Stop();
                    this.mediaPlayer.Source = null;
                    UriMediaSource _src = new UriMediaSource {
                                                 Uri = new Uri(url)
                                              };
                    if (auto_play) {
                    //  OnPlayClicked(this.mediaPlayer,null);
                        auto_play_source = _src;
                    }
                    this.mediaPlayer.Source = _src;
                    Debug.WriteLine($"Loading {url} succeeded");
                } catch (COMException comEx) {
                    Debug.WriteLine($"COMException loading media: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Media Load error", "Unable to load {url} (platform error).", "OK");
                } catch (Exception exInner) {
                    Debug.WriteLine($"Exception loading media: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Media Load error", exInner.Message, "OK");
                }
            });
        } catch (Exception ex) {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"Media-Load top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }
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

    private async void OnPlayClicked(object? sender, EventArgs? e)
    {
        if (this.mediaPlayer == null) {
            return;
        }
        if (this.mediaPlayer.Source == null) {
            if (viewModel != null && viewModel.ProgramListService != null && !string.IsNullOrWhiteSpace(this.viewModel.ProgramListService.Mp3Url))
                Load_URL(this.viewModel.ProgramListService.Mp3Url, true);

            return;
        }
        try
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                try
                {
                    this.mediaPlayer.Play();
                    Debug.WriteLine($"Play succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"COMException during Play: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Play the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"Exception during Play: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"PLAY top-level exception: {ex.GetType().FullName}: {ex.Message}");
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
                    Debug.WriteLine($"Pause succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"COMException during Pause: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Pause the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"Exception during Pause: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"PAUSE top-level exception: {ex.GetType().FullName}: {ex.Message}");
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
                    Debug.WriteLine($"Stop succeeded");
                }
                catch (COMException comEx)
                {
                    Debug.WriteLine($"COMException during Stop: {comEx.HResult:X} {comEx.Message}");
                    await Shell.Current.DisplayAlert("Playback error", "Unable to Stop the media (platform error).", "OK");
                }
                catch (Exception exInner)
                {
                    Debug.WriteLine($"Exception during Stop: {exInner.GetType().FullName}: {exInner.Message}");
                    await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                }
            });
        }
        catch (Exception ex)
        {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"STOP top-level exception: {ex.GetType().FullName}: {ex.Message}");
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
                button1 = "Resume";
                button2 = "Stop";
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
