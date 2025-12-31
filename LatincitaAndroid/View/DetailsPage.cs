using CommunityToolkit.Maui.Views;
using LatincitaAndroid.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Maui.Dispatching;
using System;

namespace LatincitaAndroid;

public partial class DetailsPage : ContentPage
{
    private RadioProgramDetailsViewModel? viewModel;

    public DetailsPage()
    {
        Debug.WriteLine("DetailsPage ctor called");
        InitializeComponent();
        // don't rely on BindingContext being set here (may be set later by Shell/DI)
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        Debug.WriteLine($"DetailsPage.OnBindingContextChanged BindingContext={BindingContext?.GetType().FullName ?? "null"}");
        if (BindingContext is RadioProgramDetailsViewModel vm) {
            viewModel = vm;
        } else {
            viewModel = null;
        }
    }

    private async void MediaPlayer_MediaOpened(object sender, EventArgs args)
    {
        MediaElement mediaElement = (MediaElement)sender;
        int start_pos = (viewModel != null) && (viewModel.ProgramListService != null) ? viewModel.ProgramListService.StartPosition : 0;
        try {
            if (start_pos > 0) {
                Debug.WriteLine($"Attempting Seek to {start_pos} s for '{mediaElement.MetadataTitle}'");

                // Guard: ensure media supports seeking and duration is known
                bool canSeek = true;
                TimeSpan duration = TimeSpan.Zero;
                try {
                    // Access Duration/CanSeek on UI thread
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        duration = mediaElement.Duration;
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
                    await MainThread.InvokeOnMainThreadAsync(async () => {
                        try {
                            await mediaElement.SeekTo(TimeSpan.FromSeconds(start_pos), CancellationToken.None);
                            Debug.WriteLine($"SeekTo succeeded to {start_pos} s");
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
                Debug.WriteLine("The track '" + mediaElement.MetadataTitle + "' has been loaded");
            }
        } catch (Exception ex) {
            // Last-resort catch. Note: corrupted-state exceptions may still escape.
            Debug.WriteLine($"MediaOpened top-level exception: {ex.GetType().FullName}: {ex.Message}");
            try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
        }
    }
}