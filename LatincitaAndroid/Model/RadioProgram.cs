//using NaturalLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LatincitaAndroid.Model;

public enum RadioProgramType
{
    RADIO,  // entire radio program
    CD,     // entire CD
    TRACK   // single (random) track
}

// items returned by REST server (members of lists)

// converted to TrackObjects using MP3 & Title

//                List<TrackObject> List<TrackObject>  -----------------+
//                     ^^^                 ^^^                          |
//                get_radio_tracks  get_radio_tracks                    |
//                     ^^^                  ^^^                         v
//  REST-SERVER:  RadioProgram  =>  TrackObject (theFsongs[])  =>   PlayListItem
//                     ^^^        \                            /       ^^^
//                GetRadioPrograms \                          /      GetRandom
//                                  get_track   TrackObject_to_PlayListItem

public class RadioProgram
{
    public int ID { get; set; }

    [JsonPropertyName("Recorded On")]
    public DateTime RecordedOn { get; set; }

    [JsonPropertyName("Article Title")]
    public string ArticleTitle { get; set; }

    [JsonPropertyName("MP3 URL")]
    public string MP3URL { get; set; }

    [JsonPropertyName("Picture URL")]
    public string PictureURL { get; set; }

    public RadioProgramType Type { get; set; } // TRACK RADIO CD
    public string mp3
    {
        get
        {
            int p = this.MP3URL.IndexOf("#");
            if (p > 0)
                return this.MP3URL.Substring(0, p);
            return this.MP3URL;
        }
    }
}


[JsonSerializable(typeof(List<RadioProgram>))]
internal sealed partial class RadioProgramContext : JsonSerializerContext
{
}


