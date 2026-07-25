//using AndroidX.Media3.Extractor.Mp4;
//using IntelliJ.Lang.Annotations;
//using GoogleGson;
//using GoogleGson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;  // for TextFieldParser
//using static Android.Provider.MediaStore.Audio;
//using static AndroidX.Media3.Common.AdOverlayInfo;
#if ANDROID
using AndroidX.Media3.Extractor.Mp4;
#endif


namespace LatincitaAndroid.Services;
public class AllLatincitaService
{
    HttpClient httpClient;
    public AllLatincitaService()
    {
        this.httpClient = new HttpClient();
    }

    Dictionary<string,TrackObject> AllLatincitaList = new();
    Dictionary<string, TrackObject> RadioTrackList = new();
    Dictionary<string, TrackObject> CdTrackList = new();
    Dictionary<string, TrackObject> FavoriteTrackList = new();
    Dictionary<int, TrackObject> theFsongs = new();

    int num_fsongs = 0;

    public async Task<TrackObject> get_track(RadioProgram RadioProgram)
    {
        TrackObject empty_track = new();
        TrackObject track = null;

        if (RadioProgram.Type == RadioProgramType.RADIO || RadioProgram.Type == RadioProgramType.RANDOM) {
            // continue and see if we can find this one item in AllLatincitaList
        } else {
            return empty_track;
        }

        if ((this.AllLatincitaList == null) || (this.AllLatincitaList.Count <= 0)) {
            await this.GetAllLatincita();
        }
        if ((this.AllLatincitaList == null) || (this.AllLatincitaList.Count <= 0))
            return empty_track;
        if ((RadioProgram == null) || (RadioProgram.ID <= 0))
            return empty_track;
        string mp3 = RadioProgram.mp3;
        if (!string.IsNullOrEmpty(mp3)) {
            int p = mp3.LastIndexOf('/');
            if (p >= 0) 
                mp3 = mp3.Substring(p + 1);
        }
        //string sid = RadioProgram.ID.ToString();      --- RadioProgram.ID has nothing to do with AllLatincitaList[sid]
        //if (this.AllLatincitaList.ContainsKey(sid)) {
        //    track = this.AllLatincitaList[sid];
        //    if (track.article_title == RadioProgram.ArticleTitle && track.mp3.Contains(mp3, StringComparison.InvariantCultureIgnoreCase)) {
        //        return track;
        //    }
        //}
        // ID does not match, search for match on title & MP3 URL
        track = this.AllLatincitaList.Values.FirstOrDefault(v => v.article_title == RadioProgram.ArticleTitle && v.mp3.Contains(mp3, StringComparison.InvariantCultureIgnoreCase));

        if (track != null)
            return track;

        return empty_track;
    }

    // get all tracks from this RADIO or BAND
    public async Task<List<TrackObject>> get_radio_tracks(RadioProgram RadioProgram)
    {
        List<TrackObject> tracks = new();

        Dictionary<string, TrackObject> track_list = new Dictionary<string, TrackObject>();

        switch (RadioProgram.Type) {
            case RadioProgramType.RADIO:
                track_list = await this.GetAllRadioTracks(RadioProgram.ID.ToString());  // <<< *** is this radio_name ?
                break;
            case RadioProgramType.CD:
                track_list = await this.GetAllCdTracks(RadioProgram.ArticleTitle);  // <<< *** is this cd_name ?
                break;
            case RadioProgramType.FAVORITE:
                track_list = await this.GetAllFavoriteTracks(RadioProgram.ArticleTitle);  // <<< *** is this play_list ?
                break;
            default:
                track_list = await this.GetAllLatincita();
                break;
        }

        if ((track_list == null) || (track_list.Count <= 0))
            return tracks;

        if (RadioProgram.Type == RadioProgramType.RADIO) {

            if ((RadioProgram == null) || String.IsNullOrWhiteSpace(RadioProgram.ArticleTitle))
                return tracks;

            string mp3 = RadioProgram.mp3;
            if (!string.IsNullOrEmpty(mp3)) {
                int p = mp3.LastIndexOf('/');
                if (p >= 0)
                    mp3 = mp3.Substring(p + 1);
            }
            // ...backend handles filtering and sorting now
            // RadioName of each track == RadioProgram.ArticleTitle + MP3 is the same
            foreach (TrackObject track in track_list.Values) {
            //  if (track.radioname.Equals(RadioProgram.ArticleTitle, StringComparison.InvariantCultureIgnoreCase) &&
            //      track.mp3.Contains(mp3, StringComparison.InvariantCultureIgnoreCase)) {
                    tracks.Add(track);
            //  }
            }
        //  tracks.Sort((a, b) => a.offset.CompareTo(b.offset));
        } else {
            foreach (TrackObject track in track_list.Values) {
                tracks.Add(track);
            }
        }
        return tracks;
    }

