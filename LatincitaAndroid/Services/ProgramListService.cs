//using AndroidX.Media3.Extractor.Mp4;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static Android.Icu.Text.CaseMap;

namespace LatincitaAndroid.Services;

public partial class ProgramListService : ObservableObject
{
    public ObservableCollection<RadioProgram> RadioPrograms { get; } = new();

    public ObservableCollection<string> CdList { get; } = new() {
        "Sola", "Echos from the Future", "Compilation 2010", "Gemstones", "Godess Latincita", "Caribbean Smile", "Idols"
    };

    [ObservableProperty]
    private RadioProgram currentRadioProgram;

    [ObservableProperty]
    private TrackObject currentTrack;

    [ObservableProperty]
    private List<TrackObject> currentTrackList;

    [ObservableProperty]
    private bool currentTrackListNotEmpty;

    [ObservableProperty]
    private RadioProgramType currentType;  // RADIO / CD / TRACK

    [ObservableProperty]
    private string? mp3;

    [ObservableProperty]
    private int startPosition;

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

        CurrentTrackListNotEmpty = (CurrentTrackList != null) && (CurrentTrackList.Count > 0);
        StartPosition = 0;
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
        } else if (_track != null && _track.soffset > 0) {
            CurrentTrackList = await AllLatincitaService.get_radio_tracks(_track);
        } else {
            CurrentTrackList = new();
        }
        CurrentTrackListNotEmpty = (CurrentTrackList != null) && (CurrentTrackList.Count > 0);

        SetTrack(_track);
    }
    public void SetTrack(TrackObject track)
    {
        CurrentTrack = track;

        RadioProgram radioProgram = CurrentRadioProgram;
        TrackObject trackObject = CurrentTrack;
        RadioProgramType type = CurrentType;

        StartPosition = 0;

        if (radioProgram is null) {

        //  Title = "Latincita Radio Programs";

            Mp3 = "https://www.latincita.com" + "/best_of_latincita/Latincita%20Opener.mp3";

            Detail_line_1 = "LATINCITA";
            Detail_line_2 = "";
            Detail_line_3 = "";
            HasLine1 = true;
            HasLine2 = false;
            HasLine3 = false;

            if (trackObject.article_title == "Best of Latincita")
                return;  // no infinite loop

            trackObject = new();
            trackObject.article_title = "Best of Latincita";
            trackObject.artist = "Latincita";
            trackObject.poster_url = "";

            Id = "";

            SetTrack(trackObject);

            return;
        }
     // Title = radioProgram?.ArticleTitle ?? this.Title;
        Mp3 = radioProgram?.mp3;
        DateTime min_date = new DateTime(2000, 1, 1);
        if ((type == RadioProgramType.RADIO) || (type == RadioProgramType.CD) || (trackObject == null) || (trackObject.SongID <= 0)) {
            // assume this is an entire radio-program (tracks exclude entire shows ... no track probably means it's a show)
            Detail_line_1 = radioProgram.RecordedOn <= min_date ? "Entire Radio Program" : string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
            Detail_line_2 = "";
            Detail_line_3 = "";
            HasLine1 = true;
            HasLine2 = false;
            HasLine3 = false;
            Id = radioProgram.ID > 0 ? string.Format("{0}",radioProgram.ID) : "";
        } else {
            // assume this is a single track or song
            if (radioProgram.RecordedOn <= min_date) {
                Detail_line_1 = "" + trackObject.song_title;
                Detail_line_2 = "" + trackObject.artist;
                Detail_line_3 = "";
                HasLine1 = true;
                HasLine2 = true;
                HasLine3 = false;
            } else {
                Detail_line_1 = "" + trackObject.song_title;
                Detail_line_2 = "" + trackObject.artist;
                Detail_line_3 = string.Format("{0:MMMM yyyy}", radioProgram.RecordedOn);
                HasLine1 = true;
                HasLine2 = true;
                HasLine3 = true;
            }
            Id = trackObject.SongID > 0 ? string.Format("{0}", trackObject.SongID) : "";

            StartPosition = trackObject.soffset;
        }
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
