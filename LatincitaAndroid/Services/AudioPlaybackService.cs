using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Maui.Views;


#if ANDROID
using Android.Media;
using AndroidX.Lifecycle;
using AndroidX.Media3.Extractor.Mp4;
#endif

namespace LatincitaAndroid.Services
{
    public class AudioPlaybackService
    {
        private readonly Channel<PlayListItem> _queue =
            Channel.CreateUnbounded<PlayListItem>();

        private TaskCompletionSource _resumeSignal = CreateResumeSignal(); // create initial signal
        private TaskCompletionSource _mediaOpenedTcs = CreateResumeSignal(); // create initial signal
        //TaskCompletionSource _mediaEndedTcs;
        //TaskCompletionSource _playerAvailableTcs;

        private volatile bool _running = true;

        private volatile bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private MediaElement mediaPlayer = null;
        private MediaSource auto_play_source;

        public PlayListItem CurrentPlayListItem = null;
        public TrackListItem CurrentTrackListItem = null;

        public int CurrentTrackListIndex = 0;  // index into CurrentPlayListItem.track_list

        public AudioPlaybackService()
        {
            Debug.WriteLine($"| INIT AudioPlaybackService");

            _isPaused = true;  // start queue paused waiting for media-player to appear and get initialized

            Task.Run(ProcessQueueAsync);
        }

