using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
//using Javax.Annotation.Meta;
//using Kotlin.Contracts;
using LatincitaAndroid.Model;
using Microsoft.Maui.Controls.Shapes;
using CommunityToolkit.Maui.Views;
using System.Web;



#if ANDROID
using static Android.Icu.Text.CaseMap;
using static Android.Provider.ContactsContract.CommonDataKinds;
using static Android.Provider.MediaStore.Audio;
using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;
using AndroidX.Media3.Extractor.Mp4;
//using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;
//using static Android.Icu.Text.CaseMap;
//using AndroidX.Media3.Extractor.Mp4;
#endif

namespace LatincitaAndroid.Services;

public partial class ProgramListService : ObservableObject
{
    public ObservableCollection<RadioProgram> RadioPrograms { get; } = new();

    public ObservableCollection<string> CdList { get; } = new() {
        "Sola", "Echos from the Future", "Compilation 2010", "Gemstones", "Godess Latincita", "Caribbean Smile", "Idols"
    };

    private List<PlayListItem> myPlaylist = new();

    private Boolean isPlaying = false;

    [ObservableProperty]
    private RadioProgram currentRadioProgram;    // main-list

    [ObservableProperty]
    private TrackObject currentTrack;            // theFsongs[]

    [ObservableProperty]
    private PlayListItem currentPlayListItem;     // jPlayerPlaylist.playlist item

    [ObservableProperty]
    private List<TrackObject> currentTrackList;

    [ObservableProperty]
    private bool currentTrackListNotEmpty;

    [ObservableProperty]
    private bool trackListHasTimestamps;

    public bool ShowTrackListWithTimestamps => CurrentTrackListNotEmpty && TrackListHasTimestamps;

    public bool ShowTrackListWithNoTimestamps => CurrentTrackListNotEmpty && !TrackListHasTimestamps;

    [ObservableProperty]
    private RadioProgramType currentType;  // RADIO / CD / FAVORITE / RANDOM

    [ObservableProperty]
    private string? mp3Url;

    [ObservableProperty]
    private int startPosition;

    [ObservableProperty]
    private int endPosition;

    [ObservableProperty]
    private bool auto_play;

    [ObservableProperty]
    private string? currentProgramTitle;  // CurrentRadioProgram.ArticleTitle

    [ObservableProperty]
    private string? currentPictureURL;  // CurrentRadioProgram.PictureURL

    [ObservableProperty]
    private string? id;

    [ObservableProperty]
    private string? detail_line_1;

    [ObservableProperty]
    private string? detail_line_2;

    [ObservableProperty]
    private string? detail_line_3;

    [ObservableProperty]
    private bool hasLine1;
    [ObservableProperty]
    private bool hasLine2;
    [ObservableProperty]
    private bool hasLine3;

    AllLatincitaService AllLatincitaService;
    AudioPlaybackService AudioPlaybackService;

    public ProgramListService(AllLatincitaService AllLatincitaService,
                              AudioPlaybackService AudioPlaybackService)
    {
        //  this.Title = this.radio_program.ArticleTitle;
        this.AllLatincitaService = AllLatincitaService;
        this.AudioPlaybackService = AudioPlaybackService;

        CurrentTrackListNotEmpty = ((CurrentTrackList != null) && (CurrentTrackList.Count > 0));
        StartPosition = 0;
        EndPosition = 0;
    }

