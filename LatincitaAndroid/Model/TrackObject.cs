//using NaturalLanguage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
#if ANDROID
using static Android.Provider.MediaStore;
using static AndroidX.ConstraintLayout.Core.Motion.Utils.HyperSpline;
using static AndroidX.Media3.Common.AdOverlayInfo;
using Android.Media;
using Android.Net;
#endif
//using Android.App.Admin;

namespace LatincitaAndroid.Model;

// emulates comma-sepatarated list sent to Music_Search  (AllLatincita)

// convert these to TrackObject (as filter.mobile does)
// copy these to PlayListItem's and push to Detail-Page

public class TrackObjectCSV  // objects sent to Music_Search => converted into TrackObject
{
    public string BoxID { get; set; }         // 00  box_id 
    public int SongID { get; set; }           // 01  song_id 
    public string recorded_on { get; set; }   // 02  month_year 
    public string released_on { get; set; }   // 03  rel_mo_yr 
    public string old_or_new { get; set; }    // 04
    public int created_with { get; set; }     // 05
    public string language { get; set; }      // 06  lang 
    public string genre { get; set; }         // 07  genre
    public string purpose { get; set; }       // 08  purpose 
    public int status { get; set; }           // 09
    public bool masterpiece { get; set; }     // 10  mesterwerk 
    public string article_title { get; set; } // 11  article_title 
    public string song_title { get; set; }    // 12  song 
    public string video_song { get; set; }    // 13  video_song 
    public string artist { get; set; }        // 14  song_id 
    public string original_song { get; set; } // 15
    public string wma_url { get; set; }       // 16  wma 
    public string song_url { get; set; }      // 17  mp3 
    public string MP3_name { get; set; }      // 18
    public string critics_review { get; set; }// 19  critics_review
    public string diary_text { get; set; }    // 20  diary_text
    public string poster_url { get; set; }    // 21  photo 
    public int TrackNumber { get; set; }      // 22  tracknum 
    public int RadioID { get; set; }          // 23  tracknum 
    public string RadioName { get; set; }     // 24  radio_name 
    public int soffset { get; set; }          // 25  soffset
    public int snxtoffset { get; set; }       // 26  soffset
    public int sduration { get; set; }        // 27  sduration 
    public int stars { get; set; }            // 28  stars 
    public string real_url { get; set; }      // 29  real_url 
    public string karaoke_url { get; set; }   // 30  karaoke_url 
    public string version_of_song { get; set; }// 31 version_of_song 
    public bool hide_if_not_on_radio { get; set; } // 32

    //   private string _curr_mp3 = "";

    //   public string mp3
    //   {
    //       get
    //       {
    //           string _mp3 = this.song_url;
    //           int p = this.song_url.IndexOf("#");
    //           if (p > 0)
    //               _mp3 = this.song_url.Substring(0,p);
    //           if (_mp3 != this._curr_mp3) {
    //               _curr_mp3 = _mp3;
    ////             OnPropertyChanged();
    //           //  OnMp3Changed?.Invoke(_mp3);
    //           }
    //           return _mp3;
    //       }
    //   }
    //   public string start_time
    //   {
    //       get
    //       {
    //           string s = TimeSpan
    //                      .FromSeconds(this.soffset)
    //                      .ToString(@"hh\:mm\:ss");
    //           return s;
    //       }
    //   }
}

public enum wmaTyp
{
    cMusic = 0,
    cKaraoke = 1,
    cVideo = 1,
    cReal = 2,
    cHdAudio = 3
}

public enum  playedTyp
{
    cNotPlayed = 0,
    cAdded = 1,      // 0            1        2           3          4    
    cPlaying = 2,    // cNotPlayed > cAdded > cPlaying  > cStopped > cRemoved
    cPlayed = 3,     // cNotPlayed > cQueued > cWaiting  > cEnabled | cDead
    cRemoved = 4,
    cDisabled = 5,   // button disabled all together >> should set for Radio's
    cEnabled = 6,    // query returned usable data
    cWaiting = 7,    // waiting for query to return
    cQueued = 8,     // query pushed onto request list, waiting to be sent to YouTube
    cDead = -1       // query returned an error or nothing
}

public static class TrackConstants
{
    public static readonly string BaseUrl = "https://example.com/"; // replace as needed
    public static readonly string bNotArrived = "Query results have not yet arrived.";
    public static readonly string bNoResults = "Query returned nothing.";
    public static readonly string bNoUseful = "Query returned nothing useful.";
    public static readonly string bNoError = "<OK>";
}

// emulates fTrackObject's in theFsongs[] in filter_mobile
// read from list sent to Music_Search  (AllLatincita)

// copy these to PlayListItem's and push to Detail-Page