        private static TaskCompletionSource CreateResumeSignal()
        {
            return new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void MediaPlayer_Register(MediaElement _mediaPlayer)
        {
            Debug.WriteLine("| AudioPlaybackService::MediaPlayer_Register");

            this.mediaPlayer = _mediaPlayer;

            this.ResumeQueue();
        }

        public void MediaPlayer_Unregister()
        {
            Debug.WriteLine("| AudioPlaybackService::MediaPlayer_Unregister");

            this.PauseQueue();

            this.ClearQueue();

            this.mediaPlayer = null;
        }

        public void MediaPlayer_MediaOpened(MediaElement _mediaPlayer)
        {
            Debug.WriteLine("| AudioPlaybackService::MediaPlayer_MediaOpened");

            this.mediaPlayer = _mediaPlayer;

            _mediaOpenedTcs?.TrySetResult();
        }

        public void MediaPlayer_MediaEnded()
        {
            Debug.WriteLine("| AudioPlaybackService::MediaPlayer_MediaEnded");

            // song ended ... read next item from the queue
            // if queue is empty & we are in RANDOM mode
            // need to fetch a new RANDOM, add it to playlist
            // and push it onto the Queue

            this.ResumeAfterSongEnds();
        }
        public void MediaPlayer_TrackEnded()
        {
            Debug.WriteLine("| AudioPlaybackService::MediaPlayer_TrackEnded");

            if (CurrentPlayListItem == null || CurrentPlayListItem.track_list == null ||
                (CurrentPlayListItem.track_list.Count < 1) ||
                CurrentTrackListIndex < 0) {
                CurrentTrackListIndex = -1;
                CurrentTrackListItem = null;
                return;
            }

            //  play head reached end of current track, advance pointer to next track

            CurrentTrackListIndex += 1;
            if (CurrentTrackListIndex >= CurrentPlayListItem.track_list.Count) {
                CurrentTrackListIndex = -1;
                CurrentTrackListItem = null;
            } else {
                CurrentTrackListItem = CurrentPlayListItem.track_list[CurrentTrackListIndex];
            }

        }

        public async Task<bool> Select_Track_from_TrackList(TrackObject track)
        {
            bool ret = false;

            PlayListItem pitem = CurrentPlayListItem;

            if ((int.Parse(track.id) != pitem.id) && (track.radioid != int.Parse(pitem.songid))) {
                Debug.WriteLine(String.Format("AudioPlaybackService::Select_Track_from_TrackList: requested different track then loaded track"));
                return false;
            }

            // CurrentPlayListItem just points to entire RADIO
            // pitem.track_list is a list of TrackListItems
            // one of these is the track we want

            if (pitem != null) {
                TrackListItem titem = null;
                int idx = 0;
                for (idx = 0; idx < pitem.track_list.Count; idx++) {
                    if (pitem.track_list[idx].offs <= track.offset) {
                        titem = pitem.track_list[idx];
                        break;
                    }
                }
                if (titem != null) {
                    CurrentTrackListIndex = idx;
                    CurrentTrackListItem = pitem.track_list[idx];

                    ret = await PlayTrackAsync(pitem);
                }
            }
            return ret;
        }

        public bool Queue_Is_Empty()
        {
            bool is_empty = true;

            PlayListItem track = null;

            if (this._queue.Reader.TryPeek(out track)) {
                if (track != null) {
                    is_empty = false;
                }
            }
            return is_empty;
        }

        public async Task QueueTrackAsync(PlayListItem request)
        {
            if (request == null) {
                Debug.WriteLine($"| AudioPlaybackService::QueueTrackAsync was passed NULL !!!");
                return;
            }
            Debug.WriteLine($"| QueueTrackAsync({request.title})");

            await this._queue.Writer.WriteAsync(request);
        }
        public void PauseQueue()
        {
            Debug.WriteLine("| AudioPlaybackService::PauseQueue");

            _isPaused = true;
        }

        public void ResumeQueue()
        {
            Debug.WriteLine("| AudioPlaybackService::ResumeQueue");

            if (_isPaused) {
                _isPaused = false;

                _resumeSignal.TrySetResult();

                _resumeSignal = CreateResumeSignal(); // need to immediately create a new signal
            }
        }
        public async void PauseUntilSongEnds()
        {
            Debug.WriteLine("| AudioPlaybackService::PauseUntilSongEnds: Pausing processing until media ends.");
            if (!this._isPaused) {
                this.PauseQueue();
            }
        }
        public void ResumeAfterSongEnds()
        {
            Debug.WriteLine("| AudioPlaybackService::ResumeAfterSongEnds: Media ended - resuming processing");
            this.ResumeQueue();
        }
        public async Task ScollDown()
        {
            Debug.WriteLine("| AudioPlaybackService::ScollDown");

            bool was_paused = this._isPaused;

            if (!was_paused) {
                this.PauseQueue();
            }

            var track =
                await this._queue.Reader.ReadAsync();

            if (track != null) {
                await this.QueueTrackAsync(track);
            }

            if (!was_paused) {
                this.ResumeQueue();
            }
        }
        public async Task ScollDown_ToID(int id)
        {
            Debug.WriteLine($"| AudioPlaybackService::ScollDown_ToID({id})");

            bool was_paused = this._isPaused;

            if (!was_paused) {
                this.PauseQueue();
            }

            int id0 = -9999;
            PlayListItem track = null;
            while (this._queue.Reader.TryPeek(out track)) {
                if (track == null) {
                    continue;
                }
                if (track.id == id) {
                    break; // leave desired track @ bottom of queue
                }
                if (id0 == -9999) {
                    id0 = track.id;
                } else if (track.id == id0) {
                    break; // looped around
                }
                var track2 = await this._queue.Reader.ReadAsync(); // pop bottom track
                if (track2 != null) {
                    await this.QueueTrackAsync(track2); // push to top
                }
            }

            if (!was_paused) {
                this.ResumeQueue();
            }
        }
        public async void ClearQueue()
        {
            Debug.WriteLine("| AudioPlaybackService::ClearQueue");

            bool was_paused = this._isPaused;

            if (!was_paused) {
                this.PauseQueue();
            }

            if (this.mediaPlayer != null) {
                try {
                    this.mediaPlayer.Stop();
                    this.mediaPlayer.Source = null;
                } catch { }
            }

            this.CurrentPlayListItem = null;
            this.CurrentTrackListIndex = -1;
            this.CurrentTrackListItem = null;

            while (this._queue.Reader.TryRead(out _)) {
                // discard item
            }

            if (!was_paused) {
                this.ResumeQueue();
            }
        }
        private async Task ProcessQueueAsync()
        {
            Debug.WriteLine("| AudioPlaybackService::ProcessQueueAsync");

            while (_running) {

                if (_isPaused)
                    await _resumeSignal.Task;

                if (this.mediaPlayer == null) {
                    this.PauseQueue();
                    continue;
                }

                var track = await _queue.Reader.ReadAsync();

                if (track == null) {
                    continue;
                }

                this.PauseUntilSongEnds();  // processing paused until current song ends

                bool loaded = await this.PlayTrackAsync(track);

                if (!loaded) {
                    this.ResumeAfterSongEnds();
                }
            }
        }

        public async Task<bool> PlayTrackAsync(PlayListItem track)
        {
            Debug.WriteLine($"| AudioPlaybackService::PlayTrackAsync({track.title})");

            //_player.Open(track.m4v);

            //await WaitUntilLoadedAsync();

            //_player.Seek(track.StartPosition);

            //_player.Play();

            //await Task.Delay(track.Duration);

            //_player.Stop();

            if (track == null) {
                return false;
            }

            _mediaOpenedTcs = CreateResumeSignal(); // create a new signal before loading track.  signal should arrive before Load_Track returns

            bool loaded = await Load_Track(track);

            if (loaded) {

                await _mediaOpenedTcs.Task; // wait until player loads track

                bool seeked = await Start_Track(track);
            }

            // have to wait for track to end
            // returning auto fetches next track

            return loaded;
        }
        public async Task<bool> Load_Track(PlayListItem track)
        {
            Debug.WriteLine($"| AudioPlaybackService::Load_Track({track.title})");

            if (track == null) {
                return false;
            }
            string url = track.m4v;
            bool auto_play = track.entireRadio;

            Debug.WriteLine($"| >>> AudioPlaybackService:Load_URL({url},{auto_play})");

            if (String.IsNullOrWhiteSpace(url)) {
                return false;
            }

            if (this.mediaPlayer == null) { // && media_player_ready <<< media-player not calling MediaPlayer_Loaded on Android
                Debug.WriteLine($"| >>> page/player not ready - regretfully ignoring laod command");
                return false;
            }
            url = HttpUtility.UrlDecode(url);  // ChatGPT says undo url encoding

            bool url_unchanged = IsSameMediaUrlIgnoringDomain(this.mediaPlayer.Source, url);

            Debug.WriteLine($"| >>> => {url}");

            this.CurrentPlayListItem = track;
            this.CurrentTrackListIndex = 0;  // *** are we sure we always want to cue up track_list[0] ???
            if ((track.track_list == null) || (track.track_list.Count <= 0)) {
                this.CurrentTrackListIndex = -1;
                this.CurrentTrackListItem = null;
            } else {
                this.CurrentTrackListItem = track.track_list[CurrentTrackListIndex];
            }

            //if (!url_unchanged) {
            //    Reset_Play_Buttons();  // would have to first call w/ empty URL to get us to reset play buttons
            //}

            bool media_loaded = false;

            this.auto_play_source = null;

            try {
                await PlayUrlAsync(url, url_unchanged, auto_play);
                media_loaded = true;
            } catch (Exception ex) {
                await Shell.Current.DisplayAlert("Media Load error", "Unable to load {url}", "OK");
            }
            return media_loaded;
        }

        public Task PlayUrlAsync(string url, bool url_unchanged, bool auto_play)
        {
            Debug.WriteLine($"| AudioPlaybackService::PlayUrlAsync({url})");

            var tcs = new TaskCompletionSource<object?>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - BeginInvokeOnMainThread");
                try {
                    //    UriMediaSource _src = new UriMediaSource {
                    //                                 Uri = new Uri(url)
                    //                              };
                    if (auto_play) {
                        //  OnPlayClicked(this.mediaPlayer,null);
                        this.auto_play_source = MediaSource.FromUri(url);
                    }
                    if (!url_unchanged) {
                        Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - Stop()");
                        this.mediaPlayer.Stop();
                        this.mediaPlayer.Source = null;
                        //  this.mediaPlayer.Source = _src;
                        Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - MediaSource.FromUri({url})");
                        this.mediaPlayer.Source = MediaSource.FromUri(url);
                        Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - Loading {url} succeeded");
                    } else {
                        Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - {url} already loaded");
                    }
                //  media_loaded = true;
                    tcs.SetResult(null);
                } catch (COMException comEx) {
                    Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - COMException loading media: {comEx.HResult:X} {comEx.Message}");
                //  await Shell.Current.DisplayAlert("Media Load error", "Unable to load {url} (platform error).", "OK");
                    tcs.SetException(comEx);
                } catch (Exception exInner) {
                    Debug.WriteLine($"| >>> AudioPlaybackService:Load_Track - Exception loading media: {exInner.GetType().FullName}: {exInner.Message}");
                //  await Shell.Current.DisplayAlert("Media Load error", exInner.Message, "OK");
                    tcs.SetException(exInner);
                }
            });
            return tcs.Task;
        }