    // if track is from a RADIO or BAND, get all tracks from that RADIO or BAND
    public async Task<List<TrackObject>> get_radio_tracks(TrackObject Track)
    {
        List<TrackObject> tracks = new();

        if ((this.AllLatincitaList == null) || (this.AllLatincitaList.Count <= 0)) {
            await this.GetAllLatincita();
        }
        if ((this.AllLatincitaList == null) || (this.AllLatincitaList.Count <= 0))
            return tracks;
        if ((Track == null) || (Track.radioid <= 0))
            return tracks;

        // RadioID of each track = RadioID of track provided
        foreach (TrackObject _track in this.AllLatincitaList.Values) {
            if ((Track.radioid == _track.radioid) && (_track.mp3 == Track.mp3)) {
                tracks.Add(_track);
            }
        }
        tracks.Sort((a, b) => a.offset.CompareTo(b.offset));

        return tracks;
    }

    public async Task<Dictionary<int, TrackObject>> GetFsongs()
    { 
        if ((this.theFsongs == null) || (this.theFsongs.Count <= 0))
           await this.GetAllLatincita();

        return this.theFsongs;
    }

    public async Task<Dictionary<string, TrackObject>> GetAllLatincita()
    {
        if ((this.AllLatincitaList != null) && (this.AllLatincitaList.Count > 0))
            return this.AllLatincitaList;

        this.AllLatincitaList = new Dictionary<string, TrackObject>();
        this.theFsongs = new Dictionary<int, TrackObject>();
        this.num_fsongs = 0;

        int num_songs = await FetchTrackList(RadioProgramType.ALL, "");

        return this.AllLatincitaList;
    }

    public async Task<Dictionary<string, TrackObject>> GetAllRadioTracks(string radio_name)
    {
        //if ((this.RadioTrackList!= null) && (this.RadioTrackList.Count > 0))  <<< can only do this if we are sure radio_name matches
        //    return this.RadioTrackList;

        this.RadioTrackList = new Dictionary<string, TrackObject>();

        int num_songs = await FetchTrackList(RadioProgramType.RADIO, radio_name);

        return this.RadioTrackList;
    }
    public async Task<Dictionary<string, TrackObject>> GetAllCdTracks(string cdname)
    {
        //if ((this.CdTrackList != null) && (this.CdTrackList.Count > 0))   <<< can only do this if we are sure cdname matches
        //    return this.CdTrackList;

        this.CdTrackList = new Dictionary<string, TrackObject>();

        int num_songs = await FetchTrackList(RadioProgramType.CD, cdname);

        return this.CdTrackList;
    }

    public async Task<Dictionary<string, TrackObject>> GetAllFavoriteTracks(string play_list)
    {
        //if ((this.FavoriteTrackList != null) && (this.FavoriteTrackList.Count > 0))  <<< can only do this if we are sure play_list matches
        //    return this.FavoriteTrackList;

        this.FavoriteTrackList = new Dictionary<string, TrackObject>();

        int num_songs = await FetchTrackList(RadioProgramType.FAVORITE, play_list);

        return this.FavoriteTrackList;
    }