public class TrackObject
{
    public string id { get; set; } = "";    // 526.01   ... if csv.TrackNumber > 0 then
                                            //                   id = csv.RadioID + "." + csv.TrackNumber
                                            //                   csv.SongID 1821 : RadioID: 526  TrackNumber: 1  => id = 526.01
                                            //              else   id = csv.SongID
    public string display_name { get; set; } = "";  // 
    public string boxid { get; set; } = "";
    public int songid { get; set; } = -1;
    public int radioid { get; set; } = -1;
    public string radioname { get; set; } = "";
    public int tracknum { get; set; } = -1;
    public string artist { get; set; } = "";
    public string song { get; set; } = "";
    public string video_song { get; set; } = "";
    public string sartist { get; set; } = "";
    public string ssong { get; set; } = "";
    public string svideo_song { get; set; } = "";
    public string genre { get; set; } = "";
    public string language { get; set; } = "";
    public string purpose { get; set; } = "";  // BAND / RADIO / other
    public string month_year { get; set; } = "";
    public string rel_mo_yr { get; set; } = "";   // only for videos
    public bool meesterwerk { get; set; } = false;
    public int stars { get; set; } = 0;
    public bool is_track { get; set; } = false;
    public bool is_track_object { get; set; } = false;
    public bool is_live { get; set; } = false;
    public string article_title { get; set; } = "";
    public string critics_review { get; set; } = "";
    public string diary_text { get; set; } = "";
    public string[] mp3s { get; set; } = new string[4];
    public string photo { get; set; } = "";
    public string[] playeds { get; set; } = new string[4];
    public string[] load_errors { get; set; } = new string[4];
    public string query { get; set; } = "";
    public string youtube_query { get; set; } = "";
    public int offset { get; set; } = -1;
    public int nxtoffset { get; set; } = -1;
    public int duration { get; set; } = -1;

    Dictionary<string, int> offs_lookup = null;

    private string _curr_mp3 = "";

    public string mp3
    {
        get
        {
            string _mp3 = this.mp3s[(int)wmaTyp.cMusic];

            if (_mp3 != this._curr_mp3) {
                _curr_mp3 = _mp3;
            //  OnPropertyChanged();
                OnMp3Changed?.Invoke(_mp3);   // *** HOW IS THIS SUPPOSED TO WORK NOW ??
            }
            return _mp3;
        }
    }
    public string start_time
    {
        get
        {
            string s = TimeSpan
                       .FromSeconds(this.offset)
                       .ToString(@"hh\:mm\:ss");
            return s;
        }
    }
    public bool isCurrentRow { get; set; }
    public string background_class { get; set; }

    public Action<string>? OnMp3Changed;

