//using NaturalLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LatincitaAndroid.Model;

public class TrackObject
{
    public string BoxID { get; set; }
    public int SongID { get; set; }
    public string recorded_on { get; set; }
    public string released_on { get; set; }
    public string old_or_new { get; set; }
    public int created_with { get; set; }
    public string language { get; set; }
    public string genre { get; set; }
    public string purpose { get; set; }
    public int status { get; set; }
    public bool masterpiece { get; set; }
    public string article_title { get; set; }
    public string song_title { get; set; }
    public string video_song { get; set; }
    public string artist { get; set; }
    public string original_song { get; set; }
    public string wma_url { get; set; }
    public string song_url { get; set; }
    public string MP3_name { get; set; }
    public string critics_review { get; set; }
    public string diary_text { get; set; }
    public string poster_url { get; set; }
    public int TrackNumber { get; set; }
    public int RadioID { get; set; }
    public string RadioName { get; set; }
    public int soffset { get; set; }
    public int snxtoffset { get; set; }
    public int sduration { get; set; }
    public int stars { get; set; }
    public string real_url { get; set; }
    public string karaoke_url { get; set; }
    public string version_of_song { get; set; }
    public bool hide_if_not_on_radio { get; set; }

    public string mp3
    {
        get
        {
            int p = this.song_url.IndexOf("#");
            if (p > 0)
                return this.song_url.Substring(0,p);
            return this.song_url;
        }
    }
    public string start_time
    {
        get
        {
            string s = TimeSpan
                       .FromSeconds(this.soffset)
                       .ToString(@"hh\:mm\:ss");
            return s;
        }
    }
    public bool isCurrentRow { get; set; }
    public string background_class { get; set; }
}


[JsonSerializable(typeof(List<TrackObject>))]
internal sealed partial class TrackObjectContext : JsonSerializerContext
{
}