    public async Task<int> FetchTrackList(RadioProgramType programType, string id)
    {
        Dictionary<string, int> offs_lookup = new();

////#if ANDROID
//        var handler = new AndroidMessageHandler();
//        handler.ServerCertificateCustomValidationCallback =
//            (req, cert, chain, errors) =>
//                req.RequestUri.Host == "www.latincita.com";

        //        var client = new HttpClient(handler);
        //        client.DefaultRequestVersion = HttpVersion.Version11;
        //        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        //        client.Timeout = TimeSpan.FromSeconds(30);

        ////#else
        ////      var client = new HttpClient();
        ////#endif

        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/csv"));

        int num_songs = 0;

        string fetch_what = "";

        string url = "";
        switch(programType) {
            case RadioProgramType.RADIO:
                url = "https://www.latincita.com/api/radio/" + id;
                fetch_what = "Radio \"" + id + "\"";
                break;
            case RadioProgramType.CD:
                url = "https://www.latincita.com/api/cd/" + id;
                fetch_what = "CD \"" + id + "\"";
                break;
            case RadioProgramType.FAVORITE:
                url = "https://www.latincita.com/api/play/" + id;
                fetch_what = "Favorite \"" + id + "\"";
                break;
            default:
                url = "https://www.latincita.com/api/all";
                fetch_what = "All Latincita";
                break;
        }
        Debug.WriteLine("| >>> " + fetch_what + "  URL: " + url);

        string csv = "";
        string error_message = "no data returned";

        try {
            using var response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            csv = await response.Content.ReadAsStringAsync();
        } catch (HttpRequestException ex) {
            // Server unavailable, DNS failure, HTTP error, etc.
            error_message = "HTTP: " + ex.Message;
            Debug.WriteLine($"| HTTP Error: {ex.Message}\n{ex.InnerException}");
        } catch (TaskCanceledException ex) {
            // Timeout (or cancellation)
            error_message = "Timeout";
            Debug.WriteLine($"| Timeout: {ex.Message}\n{ex.InnerException}");
        } catch (Exception ex) {
            // Anything unexpected
            error_message = ex.Message;
            Debug.WriteLine($"| Unexpected Error: {ex.Message}\n{ex.InnerException}");
        }

        if (!String.IsNullOrWhiteSpace(csv)) {

            List<Dictionary<string, string>> csv_data = ParseCsv(csv);

            foreach (Dictionary<string, string> row in csv_data) {
                TrackObjectCSV track_csv = new();
                foreach (string fld in row.Keys) {
                    var s = row[fld];
                    int n = 0;
                    bool is_encoded = false;
                    bool is_int = false;
                    switch (fld) {
                        case "BoxID":
                            break;
                        case "SongID":
                            is_int = true;
                            break;
                        case "recorded_on":
                            break;
                        case "released_on":
                            break;
                        case "old_or_new":
                            break;
                        case "created_with":
                            is_int = true;
                            break;
                        case "language":
                            break;
                        case "genre":
                            is_encoded = true;
                            break;
                        case "purpose":
                            break;
                        case "status":
                            is_int = true;
                            break;
                        case "smasterpiece":
                            break;
                        case "article_title":
                            is_encoded = true;
                            break;
                        case "song_title":
                            is_encoded = true;
                            break;
                        case "video_song":
                            is_encoded = true;
                            break;
                        case "artist":
                            is_encoded = true;
                            break;
                        case "original_song":
                            is_encoded = true;
                            break;
                        case "wma_url":
                            break;
                        case "song_url":
                            break;
                        case "MP3_name":
                            is_encoded = true;
                            break;
                        case "critics_review":
                            is_encoded = true;
                            break;
                        case "diary_text":
                            is_encoded = true;
                            break;
                        case "poster_url":
                            break;
                        case "TrackNumber":
                            is_int = true;
                            break;
                        case "RadioID":
                            is_int = true;
                            break;
                        case "RadioName":
                            is_encoded = true;
                            break;
                        case "soffset":
                            is_int = true;
                            break;
                        case "snxtoffset":
                            is_int = true;
                            break;
                        case "sduration":
                            is_int = true;
                            break;
                        case "stars":
                            is_int = true;
                            break;
                        case "real_url":
                            break;
                        case "karaoke_url":
                            break;
                        case "version_of_song":
                            break;
                        case "hide_if_not_on_radio":
                            break;
                        case "":
                            break;
                    }
                    if (string.IsNullOrWhiteSpace(s)) {
                        s = "";
                        n = -1;  // <<<< NOTE: set to -1 if string is empty !!!
                    } else {
                        if (is_encoded) {
                            //  text.Replace("\r\n", "<CR>").Replace("\n\r", "<CR>").Replace("\n", "<CR>").Replace("\r", "<CR>").Replace("\t", "<TAB>").Replace(",", "<COMMA>").Replace("'", "<QUOTE>");
                            s = s.Replace("&lt;CR&gt;", "\n").Replace("&lt;QUOTE&gt;", "'").Replace("&lt;COMMA&gt;", ",").Replace("&lt;TAB&gt;", "\t");
                            s = WebUtility.HtmlDecode(s);
                        }
                        if (is_int) {
                            if (!int.TryParse(s, out n)) {
                                n = 0;
                            }
                        }
                    }
                    switch (fld) {
                        case "BoxID":
                            track_csv.BoxID = s;
                            break;
                        case "SongID":
                            track_csv.SongID = n;
                            break;
                        case "recorded_on":
                            track_csv.recorded_on = s;
                            break;
                        case "released_on":
                            track_csv.released_on = s;
                            break;
                        case "old_or_new":
                            track_csv.old_or_new = s;
                            break;
                        case "created_with":
                            track_csv.created_with = n;
                            break;
                        case "language":
                            track_csv.language = s;
                            break;
                        case "genre":
                            track_csv.genre = s;
                            break;
                        case "purpose":
                            track_csv.purpose = s;
                            break;
                        case "status":
                            track_csv.status = n;
                            break;
                        case "smasterpiece":
                            if (s.ToUpper() == "TRUE") {
                                track_csv.masterpiece = true;
                            } else {
                                track_csv.masterpiece = false;
                            }
                            break;
                        case "article_title":
                            track_csv.article_title = s;
                            break;
                        case "song_title":
                            track_csv.song_title = s;
                            break;
                        case "video_song":
                            track_csv.video_song = s;
                            break;
                        case "artist":
                            track_csv.artist = s;
                            break;
                        case "original_song":
                            track_csv.original_song = s;
                            break;
                        case "wma_url":
                            track_csv.wma_url = s;
                            break;
                        case "song_url":
                            if (s.StartsWith("~"))
                                s = s.Replace("~", "https://www.latincita.com");
                            track_csv.song_url = s;
                            break;
                        case "MP3_name":
                            track_csv.MP3_name = s;
                            break;
                        case "critics_review":
                            track_csv.critics_review = s;
                            break;
                        case "diary_text":
                            track_csv.diary_text = s;
                            break;
                        case "poster_url":
                            if (s.StartsWith("~"))
                                s = s.Replace("~", "https://www.latincita.com");
                            track_csv.poster_url = s;
                            break;
                        case "TrackNumber":
                            track_csv.TrackNumber = n;
                            break;
                        case "RadioID":
                            if (s.Equals("Music by Latincita", StringComparison.InvariantCultureIgnoreCase)) {
                                track_csv.RadioID = 0; // Music by Latincita
                            } else {
                                track_csv.RadioID = n;
                            }
                            break;
                        case "RadioName":
                            track_csv.RadioName = s;
                            break;
                        case "soffset":
                            track_csv.soffset = n;
                            break;
                        case "snxtoffset":
                            track_csv.snxtoffset = n;
                            break;
                        case "sduration":
                            track_csv.sduration = n;
                            break;
                        case "stars":
                            track_csv.stars = n;
                            break;
                        case "real_url":
                            track_csv.real_url = s;
                            break;
                        case "karaoke_url":
                            track_csv.karaoke_url = s;
                            break;
                        case "version_of_song":
                            track_csv.version_of_song = s;
                            break;
                        case "hide_if_not_on_radio":
                            if (s == "" || s == "0") {
                                track_csv.hide_if_not_on_radio = false;
                            } else {
                                track_csv.hide_if_not_on_radio = true;
                            }
                            break;
                    }
                }
                if (track_csv.SongID > 0) {
                    bool is_trackobj = false;  // *** figure out how to determine this

                    //     when we are playing tracks of a SHOW (not random)
                    //     is_trackobj = true for all tracks in the show
                    //                        and false for the MP3 with the entire show

                    bool is_track = (is_trackobj || (track_csv.soffset >= 0));

                    //num_fsongs = 0;
                    //numSongs = 0;
                    //numTracks = 0;
                    //numBands = 0;
                    //numRadios = 0;

                    //var songList = $("[id^=SongListData]");
                    //var trackList = $("[id^=TrackListData]");
                    //var bandList = $("[id^=BandListData]");
                    //var radioList = $("[id^=RadioListData]");

                    //numSongs = songList.length;
                    //numTracks = trackList.length;
                    //numBands = bandList.length;
                    //numRadios = radioList.length;

                    //if (numTracks > 0) {
                    //    $.merge(songList, trackList);
                    //}
                    //if (numBands > 0) {
                    //    $.merge(songList, bandList);
                    //}
                    //if (numRadios > 0) {
                    //    $.merge(songList, radioList);
                    //}
                    //var isTrackObj = ((index >= numSongs) && (index < (numSongs + numTracks)));
                    //var isTrack = isTrackObj;  // or offset > 0

                    num_songs += 1;

                    TrackObject track = new TrackObject(track_csv, is_trackobj, is_track, offs_lookup);

                    switch (programType) {
                        case RadioProgramType.RADIO:
                            this.RadioTrackList.Add(track.id, track);
                            break;
                        case RadioProgramType.CD:
                            this.CdTrackList.Add(track.id, track);
                            break;
                        case RadioProgramType.FAVORITE:
                            this.FavoriteTrackList.Add(track.id, track);
                            break;
                        default:
                            this.AllLatincitaList.Add(track.id, track);
                            //                        ^^^^^^^^ we finally have REAL song-id's !!
                            this.theFsongs.Add(++this.num_fsongs, track);
                            break;
                    }
                }
            }
        } else {
            await MainThread.InvokeOnMainThreadAsync(() =>
                    Shell.Current.DisplayAlert($"Error fetching {fetch_what}", error_message, "OK"));
            //await dialogService.ShowAlertAsync(
            //    "Unable to contact the server. Please check your Internet connection and try again.");
        }

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        // -------------------------------- there is no sort, need to do sorting in backend

        //if (programType == RadioProgramType.RADIO) {
        //        this.RadioTrackList.Sort((TrackListItem a, TrackListItem b) =>
        //                                    {
        //                                        if (a.offs < b.offs) {
        //                                            return -1;
        //                                        } else if (a.offs < b.offs) {
        //                                            return 1;
        //                                        } else {
        //                                            return 0;
        //                                        }
        //                                    });
        //}

        return num_songs;
    }