    public async void AddProgram(RadioProgram program, bool auto_select)
    {
        if (program is null || program.mp3 == "") {
            return;
        }

        RadioPrograms.Add(program);

        if (auto_select) {
            await Task.Run(() =>
            {
                SetProgram(program);
            });
        }
    }
    public async void SetProgram(RadioProgram program)
    {
        this.AudioPlaybackService.ClearQueue();  // stop all current audio

        CurrentRadioProgram = program;
        CurrentType = program.Type;

        Id = "";

    //  CurrentProgramTitle = program.ArticleTitle;

        CurrentTrackList = new();
        CurrentTrackListNotEmpty = false;
        CurrentTrack = new();

        Detail_line_1 = "";
        Detail_line_2 = "";
        Detail_line_3 = "";
        HasLine1 = false;
        HasLine2 = false;
        HasLine3 = false;

        Mp3Url = "";

        StartPosition = -1;
        EndPosition = -1;

        Auto_play = false;

        DateTime min_date = new DateTime(2000, 1, 1);

        if (program.Type == RadioProgramType.RADIO) {
            // display radio ID on top-right
            Id = program.ID > 0 ? string.Format("{0}", program.ID) : "";
            // image is Radio Artwork
            CurrentPictureURL = CurrentRadioProgram.PictureURL;
            // CurrentProgramTitle:   Radio 2 November  [radio name]
            CurrentProgramTitle = CurrentRadioProgram.ArticleTitle;
            // text:   Aug 2013   [radio date]
            Detail_line_1 = CurrentRadioProgram.RecordedOn <= min_date ? "- * -" : string.Format("{0:MMMM yyyy}", CurrentRadioProgram.RecordedOn);
            HasLine1 = true;
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            // entire show is one MP3 with offsets   [program.mp3]
            // play entire show & detect offsets 
            // change artist/song text when offset is reached
        } else if (program.Type == RadioProgramType.CD) {
            // image is CD Cover
            CurrentPictureURL = CurrentRadioProgram.PictureURL;
            // CurrentProgramTitle:   Latincita SOLA    [cd name]
            CurrentProgramTitle = CurrentRadioProgram.ArticleTitle;
            // text:   Aug 2013    [cd date]
            Detail_line_1 = CurrentRadioProgram.RecordedOn <= min_date ? "- * -" : string.Format("{0:MMMM yyyy}", CurrentRadioProgram.RecordedOn);
            HasLine1 = true;
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            // cue up 1st number as MP3   [track.mp3]
            // display artist + song
            // ignore track image
            // start playing first song on CD
            // @ end go back to first song on CD
        } else if (program.Type == RadioProgramType.FAVORITE) {
            // image is current track image
            CurrentPictureURL = CurrentRadioProgram.PictureURL; // *** could let SetTrack handle image
            // CurrentProgramTitle:   Playlist: REH  [playlist-name]
            CurrentProgramTitle = "Playlist: " + CurrentRadioProgram.ArticleTitle;
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            Detail_line_1 = CurrentRadioProgram.RecordedOn <= min_date ? "- * -" : string.Format("{0:MMMM yyyy}", CurrentRadioProgram.RecordedOn);
            HasLine1 = true;
            // text:   Aug 2013 - Sep 2022  [playlist date / track.date]
            // cue up 1st number as MP3   [track.mp3]
            // display artist + song
            // add all tracks on favorite-list to playlist
            // start playing first song
            // @ end go back to first number on playlist
        } else if (program.Type == RadioProgramType.RANDOM) {
            // image is current track image
            CurrentPictureURL = "";  // let SetTrack handle image
            // CurrentProgramTitle:   Hubo Alguien   [track.song]
            CurrentProgramTitle = "";  // let SetTrack handle title
            // text:   Mark Anthony   [track.artist]
            // text:   Sep 2022  [track.date]
            // cue up 1st number as MP3   [track.mp3]
            // display artist + song
            // auto add all current RANDOMs to playlist
            // start playing selected track
            // @ end auto-fetch RANDOM, add to playlistand auto-play it
        }

        TrackObject _track = new TrackObject();

        if (program.Type == RadioProgramType.RADIO || program.Type == RadioProgramType.RANDOM) {

            _track = await AllLatincitaService.get_track(program);

            if (_track is null || _track.id == "") {
                return;
            }
        }


        if (program.Type == RadioProgramType.RADIO || 
            program.Type == RadioProgramType.CD||
            program.Type == RadioProgramType.FAVORITE) {

            CurrentTrackList = await AllLatincitaService.get_radio_tracks(program);
            // when someone selects a RADIO we load all the tracks on this radio to the CurrentTrackList

            // set track to entire show, let addToPlaylist handle cuing up first track
            if (_track is null || _track.id == "") {
                if (CurrentTrackList != null && CurrentTrackList.Count > 0) {
                    _track = CurrentTrackList[0];  // set current track to first track on show
                }
            }

            TrackListHasTimestamps = program.Type == RadioProgramType.RADIO;  // *** wish we knew offsets of CD tracks !

        } else if (program.Type == RadioProgramType.RANDOM) {  // _track.offset > 0) 
            //  var _tracks = await AllLatincitaService.get_radio_tracks(_track);
            // when someone selects a RANDOM, if selected item is a track, it's OK to fetch
            // all the other tracks on the select item's RADIO
            // but this is not what needs to go to the CurrentTrackList !

            // every RadioProgram on the RadioPrograms list needs to be converted
            // to a TrackObject and pushed onto the CurrentTrackList
            // then the track user clicked on needs to be selected and auto-played

            CurrentTrackList = new();

            foreach (var prog in this.RadioPrograms) {
                var _t = await AllLatincitaService.get_track(prog);
                if (_t.id != "") {
                    CurrentTrackList.Add(_t);
                    if (_t.id == _track.id) {
                        // this is our track
                    } else {
                        // this is a previous/next random track
                    }
                }
            }
            PlayListItem track0 = await addAllToPlayList();  // copy entire CurrentTrackList to playlist

            TrackListHasTimestamps = false;  // hide offset column, there is no way to get sensible data into that column
        } else {
            CurrentTrackList = new();
        }
        CurrentTrackListNotEmpty = ((CurrentTrackList != null) && (CurrentTrackList.Count > 0));

        if (CurrentTrackListNotEmpty) {
            if (program.Type == RadioProgramType.RADIO) {
                // _track should be an entire radio program
                // when converted to a PlayListItem, the item should
                // have a _track.track_list with a list of the offsets on this radio
                // we push this single _track to the AudioPlaybackService's queue

                if (_track != null && !String.IsNullOrWhiteSpace(_track.id)) {

                    PlayListItem _playListItem = await addToPlaylist(_track, wmaTyp.cMusic);

                    _track.cached_playlist_item = _playListItem;

                    await this.AudioPlaybackService.QueueTrackAsync(_playListItem);  // add item to queue

                    foreach (TrackObject track in this.CurrentTrackList) {
                        track.cached_playlist_item = _playListItem;  // point all tracks to this single playlist item
                    }
                }

            } else if (program.Type == RadioProgramType.CD) {
                // we need to convert all items in CurrentTrackList to PlayListItem's
                // and push each one from 1st to last to the AudioPlaybackService's queue

                await this.addAllToPlayList();

            } else if (program.Type == RadioProgramType.FAVORITE) {
                // we need to convert all items in CurrentTrackList to PlayListItem's
                // and push each one from 1st to last to the AudioPlaybackService's queue

                await this.addAllToPlayList();

            } else if (program.Type == RadioProgramType.RANDOM) {
                // we need to convert all items in CurrentTrackList to PlayListItem's
                // and push each one from 1st to last to the AudioPlaybackService's queue
                // then we need to convert _track to a PlayListItem and get its "id"
                // the we ask the AudioPlaybackService to scroll down to this id

                PlayListItem _item = _track.cached_playlist_item;

                //await this.addAllToPlayList();

                //PlayListItem _item = await addToPlaylist(_track, wmaTyp.cMusic);  // should find track on myPlaylist and just return it

                if (_item != null && _item.id > 0) {
                    await this.AudioPlaybackService.ScollDown_ToID(_item.id);  // scroll down so selected RANDOM is at the bottom of the queue
                }
            }
        }

        foreach (TrackObject _tt in this.CurrentTrackList) {
            _tt.background_class = "DefaultRowStyle";
            _tt.isCurrentRow = false;
        }

        await Task.Run(() =>
        {
            SetTrack(_track);
        });
    }

