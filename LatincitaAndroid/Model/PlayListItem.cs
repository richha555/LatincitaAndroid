//using NaturalLanguage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LatincitaAndroid.Model
{
    public class TrackListItem
    {
        public int offs { get; set; }
        public int nxtoffs { get; set; }
        public int duration { get; set; }
        public string title { get; set; } // song
        public string artist { get; set; }
        public string radioname { get; set; }
        public string radioid { get; set; }
        public int trackid { get; set; }   // index into theFsongs if theFsongs[trackid].is_track == true
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class PlayListItemImage
    {
        public string Title { get; set; }
        public string FullTitle { get; set; }
        public string ImageRelativeURL { get; set; }
        public string ImageFullURL { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class PlayListItem
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string m4v { get; set; }
        public string poster { get; set; }
        public int useWma { get; set; }
        public bool entireRadio { get; set; }
        public string songid { get; set; }
        public int id { get; set; }
        public List<TrackListItem> track_list { get; set; }
        public PlayListItemImage image { get; set; }
        public List<TrackListItem> video_list { get; set; }

        public string played_list { get; set; }
    }
}

[JsonSerializable(typeof(PlayListItem))]
internal sealed partial class PlayListItemContext : JsonSerializerContext
{
}