    private List<Dictionary<string, string>> ParseCsv(string csv)
    {
        var result = new List<Dictionary<string, string>>();

        if (csv.StartsWith("\"")) {
            csv = csv.Substring(1);
            if (csv.EndsWith("\"")) {
                csv = csv.Substring(0, csv.Length - 1);
            }
        }
        csv = csv.Replace("\\r\\n", "\n");
        csv = csv.Replace("\\r", "\n");
        csv = csv.Replace("\\n", "\n");
        csv = csv.Replace("\\\"", "\"");
        csv = csv.Replace("\\\\", "\\");
        if (!csv.StartsWith("BoxID")) {
            csv = "BoxID,SongID,recorded_on,released_on,old_or_new,created_with,language,genre,purpose,status,smasterpiece,article_title,song_title,video_song,artist,original_song,wma_url,song_url,MP3_name,critics_review,diary_text,poster_url,TrackNumber,RadioID,RadioName,soffset,snxtoffset,sduration,stars,real_url,karaoke_url,version_of_song,hide_if_not_on_radio" + "\n" + csv;
        }

        using var reader = new StringReader(csv);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            Delimiters = new[] { "," },
            HasFieldsEnclosedInQuotes = false,
            TrimWhiteSpace = false
        };

        // Read header row
        if (parser.EndOfData)
            return result;

        var headers = parser.ReadFields();
        if (headers == null)
            return result;

        // Read data rows
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null)
                continue;

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Length && i < fields.Length; i++)
            {
                row[headers[i]] = fields[i];
            }

            result.Add(row);
        }

        return result;
    }

}