    public async void SetTrack(TrackObject track)
    {
        DateTime min_date = new DateTime(2000, 1, 1);

        CurrentTrack = track;

        RadioProgram radioProgram = CurrentRadioProgram;
        TrackObject trackObject = CurrentTrack;
        RadioProgramType type = CurrentType;

        StartPosition = track.offset;
        EndPosition = track.nxtoffset;

        string new_mp3 = "";

        if (radioProgram is null) {

        //  Title = "Latincita Radio Programs";

            Detail_line_1 = "LATINCITA";
            Detail_line_2 = "";
            Detail_line_3 = "";
            HasLine1 = true;
            HasLine2 = false;
            HasLine3 = false;

            Auto_play = false;

            new_mp3 = "https://www.latincita.com" + "/best_of_latincita/Latincita%20Opener.mp3";

            if (trackObject.article_title == "Best of Latincita")
                return;  // no infinite loop

            trackObject = new();
            trackObject.article_title = "Best of Latincita";
            trackObject.artist = "Latincita";
            trackObject.photo = ""; //  <<<< find URL of Latincita-Letters

            Id = "";

            SetTrack(trackObject);

            return;
        }

        if (type == RadioProgramType.RADIO) {
            // image is Radio Artwork
            // CurrentProgramTitle:   Radio 2 November  [radio name]
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            // text:   Aug 2013   [radio date]
            // entire show is one MP3 with offsets   [program.mp3]
            new_mp3 = radioProgram?.mp3;
            // play entire show & detect offsets 
            // change artist/song text when offset is reached
        } else if (type == RadioProgramType.CD) {
            // image is CD Cover
            // CurrentProgramTitle:   Latincita SOLA    [cd name]
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            // text:   Aug 2013    [cd date]
            // cue up 1st number as MP3   [track.mp3]
            new_mp3 = track?.mp3s_music;
            // display artist + song
            // ignore track image
            // start playing first song on CD
            // @ end go back to first song on CD
        } else if (type == RadioProgramType.FAVORITE) {
            // image is current track image
            // CurrentProgramTitle:   Playlist: REH  [playlist-name]
            // text:   Mark Anthony - Hubo Alguien   [track.artist] - [track.song]
            // text:   Aug 2013 - Sep 2022  [playlist date / track.date]
            // cue up 1st number as MP3   [track.mp3]
            new_mp3 = track?.mp3s_music;
            // display artist + song
            // add all tracks on favorite-list to playlist
            // start playing first song
            // @ end go back to first number on playlist
        } else if (type == RadioProgramType.RANDOM) {
            // image is current track image
            CurrentPictureURL = track.photo;

            if (radioProgram.RecordedOn <= min_date) {
                CurrentProgramTitle = "" + trackObject.display_name;
                Detail_line_1 = "" + trackObject.artist;
                HasLine1 = true;
                HasLine2 = false;
                HasLine3 = false;
            } else {
                CurrentProgramTitle = track.song;
                Detail_line_1 = "" + trackObject.artist;
                Detail_line_2 = string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
                HasLine1 = true;
                HasLine2 = true;
                HasLine3 = false;
            }

            // CurrentProgramTitle:   Hubo Alguien   [track.song]
            // text:   Mark Anthony   [track.artist]
            // text:   Sep 2022  [track.date]
            // cue up 1st number as MP3   [track.mp3]
            new_mp3 = track?.mp3s_music;
            // display artist + song
            // auto add all current RANDOMs to playlist
            // start playing selected track
            // @ end auto-fetch RANDOM, add to playlistand auto-play it
        }

        foreach (TrackObject _tt in this.CurrentTrackList) {
            if (_tt != null) {
                if (_tt.id == track.id && _tt.offset == track.offset) {
                    _tt.background_class = "HighlightedRowStyle";
                    _tt.isCurrentRow = true;
                } else {
                    _tt.background_class = "DefaultRowStyle";
                    _tt.isCurrentRow = false;
                }
            }
        }

        //if ((type == RadioProgramType.RADIO) || (type == RadioProgramType.CD) || (trackObject == null) || (trackObject.songid <= 0)) {
        //    // assume this is an entire radio-program (tracks exclude entire shows ... no track probably means it's a show)
        //    Detail_line_1 = radioProgram.RecordedOn <= min_date ? "Entire Radio Program" : string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
        //    Detail_line_2 = "";
        //    Detail_line_3 = "";
        //    HasLine1 = true;
        //    HasLine2 = false;
        //    HasLine3 = false;
        //    Id = radioProgram.ID > 0 ? string.Format("{0}",radioProgram.ID) : "";

        //                         // *** TODO ***
        //    StartPosition = -1;  // could try setting to begin & end of first track
        //    EndPosition = -1;    // then DetailsPage::OnPositionChanged could wait for Position to
        //                         // move out of cuurent track and update StartPosition/EndPosition
        //                         // to next track ... plus change highlighting in list

        //    //if ((CurrentTrackList != null) && (CurrentTrackList.Count > 0)) {
        //    //    TrackObject ttrack = CurrentTrackList[0];
        //    //    if ((ttrack.offset >= 0) && (ttrack.nxtoffset > 0) && (ttrack.nxtoffset > ttrack.offset)) {
        //    //        StartPosition = ttrack.offset;
        //    //        EndPosition = ttrack.nxtoffset;
        //    //    }
        //    //}

        //    new_mp3 = radioProgram?.mp3;

        //} else {
        //    // assume this is a single track or song
        //    if (radioProgram.RecordedOn <= min_date) {
        //        Detail_line_1 = "" + trackObject.display_name;
        //        Detail_line_2 = "" + trackObject.artist;
        //        Detail_line_3 = "";
        //        HasLine1 = true;
        //        HasLine2 = true;
        //        HasLine3 = false;
        //    } else {
        //        Detail_line_1 = "" + trackObject.song;
        //        Detail_line_2 = "" + trackObject.artist;
        //        Detail_line_3 = string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
        //        HasLine1 = true;
        //        HasLine2 = true;
        //        HasLine3 = true;
        //    }
        //    Id = trackObject.id;

        //    StartPosition = trackObject.offset;
        //    EndPosition = trackObject.nxtoffset;

        //    new_mp3 = radioProgram?.mp3;
        //}

        //////// should have been already handled in caller

        //if (!String.IsNullOrWhiteSpace(new_mp3)) {
        //    if (new_mp3.ToLower().Contains("www.latincita.com/best") && !new_mp3.Contains("?")) {
        //        Mp3Url = new_mp3;  // triggers ProgramListService_PropertyChanged on RadioProgramDetailsViewModel
        //    }
        //}

        //PlayListItem playListItem = await addToPlaylist(track, wmaTyp.cMusic);

        //CurrentPlayListItem = playListItem;  // *** here is where it should start playing playListItem
    }

