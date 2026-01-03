//using AndroidX.Media3.Extractor.Mp4;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
//using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;
//using static Android.Icu.Text.CaseMap;

namespace LatincitaAndroid.Services;

public partial class ProgramListService : ObservableObject
{
    public ObservableCollection<RadioProgram> RadioPrograms { get; } = new();

    public ObservableCollection<string> CdList { get; } = new() {
        "Sola", "Echos from the Future", "Compilation 2010", "Gemstones", "Godess Latincita", "Caribbean Smile", "Idols"
    };

    [ObservableProperty]
    private RadioProgram currentRadioProgram;    // main-list

    [ObservableProperty]
    private TrackObject currentTrack;            // theFsongs[]

    [ObservableProperty]
    private TrackObject currentPlayListItem;     // jPlayerPlaylist.playlist item

    [ObservableProperty]
    private List<TrackObject> currentTrackList;

    [ObservableProperty]
    private bool currentTrackListNotEmpty;

    [ObservableProperty]
    private RadioProgramType currentType;  // RADIO / CD / TRACK

    [ObservableProperty]
    private string? mp3Url;

    [ObservableProperty]
    private int startPosition;

    [ObservableProperty]
    private int endPosition;

    [ObservableProperty]
    private bool auto_play;

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

    public ProgramListService(AllLatincitaService AllLatincitaService)
    {
        //  this.Title = this.radio_program.ArticleTitle;
        this.AllLatincitaService = AllLatincitaService;

        CurrentTrackListNotEmpty = ((CurrentTrackList != null) && (CurrentTrackList.Count > 0));
        StartPosition = 0;
        EndPosition = 0;
    }

    public void AddProgram(RadioProgram program)
    {
        RadioPrograms.Add(program);

        SetProgram(program);
    }
    public async void SetProgram(RadioProgram program)
    {
        CurrentRadioProgram = program;

        CurrentType = program.Type;

        TrackObject _track = await AllLatincitaService.get_track(program);

        if (program.Type == RadioProgramType.RADIO) {
            CurrentTrackList = await AllLatincitaService.get_radio_tracks(program);
        } else if (_track != null && _track.offset > 0) {
            CurrentTrackList = await AllLatincitaService.get_radio_tracks(_track);
        } else {
            CurrentTrackList = new();
        }
        CurrentTrackListNotEmpty = ((CurrentTrackList != null) && (CurrentTrackList.Count > 0));

        SetTrack(_track);
    }
    public void SetTrack(TrackObject track)
    {
        CurrentTrack = track;

        RadioProgram radioProgram = CurrentRadioProgram;
        TrackObject trackObject = CurrentTrack;
        RadioProgramType type = CurrentType;

        StartPosition = 0;
        EndPosition = 0;

        if (radioProgram is null) {

        //  Title = "Latincita Radio Programs";


            Detail_line_1 = "LATINCITA";
            Detail_line_2 = "";
            Detail_line_3 = "";
            HasLine1 = true;
            HasLine2 = false;
            HasLine3 = false;

            Auto_play = false;

            Mp3Url = "https://www.latincita.com" + "/best_of_latincita/Latincita%20Opener.mp3";

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
        // Title = radioProgram?.ArticleTitle ?? this.Title;
        DateTime min_date = new DateTime(2000, 1, 1);
        if ((type == RadioProgramType.RADIO) || (type == RadioProgramType.CD) || (trackObject == null) || (trackObject.songid <= 0)) {
            // assume this is an entire radio-program (tracks exclude entire shows ... no track probably means it's a show)
            Detail_line_1 = radioProgram.RecordedOn <= min_date ? "Entire Radio Program" : string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
            Detail_line_2 = "";
            Detail_line_3 = "";
            HasLine1 = true;
            HasLine2 = false;
            HasLine3 = false;
            Id = radioProgram.ID > 0 ? string.Format("{0}",radioProgram.ID) : "";

                                 // *** TODO ***
            StartPosition = -1;  // could try setting to begin & end of first track
            EndPosition = -1;    // then DetailsPage::OnPositionChanged could wait for Position to
                                 // move out of cuurent track and update StartPosition/EndPosition
                                 // to next track ... plus change highlighting in list

            //if ((CurrentTrackList != null) && (CurrentTrackList.Count > 0)) {
            //    TrackObject ttrack = CurrentTrackList[0];
            //    if ((ttrack.offset >= 0) && (ttrack.nxtoffset > 0) && (ttrack.nxtoffset > ttrack.offset)) {
            //        StartPosition = ttrack.offset;
            //        EndPosition = ttrack.nxtoffset;
            //    }
            //}

            Mp3Url = radioProgram?.mp3;  // triggers ProgramListService_PropertyChanged on RadioProgramDetailsViewModel
        } else {
            // assume this is a single track or song
            if (radioProgram.RecordedOn <= min_date) {
                Detail_line_1 = "" + trackObject.display_name;
                Detail_line_2 = "" + trackObject.artist;
                Detail_line_3 = "";
                HasLine1 = true;
                HasLine2 = true;
                HasLine3 = false;
            } else {
                Detail_line_1 = "" + trackObject.song;
                Detail_line_2 = "" + trackObject.artist;
                Detail_line_3 = string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
                HasLine1 = true;
                HasLine2 = true;
                HasLine3 = true;
            }
            Id = trackObject.id;

            StartPosition = trackObject.offset;
            EndPosition = trackObject.nxtoffset;

            Mp3Url = radioProgram?.mp3;  // triggers ProgramListService_PropertyChanged on RadioProgramDetailsViewModel
        }
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

        radioProgram.Type = RadioProgramType.TRACK;

        AddProgram(radioProgram);
    }

    public async Task<PlayListItem> TrackObject_to_PlayListItem(TrackObject track, wmaTyp useWma)
    {
        PlayListItem PlayListItem = new();

        Dictionary<string, TrackObject> AllLatincitaList = null;

        AllLatincitaList = await AllLatincitaService.GetAllLatincita();

        if (AllLatincitaList == null)
            return null;

        Dictionary<int, TrackObject> theFsongs = new();

        int i1 = 0;
        foreach (var _track in theFsongs.Values) {
            i1++;
            theFsongs.Add(i1, _track);
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

        var songidx = -1;
        // var sobj2 = null;

        if (n <= 0)
            return null;

        bool playing_track = false;

        int radioid = -1;    // $$$ test playing track in Search & track in Radios
        int trackidx = -1;
        if ((track.is_track == true) && (track.radioid > 0) && (useWma == wmaTyp.cMusic)) {
            //  ------------------ if song[n] is a track, find the radio that contains it
            if (songidx > 0) {
                trackidx = songidx;
            } else {
                trackidx = n;
            }
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
            if (theFsongs[trackidx].is_track_object == true) {
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
                        if (radioid > 0) {
                            radioid = theFsongs[n].songid; // selected track IS a RADIO show
                            songidx = n;
                        }
                        if ((trackidx < 0) || (theFsongs[i].tracknum < track_num)) {
                            trackidx = i;  // select the 1st track
                            track_num = theFsongs[i].tracknum;
                            playing_track = true;
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

        return PlayListItem;
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

        if (_RadioPrograms.Count > 0)
            SetProgram(_RadioPrograms[0]);  // this method is only used to append RADIO programs to list
        else
            SetProgram(null);
    }
}