    public TrackObject()
    {
    }
    public TrackObject(TrackObjectCSV csv, bool is_trackobj, bool is_track, Dictionary<string, int> _offs_lookup)
    {
        this.offs_lookup = _offs_lookup;

        this.id = csv.SongID < 0 ? "" : string.Format("{0}", csv.SongID);
        this.boxid = csv.BoxID;
        this.songid = csv.SongID;
        this.radioid = csv.RadioID;
        this.radioname = csv.RadioName;
        this.tracknum = csv.TrackNumber;
        this.artist = csv.artist;
        this.song = csv.song_title; // song_title == song
        this.video_song = csv.video_song;
        this.sartist = RemoveAccents(csv.artist.Trim()).ToLower();
        this.ssong = RemoveAccents(csv.song_title.Trim()).ToLower(); // song_title == tsong ???
        this.svideo_song = RemoveAccents(csv.video_song.Trim()).ToLower();
        this.genre = csv.genre;
        this.purpose = csv.purpose;
        this.language = csv.language;
        this.month_year = csv.recorded_on;  // recorded_on == tmonth_year
        this.rel_mo_yr = csv.released_on;   // released_on == trel_mo_yr
        this.meesterwerk = csv.masterpiece;

        //    LIVE = [Radio Contents].[Version] == "Live" OR [Latincitas Songs].[Purpose] == "BAND"

        //    [Radio Contents].[Version] => [Version of Song] => version_of_song => 31
        //    [Latincitas Songs].[Purpose] => Purpose => purpose => 8

        if (csv.version_of_song.ToUpper() == "LIVE" || csv.purpose.ToUpper() == "BAND") {
            this.is_live = true;
        } else {
            this.is_live = false;
        }

        this.article_title = csv.article_title;
        this.critics_review = csv.critics_review;
        this.diary_text = csv.diary_text;

        this.mp3s[(int)wmaTyp.cMusic] = CleanURL(csv.song_url);  // song_url = tmp3 ???

        if (!string.IsNullOrWhiteSpace(csv.wma_url)) {
            if (csv.wma_url.IndexOf(".wav") > 0 || csv.wma_url.IndexOf(".wma") > 0) {
                this.mp3s[(int)wmaTyp.cHdAudio] = CleanURL(csv.wma_url);
            } else {
                this.mp3s[(int)wmaTyp.cVideo] = CleanURL(csv.wma_url); // stores both karaoke's and videos
            }
        }

        if (songIsVideo(this.mp3s[(int)wmaTyp.cMusic])) {
            this.meesterwerk = true;  // bug... all videos seem to have this set to false
        }

        this.photo = CleanURL(csv.poster_url);  // poster_url = tphoto ???
        this.is_track_object = is_trackobj;
        this.is_track = is_track;
        this.stars = csv.stars < 0 ? 0 : csv.stars;

        if (csv.soffset >= 0) {
            this.offset = csv.soffset; // already converted to integer
            this.nxtoffset = csv.snxtoffset; // already converted to integer
            this.duration = csv.sduration; // already converted to integer
            var sradioid = string.Format("{0}", csv.RadioID);
            var itracknum = csv.TrackNumber; // already converted to integer
            var sntracknum = string.Format("{0}", itracknum);
            var key = "x_" + sradioid + "_" + sntracknum;

            this.offs_lookup.Add(key,this.offset);  // hopefully won't need this anymore
        } else {
            this.offset = -1;
            this.nxtoffset = -1;
            this.duration = -1;
        }
        if (this.offset >= 0) {
            this.is_track = true;   // if it has an offset mark it as a track
        } // offset = 0 (first track) ...caller must do this

        if (this.is_track) {
            if ((this.radioid > 0) && (this.tracknum > 0)) {
                double songid_num = 0.0;
                if (!double.TryParse(this.id, out songid_num)) {
                    songid_num = 0.0;
                }
                if ((songid_num > 1000) && (songid_num < 3000)) {
                    // songid was already collision proofed by caller (see CollectioLoader::Bind_SongItom)
                } else {
                    this.id = string.Format("{0:0}.{1:00}", this.radioid, this.tracknum);
                }
            }
        } else {
            double songid_num2 = 0.0;
            if (!double.TryParse(this.id, out songid_num2)) {
                songid_num2 = 0.0;
            }
            if ((songid_num2 > 1000) && (songid_num2 < 3000)) {
                this.is_track = true;   // only tracks are given this special songid
            }
        }
        var dname = this.song + " / " + this.artist;
        if (!string.IsNullOrWhiteSpace(this.month_year)) {
            dname += " [" + this.month_year + "]";
        }
        dname += " (#" + this.id + ") ";
        this.display_name = dname;
    }
    public int next_Offset()
    {
        if (this.offset < 0) {
            return -1;
        }
        int nxtoffs = -1;
        string sradioid = string.Format("{0}", this.radioid);
        for (var i = 1; i <= 100; i++) {
            int itracknum = this.tracknum + i;
            string sntracknum = string.Format("{0}",itracknum);
            var key = "x_" + sradioid + "_" + sntracknum;
            if (this.offs_lookup.ContainsKey(key)) {
                nxtoffs = this.offs_lookup[key];
                if (nxtoffs > 0) {
                    break;
                } // else skip unmarked track
            }
        }
        ////////////////////////////////// if offset >= 0 and nxtoffs < 0, then "this" is the last track
        //  if (nxtoffs < 0) {
        //      nxtoffs = 32000;  // assume "this" is last track, set next-offset to infinite
        //  } 
        return nxtoffs;
    }

    public static bool songIsVideo(string tmedia)
    {
        var ext = extension(tmedia);
        if (tmedia.IndexOf("youtu.be") > 0 || tmedia.IndexOf(".youtube.") > 0 || ext == "mp4" || ext == "flv" || ext == "wmv") {
            return true;
        }
        return false;
    }
    public static string extension(string s)
    {
        if (!string.IsNullOrWhiteSpace(s)) {
            var p = s.LastIndexOf(".");
            if (p > 0) {
                return s.Substring(p + 1).ToLower();
            }
        }
        return "";
    }

    public static string CleanURL(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) {
            return "";
        }
        int p = url.IndexOf("#");
        if (p > 0)
            url = url.Substring(0, p);

        url = url.Replace("~", TrackConstants.BaseUrl);

        return url;
    }

    public static string RemoveAccents(string text)
    {
        if (text == null) return null;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized) {
            if (Char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)  // Normalize translates é into e'  ... this eleimates the trailing  '
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}


[JsonSerializable(typeof(List<TrackObject>))]
internal sealed partial class TrackObjectContext : JsonSerializerContext
{
}