    public async void AddRandom(PlayListItem _Random)
    {
        // _Random is actually a PlayListItem with most fields empty
        //
        // AllLatincitaService has nearly all TrackObject's in the DB
        // get_track tries to identify the TrackObject for the random item
        //
        // the TrackObject can be then added to RadioProgramDetailsViewModel's PlayList
        // by converting it to a 'real' PlayListItem w/ TrackObject_to_PlayListItem

        RadioProgram radioProgram = new();

        radioProgram.ID = _Random.id;
        radioProgram.ArticleTitle = _Random.title;
        radioProgram.MP3URL = _Random.m4v;
        radioProgram.PictureURL = _Random.image.ImageFullURL;
    //  radioProgram.RecordedOn = ???   .... Random does not provide date !

        TrackObject _track = await AllLatincitaService.get_track(radioProgram);

        DateTime recorded_on = DateTime.MinValue;

        if ((_track != null) && !string.IsNullOrWhiteSpace(_track.month_year)) {
            recorded_on = DateTime.ParseExact(
                            _track.month_year,
                            "MMMM yyyy",
                            CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None);
        }

        radioProgram.RecordedOn = recorded_on;

        radioProgram.Type = RadioProgramType.RANDOM;

        await Task.Run(() =>
        {
            AddProgram(radioProgram, false);
            //                           ^^^^^ only when RANDOM is added from Detail scherm
            //                                 do we auto-select it
        });
    }

