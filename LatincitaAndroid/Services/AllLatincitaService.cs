//using AndroidX.Media3.Extractor.Mp4;
//using IntelliJ.Lang.Annotations;
//using GoogleGson;
//using GoogleGson;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
//using static Android.Provider.MediaStore.Audio;
//using static AndroidX.Media3.Common.AdOverlayInfo;


namespace LatincitaAndroid.Services;
public class AllLatincitaService
{
    HttpClient httpClient;
    public AllLatincitaService()
    {
        this.httpClient = new HttpClient();
    }

    Dictionary<int,TrackObject> AllLatincitaList;

    public async Task<TrackObject> get_track(RadioProgram RadioProgram)
    {
        TrackObject empty_track = new();
        TrackObject track = null;

        if ((AllLatincitaList == null) || (AllLatincitaList.Count <= 0)) {
            await this.GetAllLatincita();
        }
        if ((AllLatincitaList == null) || (AllLatincitaList.Count <= 0))
            return empty_track;
        if ((RadioProgram == null) || (RadioProgram.ID <= 0))
            return empty_track;
        string mp3 = RadioProgram.mp3;
        if (!string.IsNullOrEmpty(mp3)) {
            int p = mp3.LastIndexOf('/');
            if (p >= 0) 
                mp3 = mp3.Substring(p + 1);
        }
        if (AllLatincitaList.ContainsKey(RadioProgram.ID)) {
            track = AllLatincitaList[RadioProgram.ID];
            if (track.article_title == RadioProgram.ArticleTitle && track.song_url.Contains(mp3,StringComparison.InvariantCultureIgnoreCase)) {
                return track;
            }
        }
        // ID does not match, search for match on title & MP3 URL
        track = AllLatincitaList.Values.FirstOrDefault(v => v.article_title == RadioProgram.ArticleTitle && v.song_url.Contains(mp3, StringComparison.InvariantCultureIgnoreCase));

        if (track != null)
            return track;

        return empty_track;
    }
    public async Task<List<TrackObject>> get_radio_tracks(RadioProgram RadioProgram)
    {
        List<TrackObject> tracks = new();

        if ((AllLatincitaList == null) || (AllLatincitaList.Count <= 0)) {
            await this.GetAllLatincita();
        }
        if ((AllLatincitaList == null) || (AllLatincitaList.Count <= 0))
            return tracks;
        if ((RadioProgram == null) || String.IsNullOrWhiteSpace(RadioProgram.ArticleTitle))
            return tracks;

        string mp3 = RadioProgram.mp3;
        if (!string.IsNullOrEmpty(mp3)) {
            int p = mp3.LastIndexOf('/');
            if (p >= 0)
                mp3 = mp3.Substring(p + 1);
        }
        foreach (TrackObject track in AllLatincitaList.Values) {
            if (track.RadioName.Equals(RadioProgram.ArticleTitle, StringComparison.InvariantCultureIgnoreCase) && track.song_url.Contains(mp3, StringComparison.InvariantCultureIgnoreCase)) {
                tracks.Add(track);
            }
        }
        tracks.Sort((a, b) => a.soffset.CompareTo(b.soffset));

        return tracks;
    }


    public async Task<Dictionary<int, TrackObject>> GetAllLatincita()
    {
        if ((AllLatincitaList != null) && (AllLatincitaList.Count > 0))
            return AllLatincitaList;

        AllLatincitaList = new Dictionary<int, TrackObject>();

        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("text/csv"));

        // Online
        var response = await httpClient.GetAsync("https://www.latincita.com/api/AllLatincita");
        if (response.IsSuccessStatusCode)
        {
            var csv = await response.Content.ReadAsStringAsync();

            List<Dictionary<string, string>> csv_data = ParseCsv(csv);

            foreach (Dictionary<string, string> row in csv_data) {
                TrackObject track = new();
                foreach (string fld in row.Keys) {
                    var s = row[fld];
                    int n = 0;
                    bool is_encoded = false;
                    bool is_int = false;
                    switch(fld) {
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
                        n = 0;
                    } else {
                        if (is_encoded) {
                            //  text.Replace("\r\n", "<CR>").Replace("\n\r", "<CR>").Replace("\n", "<CR>").Replace("\r", "<CR>").Replace("\t", "<TAB>").Replace(",", "<COMMA>").Replace("'", "<QUOTE>");
                            s = s.Replace("&lt;CR&gt;", "\n").Replace("&lt;QUOTE&gt;","'").Replace("&lt;COMMA&gt;", ",").Replace("&lt;TAB&gt;", "\t");
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
                            track.BoxID = s;
                            break;
                        case "SongID":
                            track.SongID = n;
                            break;
                        case "recorded_on":
                            track.recorded_on = s;
                            break;
                        case "released_on":
                            track.released_on = s;
                            break;
                        case "old_or_new":
                            track.old_or_new = s;
                            break;
                        case "created_with":
                            track.created_with = n;
                            break;
                        case "language":
                            track.language = s;
                            break;
                        case "genre":
                            track.genre = s;
                            break;
                        case "purpose":
                            track.purpose = s;
                            break;
                        case "status":
                            track.status = n;
                            break;
                        case "smasterpiece":
                            if (s.ToUpper() == "TRUE") {
                                track.masterpiece = true;
                            } else {
                                track.masterpiece = false;
                            }
                            break;
                        case "article_title":
                            track.article_title = s;
                            break;
                        case "song_title":
                            track.song_title = s;
                            break;
                        case "video_song":
                            track.video_song = s;
                            break;
                        case "artist":
                            track.artist = s;
                            break;
                        case "original_song":
                            track.original_song = s;
                            break;
                        case "wma_url":
                            track.wma_url = s;
                            break;
                        case "song_url":
                            if (s.StartsWith("~"))
                                s = s.Replace("~","https://www.latincita.com");
                            track.song_url = s;
                            break;
                        case "MP3_name":
                            track.MP3_name = s;
                            break;
                        case "critics_review":
                            track.critics_review = s;
                            break;
                        case "diary_text":
                            track.diary_text = s;
                            break;
                        case "poster_url":
                            if (s.StartsWith("~"))
                                s = s.Replace("~", "https://www.latincita.com");
                            track.poster_url = s;
                            break;
                        case "TrackNumber":
                            track.TrackNumber = n;
                            break;
                        case "RadioID":
                            if (s == "Music by Latincita") {
                                track.RadioID = 0; // Music by Latincita
                            } else {
                                track.RadioID = n;
                            }
                            break;
                        case "RadioName":
                            track.RadioName = s;
                            break;
                        case "soffset":
                            track.soffset = n;
                            break;
                        case "snxtoffset":
                            track.snxtoffset = n;
                            break;
                        case "sduration":
                            track.sduration = n;
                            break;
                        case "stars":
                            track.stars = n;
                            break;
                        case "real_url":
                            track.real_url = s;
                            break;
                        case "karaoke_url":
                            track.karaoke_url = s;
                            break;
                        case "version_of_song":
                            track.version_of_song = s;
                            break;
                        case "hide_if_not_on_radio":
                            if ("hide_if_not_on_radio" == "" || "hide_if_not_on_radio" == "0") {
                                track.hide_if_not_on_radio = false;
                            } else {
                                track.hide_if_not_on_radio = true;
                            }
                            break;
                    }
                }
                if (track.SongID > 0) {
                    AllLatincitaList.Add(track.SongID, track);
                }
            }
        }

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        return AllLatincitaList;
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