        private async Task<bool> Start_Track(PlayListItem track)
        {
            Debug.WriteLine($"| AudioPlaybackService::Start_Track({track.title})");

            int start_pos = (this.CurrentTrackListItem == null ? -1 : this.CurrentTrackListItem.offs);
            bool seeked = false;

            // *** need to find a way to determine if this start_pos is for currently loaded Source
            try {
                if (start_pos > 0) {
                    Debug.WriteLine($"| Attempting Seek to {start_pos} secs for '{this.mediaPlayer.MetadataTitle}'");

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
                            Debug.WriteLine("| Media duration unknown; SeekTo may result in a native crash.");
                            canSeek = true;
                        } else if (start_pos > duration.TotalSeconds) {
                            Debug.WriteLine($"| Requested start_pos {start_pos} > duration {duration.TotalSeconds}; skipping SeekTo.");
                            canSeek = false;
                        }
                    } catch (Exception ex) {
                        Debug.WriteLine($"| Failed to query Duration/CanSeek: {ex.GetType().FullName}: {ex.Message}");
                        // If querying Duration itself fails, avoid calling SeekTo
                        canSeek = false;
                    }

                    if (canSeek) {
                        // Execute the Seek on the UI thread and await it so exceptions surface on this Task
                        try {
                            await SeekAsync(start_pos);
                            seeked = true;
                        } catch (Exception ex) {
                            await Shell.Current.DisplayAlert("Playback error", "Unable to seek in the media", "OK");
                        }
                    } else {
                        seeked = true;  // fake it
                    }
                } else {
                    Debug.WriteLine("| The track '" + this.mediaPlayer.MetadataTitle + "' has been loaded");
                    seeked = true;  // nothing to seek ... already where we want to be
                }
            } catch (Exception ex) {
                // Last-resort catch. Note: corrupted-state exceptions may still escape.
                Debug.WriteLine($"| MediaOpened top-level exception: {ex.GetType().FullName}: {ex.Message}");
                try { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); } catch { }
            }

            return seeked;
        }

        public Task SeekAsync(int start_pos)
        {
            Debug.WriteLine($"| AudioPlaybackService::SeekAsync({start_pos})");

            var tcs = new TaskCompletionSource<object?>();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try {
                    Debug.WriteLine($"| Seeking to {start_pos} secs...");
                    this.mediaPlayer.SeekTo(TimeSpan.FromSeconds(start_pos), CancellationToken.None);
                    Debug.WriteLine($"| SeekTo succeeded to {start_pos} secs");
                    //  seeked = true;
                    tcs.SetResult(null);
                } catch (COMException comEx) {
                    Debug.WriteLine($"| COMException during SeekTo: {comEx.HResult:X} {comEx.Message}");
                    //  await Shell.Current.DisplayAlert("Playback error", "Unable to seek in the media (platform error).", "OK");
                    tcs.SetException(comEx);
                } catch (Exception exInner) {
                    Debug.WriteLine($"| Exception during SeekTo: {exInner.GetType().FullName}: {exInner.Message}");
                    //  await Shell.Current.DisplayAlert("Playback error", exInner.Message, "OK");
                    tcs.SetException(exInner);
                }
            });
            return tcs.Task;
        }

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

    }
}