    public async Task<TrackObject> Goto_NextTrack()
    {
        TrackObject the_track = null;
        return the_track;
    }
    public async Task<TrackObject> Goto_PrevTrack()
    {
        TrackObject the_track = null;
        return the_track;
    }

    public async Task<TrackObject> MediaToPlaylist(MediaSource source, int offset)
    {
        TrackObject the_track = null;

        // try to find what is currently playing in CurrentTrackList

        // if it is a RADIO, we need to find the item using the current offset
        // else, we only need the URL

        foreach (TrackObject _track in this.CurrentTrackList) {
            PlayListItem _playListItem = _track.cached_playlist_item;
            if (_playListItem != null) {
                string _url = _playListItem.m4v;
                _url = HttpUtility.UrlDecode(_url);  // ChatGPT says undo url encoding

                bool url_matches = IsSameMediaUrlIgnoringDomain(source, _url);

                if (url_matches) {
                    // probably track in CurrentTrackList that is being played.
                    // check the offset
                    if (_track.offset < 0) {
                        the_track = _track;  // found it
                        break;
                    } else if ((_track.offset <= offset) && (_track.nxtoffset >= offset)) {
                        the_track = _track;  // found it
                        break;
                    }
                }
            }
        }
        return the_track;
    }

    public async Task<PlayListItem> addAllToPlayList ()
    {
        PlayListItem playListItem0 = null;

        foreach (TrackObject track in this.CurrentTrackList) {

            PlayListItem playListItem = await addToPlaylist(track, wmaTyp.cMusic);

            if ((playListItem != null) && !String.IsNullOrWhiteSpace(playListItem.m4v)) {

                track.cached_playlist_item = playListItem;

                await this.AudioPlaybackService.QueueTrackAsync(playListItem);  // add item to queue

                if (playListItem0 == null) {
                    playListItem0 = playListItem;
                }
            }
        }
        return playListItem0;
    }

