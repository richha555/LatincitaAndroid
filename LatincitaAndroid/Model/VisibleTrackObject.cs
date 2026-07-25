using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatincitaAndroid.Model
{
    public partial class VisibleTrackObject : ObservableObject
    {
        public TrackObject my_track {  get; set; }

        [ObservableProperty]
        private Boolean isCurrentRow = false;

        [ObservableProperty]
        private string background_class = "";

        [ObservableProperty]
        private string start_time = "0:00:00";

        [ObservableProperty]
        private string article_title = "";

        [ObservableProperty]
        private string song = "";

        [ObservableProperty]
        private string artist = "";


        public VisibleTrackObject(TrackObject track)
        {
            this.my_track = track;
            this.isCurrentRow = false;
            this.start_time = track.start_time;
            this.article_title = track.article_title;
            this.song = track.song; 
            this.artist = track.artist;
        }
    }
}