    public async Task<PlayListItem> addToPlaylist(TrackObject track, wmaTyp useWma)
    {
        PlayListItem PlayListItem = new();

        Dictionary<int, TrackObject> theFsongs = await AllLatincitaService.GetFsongs();

        if (theFsongs == null || theFsongs.Count <= 0) {
            Debug.WriteLine("ERROR: no songs available to play");
            return null;
        }

        int num_fsongs = theFsongs.Count;

        int n = 0;
        for (int i2 = 1; i2 <= num_fsongs; i2++) {
            if (theFsongs[i2].id == track.id) {
                if (theFsongs[i2].mp3 == track.mp3) {
                    n = i2;
                    break;
                }
            }
        }
        if (n <= 0) {
            Debug.WriteLine($"ERROR: track id {track.id} [{track.mp3}] not found in theFsongs");
            return null;
        }

        var songidx = -1;    // theFsongs[songidx] is entire Radio
        // var sobj2 = null;

        bool playing_track = false;

        int radioid = -1;    // $$$ test playing track in Search & track in Radios
        int trackidx = -1;   // theFsongs[trackidx] is track on radio to play (default is 1st track)

        if ((track.is_track == true) && (track.radioid > 0) && (useWma == wmaTyp.cMusic)) {
            //  ------------------ if song[n] is a track, find the radio that contains it
            if (songidx > 0) {
                trackidx = songidx;
            } else {
                trackidx = n;
            }
            radioid = theFsongs[n].radioid;
            songidx = -1;
            for (int i3 = 1; i3 <= num_fsongs; i3++) {
                if (theFsongs[i3].songid == track.radioid) {
                    songidx = i3;  // radio that track belongs to
                    break;
                }
            }
            if (songidx < 0) {
                Debug.WriteLine($"ERROR: can't locate radio [{radioid}] for track ({n})");
                return null;
            } else {
                n = songidx;  // queue up radio "n"  &  request track "trackidx"
            }
            if (theFsongs[trackidx].is_track_object(CurrentType) == true) {
                playing_track = true;
            }
        } else if (useWma == wmaTyp.cMusic) {
            //                     in the past,playing an entire show highlighted tracks as they were played
            //                     as of Feb 2021, this no longer happens !!!   Try seeing if passing a track_list fixes this...
            //  ------------------ song[n] is not a track, but it could be a radio
            var track_num = -1;
            for (var i = 1; i <= num_fsongs; i++) {
                if ((theFsongs[i].is_track == true) && (theFsongs[i].tracknum > 0)) {
                    if (theFsongs[i].radioid == theFsongs[n].songid) {
                    //  if (radioid > 0) {
                        if (radioid <= 0) {
                            radioid = theFsongs[n].songid; // selected track IS a RADIO show
                            songidx = n;
                        }
                        if ((trackidx < 0) || (theFsongs[i].tracknum < track_num)) {
                            trackidx = i;  // select the 1st [visible]] track
                            track_num = theFsongs[i].tracknum;
                            if (theFsongs[trackidx].is_track_object(CurrentType) == true) {
                                playing_track = true;
                            }
                        }
                    }
                }
            }
            if (playing_track) {
                trackidx = -1;  // deselect 1st track & just play entire show
            }
        }

        string tmedia = theFsongs[n].mp3s[(int)useWma];
        if (string.IsNullOrWhiteSpace(tmedia) || extension(tmedia) == "" || tmedia == "<NONE>") {
            Debug.WriteLine($"ERROR: item [{n}] does not have a media-url");
            return null;
        }
        //  tmedia_fav = Curr_Favorite(n, useWma);

        var already_added = false;
        var found_id = -1;
        var playlist_idx = -1;
        var track_to_play = -1;

        if ((trackidx >= 0) && (trackidx != n)) {
            for (var j = 0; j < this.myPlaylist.Count; j++) {
                if ((this.myPlaylist[j].id == trackidx) && (this.myPlaylist[j].useWma == (int)useWma)) {
                    already_added = true;  // already have this track
                    found_id = trackidx;
                    playlist_idx = j;
                    break;
                }
            }
            // if we don't find track, go back and see if we can find track's radio and see if it already has this track
            for (var j = 0; j < this.myPlaylist.Count; j++) {
                if (myPlaylist[j].songid == radioid.ToString()) {
                    var toffs = theFsongs[trackidx].offset;
                    var tlist = myPlaylist[j].track_list;
                    for (var tt = 0; tt < tlist.Count; tt++) {
                        if (tlist[tt].offs == toffs) {
                            already_added = true;  // already have this track
                            found_id = myPlaylist[j].id;
                            playlist_idx = j;
                            track_to_play = tt + 1;
                            break;
                        }
                    }
                }
            }
        }
        if (found_id < 0) {
            for (var j = 0; j < myPlaylist.Count; j++) {
                if ((myPlaylist[j].id == n) && (myPlaylist[j].useWma == (int)useWma)) {
                    already_added = true;  // already have this one
                    found_id = n;
                    playlist_idx = j;
                    break;
                }
            }
        }

        List<TrackListItem> track_list = new();  // key = track_num
        //int video_to_play = -1;
        //List<TrackListItem> video_list = new();
        int tnum = 0;

        if (already_added == false) {
            if (playing_track) {
                // playing a track on RADIO/BANDS, find all the other tracks from the same show  >>> *** NOTE: give player all the tracks, but tell it to only play the one selected
                if (useWma == wmaTyp.cMusic) {
                    if (trackidx < 0) {
                        for (var i = num_fsongs; i >= 1; i--) {
                            if ((theFsongs[i].is_track == true) && (theFsongs[i].radioid == theFsongs[n].songid)) {
                                var track_num = theFsongs[i].tracknum;
                                var track_obj = new TrackListItem
                                {
                                    offs = theFsongs[i].offset,
                                    nxtoffs = theFsongs[i].nxtoffset,
                                    duration = theFsongs[i].duration,
                                    title = theFsongs[i].song,
                                    artist = theFsongs[i].artist,
                                    radioname = theFsongs[i].radioname,
                                    radioid = theFsongs[i].radioid.ToString(),
                                    trackid = i
                                };
                                track_list.Add(track_obj);
                            }
                        }
                        if (track_list.Count > 0) {
                            track_list.Sort((TrackListItem a, TrackListItem b) =>
                                            {   if (a.offs < b.offs) {
                                                    return -1;
                                                } else if (a.offs < b.offs) {
                                                    return 1;
                                                } else {
                                                    return 0;
                                                }
                                            });
                            if (trackidx > 0) {
                                for (int tt = 0; tt < track_list.Count; tt++) {
                                    if (track_list[tt].trackid == trackidx) {
                                        track_to_play = tt; break;
                                    }
                                }
                            }
                        }
                    } else {
                        // if one track was selected, only add that one track to track-list
                        var track_num = theFsongs[trackidx].tracknum;
                        var track_obj = new TrackListItem {
                            offs = theFsongs[trackidx].offset,
                            nxtoffs = theFsongs[trackidx].nxtoffset,
                            duration = theFsongs[trackidx].duration,
                            title = theFsongs[trackidx].song,
                            artist = theFsongs[trackidx].artist,
                            radioname = theFsongs[trackidx].radioname,
                            radioid = theFsongs[trackidx].radioid.ToString(),
                            trackid = trackidx
                        };
                        track_list.Add(track_obj);
                        tnum = 0;
                    //  track_to_play = 1;
                        track_to_play = tnum;
                    }
                }
            } else {
                // playing anything except for tracks on RADIO/BANDS
                if (trackidx >= 0) {
                    if ((useWma == wmaTyp.cMusic) && (theFsongs[trackidx].tracknum > 0) && (theFsongs[trackidx].offset >= 0) && (theFsongs[trackidx].duration > 0)) {
                        // if one track was selected, only add that one track to track-list
                        var track_obj = new TrackListItem {
                            offs = theFsongs[trackidx].offset,
                            nxtoffs = theFsongs[trackidx].nxtoffset,
                            duration = theFsongs[trackidx].duration,
                            title = theFsongs[trackidx].song,
                            artist = theFsongs[trackidx].artist, // theFsongs[trackidx].radioname
                            radioname = theFsongs[trackidx].radioname,
                            radioid = theFsongs[trackidx].radioid.ToString(),
                            trackid = trackidx
                        };
                        track_list.Add(track_obj);
                        tnum++;
                        //  track_to_play = 1;
                        track_to_play = tnum;
                    }
                }
            }
        }
        
        //var sobj2 = undefined;

        if ((useWma == wmaTyp.cReal) || (useWma == wmaTyp.cKaraoke)) {
            //var video_num = 0;
            //if (useWma == wmaTyp.cReal) {
            //    for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
            //        if ((theRsongs[i].query == theFsongs[n].query) || (theRsongs[i].radioid === theFsongs[n].songid)) {
            //            video_num++;
            //            //--------- look for whatever user currently has queued up, don't force jump to favorite
            //            //if ((tmedia_fav !== "") && (theRsongs[i].mp3[useWma] === tmedia_fav)) {
            //            //    sobj2 = theRsongs[i];   // video stored in theFsongs[n].mp3[useWma]
            //            //    video_to_play = video_num;
            //            //}
            //            if (theRsongs[i].mp3[useWma] == tmedia) {
            //                if (video_to_play < 0) {
            //                    sobj2 = theRsongs[i];   // video stored in theFsongs[n].mp3[useWma]
            //                    video_to_play = video_num;
            //                }
            //            }
            //            var video_obj = {
            //                    title: theRsongs[i].article_title, artist: theRsongs[i].artist, desc: theRsongs[i].critics_review, songid: theRsongs[i].songid,
            //                    poster: theRsongs[i].photo, media: theRsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
            //                };
            //            video_list[video_num] = video_obj;
            //        }
            //    }
            //} else if (useWma == wmaTyp.cKaraoke) {
            //    for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
            //        if ((theKsongs[i].query == theFsongs[n].query) || (theKsongs[i].radioid === theFsongs[n].songid)) {
            //            video_num++;
            //            //--------- look for whatever user currently has queued up, don't force jump to favorite
            //            //if ((tmedia_fav !== "") && (theKsongs[i].mp3[useWma] === tmedia_fav)) {
            //            //    sobj2 = theKsongs[i];   // video stored in theFsongs[n].mp3[useWma]
            //            //    video_to_play = video_num;
            //            //}
            //            if (theKsongs[i].mp3[useWma] == tmedia) {
            //                if (video_to_play < 0) {
            //                    sobj2 = theKsongs[i];  // video stored in theFsongs[n].mp3[useWma]
            //                    video_to_play = video_num;
            //                }
            //            }
            //            var video_obj = {
            //                        title: theKsongs[i].article_title, artist: theKsongs[i].artist, desc: theKsongs[i].critics_review, songid: theKsongs[i].songid,
            //                        poster: theKsongs[i].photo, media: theKsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
            //                    };
            //            video_list[video_num] = video_obj;
            //        }
            //    }
            //}
            Debug.WriteLine($"Videos are not yet supported");
            return null;
        }
        //if (sobj2 === undefined) {
        //    if (useWma === cReal) {
        //        notify_add_error(n, useWma, "youtube did not return a 'real' version of this song");
        //    } else {
        //        notify_add_error(n, useWma, "youtube did not return a 'karaoke' of this song");
        //    }
        //    return;
        //}

        if (!already_added) {
            if ((trackidx >= 0) && (trackidx != n) && (playing_track == false)) {
            //  showPlayState(trackidx, useWma, cAdded);
            } else if (playing_track == false) {
            //  showPlayState(n, useWma, cAdded);  // only set color when we know song was added
            }
        }
        var added_id = -1;
        var added_photo = "";
        var entire_radio = false;

        if (!already_added) {

            var play_now = false;  // track_to_play handles play_now below

            //if (sobj2 !== undefined) {
            //    added_id = n;
            //    added_photo = sobj2.photo;
            //    myPlaylist.add({
            //    title: sobj2.article_title,
            //            artist: sobj2.artist,
            //            m4v: tmedia,
            //            poster: sobj2.photo,
            //            useWma: useWma,
            //            entireRadio: entire_radio,
            //            songid: sobj2.songid,  // >= 10000 - see store_video
            //            id: n,
            //            track_list: track_list,
            //            video_list: video_list,
            //            image: { }
            //    }, play_now);
            //} else if (songIsVideo(tmedia)) {
            //    added_id = n;
            //    added_photo = theFsongs[n].photo;
            //    myPlaylist.add({
            //    title: theFsongs[n].song,
            //            artist: theFsongs[n].artist,
            //            m4v: tmedia,
            //            poster: theFsongs[n].photo,
            //            useWma: useWma,
            //            entireRadio: entire_radio,
            //            songid: theFsongs[n].songid,
            //            id: n,
            //            track_list: track_list,
            //            video_list: video_list,
            //            image: { }
            //    }, play_now);     //                               vvvv - before 8.75, only did this when playing_track was false
            //} else 
            if ((trackidx >= 0) && (trackidx != n)) { // && (playing_track === false)) {
                // *** could try to see if another track from this show is already added and just add this track to the list of tracks
                //     but for now, just queue up the track
                added_id = trackidx;
                added_photo = theFsongs[trackidx].photo;
                var track_obj = new PlayListItem
                {
                    title = theFsongs[trackidx].song,
                    artist = theFsongs[trackidx].artist,
                    m4v = tmedia,
                    //mp3 = tmedia,
                    //wav = theFsongs[trackidx].mp3[1],
                    poster = theFsongs[trackidx].photo,
                    useWma = (int)useWma,
                    entireRadio = entire_radio,
                    songid = theFsongs[trackidx].songid.ToString(),
                    id = trackidx,
                    track_list = track_list //,
                    //video_list = video_list,
                    //image = { }
                };
                myPlaylist.Add(track_obj); // , play_now);
                playlist_idx = myPlaylist.Count - 1;
            } else {
                entire_radio = playing_track && (trackidx < 0);
                added_id = n;
                added_photo = theFsongs[n].photo;
                var track_obj = new PlayListItem
                {
                    title = theFsongs[n].song,
                    artist = theFsongs[n].artist,
                    m4v = tmedia,
                    //mp3 = tmedia,
                    //wav = theFsongs[n].mp3[1],
                    poster = theFsongs[n].photo,
                    useWma = (int)useWma,
                    entireRadio = entire_radio,
                    songid = theFsongs[n].songid.ToString(),
                    id = n,
                    track_list = track_list //,
                    //video_list = video_list,
                    //image = { }
                };
                myPlaylist.Add(track_obj); // , play_now);
                playlist_idx = myPlaylist.Count - 1;
            }
        }

        if (added_photo != "") {
        //  fetchImage(added_photo);
        }
        if (already_added) {
            //if (video_list.length > 0) {
            //    if (playlist_idx >= 0) {
            //        myPlaylist.playlist[playlist_idx].video_list = video_list;
            //        track_to_play = playlist_idx; // *** how do we queue up selected video ?
            //    }
            //}
        }
        if ((useWma == wmaTyp.cMusic) && (added_id >= 0)) {
            //var imatches = (selIds.indexOf("|" + theFsongs[added_id].songid + "|") >= 0);
            //if (!imatches) {
            //    if (selIds === "") {
            //        selIds = "|";
            //    }
            //    selIds = selIds + theFsongs[added_id].songid + "|";
            //}
        }
        if ((already_added) && (track_to_play < 0) && !this.isPlaying) {
            if (found_id < 0) found_id = n;
            // TODO: skip this when adding all
            for (var j = 0; j < myPlaylist.Count; j++) {
                if ((myPlaylist[j].id == found_id) && (myPlaylist[j].useWma == (int)useWma)) {
                //  if (!isPlaying()) {
                    //   setTimeout(function() {
                    //                  myPlaylist.select(j);
                    //              }, removeWait); // give myPlaylist.remove(j) time to get processed
                //  }
                    break;
                }
            }
        } else {
            // only add to list, don't change current selection, and certainly don't stop something if it's playing
        }

        if ((track_to_play >= 0) && !this.isPlaying) {
        //  setTimeout(function() { myPlaylist.play_track(track_to_play); }, removeWait);
        }

        if (playlist_idx >= 0) {
            PlayListItem = this.myPlaylist[playlist_idx];
        }

        return PlayListItem;
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


    public string extension(string s)
    {
        if (!string.IsNullOrWhiteSpace(s)) {
            var p = s.LastIndexOf(".");
            if (p > 0) {
                return s.Substring(p + 1).ToLower();
            }
        }
        return "";
    }

    //public async TrackObject TrackObject_to_PlayListItem (RadioProgram program)
    //{
    //    TrackObject _track = await this.AllLatincitaService.get_track(program);

    //    return _track;
    //}

    public void ClearList()
    {
        CurrentRadioProgram = null;
        CurrentTrack = null;

        if (RadioPrograms.Count > 0)
            RadioPrograms.Clear();
    }
    public void AddToList(List<RadioProgram> _RadioPrograms)
    {
        foreach (var pgm in _RadioPrograms)
            RadioPrograms.Add(pgm);

        //if (_RadioPrograms.Count > 0)
        //    SetProgram(_RadioPrograms[0]);  // this method is only used to append RADIO programs to list
        //else
        //    SetProgram(null);
    }

    partial void OnCurrentTrackListNotEmptyChanged(bool value)
    {
        // properties are not Observable ... need to generate change
        OnPropertyChanged(nameof(ShowTrackListWithTimestamps));
        OnPropertyChanged(nameof(ShowTrackListWithNoTimestamps)); // wish order could be effected
    }
    partial void OnTrackListHasTimestampsChanged(bool value)
    {
        // properties are not Observable ... need to generate change
        OnPropertyChanged(nameof(ShowTrackListWithTimestamps));
        OnPropertyChanged(nameof(ShowTrackListWithNoTimestamps)); // wish order could be effected
    }
}
