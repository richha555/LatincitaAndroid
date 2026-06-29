var JUL_2018_GOOGLE_KEY = '...0ylgc';  /* 28-JUL-2018 */
var APR_2019_GOOGLE_KEY = "...E4uz8"; // latincita-iv for richha@xs4all.nl

var USE_YOUTUBE_LOAD_API = true;  // false = lets us load videos even if API is unavailable
                                  // true  = loads meta-data for favorites

var bNotArrived = "Query results have not yet arrived.";
var bNoResults = "Query returned nothing.";
var bNoUseful = "Query returned nothing useful.";
var bNoError = "<OK>";

// A = studio
// B = solo latincita
// C = orchestra (con latincita)
// D = orchestra (other)

//  4% chance of selecting a "D" ... good live
// 10% chance of selecting a "C" ... orchestra latincita
// 24% chance of selecting a "B" ... latincita live
// 62% chance of selecting a "A" ... studio

//  cWEIGHT_PATTERN = "DDDCCCCCCCCCCCCBBBBBBBBBBBBBBBBBBBBBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
//  cWEIGHT_PATTERN = "CCBBDADDCABDDBAAAABCAACBBCCBBAACAAACAACAACACAAABABBBAABBAACBBCBCAAAABABABBAAABAAAAAAAABAAABAAAAAAAAA";

var cWEIGHT_PATTERN = Make_Bar(62,24,10,4);

var cMAX_VIDEO_LOAD = 20;

//  ========================= event          theFsongs[i].played[useWma] = event
var cNotPlayed = 0;
var cAdded = 1;      // 0            1        2           3          4    
var cPlaying = 2;    // cNotPlayed > cAdded > cPlaying  > cStopped > cRemoved
var cPlayed = 3;     // cNotPlayed > cQueued > cWaiting  > cEnabled | cDead
var cRemoved = 4;
var cDisabled = 5;  // button disabled all together >> should set for Radio's
var cEnabled = 6;   // query returned usable data
var cWaiting = 7;   // waiting for query to return
var cQueued = 8;    // query pushed onto request list, waiting to be sent to YouTube
var cDead = -1;     // query returned an error or nothing

//  ========================= useWma
var cMusic = 0;
var cKaraoke = 1;
var cReal = 2;
var cVideo = 3;  // video was "1"
var cHdAudio = 4;  // hd audio was 3

var cMonths = ["january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december"];

var selGenres = "";
var selLang = "";
var selArtists = "";
var selSongs = "";
var selShows = "";
var selGems = "";
var selStars = "";
var selLiveStudio = "";
var selIds = "";
var changedStars = ";";
var starsUnlocked = false;
var laststartime = 0;

var currplaylist = "";
var defaultTitle = "";

var sortPanel = undefined;

var sort_music_1 = undefined;
var sort_music_2 = undefined;
var sort_music_3 = undefined;

var play_but = undefined;
var stop_but = undefined;

var list_is_sorted = false;

var on_video_page = false;

var sortable_divs = [];

var playing = { 'playlistidx': -1, 'id': -1, 'useWma': cMusic, 'curr_video': undefined, 'action': "stopped", 'time': new Date() };

var match_all = false;
var top_to_bottom = false;
var hide_all_page = false;
var random_from_playlist = false;  // true = pick random song from playlist  false = pick random song from all non-played songs

var only_gemstones = true;

var clicking_checkbox = false;

var theFsongs = new Array();
var num_fsongs = 0;
var theKsongs = new Array();
var num_ksongs = 0;
var theRsongs = new Array();
var num_rsongs = 0;
var theVideoList = new Array();
var num_videoOnList = 0;

var numSongs = 0;
var numTracks = 0;
var numBands = 0;
var numRadios = 0;

var myPlaylist = null;

var removedItem = null;
var lastTrackSelector = null;
var currentTrack = -1;

var video_load_list = [];
var video_to_play = -1;
var video_useWma = cMusic;

var star_data = new Object();
var star_list = []; // star_list[star_id] = SongId;
var offs_lookup = [];

var youtube_queries = [];

function onYouTubeLoad() {
    gapi.client.load('youtube', 'v3', onYouTubeApiLoad);
}
// Called automatically when YouTube API interface is loaded
function onYouTubeApiLoad() {
    var vNEWEST = $("#vNEWEST");
    if (vNEWEST.length > 0) {
        APR_2019_GOOGLE_KEY = vNEWEST.text();
    }
    gapi.client.setApiKey(APR_2019_GOOGLE_KEY);
    start_loader();
}

function find_all_matching_videos() {

    var video_srch_cnt = 0;

    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].is_track_object === false) { // tracks don't have buttons
            if (video_srch_cnt < cMAX_VIDEO_LOAD) { // <<<<<< FIND LIMIT
                if (find_matching_videos(i)) {
                    video_srch_cnt++;
                }
            }
        }
    }
    start_loader();
}

function start_loader() {

    if (video_load_list.length > 0) {

        var sidx = video_load_list.shift();

        var requested = matching_video_finder(sidx); // if not already done, issue youtube_search

        if (!requested) { // "requested" is only false if youtube_search threw an exception
                          // push request back on queue.  onYouTubeApiLoad will call us again and we can retry
            video_load_list.push(sidx);
            try {
                gapi.client.load('youtube', 'v3', onYouTubeApiLoad);
            } catch (err) {
                var errmsg = "YouTube GAPI Client Load Failure: " + err.message;
                console.log("filter_mobile::start_loader: " + errmsg);
            //  alert(errmsg);  --- until we figure out how to recreate the gapi object
            //                      don't bother end user with these errors
            }
        }
    }
    // rest of list will (hopefully) get processed as videos arrive (by onYoutubeSearchResponse)
}

function find_matching_videos(sidx) {

    var vondr = false;
    var vondk = false;

    if ((sidx >= 1) && (sidx <= num_fsongs)) {
        if ((theFsongs[sidx].played[cReal] === cNotPlayed) || (theFsongs[sidx].played[cReal] === cDead)) {
            showPlayState(sidx, cReal, cQueued);
            vondr = true;
        }
        if ((theFsongs[sidx].played[cKaraoke] === cNotPlayed) || (theFsongs[sidx].played[cKaraoke] === cDead)) {
            showPlayState(sidx, cKaraoke, cQueued);
            vondk = true;
        }
    }
    if (vondr || vondk) {
        video_load_list.push(sidx);
        return true;
    }
    return false;
}

function matching_video_finder(sidx) {

    var sobj = theFsongs[sidx];
    if (sobj.purpose === 'BAND' || sobj.purpose === 'RADIO' || sobj.purpose === 'YOUTUBE') {
        // radio shows won't be findable on youtube
        return true;
    }
    if ((sobj.mp3[cReal] !== "") && (sobj.mp3[cKaraoke] !== "")) {
        // already have a karaoke & real video
        return true;
    }
    if ((sobj.played[cReal] === cWaiting) && (sobj.mp3[cKaraoke] === cWaiting)) {
        // already requested these video's and haven't received a reply yet
        return true;
    }

    var srch_artist = cleanString(sobj.artist);
    srch_artist = srch_artist.replace(/[-+,()"']/g, " ");
    srch_artist = srch_artist.replace(/\s+/g, " ");
    srch_artist = srch_artist.replace(/\s+$/, "");
    srch_artist = srch_artist.replace(/ /g, " +");
    var srch_song = cleanString(sobj.song);
    srch_song = srch_song.replace(/[-+,()"']/g, " ");
    srch_song = srch_song.replace(/\s+/g, " ");
    srch_song = srch_song.replace(/\s+$/, " ");
    srch_song = srch_song.replace(/ /g, " +");
    var query = srch_artist + " +" + srch_song;

    sobj.query = query;

    var issued_query = false;
    var vondk = false;
    var vondr = false;

    for (var i = 1; i <= num_fsongs; i++) {
        if (i !== sidx) {
            if (theFsongs[i].query === query) {
                issued_query = true;
                // this query has already been submitted
                if (sobj.mp3[cReal] === "") {
                    // copy the query's results to current item (if they have already arrived)
                    if (theFsongs[i].load_error[cReal] === "" || theFsongs[i].load_error[cReal] === bNotArrived) {
                        // not yet arrived
                    } else {
                        sobj.mp3[cReal] = theFsongs[i].mp3[cReal];
                        sobj.load_error[cReal] = theFsongs[i].load_error[cReal];
                        if (theFsongs[i].load_error[cReal] === bNoError) {
                            showPlayState(sidx, cReal, cEnabled);
                        } else {
                            showPlayState(sidx, cReal, cDead);
                        }
                        vondr = true;
                    }
                }
                if (sobj.mp3[cKaraoke] === "") {
                    if (theFsongs[i].load_error[cKaraoke] === "" || theFsongs[i].load_error[cKaraoke] === bNotArrived) {
                        // not yet arrived
                    } else {
                        sobj.mp3[cKaraoke] = theFsongs[i].mp3[cKaraoke];
                        sobj.load_error[cKaraoke] = theFsongs[i].load_error[cKaraoke];
                        if (theFsongs[i].load_error[cKaraoke] === bNoError) {
                            showPlayState(sidx, cKaraoke, cEnabled);
                        } else {
                            showPlayState(sidx, cKaraoke, cDead);
                        }
                        vondk = true;
                    }
                }
                if (vondk && vondr) {
                    break;
                }
            }
        }
    }

    if (issued_query) {
        return true;  // already issued this query
    }
    if (vondk && vondr) {
        return true;  // found answers to this query
    }
    var ok = true;

    if (sobj.mp3[cReal] === "") {
        var real_favorite = Curr_Favorite(sidx, cReal);
        var fav_id = videoId_from_url(real_favorite);
        var playBoxR = null;
        if (sobj.divobj !== undefined) {
            playBoxR = sobj.divobj.find(play_sel(cReal));
        }
        var requestedR = youtube_search(query, "", fav_id, playBoxR, sidx, cReal);
        if (!requestedR) ok = false;
    }
    if (sobj.mp3[cKaraoke] === "") {
        var karaoke_favorite = Curr_Favorite(sidx, cKaraoke);
        var fav_id = videoId_from_url(karaoke_favorite);
        var playBoxK = null;
        if (sobj.divobj !== undefined) {
            playBoxK = sobj.divobj.find(play_sel(cKaraoke));
        }
        var requestedK = youtube_search(query, "", fav_id, playBoxK, sidx, cKaraoke);
        if (!requestedK) ok = false;
    }
    return ok;
}

function youtube_fetchvideo(sidx, useWma, video_id) {

    var sobj = theFsongs[sidx];
    var query = sobj.query;
    var requested = false;
    if (useWma === cReal) {
        var playBoxR = null;
        if (sobj.divobj !== undefined) {
            playBoxR = sobj.divobj.find(play_sel(cReal));
        }
        requested = youtube_search(query, "", video_id, playBoxR, sidx, cReal);
    }
    if (useWma === cKaraoke) {
        var playBoxK = null;
        if (sobj.divobj !== undefined) {
            playBoxK = sobj.divobj.find(play_sel(cKaraoke));
        }
        requested = youtube_search(query, "", video_id, playBoxK, sidx, cKaraoke);
    }
    return requested;
}

function youtube_requery(sidx, useWma, youtube_query) {

    var sobj = theFsongs[sidx];
    var query = sobj.query;
    var requested = false;
    if (useWma === cReal) {
        var playBoxR = null;
        if (sobj.divobj !== undefined) {
            playBoxR = sobj.divobj.find(play_sel(cReal));
        }
        requested = youtube_search(query, youtube_query, "", playBoxR, sidx, cReal);
    }
    if (useWma === cKaraoke) {
        var playBoxK = null;
        if (sobj.divobj !== undefined) {
            playBoxK = sobj.divobj.find(play_sel(cKaraoke));
        }
        requested = youtube_search(query, youtube_query, "", playBoxK, sidx, cKaraoke);
    }
    return requested;
}

// Called when the search button is clicked in the html code
function youtube_search(query, youtube_query, video_id, button, sidx, useWma) {

    var requery = false;
    if (youtube_query === "") {
        var query_karaoke = "karaoke +" + query;
        var query_real = query + " -karaoke";
        if (useWma === cKaraoke) {
            youtube_query = query_karaoke;
        } else {
            youtube_query = query_real;
        }
    } else {
        requery = true;
    }
    var sobj = theFsongs[sidx];
    if (!requery) {
        if (sobj.load_error[useWma] === "") {
            sobj.load_error[useWma] = bNotArrived;
        }
        showPlayState(sidx, useWma, cWaiting); // show user we are waiting for a response from YouTube
    }
    var num_to_return = 5;  // default
    if (requery) {
        num_to_return = 50; // MAX ^#*!*(#^(^#))
    } 
    var request = null;

    try {
        if (video_id === "") {
            request = gapi.client.youtube.search.list({
                part: 'snippet',
                type: 'video',
                order: 'viewCount',
                maxResults: num_to_return,
                q: youtube_query // only send the +/-karaoke to youtube ... for us REAL query == KARAOKE query
            });
        } else {
            request = gapi.client.youtube.videos.list({
                part: [ 'snippet' ],
                id: [ video_id ] // only fetch this one video
            });
        }
    } catch (err) {
        sobj.load_error[useWma] = "YouTube Failure: " + err.message;
    }
    if (request === null) {
        // button remains dead
        return false;
    }
    var req = {
        query: query,
        youtube_query: youtube_query,
        button: button,
        sidx: sidx,
        useWma: useWma,
        requery: requery,
        getResults: function (response) {
            onYoutubeSearchResponse(this.query, this.youtube_query, this.button, this.sidx, this.useWma, this.requery, response);
        }
    };
    var requestor = $.proxy(req.getResults, req);

    console.log("filter_mobile::youtube_search( " + youtube_query + " )");

    // Send the request to the API server, call the onSearchResponse function when the data is returned
    request.execute(requestor);

    return true;
}

function onYoutubeSearchResponse(query, youtube_query, button, sidx, useWma, requery, response) {

    var sobj = theFsongs[sidx];
    var track_num = 0;
    var error_message = "";

    // one query has just been received & processed by youtube, queue up the next

    console.log("filter_mobile::onYoutubeSearchResponse( " + youtube_query + " )");

    if (video_load_list.length > 0) {  // when a response is received from youtube, fetch the next query from the stack
                                       // and send it to YouTube
        var tidx = video_load_list.shift();

        var requested = matching_video_finder(tidx);

    //  we don't use the retry mechanism in start_loader(), because if youtube_search fails just after a response was received
    //  something is seriously wrong... just give up entirely
    }

    // now process what we just received

    var stored_videos = [];

    if (response === "") {
        if (requery) {
            error_message = "YouTube returned no results.";
        } else {
            sobj.load_error[useWma] = bNoResults;
            //  should already be marked as dead
        }
    } else {
        var responseString = JSON.stringify(response, '', 2);
        var butid = "???";
        if (button && button.length > 0) {
            butid = button.attr("id");
        }

        var items = response.items;

        //{
        //    "kind": "youtube#videoListResponse",
        //        "etag": "VHIDxlnyJLqxPZkeMdCJfWO0MQg",
        //            "items": [
        //                {
        //                    "kind": "youtube#video",
        //                    "etag": "TTM8Fy28BRQYFJjsGRX8_ZNkVPg",
        //                    "id": "BAl4AGzege4",
        //                    "snippet": {
        //                        "publishedAt": "2014-09-12T14:23:50Z",
        //                        "channelId": "UCJ6DkebEyOZvXG3LNrP5OWA",
        //                        "title": "Gran Combo de Puerto Rico - Arroz con Habichuela - HiFi -HD",
        //                        "description": "Esto no es balada, esto no es rock \nEsto es Salsa, Son y Rumba. \nEsto no es ensaladita Light \nArroz con habichuela y vianda es lo que hay. \n\nEsto no es lo que tu piensas \nNo te vayas a tirar. \nSi no aprendiste a conciencia \nLa clave te va a tumbar. \n\nEsto tiene su truquito. \nEsto no es llegué y pegué. \nEsto lleva sus añitos (Rafael!) \nPa tocarse como es \n\nESTRIBILLO \n\nEsto no es la A y la B, \nEsto llega hasta la Z. \nEsto no es mamá, papá y nené, \nEsta es la lección completa. \n\nEsto no viene en los libros, \nNo se enseña en la academia. \nEsto es poquito a poquito, \nLuego el esfuerzo se premia. \n\nESTRIBILLO \n\nCuatro décadas Gran Combo en la cocina \nCocinando Salsa para la gente latina. \n\nNo, no es ensaladita Light de dieta \nPregúntale a Juan José que trajo la receta. \n\nEs la cadencia del Son de Cuba elegante \nY el Swing de Nueva York siempre pa adelante. \n\nSabor, sabor, sabor de la vieja escuela \nSalsa caribeña, rumba, plena. \nNo se aprende ni en la academia más fina. \nEn la calle, calle, calle, rumbón de esquina. \nTraigo melao. Que rico que sabroso el tumbao. \n\nLo que hay, esto es lo que hay.",
        //                        "thumbnails": {
        //                            "default": {
        //                                "url": "https://i.ytimg.com/vi/BAl4AGzege4/default.jpg",
        //                                "width": 120,
        //                                "height": 90
        //                            },
        //                            "medium": {
        //                                "url": "https://i.ytimg.com/vi/BAl4AGzege4/mqdefault.jpg",
        //                                "width": 320,
        //                                "height": 180
        //                            },
        //                            "high": {
        //                                "url": "https://i.ytimg.com/vi/BAl4AGzege4/hqdefault.jpg",
        //                                "width": 480,
        //                                "height": 360
        //                            },
        //                            "standard": {
        //                                "url": "https://i.ytimg.com/vi/BAl4AGzege4/sddefault.jpg",
        //                                "width": 640,
        //                                "height": 480
        //                            },
        //                            "maxres": {
        //                                "url": "https://i.ytimg.com/vi/BAl4AGzege4/maxresdefault.jpg",
        //                                "width": 1280,
        //                                "height": 720
        //                            }
        //                        },
        //                        "channelTitle": "Santy Djockey",
        //                        "tags": [
        //                            "El Gran Combo De Puerto Rico (Musical Artist)",
        //                            "Arroz Con Habichuela (Musical Recording)",
        //                            "Music (TV Genre)",
        //                            "De Puerto Rico (Musical Album)"
        //                        ],
        //                        "categoryId": "10",
        //                        "liveBroadcastContent": "none",
        //                        "localized": {
        //                            "title": "Gran Combo de Puerto Rico - Arroz con Habichuela - HiFi -HD",
        //                            "description": "Esto no es balada, esto no es rock \nEsto es Salsa, Son y Rumba. \nEsto no es ensaladita Light \nArroz con habichuela y vianda es lo que hay. \n\nEsto no es lo que tu piensas \nNo te vayas a tirar. \nSi no aprendiste a conciencia \nLa clave te va a tumbar. \n\nEsto tiene su truquito. \nEsto no es llegué y pegué. \nEsto lleva sus añitos (Rafael!) \nPa tocarse como es \n\nESTRIBILLO \n\nEsto no es la A y la B, \nEsto llega hasta la Z. \nEsto no es mamá, papá y nené, \nEsta es la lección completa. \n\nEsto no viene en los libros, \nNo se enseña en la academia. \nEsto es poquito a poquito, \nLuego el esfuerzo se premia. \n\nESTRIBILLO \n\nCuatro décadas Gran Combo en la cocina \nCocinando Salsa para la gente latina. \n\nNo, no es ensaladita Light de dieta \nPregúntale a Juan José que trajo la receta. \n\nEs la cadencia del Son de Cuba elegante \nY el Swing de Nueva York siempre pa adelante. \n\nSabor, sabor, sabor de la vieja escuela \nSalsa caribeña, rumba, plena. \nNo se aprende ni en la academia más fina. \nEn la calle, calle, calle, rumbón de esquina. \nTraigo melao. Que rico que sabroso el tumbao. \n\nLo que hay, esto es lo que hay."
        //                        }
        //                    }
        //                }
        //            ],
        //                "pageInfo": {
        //        "totalResults": 1,
        //            "resultsPerPage": 1
        //    }
        //}


        if (items && (items.length > 0)) {
            for (i = 0; i < items.length; i++) {
                var item = items[i];
                var video_id = "";
                var title = sobj.article_title;
                var desc = "";
                var poster = sobj.photo;
                var thumbnail = sobj.photo;
                var month_year = "<date unkown>";
                try { video_id = item.id.videoId; } catch (ex) { }
                if ((video_id === undefined) || (video_id === "")) {
                    // retrieving single video returns video_id in item.id
                    try { video_id = item.id; } catch (ex) { }
                }
                if (video_id === undefined) {
                    video_id = "";
                }
                try { title = item.snippet.title; } catch (ex) { }
                try { desc = item.snippet.description; } catch (ex) { }
                try { thumbnail = item.snippet.thumbnails.default.url; } catch (ex) { }
                try { poster = item.snippet.thumbnails.high.url; } catch (ex) { }
                try {
                    month_year = item.snippet.publishedAt;

                    var d = new Date(item.snippet.publishedAt);
                    var now = new Date();
                    var m = d.getMonth();
                    var y = d.getFullYear();
                    if (isNaN(m) || isNaN(y)) {
                        month_year = item.snippet.publishedAt;
                    } else { // 
                        if ((m === now.getMonth()) && (y === now.getFullYear())) {
                            month_year = d.getDate() + " " + cMonths[m]; // display  day month, instead of month year
                        } else {
                            month_year = cMonths[m] + " " + y;
                        }
                    }
                } catch (ex) { }

                if (video_id !== "") {  //                                                                                  vvvvv--- we want to let user decide which is the 'default'
                    track_num++;
                    var eidx = store_video(sidx, track_num, useWma, video_id, title, desc, poster, month_year, youtube_query, false);
                    //  eidx is index into theRsongs or theKsongs
                    //  theFsongs[i].mp3[useWma] = video_url;  // that is video_url of default (first) film
                    //  so we can determine eidx if we need it by searching for theFsongs[i].mp3[useWma]
                    if (eidx < 0) {
                        track_num--;  // error, throw out this track
                    } else {
                        stored_videos.push(eidx);
                    }
                }
            }
        } else {
            if (requery) {
                error_message = "YouTube returned no results.";
            } else {
                sobj.load_error[useWma] = bNoResults;
                //  should already be marked as dead
            }
            if (response.hasOwnProperty('error')) {
                var err = response.error;
                if (err.hasOwnProperty('message')) {
                    var msg = err.message;
                    if (msg !== "") {
                        if (requery) {
                            error_message = msg;
                        } else {
                            sobj.load_error[useWma] = msg;
                        }
                    }
                }
            }
        }
    }

    if (!requery) {
        if (stored_videos.length <= 0) {
            showPlayState(sidx, useWma, cDead);
        }
    }

    var playlist_idx = -1;

    if (requery) {

        var num_new_videos = 0;

        if (stored_videos.length <= 0) {
            if (error_message === "") {
                error_message = "YouTube query returned no results.";
            }
            alert("Error executing query: " + error_message + "\n\nQUERY: " + youtube_query);
        } else {
            // ----------------------------- update video list for current song if it is on the playlist
            //                               other video-lists are updated below
            //
            // ----------------------------- we could do this through the call to updatePlayList below
            //                               but we need to know if the URL's we just received are
            //                               indeed new
            var video_list = undefined;
            var video_num = 0;
            for (var j = 0; j < myPlaylist.playlist.length; j++) {
                if ((myPlaylist.playlist[j].id === sidx) && (myPlaylist.playlist[j].useWma === useWma)) {
                    video_list = myPlaylist.playlist[j].video_list;  // current video list for the current song
                    playlist_idx = j; 
                    break;
                }
            }
            if (playlist_idx >= 0) {
                for (var i = 0; i < stored_videos.length; i++) {
                    var teidx = stored_videos[i];
                    var sobj2 = undefined;
                    if (useWma === cReal) {
                        sobj2 = theRsongs[teidx];
                    } else if (useWma === cKaraoke) {
                        sobj2 = theKsongs[teidx];
                    }
                    //  alert(sobj2.mp3[useWma]);
                    var already_added = false;
                    if (video_list !== undefined) { // video_list doen't have an element 0
                        for (var video_num in video_list) {
                            if (video_list[video_num].media === sobj2.mp3[useWma]) {
                                already_added = true;  // already have this one
                                break;
                            }
                        }
                    }
                    if (!already_added) {
                        var video_obj = {
                            title: sobj2.article_title, artist: sobj2.artist, desc: sobj2.critics_review, songid: sobj2.songid,
                            poster: sobj2.photo, media: sobj2.mp3[useWma], songidx: sidx, useWma: useWma, videoidx: teidx
                        };
                        if (video_list === undefined) {
                            video_list = [];
                            video_num = 1;
                            video_list[video_num] = video_obj;
                            myPlaylist.playlist[playlist_idx].video_list = video_list;  // point to newly created video_list
                        } else {
                            video_num = video_list.length; // last index = 1, but length = 2, next index = 2
                            video_list[video_num] = video_obj;
                        //  myPlaylist.playlist[playlist_idx].video_list = video_list;  // already did this because video_list is a pointer to myPlaylist.video_list
                        }
                        num_new_videos++;
                    }
                }
                if (num_new_videos > 0) {
                    if ((playing.id === sidx) && (playing.useWma === useWma)) {
                        // if id is currently queued up song, then need to update '# / total' button on player
                        // callback is keeping track of current video or current track,  use this to figure out if [ # / # ] needs updating
                        // $("#v-n-of-m").html("5 / 22")  <<< only update 22
                        // don't forget count of videos in memory of playlist is probably wrong now
                        var teller = $("#v-n-of-m");
                        if (teller.length > 0) {
                            var teller_text = teller.text();
                            if (teller_text !== undefined && teller_text !== "") {
                                var p = teller_text.indexOf("/");
                                if (p > 0) {
                                    var num_videos = video_list.length - 1;  // length is 1 greater than num videos, because we don't use index 0
                                    teller_text = teller_text.replace(/([^0-9]*[0-9]+[^/]*[/][^0-9]*)([0-9]+)([^0-9]*)/, "$1" + num_videos + "$3");
                                    teller.html(teller_text);
                                }
                            }
                        }
                    }
                }
            } else { // no video list to add them to
                if (stored_videos.length > 0) {
                    num_new_videos = stored_videos.length;
                }
            }
        }

        var loaded_message = "";
        if (num_new_videos > 0) {
            if (num_new_videos === 1) {
                loaded_message = "Received one new video.";
            } else {
                loaded_message = "Received " + num_new_videos + " new videos.";
            }
        } else {
            if (error_message === "") {
                loaded_message = "No new videos found.";
            } else {
                loaded_message = "Failure Loading Videos: " + error_message;
            }
        }
    }

    // copy results to all other issueers of query
    for (var i = 1; i <= num_fsongs; i++) {
        if ((theFsongs[i].query === query) && ((theFsongs[i].mp3[useWma] === "") || (theFsongs[i].mp3[useWma] === "<NONE>"))) { 
            if (stored_videos.length <= 0) { // kill us and everyone else who issued this query and still doesn't have a video
                theFsongs[i].mp3[useWma] = "<NONE>"; // there is no i != id filter, so this will kill us as well
                if (theFsongs[i].load_error[useWma] === "" || theFsongs[i].load_error[useWma] === bNotArrived) {
                    theFsongs[i].load_error[useWma] = sobj.load_error[useWma];
                }
                showPlayState(i, useWma, cDead);
            } else { // copy our selection to everyone else who issued the same query
                if (i !== sidx) {
                    theFsongs[i].mp3[useWma] = theFsongs[sidx].mp3[useWma];
                    theFsongs[i].load_error[useWma] = sobj.load_error[useWma];
                    showPlayState(i, useWma, cEnabled);
                }
            }
        }
        // ** think about this some more  ... when do we need to update songs on playlist ?
        if (stored_videos.length > 0) {
            if ((theFsongs[i].query === query) && (theFsongs[i].mp3[useWma] !== "") && (theFsongs[i].mp3[useWma] !== "<NONE>")) {
                if (!requery || (requery && (i !== sidx))) {
                    updatePlayList(i, useWma);  // incase song was added to playlist ... update list of videos
                }
            }
        }
    }

    Select_Favorite(sidx, useWma);  // if there is a favorite defined for this song, see if it's in the list & scroll to it

    if (requery) {
        edit_videolist(playlist_idx, sidx, useWma, "", true, loaded_message);  // see what happens if we call this here without closing& opening panel
    }

 // var msg = "query: " + query + "\n" +
 //           "target: " + butid + "\n" +
 //           responseString;
 // alert(msg);
}

function Curr_Favorite(sidx, useWma) {

    var sobj = theFsongs[sidx];
    if (sobj === undefined) {
        return "";
    }
    var song_id = sobj.songid;
    var elem = star_data[song_id];
    if (elem === undefined) {
        return "";
    }
    var currUrl = "";
    if (useWma === cReal) {
        currUrl = elem.real_url;
    } else if (useWma === cKaraoke) {
        currUrl = elem.karaoke_url;
    }
    if (currUrl === "") {
        return "";
    }
    currUrl = url_from_videoId(currUrl);  // make sure it is a full url, so it matches media

    return currUrl;
}

function Select_Favorite(sidx, useWma) {

    // ------------------------ if we have a favorite video for song id
    //                          and song id is on the playlist
    //                          then we should search through the videos we just loaded
    //                          and try to find the favorite
    //                          load this video's url

    var currUrl = Curr_Favorite(sidx, useWma);
    if (currUrl === "") {
        return false;
    }
    var video_id = videoId_from_url(currUrl);

    var sobj = theFsongs[sidx];
    var query = sobj.query;
    var fav_idx = -1;
    if (useWma === cReal) {
        for (i = 1; i <= num_rsongs; i++) {
            if ((theRsongs[i].video_id === video_id) && (theRsongs[i].query === query)) {
                // already received and saved this video (don't consider same video returned for another query)
                fav_idx = i;
                break;
            }
        }
    } else if (useWma === cKaraoke) {
        for (i = 1; i <= num_ksongs; i++) {
            if ((theKsongs[i].video_id === video_id) && (theKsongs[i].query === query)) {
                // already received and saved this video (don't consider same video returned for another query)
                fav_idx = i;
                break;
            }
        }
    }
    if (fav_idx < 0) {
        return false;  // favorite video isn't even loaded in memory
    }
    if (theFsongs[sidx].mp3[useWma] !== currUrl) {
    //  probably first video loaded has been selected as 'current' video
    //  change this to point to favorite video
        theFsongs[sidx].mp3[useWma] = currUrl;
    }

    var video_list = undefined;
    var playlist_idx = -1;
    var video_idx = -1;
    for (var j = 0; j < myPlaylist.playlist.length; j++) {
        if ((myPlaylist.playlist[j].id === sidx) && (myPlaylist.playlist[j].useWma === useWma)) {
            video_list = myPlaylist.playlist[j].video_list;  // current video list for the current song
            playlist_idx = j;
            break;
        }
    }
    if (video_list === undefined) {
        return false;
    }
    // video_list doen't have an element 0
    for (var video_num in video_list) {
        if (video_list[video_num].media === currUrl) {
            video_idx = video_num;  // found favorite
            break;
        }
    }
    if (video_idx <= 0) {
        return false;
    }
    myPlaylist.select_video(currUrl);

    return true;
}

function store_video(sidx, tracknum, useWma, video_id, title, desc, poster, month_year, youtube_query, mark_as_default) {

    if (video_id === "") { return -1; }
    if ((sidx <= 0) || (sidx > num_fsongs)) { return -1; }
    if (useWma <= 0) { return -1; }

    var sobj = theFsongs[sidx];
    var query = sobj.query;

    if (query === "") {
        var srch_artist = cleanString(sobj.artist);
        srch_artist = srch_artist.replace(/[-,]/g, " ");
        srch_artist = srch_artist.replace(/    /g, " ");
        srch_artist = srch_artist.replace(/   /g, " ");
        srch_artist = srch_artist.replace(/  /g, " ");
        srch_artist = srch_artist.replace(/ /g, " +");
        var srch_song = cleanString(sobj.song);
        srch_song = srch_song.replace(/[-,]/g, " ");
        srch_song = srch_song.replace(/    /g, " ");
        srch_song = srch_song.replace(/   /g, " ");
        srch_song = srch_song.replace(/  /g, " ");
        srch_song = srch_song.replace(/ /g, " +");
        query = srch_artist + " +" + srch_song;
        var query_karaoke = "karaoke +" + query;
        var query_real = query + " -karaoke";
        if (useWma === cReal) {
            query = query_real;
        } else {
            query = query_karaoke;
        }
        theFsongs[sidx].query = query; // query is based on song+artist, but it's not necessarily what was used to fetch this video
    //  youtube_query = anyone's guess
    }

    //  video_url = "https://www.youtube.com/watch?v=" + video_id;
    //  video_url = "https://www.youtube.com/v/" + video_id + "?version=3";
    var video_url = url_from_videoId(video_id);

    if (video_id.startsWith("http")) {  // we received a url, try to find ID
        video_url = video_id;
        video_id = videoId_from_url(video_id);
    }

    ///////////////////////////////////// may have already recorded this video_id, but theRsongs/theKsongs
    //                                    most certainly points to a different sobj !   We need out own video record
    ///////////////////////////////////////////// just need to accept this, they are different theFsongs, but they
    //                                            are the same sone/artist just a different rendition on a different date
    //////////////////////////////////////////////////// the real problem is the thumbnail, we'll just have to try and
    //                                                   but these are videos so youtube supplies the 'thumbnai'!
    var track_cnt = 0;
    var idx = -1;
    var sobj2 = undefined;

    if (useWma === cReal) {
        for (i = 1; i <= num_rsongs; i++) {
            if ((theRsongs[i].query === query) || (theRsongs[i].radioid === sobj.songid)) {
                track_cnt += 1;
            }
            if ((theRsongs[i].video_id === video_id) &&
                ((theRsongs[i].query === query) || ((theRsongs[i].youtube_query === youtube_query) && (youtube_query !== "")))) {
                // already received and saved this video (don't consider same video returned for another query)
                idx = i;
                sobj2 = theRsongs[i];
            }
        }
    } else if (useWma === cKaraoke) {
        for (i = 1; i <= num_ksongs; i++) {
            if ((theKsongs[i].query === query) || (theKsongs[i].radioid === sobj.songid)) {
                track_cnt += 1;
            }
            if ((theKsongs[i].video_id === video_id) &&
                ((theKsongs[i].query === query) || ((theKsongs[i].youtube_query === youtube_query) && (youtube_query !== "")))) {
                // already received and saved this video (don't consider same video returned for another query)
                idx = i;
                sobj2 = theKsongs[i];
            }
        }
    }
    if ((idx < 0) || (sobj2 === undefined)) {
        sobj2 = new fTrackObject();
    }
//  else  video was already loaded in memory, but we use new infor from youtube to
//        overwrite meta-data
//        *** hopefully we don't trash anything !
    var songid2 = "";

    //if (useWma === cReal) {    <<<<<<<<<<<<<< moved down below
    //    songid2 = 20000 + num_rsongs + 1;
    //} else if (useWma === cKaraoke) {
    //    songid2 = 10000 + num_ksongs + 1;
    //}

    sobj2.query = query;
    if (youtube_query === "") {
        sobj2.youtube_query = query;
    } else {
        sobj2.youtube_query = youtube_query;
    }
    sobj2.mp3[useWma] = video_url;

    sobj2.divobj = sobj.divobj;  // there may be lots of other sobj's that point to this video, we record only first one we run into
    sobj2.boxid = sobj.boxid;
    sobj2.genre = sobj.genre;
    sobj2.artist = sobj.artist;
    sobj2.sartist = sobj.sartist;
    sobj2.song = sobj.song;
    sobj2.ssong = sobj.ssong;
    sobj2.radioid = sobj.songid;  // radioid -> parent.songid  NOTE: this is allowed to be a track in a RADIO
    sobj2.radioname = sobj.song;
    sobj2.tracknum = tracknum; 
    sobj2.video_id = video_id;

    var dname = "";
    if (title !== "") {
        dname = title;
        if (desc !== "") {
        //  dname += " (" + desc + ")";
        }
    } else {
        dname = sobj.song + " / " + sobj.artist;
        if (sobj.month_year !== "") {
            dname += " [" + sobj.month_year + "]";
        }
    }
    dname += " (#" + sidx + "/" + tracknum + ")";
    sobj2.display_name = dname;

    if (title === "") { title = sobj.article_title; }
    if (poster === "") { poster = sobj.photo; }
    if (month_year === "") { month_year = sobj.month_year; }

    if (idx < 0) {
        if (track_cnt === 0) {
            track_cnt = 1;
        } else {
            track_cnt += 1;
        }
    }
    // ---------------------- generate UNIQUE id
    // We are avoiding turning tracks into RADIO.ID and leaving them just [Radio Contents].ID + 1000
    // But this doesn't mean we have to do the same with tracks
    // This new methodology should work well.
    var songid_num = parseFloat(sobj.songid + ""); // <<< NOTE: id songid is 518.03.4788 then  .4788 is just discarded
    var songid2_num = songid_num;
    if (useWma === cReal) {
        songid2_num = 20000 + songid_num; // 518.03 =>  20518.03  =>  20518.03.0947
    } else if (useWma === cKaraoke) {
        songid2_num = 10000 + songid_num; // 518.03 =>  10518.03  =>  10518.03.0947
    }
    if (track_cnt < 10) {
        songid2 = songid2_num + ".000" + track_cnt + "";  // songid is a string  
    } else if (track_cnt < 100) {
        songid2 = songid2_num + ".00" + track_cnt + ""; 
    } else if (track_cnt < 1000) {
        songid2 = songid2_num + ".0" + track_cnt + ""; 
    } else {
        songid2 = songid2_num + "." + track_cnt + "";  // 1000 or more, could be 19644
    }
    sobj2.songid = songid2;     //  10518.0947  20518.03.0947

    sobj2.article_title = title;
    sobj2.photo = poster;  // we want a poster , not a thumbnail
    sobj2.month_year = month_year;
    sobj2.critics_review = desc;

    sobj2.meesterwerk = false;  // try to keep it hidden
    sobj2.is_track_object = false;
    sobj2.is_track = false;
    sobj2.purpose = "YOUTUBE";

    if (idx < 0) {
        if (useWma === cReal) {
            num_rsongs++;
            theRsongs[num_rsongs] = sobj2;
            idx = num_rsongs;
        } else if (useWma === cKaraoke) {
            num_ksongs++;
            theKsongs[num_ksongs] = sobj2;
            idx = num_ksongs;
        }
    }
    if (((sobj.mp3[useWma] === "") || (sobj.mp3[useWma] === "<NONE>")) || (mark_as_default === true)) {
        sobj.mp3[useWma] = video_url;
        sobj.load_error[useWma] = bNoError;

        showPlayState(sidx, useWma, cEnabled);
    }
    return idx;
}

function print_error(msg) {
//  console.log(msg);
}

function sleep(miliseconds) {
    var currentTime = new Date().getTime();
    while (currentTime + miliseconds >= new Date().getTime()) {
        console.log(".");
    }
}

function videoId_from_url(url) {

    if (url === "") {
        return "";
    }

    var video_id = url;

    if (video_id.startsWith("http")) {  // we received a url, try to find ID
        var p = video_id.lastIndexOf("/");
        if (p > 0) {
            video_id = video_id.substr(p + 1);
            p = video_id.indexOf("?");
            if (p > 0) {
                video_id = video_id.substr(0, p);
            }
        }
    }
    return video_id;
}

function url_from_videoId(video_id) {

    if (video_id === "") {
        return "";
    }
    var video_url = video_id;
    if (!video_id.startsWith("http")) {  // we received a url, try to find ID
        video_url = "https://www.youtube.com/v/" + video_id + "?version=3";
    }
    return video_url;
}

function removeAccents(s) {
    return s
        .replace(/[áàãâä]/g, "a")
        .replace(/[éèëê]/g, "e")
        .replace(/[íìïî]/g, "i")
        .replace(/[óòöôõ]/g, "o")
        .replace(/[úùüû]/g, "u")
        .replace(/[ç]/g, "c")
        .replace(/[ñ]/g, "n")
        .replace(/[ÁÀÃÂÄ]/g, "A")
        .replace(/[ÉÈËÊ]/g, "E")
        .replace(/[ÍÌÏÎ]/g, "I")
        .replace(/[ÓÒÖÔÕ]/g, "O")
        .replace(/[ÚÙÜÛ]/g, "U")
        .replace(/[Ç]/g, "C")
        .replace(/[Ñ]/g, "N");
}

function convertToCSV(data) {

//  INTEGER: offset,nxtoffs,duration,stars
//  MULTI-LINE: critics_review,diary_text
//  Field-Separator is ","
//  mp3, played and load_error are arrays w/ '|' as separator

//  const header = [ "divobj", "boxid", "songid", "radioid", "radioname", "tracknum", "offset", "nxtoffs", "duration", "artist", "song", "video_song", "sartist", "ssong", "svideo_song", "genre", "language", "purpose", "month_year", "rel_mo_yr", "meesterwerk", "stars", "is_track", "is_track_object", "is_live", "article_title", "critics_review", "diary_text", "display_name", "mp3", "photo", "played", "load_error", "query", "youtube_query", "Set", "next_Offset", "disp_name" ];
    const header = ["#", "display_name", "boxid", "songid", "radioid", "radioname", "tracknum", "offset", "nxtoffs", "duration", "artist", "song", "video_song", "sartist", "ssong", "svideo_song", "genre", "language", "purpose", "month_year", "rel_mo_yr", "meesterwerk", "stars", "is_track", "is_track_object", "is_live", "weight_id", "article_title", "critics_review", "diary_text", "mp3", "photo", "query", "youtube_query", "played", "load_error" ];

//  const header = Object.keys(data[1]); // data[0] does not exist
    const csvRows = [];

    // Function to escape double-quotes within a value
    const escapeQuotes = value => {
        if (typeof value === 'string') {
            return `"${value.replace(/"/g, '""').replace(/\r\n/g, '<CRLF>').replace(/\n/g, '<LF>').replace(/\r/g, '<CR>').replace(/\t/g, '<TAB>')}"`;
        }
        return value;
    };

    const header_row = header.map(escapeQuotes).join(',');

    csvRows.push(header_row);

    var rnum = 0;
    for (const object of data) {
        if (object) {
            rnum += 1;
            const values = header.map(key => {
                if (key === 'mp3' || key === 'played' || key === 'load_error') {
                    return escapeQuotes(object[key].join("|"));
                } else if (key === '#') {
                    return escapeQuotes(rnum);
                } else {
                    return escapeQuotes(object[key]);
                }
            });
            const data_row = values.join(',');
            csvRows.push(data_row);
        }
    }

    return csvRows;
}

function fTrackObject() {

    this.divobj = undefined;
    this.boxid = "";
    this.songid = "";
    this.radioid = "";
    this.radioname = "";
    this.tracknum = "";
    this.offset = -1;
    this.nxtoffs = -1;
    this.duration = -1;
    this.artist = "";
    this.song = "";
    this.video_song = "";
    this.sartist = "";
    this.ssong = "";
    this.svideo_song = "";
    this.genre = "";
    this.language = "";
    this.purpose = "";
    this.month_year = "";
    this.rel_mo_yr = "";
    this.meesterwerk = false;
    this.stars = 0;
    this.is_track = false;         // parent.songid = this.radioid   media = parent.mp3
    this.is_track_object = false;  // track w/o it's own play
    this.is_live = false;

    this.weight_id = "-";

    this.article_title = "";
    this.critics_review = "";
    this.diary_text = "";
    this.display_name = "";
    this.mp3 = [];
    this.photo = "";
    this.played = [];
    this.load_error = [];
    this.query = "";
    this.youtube_query = "";

    for (var useWma = 0; useWma <= 4; useWma++) {
        this.played[useWma] = cNotPlayed;
        this.mp3[useWma] = "";
        this.load_error[useWma] = ""; // "Not yet loaded"
    }
    this.played[cReal] = cNotPlayed; // cWaiting;     --- not yet sent to YouTube
    this.played[cKaraoke] = cNotPlayed; // cWaiting;

    this.Set = setTrack;

    function setTrack(tboxid, tsongid, tradioid, tradioname, ttracknum, toffset, tnxtoffset, tduration, tartist, tsong, tvideo_song, tgenre, tlanguage, tpurpose, tversion_of_song, tmonth_year, trel_mo_yr, tmeesterwerk,
                      tarticle_title, tcritics_review, tdiary_text, tmp3, twma, tphoto, tstars, tis_trackobj, tis_track) {

        if (tboxid === "") {
            this.divobj = undefined;
        } else if (tis_trackobj) {
            this.divobj = $(tboxid);
        } else {
            this.divobj = $(tboxid).parent();
        }
        this.boxid = tboxid;
        this.songid = tsongid;  // changed below for tracks
        this.radioid = tradioid;
        this.radioname = tradioname;
        this.tracknum = ttracknum;
        this.artist = tartist;
        this.song = tsong;
        this.video_song = tvideo_song;
        this.sartist = removeAccents(tartist.trim().toLowerCase());
        this.ssong = removeAccents(tsong.toLowerCase());
        this.svideo_song = removeAccents(tvideo_song.toLowerCase());
        this.genre = tgenre;
        this.purpose = tpurpose;
        this.language = tlanguage;
        this.month_year = tmonth_year;
        this.rel_mo_yr = trel_mo_yr;
        this.meesterwerk = tmeesterwerk === "True" || tmeesterwerk === "true";

        //    LIVE = [Radio Contents].[Version] == "Live" OR[Latincitas Songs].[Purpose] == "BAND"

        //    [Radio Contents].[Version] => [Version of Song] => version_of_song => 31
        //    [Latincitas Songs].[Purpose] => Purpose => purpose => 8

        if (tversion_of_song.toUpperCase() === "LIVE" ||
            tpurpose.toUpperCase() === "BAND") {
            this.is_live = true;
        }

        this.article_title = tarticle_title;
        this.critics_review = tcritics_review;
        this.diary_text = tdiary_text;
        this.mp3[cMusic] = tmp3.replace("~", base_url);
        if (twma !== "") {
            if (twma.indexOf(".wav") > 0 || twma.indexOf(".wma") > 0) {
                this.mp3[cHdAudio] = twma.replace("~", base_url);  
            } else {
                this.mp3[cVideo] = twma.replace("~", base_url); // stores both karaoke's and videos
            }
        }
        this.photo = tphoto.replace("~", base_url);
        this.is_track_object = tis_trackobj;
        this.is_track = tis_track;
        this.stars = tstars;

        if (toffset !== "") {
            this.offset = parseInt(toffset);
            if (tnxtoffset !== "") {
                this.nxtoffs = parseInt(tnxtoffset);
            }
            if (tduration !== "") {
                this.duration = parseInt(tduration);
            }
            var sradioid = this.radioid + "";
            var itracknum = parseInt(this.tracknum);
            var sntracknum = itracknum + "";
            var key = "x_" + sradioid + "_" + sntracknum;
            offs_lookup[key] = this.offset;  // hopefully won't need this anymore

            ///////////////////////////////////////////// should be set by .xsd
            //  if ((itracknum >= 1) && (this.duration > 0)) {
            //      this.meesterwerk = true;  // *** until we have a chance to mark radio tracks as gemstones, mark them so they are not filtered out
            //  }
        } else {
            this.offset = -1;
        } 
        if (this.offset >= 0) {
            this.is_track = true;   // if it has an offset mark it as a track
        } // offset = 0 (first track) ...caller must do this

        if (this.is_track) {
            if ((this.radioid !== "") && (this.tracknum !== "")) {
                var songid_num = parseFloat(this.songid);
                if ((songid_num > 1000) && (songid_num < 3000)) {
                    // songid was already collision proofed by caller (see CollectioLoader::Bind_SongItom)
                } else {
                    var tracknum_num = parseInt(this.tracknum);
                    if (tracknum_num < 10) {
                        this.songid = this.radioid + ".0" + this.tracknum + "";
                    } else {
                        this.songid = this.radioid + "." + this.tracknum + "";
                    }
                }
            }
        } else {
            var songid_num2 = parseFloat(this.songid);
            if ((songid_num2 > 1000) && (songid_num2 < 3000)) {
                this.is_track = true;   // only tracks are given this special songid
            }
        }
        var dname = this.song + " / " + this.artist;
        if (this.month_year !== "") {
            dname += " [" + this.month_year + "]";
        }
        dname += " (#" + this.songid + ") ";
        this.display_name = dname;

        this.disp_name = Disp_Name;

        // A = studio (6 star)
        // B = solo latincita (6 star)
        // C = orchestra (con latincita) (6 star)
        // D = orchestra (other) (6 star)

        // a = studio (5 star)
        // b = solo latincita (5 star)
        // c = orchestra (con latincita) (5 star)
        // d = orchestra (other) (5 star)

        // + = studio (4 star)

        this.weight_id = '-';
        if (this.is_track) {
            if (this.is_live) {
                // live performance
                if (this.radioname.match(/latincita/gi)) {
                    // solo latincita
                    this.weight_id = 'B';
                } else {
                    // latincita bands
                    if (this.article_title.match(/latincita/gi)) {
                        this.weight_id = 'C';
                    } else {
                        this.weight_id = 'D';
                    }
                }
            } else {
                // studio recording (from radio show)
                this.weight_id = 'A';
            }
        } else {
            if (this.purpose === "BAND" || this.purpose === "RADIO") {
                this.weight_id = '-';
            } else {
                // studio recording
                this.weight_id = 'A';
            }
        }

        // A = studio
        // B = solo latincita
        // C = orchestra (con latincita)
        // D = orchestra (other)

        //  5% chance of selecting a "D" ... good live
        // 20% chance of selecting a "C" ... orchestra latincita
        // 30% chance of selecting a "B" ... latincita live
        // 45% chance of selecting a "A" ... studio

     // cWEIGHT_PATTERN = "DDDDCCCCCCCCCCCCCCBBBBBBBBBBBBBBBBBBBBBBBBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

     // var widx = Math.floor(Math.random() * cWEIGHT_PATTERN.length);
     // var weight_id = cWEIGHT_PATTERN[widx];
     // only select tracks w/ this weight_id

        this.next_Offset = next_offset;
    }

    function next_offset() {
        if (this.offset < 0) {
            return -1;
        }
        var nxtoffs = -1;
        var sradioid = this.radioid + "";
        for (var i = 1; i <= 20; i++) {
            var itracknum = parseInt(this.tracknum) + i;
            var sntracknum = itracknum + "";
            var key = "x_" + sradioid + "_" + sntracknum;
            if (offs_lookup.hasOwnProperty(key) >= 0) {
                nxtoffs = offs_lookup[key];
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
    function Disp_Name() {


    }
}

function Make_Bar(A,B,C,D) {  // cWEIGHT_PATTERN = Make_Bar()

    var tot = A + B + C + D;
    var bar = "";

    while (tot > 0) {
        var retry = true;
        //  n = Int((4 - 1 + 1) * Rnd(1) + 1)
        var n = Math.floor(Math.random() * 4) + 1;

        while (retry && (tot > 0)) {
            retry = false;
            if (n <= 1) {
                if (A > 0) {
                    A -= 1; tot -= 1;
                    bar += 'A';
                } else {
                    n = 2;
                    retry = true;
                }
            } else if (n === 2) {
                if (B > 0) {
                    B -= 1; tot -= 1;
                    bar += 'B';
                } else {
                    n = 3;
                    retry = true;
                }
            } else if (n === 3) {
                if (C > 0) {
                    C -= 1; tot -= 1;
                    bar += 'C';
                } else {
                    n = 4;
                    retry = true;
                }
            } else {
                if (D > 0) {
                    D -= 1; tot -= 1;
                    bar += 'D';
                } else {
                    n = 1;
                    retry = true;
                }
            }
        }
    }

    return bar;
}

function loadVideoTracks(hide_all) {
    loadTracks(hide_all);
}

function loadTracks(hide_all) {

//  $.mobile.loading('show');

    defaultTitle = document.title;

    hide_all_page = hide_all;

    if (!String.prototype.trim) {
        String.prototype.trim = function () {
            return this.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
        };
    }
    if (!Math.trunc) {
        Math.trunc = function (v) {
            return v < 0 ? Math.ceil(v) : Math.floor(v);
        };
    }

    check_only_gemstones();

    insertStars();

    var playButtons = $("[id^=PlayButton]");

    if (playButtons && playButtons.length > 0) {
        playButtons.prop('title', 'Cue/Play/Stop/DeCue');  // someone is removing title from all Play buttons !!
    }
    on_video_page = false;
    if (current_page.match(/.*VIDEO.*/)) {
        on_video_page = true;
    }
    var sfx = '';
    if (on_video_page) {
        sfx = 'V';
    }

    var hidePanel = undefined;
    if (on_video_page) {
        sortPanel = $("#sortControlGroupCV");
        hidePanel = $("#sortControlGroupC");
    } else {
        sortPanel = $("#sortControlGroupC");
        hidePanel = $("#sortControlGroupCV");
    }
    if (hidePanel && hidePanel.length > 0) {
        //  hidePanel.removeClass("css_hidden");
        hidePanel.hide();
    } else {
        hidePanel = undefined;
    }

    if (sortPanel && sortPanel.length > 0) {
    //  sortPanel.removeClass("css_hidden");
        sortPanel.hide();
    } else {
        sortPanel = undefined;
    }
    top_to_bottom = false;

    switch (current_page) {
        case "#MUSIC.search1":
        case "#MUSIC.search2":
        case "#MUSIC.search":
            // allowed to sort list
            break;
        case "#MUSIC.merengue":
        case "#MUSIC.salsa":
        case "#MUSIC.idols":
            top_to_bottom = true; // video's are pre-sorted
            // allowed to sort list - sort is always visible
            break;
        case "#VIDEOS.mtv":
        case "#VIDEOS.live":
        case "#VIDEOS.solo":
        case "#VIDEOS.prive":
            top_to_bottom = true; // video's are pre-sorted
            // allowed to sort list - sort is always visible
            break;
        default:
            top_to_bottom = true;  // CD's need to be loaded onto track-list in order of track on CD
            sortPanel = undefined;
    }
    if (sortPanel !== undefined) {

        init_sort_menu();

        $("#SortSongs" + sfx).click(sortSongs);
    }

    num_fsongs = 0;
    numSongs = 0;
    numTracks = 0;
    numBands = 0;
    numRadios = 0;

    var songList = $("[id^=SongListData]");
    var trackList = $("[id^=TrackListData]");
    var bandList = $("[id^=BandListData]");
    var radioList = $("[id^=RadioListData]");

    numSongs = songList.length;
    numTracks = trackList.length;
    numBands = bandList.length;
    numRadios = radioList.length;

    if (numTracks > 0) {
        $.merge(songList, trackList);
    }
    if (numBands > 0) {
        $.merge(songList, bandList);
    }
    if (numRadios > 0) {
        $.merge(songList, radioList);
    }
    songList.each(function (index) {

        // Label1 = Song
        // Label2 = Artist
        // Label3 = Language - Genre
        // Label4 = "month YYYY"
        // Label5 = "  (*)"
        // Label6 = ID

        var isTrackObj = ((index >= numSongs) && (index < (numSongs + numTracks)));
        var isTrack = isTrackObj;  // or offset > 0

        var data = $(this).text();
        var fields = data.split(",");
        for (var i = 0; i < fields.length; i++) {
            fields[i] = fields[i].replace(/<COMMA>/g, ",").replace(/<QUOTE>/g, "'").replace(/<CR>/g, "\r\n").replace(/<TAB>/g, "\t");
        }
                //    LIVE = [Radio Contents].[Version] == "Live" OR[Latincitas Songs].[Purpose] == "BAND"

                //    [Radio Contents].[Version] => [Version of Song] => version_of_song => 31
                //    [Latincitas Songs].[Purpose] => Purpose => purpose => 8 => purpose

        var box_id = fields[0] + "";
        var song_id = fields[1] + "";
        var artist = fields[14] + "";
        var song = fields[12] + "";
        var video_song = fields[13] + "";
        var genre = fields[7] + "";
        var lang = fields[6] + "";
        var month_year = fields[2];    // recorded on
        var rel_mo_yr = fields[3];     // released on
        var mesterwerk = fields[10] + "";
        var purpose = fields[8] + "";

        var article_title = fields[11] + "";
        var critics_review = fields[19] + "";
        var diary_text = fields[20] + "";
        var mp3 = fields[17] + "";   // song_url
        var wma = fields[16] + "";
        var photo = fields[21] + "";
        var tracknum = fields[22] + "";
        var radio_id = fields[23] + "";
        var radio_name = fields[24] + "";
        var soffset = fields[25] + "";
        var snxtoffset = fields[26] + "";
        var sduration = fields[27] + "";
        var stars = parseInt(fields[28]);
        var real_url = fields[29];
        var karaoke_url = fields[30];
        var version_of_song = fields[31];

        if (songIsVideo(mp3)) {
            mesterwerk = "True";  // bug... all videos seem to have this set to false
        }

        var track = new fTrackObject();

        track.Set(box_id, song_id, radio_id, radio_name, tracknum, soffset, snxtoffset, sduration, artist, song, video_song, genre, lang, purpose, version_of_song, month_year, rel_mo_yr, mesterwerk,
                  article_title, critics_review, diary_text, mp3, wma, photo, stars, isTrackObj, isTrack);

        new_song_id = track.songid; // fetch final songid

        var elem = new Object();
        elem.stars = stars;
        elem.real_url = real_url; 
        elem.karaoke_url = karaoke_url;
        elem.songId = new_song_id;

        star_data[new_song_id] = elem;  // up to now, Lord knows what songid was for tracks, it is now "radio.track"

        num_fsongs++;
        theFsongs[num_fsongs] = track;
                                      // storing video directly lets us load video when API is not working
        if (!USE_YOUTUBE_LOAD_API) {  // but it skips retrieval of meta-data
            if (real_url !== "") {
                //  theFsongs[num_fsongs].mp3[cReal] = real_url;
                //  theFsongs[num_fsongs].load_error[cReal] = bNoError;
                //  showPlayState(num_fsongs, cReal, cEnabled);
                //  store_video(id, tracknum, useWma, video_id, title, desc, poster, month_year, youtube_query, mark_as_default)
                store_video(num_fsongs, 1, cReal, real_url, "", "", "", "", "", true);
            }
            if (karaoke_url !== "") {
                //  theFsongs[num_fsongs].mp3[cKaraoke] = karaoke_url;
                //  theFsongs[num_fsongs].load_error[cKaraoke] = bNoError;
                //  showPlayState(num_fsongs, cKaraoke, cEnabled);
                //  store_video(id, tracknum, useWma, video_id, title, desc, poster, month_year, youtube_query, mark_as_default)
                store_video(num_fsongs, 1, cKaraoke, karaoke_url, "", "", "", "", "", true);
            }
        }

        if (track.is_track_object === false) { // tracks don't have their own play-buttons
            if (mp3 === "") {
                if (theFsongs[num_fsongs].divobj != undefined) {
                    var playBox = theFsongs[num_fsongs].divobj.find(play_sel(cMusic));
                    if (playBox && playBox.length > 0) {
                        playBox.addClass('ui-disabled');
                    }
                }
            }
            if (wma === "") {
                if (current_page === "#MUSIC.karaoke") { // only do this on Karaoke's page
                    if (theFsongs[num_fsongs].divobj != null) { //               vvvvvv --- why cVideo ???
                        var playBox = theFsongs[num_fsongs].divobj.find(play_sel(cVideo));
                        if (playBox && playBox.length > 0) { //                  ^^^^^^ --- cVideo was 1 == cKaraoke
                            playBox.addClass('ui-disabled');
                        }
                    }
                }
            }
            var tpurpose = theFsongs[num_fsongs].purpose;
            if (tpurpose === 'BAND' || tpurpose === 'RADIO' || tpurpose === 'YOUTUBE') {
                showPlayState(num_fsongs, cReal, cDisabled);
                showPlayState(num_fsongs, cKaraoke, cDisabled);
            } else {
                showPlayState(num_fsongs, cReal, cNotPlayed);
                showPlayState(num_fsongs, cKaraoke, cNotPlayed);
            }
            //var playBox = theFsongs[num_fsongs].divobj.find(play_sel(cReal));
            //if (playBox && playBox.length > 0) {
            //    playBox.addClass('ui-disabled-x');
            //    var playBox = theFsongs[num_fsongs].divobj.find(play_sel(cKaraoke));
            //    if (playBox && playBox.length > 0) {
            //        playBox.removeClass('ui-disabled'); // in case code up above (cVideo) was executed
            //        playBox.addClass('ui-disabled-x');
            //    }
            //}

            if (track.divobj !== undefined) {
                if (hide_all_page) {
                    track.divobj.hide();
                } else {
                    track.divobj.show();
                }
            }
        }
    });
    // alert("num songs = " + num_fsongs);  <<---  need some place tocopy this to !

    var csv_rows = convertToCSV(theFsongs);
    console.log(csv_rows.join('\n'));

    // alert("theFsongs was dumped to the Console.")

    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].divobj !== undefined) {
            if (theFsongs[i].is_track_object === true) {
                //  theFsongs[i].nxtoffs = theFsongs[i].next_Offset();
                //  if (theFsongs[i].nxtoffs > theFsongs[i].offset) {
                //      theFsongs[i].duration = theFsongs[i].nxtoffs - theFsongs[i].offset;
                //  } else {
                //      theFsongs[i].duration = 2 * 60 * 60;  // 2 hours
                //  }
                var clickCels = theFsongs[i].divobj.find(".c");
                if (clickCels && clickCels.length > 0) {
                    clickCels.click(function (e) {
                        // var event = e || window.event;
                        // seems to block any events sent to children and propagated to $(this)
                        // event.stopPropagation ? event.stopPropagation() : (event.cancelBubble = true);
                        var click_cel = $(this);
                        play_track(click_cel);
                    });
                }
            }
        }
    }

    showAllStars();

//  $.mobile.loading('hide');

}

function addDummyTrack(data) {

    var fields = data.split(",");
    for (var i = 0; i < fields.length; i++) {
        fields[i] = fields[i].replace(/<COMMA>/g, ",").replace(/<QUOTE>/g, "'").replace(/<CR>/g, "\r\n").replace(/<TAB>/g, "\t");
    }
            //    LIVE = [Radio Contents].[Version] == "Live" OR[Latincitas Songs].[Purpose] == "BAND"

            //    [Radio Contents].[Version] => [Version of Song] => version_of_song => 31
            //    [Latincitas Songs].[Purpose] => Purpose => purpose => 8 => purpose

    var box_id = fields[0] + "";
    var song_id = fields[1] + "";
    var artist = fields[14] + "";
    var song = fields[12] + "";
    var video_song = fields[13] + "";
    var genre = fields[7] + "";
    var lang = fields[6] + "";
    var month_year = fields[2];    // recorded on
    var rel_mo_yr = fields[3];     // released on
    var mesterwerk = fields[10] + "";
    var purpose = fields[8] + "";

    var article_title = fields[11] + "";
    var critics_review = fields[19] + "";
    var diary_text = fields[20] + "";
    var mp3 = fields[17] + "";   // song_url
    var wma = fields[16] + "";
    var photo = fields[21] + "";
    var tracknum = fields[22] + "";
    var radio_id = fields[23] + "";
    var radio_name = fields[24] + "";
    var soffset = fields[25] + "";
    var snxtoffset = fields[26] + "";
    var sduration = fields[27] + "";
    var stars = parseInt(fields[28]);
    var real_url = fields[29];
    var karaoke_url = fields[30];
    var version_of_song = fields[31];

    var isTrackObj = false;
    var isTrack = false;

    if (songIsVideo(mp3)) {
        mesterwerk = "True";  // bug... all videos seem to have this set to false
    }

    var track = new fTrackObject();

    track.Set(box_id, song_id, radio_id, radio_name, tracknum, soffset, snxtoffset, sduration, artist, song, video_song, genre, lang, purpose, version_of_song, month_year, rel_mo_yr, mesterwerk,
              article_title, critics_review, diary_text, mp3, wma, photo, stars, isTrackObj, isTrack);

    new_song_id = track.songid; // fetch final songid

    var elem = new Object();
    elem.stars = stars;
    elem.real_url = real_url; 
    elem.karaoke_url = karaoke_url;
    elem.songId = new_song_id;

    star_data[new_song_id] = elem;  // up to now, Lord knows what songid was for tracks, it is now "radio.track"

    num_fsongs++;
    theFsongs[num_fsongs] = track;

    return num_fsongs;
}

function init_select_language() {

    $("#selectLanguage").on("change", function () {
        // Get the selected option's text
        var selectedText = $(this).find("option:selected").text();
        var selectedVal  = $(this).val();
    //  alert("You selected: " + selectedText);
        var page_lang = "";
        if (selectedText === "Español..." || selectedVal === "esp") {
            page_lang = "es-ES";
        } else if (selectedText === "Nederlands..." || selectedVal === "ned") {
            page_lang = "nl-NL";
        } else {
            page_lang = "en-US";
        }
        select_language(page_lang,false);
    });

//  alert("Select Language Ready");
}

function select_language(page_lang,select_option) {

    var who_is = "";
    var page_title = "";
    var page_header = "";
    var sel_option = "";
    if (page_lang === "es-ES") {
        who_is = "quien_es_latincita_3.html";
        page_title = "Latincita - Página de Wikipedia";
        page_header = "Página de Wikipedia sobre Laticita";
        sel_option = "esp";
    } else if (page_lang === "nl-NL") {
        who_is = "wie_is_latincita_3.html";
        page_title = "Laticita - Wikipedia Pagina";
        page_header = "Laticita's Wikipedia Pagina";
        sel_option = "ned";
    } else {
        who_is = "who_is_latincita_3.html";
        page_title = "Laticita - Wikipedia Page";
        page_header = "Laticita's Wikipedia Page";
        page_lang = "en-US";
        sel_option = "eng";
    }
    $(document).prop('title', page_title);
    $("#HeaderText1").text(page_header);
    $("html").attr('lang', page_lang);
    $("#text-whois").load("/Mobile/" + who_is);
    if (select_option) {
        $("#selectLanguage").val(sel_option).selectmenu("refresh");
    }
}

function init_sort_menu() {

    if (sortPanel === undefined) { return; }

    var sfx = '';
    if (on_video_page) {
        sfx = 'V';
    }

    $('#sortMusicList1' + sfx).delegate('li', 'click', function () {
        sort_music_1 = $(this).text();
        $("#sortMusic1" + sfx).popup("close");
        show_sort_orders();
    });
    $('#sortMusicList2' + sfx).delegate('li', 'click', function () {
        sort_music_2 = $(this).text();
        $("#sortMusic2" + sfx).popup("close");
        show_sort_orders();
    });
    $('#sortMusicList3' + sfx).delegate('li', 'click', function () {
        sort_music_3 = $(this).text();
        $("#sortMusic3" + sfx).popup("close");
        show_sort_orders();
    });

    if (!hide_all_page) {
        //  for pages that support sorting
        //  if tiles are not initially hidden
        //  show sort-controls
        sortPanel.show();
    }
    show_sort_orders();
}

function show_sort_orders() {

    var sfx = '';
    if (on_video_page) {
        sfx = 'V';
    }
    if (sort_music_1 !== undefined) {
        if (sort_music_1.indexOf("-") > 0) {
            sort_music_1 = undefined;
        }
    }
    if (sort_music_2 !== undefined) {
        if (sort_music_2.indexOf("-") > 0) {
            sort_music_2 = undefined;
        }
    }
    if (sort_music_3 !== undefined) {
        if (sort_music_3.indexOf("-") > 0) {
            sort_music_3 = undefined;
        }
    }
    if (sort_music_1 === undefined) {
        $("#sortMusicBut1" + sfx).html("sort...");
        $("#sortMusicBut1" + sfx).addClass("ui-right-button-x");
        $("#sortMusicBut1" + sfx).addClass("ui-last-button-x");
        $("#sortMusicBut2" + sfx).hide();
        $("#sortMusicBut3" + sfx).hide();
        sort_music_2 = undefined;
        sort_music_3 = undefined;

        $("#SortSongs" + sfx).hide();
    } else {
        $("#sortMusicBut1" + sfx).html(sort_music_1);

        $("#SortSongs" + sfx).show();

        if (sort_music_1.indexOf("ID") > 0) {
            // if sorted by ID, then can't sort on anything else, because ID is unique
            $("#sortMusicBut1" + sfx).addClass("ui-right-button-x");
            $("#sortMusicBut1" + sfx).addClass("ui-last-button-x");
            $("#sortMusicBut2" + sfx).hide();
            $("#sortMusicBut3" + sfx).hide();
            sort_music_2 = undefined;
            sort_music_3 = undefined;
        } else {
            $("#sortMusicBut1" + sfx).removeClass("ui-right-button-x");
            $("#sortMusicBut1" + sfx).removeClass("ui-last-button-x");
            $("#sortMusicBut2" + sfx).show();

            if (sort_music_2 === undefined) {
                $("#sortMusicBut2" + sfx).html("sort...");
                $("#sortMusicBut2" + sfx).addClass("ui-right-button-x");
                $("#sortMusicBut2" + sfx).addClass("ui-last-button-x");
                $("#sortMusicBut3" + sfx).hide();
                sort_music_3 = undefined;
            } else {
                $("#sortMusicBut2" + sfx).html(sort_music_2);

                if (sort_music_2.indexOf("ID") > 0) {
                    // if sorted by ID, then can't sort on anything else, because ID is unique
                    $("#sortMusicBut2" + sfx).addClass("ui-right-button-x");
                    $("#sortMusicBut2" + sfx).addClass("ui-last-button-x");
                    $("#sortMusicBut3" + sfx).hide();
                    sort_music_3 = undefined;
                } else {
                    $("#sortMusicBut2" + sfx).removeClass("ui-right-button-x");
                    $("#sortMusicBut2" + sfx).removeClass("ui-last-button-x");
                    $("#sortMusicBut3" + sfx).addClass("ui-right-button-x");
                    $("#sortMusicBut3" + sfx).addClass("ui-last-button-x");
                    $("#sortMusicBut3" + sfx).show();

                    if (sort_music_3 === undefined) {
                        $("#sortMusicBut3" + sfx).html("sort...");
                    } else {
                        $("#sortMusicBut3" + sfx).html(sort_music_3);
                    }
                }
            }
        }
    }
}


function sortSongs() {

    console.log("filter_mobile::start sort");

    var song_list = track_list_items();  // set's sortable_divs

    //sortable_divs = [];

    //for (var i = 1; i <= num_fsongs; i++) {
    //    if (theFsongs[i].is_track_object === false) {
    //        if (theFsongs[i].divobj.is(':visible') === true) {
    //            var songid = theFsongs[i].songid;

    //            sortable_divs[songid] = theFsongs[i].divobj;

    //            if (song_list === "") {
    //                song_list = songid;
    //            } else {
    //                song_list = song_list + "," + songid;
    //            }
    //        }
    //    }
    //}
    var target = this_target;
    if (current_page === "#MUSIC.search1") {
        // NASTY... we are on music.search1, but music.search2 which is running in an iframe on
        //          our page is the only one who knows which songs were loaded
        //          so, we need to redirect our sort request to him
        target = target.replace(/search1/, "search2");
    }

    sendSortRequest(target, sort_music_1, sort_music_2, sort_music_3, song_list, process_sort_music);
}

function process_sort_music(curr_sort_ids, new_sort_ids) {

    if (new_sort_ids === "") {
        alert("NO SORT ORDER RECEIVED FROM SERVER.");
    }
    if (new_sort_ids === curr_sort_ids) {
        alert("LIST IS ALREADY SORTED.");
    }

    var id_list = new_sort_ids.split(",");

    var sorted = "";

    for (var i = id_list.length - 1; i >= 0; i--) {  // need to reverse process the list
        var songid = id_list[i];
        var div = sortable_divs[songid];
        if (div !== undefined) {
            if (div.length > 0) {
                $('#dataItemList').prepend(div);  // move to top of list
                list_is_sorted = true;
            }
        }
    }
    format_track_list();

    console.log("filter_mobile::done sorting");
}

function check_sortable() {

    if (sortPanel === undefined) { return; }

    var num_sortable = 0;

    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].divobj !== undefined) {
            if (theFsongs[i].is_track_object === false) {
                if (theFsongs[i].divobj.is(':visible') === true) {
                    num_sortable++;
                    if (num_sortable >= 2) {
                        break;
                    }
                }
            }
        }
    }
    if (num_sortable > 1) {  // should work regardless of hide_all_page
        sortPanel.show();
    } else {
        sortPanel.hide();
        list_is_sorted = false;
    }
}

function initPlaylist_bad() {

    myPlaylist = new jPlayerPlaylist({
        jPlayer: "#jquery_jplayer_1",
        cssSelectorAncestor: "#jp_container_1"
    }, [
            {
                //title:"Cro Magnon Man", 
                //artist:"The Stark Palace", 
                //mp3:"http://www.jplayer.org/audio/mp3/TSP-01-Cro_magnon_man.mp3", 
                //oga:"http://www.jplayer.org/audio/ogg/TSP-01-Cro_magnon_man.ogg", 
                poster: base_url + "/Images/Mobile/gold_find_music-P.png"
            }
        ], {
            playlistOptions: {
                enableRemoveControls: true
            },
            swfPath: "js",
            supplied: "m4v, mp3"
        });
}

function initPlaylist_works() {

    myPlaylist =
        new jPlayerPlaylist({
            jPlayer: "#jquery_jplayer_1",
            cssSelectorAncestor: "#jp_container_1"
        }, [
            ], {
                //  swfPath: "../dist/jplayer",
                supplied: "mp3, m4v",
                enableRemoveControls: true,
                useStateClassSkin: true,
                autoBlur: false,
                smoothPlayBar: true,
                keyEnabled: true,
                autoPlay: false
            });
    init_playlist_drag(myPlaylist);
}

function track_list_items() {
    // fetch currently ordered list of visible music selections

//  var tplaylist = [];
    var song_list = "";

    sortable_divs = [];

    var trackList = $('#dataItemList').children("li");
    if (trackList.length > 0) {
        trackList.each(function (index) {
            var n = 0;
            var div1 = $(this);
            if (div1.is(':visible')) {
                for (var i = 1; i <= num_fsongs; i++) {
                    if (theFsongs[i].divobj !== undefined) {
                        if (theFsongs[i].divobj[0] === div1[0]) {
                            n = i; break;
                        }
                    }
                }
            }
            if (n > 0) {
                var songid = theFsongs[n].songid;
                sortable_divs[songid] = div1;
            //  tplaylist.push(n);
                if (song_list === "") {
                    song_list = songid;
                } else {
                    song_list = song_list + "," + songid;
                }
            }
        });
    }
    console.log("current sort-list: " + song_list);
    return song_list;
}

function format_track_list() {
    // round corners @ top and bottom of list
    var trackList = $('#dataItemList').children("li");
    var firstDiv = -1;
    var lastDiv = -1;
    if (trackList.length > 0) {
        trackList.removeClass("ui-first-child");
        trackList.removeClass("ui-last-child");
        trackList.each(function (index) {
            var div1 = $(this);
            if (div1.is(':visible')) {
                lastDiv = index;
                if (firstDiv === -1) firstDiv = index;
            }
        });
    }
    if (firstDiv >= 0) {
        var div2 = trackList.eq(firstDiv);
        div2.addClass("ui-first-child");
    }
    if (lastDiv >= 0) {
        var div3 = trackList.eq(lastDiv);
        div3.addClass("ui-last-child");
    }
}

function unformat_track_list() {
    // remove rounded corners @ top & bottom
    var trackList = $('#dataItemList').children("li");
    if (trackList.length > 0) {
        trackList.addClass("ui-first-child");
        trackList.removeClass("ui-last-child");
    }
}

//       mobileCallback
function playlistEventHandler(playlistidx, sidx, useWma, entireRadio, event, curr_video) {

    var dup_event = false;
    var dup_obj = false;
    if ((playlistidx === playing.playlistidx) &&
        (sidx === playing.id) &&
        (useWma === playing.useWma) &&
        (curr_video === playing.curr_video)) {
        if (event === playing.action) {
            dup_event = true;  // skip redundant event triggers
        }
        dup_obj = true;
    }
    if (dup_obj && (!dup_event)) {
        if (((playing.action === 'tell_end') || (playing.action === 'stopped')) && ((event === 'pause') || (event === 'paused'))) {
            event = 'stopped';  // jPlayerCallback is sending us pause after tell_end when user press stop
            dup_event = true;   //                                 and after stopped when track ends
        }
    }
    var playBox = undefined;
    if (sidx === "") {
        sidx = -1;
    }
    if (sidx >= 1 && sidx <= num_fsongs) {
        if (event === 'info') {
            if (theFsongs[sidx].is_track_object === false) { // sometimes radio comes in, when it should be a track
                if (currentTrack >= 1 && currentTrack <= num_fsongs) {
                    if (theFsongs[currentTrack].radioid === theFsongs[sidx].songid) {
                        sidx = currentTrack;  // redirect event to current track
                    } else {
                        currentTrack = -1; // throw out track, it's not valid
                    }
                }
            } else {
                if (theFsongs[sidx].is_track_object === true) { // "info" is sometimes passing a different tracknum than current track-num
                    if (currentTrack >= 1 && currentTrack <= num_fsongs) {
                        if (theFsongs[currentTrack].radioid === theFsongs[sidx].radioid) {
                            sidx = currentTrack;  // redirect event to current track
                        } else {
                            currentTrack = -1; // throw out track, it's not valid
                        }
                    }
                }
            }
        }
        if (theFsongs[sidx].is_track_object === false) { // else no own playBox
            if (theFsongs[sidx].divobj !== undefined) {
                playBox = theFsongs[sidx].divobj.find(play_sel(useWma));
            }
        } else {
            if (event === "selected-track") { // after selected track is sending us a unmute w/ previous track #
                currentTrack = sidx;
            }
        }
    }
    console.info("filter_mobile:playlistEventHandler: playlistidx[" + playlistidx + "]  sidx[" + sidx + "]  event[" + event + "]  curr_video[" + curr_video + "]  currentTrack[" + currentTrack + "]  dup_event[" + dup_event + "]");

    var bord_color = "CornflowerBlue";
    switch (event) {
        case 'play':
        case 'played':
        case 'playing':
            if (!dup_event) {
                showPlayState(sidx, useWma, cPlaying);
                play_but.removeClass("ui-icon-v-play");
                play_but.addClass("ui-icon-v-pauze");
                stop_but.removeClass("ui-icon-v-empty");
                stop_but.addClass("ui-icon-v-stop");
            }
            bord_color = "Crimson";
            break;
        case 'deleted':
            if (!dup_event) {
                showPlayState(sidx, useWma, cRemoved);
                var empty = false;
                if (myPlaylist.playlist.length <= 0) {
                    empty = true;
                } else if (myPlaylist.playlist.length === 1) {
                    if ((myPlaylist.playlist[0].id === sidx) && (myPlaylist.playlist[0].useWma === useWma)) {
                        empty = true;
                    }
                }
                if (empty) {
                    if (removedItem !== null) {
                        myPlaylist.add(removedItem, false);
                        setTimeout(function () {
                            myPlaylist.select(0);
                        }, 1200); // give playlist time to process add before trying to select
                    }
                }
                init_playlist_drag(myPlaylist);
                check_clean_selids(sidx);
                if (myPlaylist.playlist.length <= 1) {
                    random_from_playlist = false;  // nothing left to select from, go back to random from all non-played
                }
            }
            break;
        case 'info':
            if (sidx >= 1 && sidx <= num_fsongs) {
                var tmedia = theFsongs[sidx].mp3[useWma];  // what do we do if track has changed ?
                if (songIsVideo(tmedia)) {
                    showVideoDetails(theFsongs[sidx],useWma);
                    $("#videoDetails").popup("open");
                } else {
                    showDetails(theFsongs[sidx],useWma);
                    $("#songDetails").popup("open");
                }
            } else {
                alert("Sorry no info available.");
            }
            break;
        case 'clicked':
            break;
        case 'pause':
        case 'paused':
            if (!dup_event) {
                showPlayState(sidx, useWma, cPlayed);
                play_but.removeClass("ui-icon-v-pauze");
                play_but.addClass("ui-icon-v-play");
            }
            bord_color = "DarkGoldenRod";
            break;
        case 'stop':
        case 'ended':
        case 'stopped':
        case 'track_ended':
        case 'tell_end':
        case 'tellEnd':
            if (!dup_event) {
                if (event !== 'track_ended') {
                    showPlayState(sidx, useWma, cPlayed); // if track ends, don't mark entire radio as 'played'
                }
                play_but.removeClass("ui-icon-v-pauze");
                play_but.addClass("ui-icon-v-play");
                stop_but.removeClass("ui-icon-v-stop");
                stop_but.addClass("ui-icon-v-empty");
                if (lastTrackSelector && lastTrackSelector.length > 0) {
                    lastTrackSelector.removeClass("tracklist-currtrack");
                    lastTrackSelector = undefined;
                }
                if (sidx >= 1 && sidx <= num_fsongs) {
                    if (theFsongs[sidx].is_track_object === false) {
                        var tmedia = theFsongs[sidx].mp3[useWma];
                        if (songIsVideo(tmedia)) {
                            if (event === "ended") {
                                // is a video stops playing, assume next item will automatically get played
                                // and assume next item is a video
                                // pause for 5 seconds before allowing auto-play next to occur
                            //  sleep(5000);  // note, before 8.80 this never worked, because auto-next came before callback !
                                sleep(2000);  // sleep(5000) seems to wait for a long long time
                            }
                        }
                    }
                }
                if (event == 'stopped' && entireRadio) {
                    // assume show just ended, try and queue up 1st song in show
                    var idx = -1;
                    for (var i = myPlaylist.playlist.length - 1; i >= 0; i--) { // does this work from MUSIC if item is a track ?
                        if ((myPlaylist.playlist[i].id === sidx) && (myPlaylist.playlist[i].useWma === useWma)) {
                            idx = i; break;
                        }
                    }
                    if (idx >= 0) {
                        myPlaylist.clearCurrent();
                        myPlaylist.select(idx);
                    }
                }
            }
            bord_color = "DarkRed";
            break;
        case 'muted':
            $("#v-mute").removeClass("ui-icon-v-mute");
            $("#v-mute").addClass("ui-icon-v-unmute");
            break;
        case 'unmuted':
            $("#v-mute").removeClass("ui-icon-v-unmute");
            $("#v-mute").addClass("ui-icon-v-mute");
            break;
        case 'full-screen':
            break;
        case 'exit-full-screen':
            break;
        case 'selected-track':
            if (lastTrackSelector && lastTrackSelector.length > 0) {
                lastTrackSelector.removeClass("tracklist-currtrack");
                lastTrackSelector = undefined;
            }
            if (sidx >= 1 && sidx <= num_fsongs) {
                if (theFsongs[sidx].divobj !== undefined) {
                    playBox = theFsongs[sidx].divobj.find(".h");
                    if (playBox && playBox.length > 0) {
                        playBox.addClass("tracklist-currtrack");
                        lastTrackSelector = playBox;
                    }
                }
            //  playBox = theFsongs[id].divobj.find("th");
            //  if (playBox && playBox.length > 0) {
            //      playBox = playBox.first();
            //      if (playBox && playBox.length > 0) {
            //          playBox.addClass("tracklist-currtrack");
            //          lastTrackSelector = playBox;
            //      }
            //  }
            }
            break;
        case 'selected-video':
            if (sidx >= 1 && sidx <= num_fsongs) {
                if (curr_video !== null) {
                    if (curr_video.hasOwnProperty('media')) {
                        theFsongs[sidx].mp3[useWma] = curr_video.media;
                    }
                }
                var curr_fav = Curr_Favorite(sidx, useWma);
                var clr = "#dac8c9";
                if ((curr_fav !== "") && (curr_video.media === curr_fav)) {
                    clr = "red";
                }
                var heart = $('#v-choose-video');
                if (heart.length > 0) {
                    heart.css("background-color", clr);
                }
            }
            break;
        case 'edit-video':
            edit_videolist(playlistidx, sidx,useWma,curr_video,false,"");
            break;
        case 'choose-video':
            chooseVideo(playlistidx, sidx, useWma, curr_video);
            break;
        case 'reject-video':
            rejectVideo(playlistidx, sidx, useWma, curr_video);
            break;
    }
    $("#jp_container_1").css("border-color", bord_color);

    if (event !== "info" && sidx >= 0) {
        playing = { 'playlistidx': playlistidx, 'id': sidx, 'useWma': useWma, 'curr_video': curr_video, 'action': event, 'time': new Date() };
    }
}

function isPlaying() {
    var is_playing = 0;
    if (playing.action === "played" || playing.action === "pause" || playing.action === "paused") {
        var thirtyMinAgo = new Date();
        thirtyMinAgo.setTime(thirtyMinAgo.getTime() - 30 * 60000);
        if (playing.time > thirtyMinAgo) {
            is_playing = 1;
        }
    }
    return is_playing;
}

function initCdPosters(cd_parms) {
    retrievePosterList(this_target, cd_parms, initCdPosters2);
}

function initCdPosters2(cd_parms) {
    var have_playlist = false;
    if (cd_parms.poster_list) {
        if (cd_parms.poster_list.length > 0) {
            cd_parms.poster_list.forEach(function (part, idx) {
                var art_work = this[idx];
                if (art_work.startsWith("~")) {
                    this[idx] = base_url + art_work.substring(1);
                } else {
                    this[idx] = base_url + "/Images/Mobile/" + art_work;
                }
            }, cd_parms.poster_list);

            var def_poster = cd_parms.poster_list[0];
            var p = def_poster.lastIndexOf("/");
            if (p > 0) {
                def_poster = def_poster.substr(p + 1);
            }
            cd_parms.def_poster = def_poster;

            have_playlist = true;
        }
    }
    initPlaylist(cd_parms.def_poster, false);

    setTimeout(function () {

        ////////////////////////////  need to pause play here after PAUSE is done
        ////////////////////////////  but before the rest

        myPlaylist.tell_end();

        if (have_playlist) {
            myPlaylist.set_poster_list(cd_parms.poster_list);
        }
        if (cd_parms.wants_music) {
            setWants(cd_parms2.user_wants);
        } else {
            addAllToPlaylist();
        }

        catchPlaylistClicks();

        setTimeout(function () { find_all_matching_videos(); }, 1200);

    }, 1200); // give pause time to get processed
}

function initPlaylist(art_work, start_with_video) {

    if (art_work.startsWith("~")) {
        art_work = base_url + art_work.substring(1);
    } else {
        art_work = base_url + "/Images/Mobile/" + art_work;
    }

    var items;
    var added_photo = "";
    if (start_with_video) {
        items = [
            {
                title: "LATINCITA",
                artist: "Latincita",
                m4v: "https://www.youtube.com/v/MxiH711SkUI",
                useWma: cMusic,  // ???
                id: 0
            }
        ];
    } else {
        items = [
            {
                title: "LATINCITA",
                artist: "Latincita",
                mp3: base_url + "/best_of_latincita/Latincita%20Opener.mp3",
                poster: art_work,
                useWma: cMusic,
                songid: "0",
                id: 0
            }
        ];
        added_photo = art_work;
    }

    play_but = $("#v-play");
    stop_but = $("#v-stop");

    stop_but.removeClass("ui-icon-v-stop");  // *** try and figure out how to do this in HTML
    stop_but.addClass("ui-icon-v-empty");

    myPlaylist =
        new jPlayerPlaylist({
            jPlayer: "#jquery_jplayer_1",
            cssSelectorAncestor: "#jp_container_1",
            jPlayerContainer: "#j_player",
            ytPlayerContainer: "#yt_player"
        }, items, {
                supplied: "m4v, mp3",
                useStateClassSkin: true,
                smoothPlayBar: true,
                keyEnabled: true,
                autoPlay: false,
                startWithVideo: start_with_video,
                mobileCallback: playlistEventHandler
            });

    myPlaylist.option("enableRemoveControls", true);
    myPlaylist.option("addTime", 'slow');
    myPlaylist.option("removeTime", 'slow');

    myPlaylist.select(0);
    if (start_with_video) {
        // only queue up 1st item, don't try to play it
        myPlaylist.pause();
    } else {
        myPlaylist.play(0);  // have to do this to force poster to be displayed
        myPlaylist.pause();
    }
    if (added_photo) {
        fetchImage(added_photo);
    }
    init_playlist_drag(myPlaylist);
////////////////////////////////// caller needs to do this
//  setTimeout(function () {
//      myPlaylist.tell_end();
//  }, 1200); // give pause time to get processed
}

function init_search_panel() {  // called from on("panelopen")

    nothing_selected = false;
    if (selIds === '' || selIds === '|' || selIds === '||') {
        if (selGenres === '' || selGenres === '|' || selGenres === '||') {
            if (selLang === '' || selLang === '|' || selLang === '||') {
                if (selArtists === '' || selArtists === '|' || selArtists === '||') {
                    if (selSongs === '' || selSongs === '|' || selSongs === '||') {
                        if (selShows === '' || selShows === '|' || selShows === '||') {
                            if (selGems === '' || selGems === '|' || selGems === '||') {
                                if (selStars === '' || selStars === '|' || selStars === '||') {
                                    if (selLiveStudio === '' || selLiveStudio === '|' || selLiveStudio === '||') {
                                        nothing_selected = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    if (nothing_selected === false) {
        return;  // retain previous selections
    }            // *** NOTE:  when panel is re-opened, all sections are collapsed
    clearClicked(); //         so user doesn't have a clue that previous selection is in effect
}                   //         when they try to select something different, nothing happens
                    //         because all filters use AND instead of OR
function extension(s) {
    if (s) {
        var p = s.lastIndexOf(".");
        if (p > 0) {
            return s.substr(p + 1).toLowerCase();
        }
    }
    return "";
}

function songIsVideo(tmedia) {
    var ext = extension(tmedia);
    if (tmedia.indexOf("youtu.be") > 0 || tmedia.indexOf(".youtube.") > 0 || ext === "mp4" || ext === "flv" || ext === "wmv") {
        return true;
    }
    return false;
}

function songIsShown(n) {
    if ((n < 1) || (n > num_fsongs)) { return false; }
    // tracks can't be hidden
    if (theFsongs[n].is_track_object === true) { return true; }
    if (theFsongs[n].divobj === null) { return true; }
    if (theFsongs[n].divobj.is(':visible') === true) {
        return true;
    } else {
        return false;
    }
}

function radio_check(n) {
    // no radio unless we're on the radio page
    if ((n < 1) || (n > num_fsongs)) { return false; }
    if ((current_page === "#MUSIC.radios") || (current_page === "#MUSIC.bands")) {
        if (theFsongs[n].purpose === 'BAND' || theFsongs[n].purpose === 'RADIO') {
            return true;
        }
    } else {
        if (theFsongs[n].purpose !== 'BAND' && theFsongs[n].purpose !== 'RADIO') {
            return true;
        }
    }
    return false;
}

function play_sel(useWma) {
    var sel = "EXISTS_NOT";
    switch (useWma) {
        case 0:
        case false:
            sel = ".ui-btn.ui-btn-icon-notext.ui-icon-audio";  // useWma can be false or 0
            break;
        case 1:
        case true:
            sel = ".ui-btn.ui-btn-icon-notext.ui-icon-bars";   // useWma can be 1 or true = karaoke
            break;
        case 2: 
            sel = ".ui-btn.ui-btn-icon-notext.ui-icon-video";  // useWma 2 = real
            break;
    }
    return sel;
}

function showSelection(show_all) {

    //  $.mobile.loading('show', {text: '...', textVisible: true, theme: 'a'});

    console.log("filter_mobile::showSelection .. start");

    showPageWait();

    print_error("Genres: " + selGenres);
    print_error("Langs: " + selLang);
    print_error("Artists: " + selArtists);
    print_error("Songs: " + selSongs);
    print_error("Shows: " + selShows);
    print_error("Gems: " + selGems);
    print_error("Stars: " + selStars);
    print_error("Live/Studio: " + selLiveStudio);
    print_error("Ids: " + selIds);

    var numsel = 0;
    var cnt = { match: 0, gems: 0 };
    var video_srch_cnt = 0;

    // display from old to new, this puts new on top
    for (var i = num_fsongs; i >= 1; i--) {

        var matches = false;

        if (show_all) {
            matches = true;
            var matchesSelId = false;
            var imatches = (selIds.indexOf("|" + theFsongs[i].songid + "|") >= 0);
            if (imatches) {
                matchesSelId = true;
            } else { // try ignoring usewma & video_id in selIds
                imatches = (selIds.indexOf("|" + theFsongs[i].songid + ".") >= 0);
                if (imatches) {
                    matchesSelId = true;
                }
            }
            if (radio_check(i) === false) {
                if (!matchesSelId) {
                    matches = false;
                }
            }
            if (only_gemstones) {
                if (!matchesSelId) {
                    if (theFsongs[i].meesterwerk !== true) {
                        matches = false;
                    }
                }
            }
            if (matches) {
                cnt.match++;
                if (theFsongs[i].meesterwerk === true) {
                    cnt.gems++;
                }
            }
        } else {
            matches = songMatchesFilter(i, cnt);
        }

        //print_error("Song[" + i + "] matches: " + matches +
        //                          "  genre: " + theFsongs[i].genre +
        //                          "  lang: " + theFsongs[i].language +
        //                          "  artist: " + theFsongs[i].artist +
        //                          "  song: " + theFsongs[i].song +
        //                          "  songid: " + theFsongs[i].songid);
        if (matches) {

        //  if (!show_all) {  // when showing all, don't fetch videos
                if (video_srch_cnt < cMAX_VIDEO_LOAD) { // <<<<<< FIND LIMIT
                    // search for loads of videos overloads the system
                    if (find_matching_videos(i)) { // <<< only pushes "i" onto queue ...if i has not been pushed or i is dead
                        video_srch_cnt++;
                    }
                }
        //  }

            if (theFsongs[i].is_track_object === false) {
                //  if (match_all === false) {


                //  // ========================================================= TEST
                //  for (var useWma = 0; useWma <= 4; useWma++) {
                //     if (theFsongs[i].mp3[useWma] === "") {
                //        theFsongs[i].mp3[useWma] = "https://www.youtube.com/watch?v=" + "7Yqlv0EhCkA";
                //        showPlayState(i, useWma, cEnabled);
                //     }
                //  }
                //  // ========================================================= TEST

                if (theFsongs[i].divobj !== undefined) {
                    if (theFsongs[i].divobj.is(':visible') === false) {
                        if (!show_all) { // when showing all, don't move things
                            $('#dataItemList').prepend(theFsongs[i].divobj);  // move to top of list
                        }
                        theFsongs[i].divobj.show();
                    }
                }
                //  }
                // ------------------- repair all colors
                for (var useWma = 0; useWma <= 4; useWma++) {
                    showPlayState(i, useWma, theFsongs[i].played[useWma]);
                }
            }

            numsel++;
        } else {
            if (hide_all_page) {
                if (theFsongs[i].divobj !== undefined) {
                    if (theFsongs[i].is_track_object === false) {
                        if (theFsongs[i].divobj.is(':visible') === true) {
                            theFsongs[i].divobj.hide();
                        }
                    }
                }
            }
        }
    }
    format_track_list();

    list_is_sorted = false;  // *** may want to auto-sort after appending new items

    if (numsel > 1) {
        random_from_playlist = true; // we've loaded songs via a query ... select random songs from this collection 
    } else {
        random_from_playlist = false;
    }

    //  alert("FOUND " + numsel + " MATCHES");

    check_sortable();

    if (video_srch_cnt > 0) {
        start_loader();
    }

    if (!clicking_checkbox) {
        //  loadNewMatchList();
    }
    var num_gems = cnt.gems;
    var num_other = cnt.match - cnt.gems;
    var gemS = "s";
    var otherS = "s";
    if (num_gems === 1) { gemS = ""; }
    if (num_other === 1) { otherS = ""; }
    var msg = num_gems + " gem" + gemS + " / " + num_other + " other" + otherS;
    $("#searh_results").html(msg);

    //  $.mobile.loading('hide');

    hidePageWait();

    console.log("filter_mobile::showSelection .. done");

}

function removeAllFromPlaylist() {

    myPlaylist.option("removeTime", 0);

    var idx = myPlaylist.playlist.length - 1;

    while (idx >= 0) {
        var n = myPlaylist.playlist[idx].id;
        var useWma = myPlaylist.playlist[idx].useWma;
        delFromPlaylist(n, useWma, idx);
        idx = idx - 1;
    }
    if (myPlaylist.playlist.length <= 0) {
        if (removedItem !== null) {
            myPlaylist.add(removedItem, false);
            myPlaylist.select(0);
        }
    }
    myPlaylist.option("removeTime", 'slow');

    clear_all_red();

    selIds = ""; // <<<< just clear it all together

    var num_sortable = 0;
    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].divobj !== undefined) {
            if (theFsongs[i].is_track_object === false) {
                if (theFsongs[i].divobj.is(':visible') === true) {
                    num_sortable++;
                    if (num_sortable >= 2) {
                        break;
                    }
                }
            }
        }
    }
    if (num_sortable <= 1) {
        random_from_playlist = false;  // nothing left to select from, go back to random from all non-played
    }
    init_playlist_drag(myPlaylist);
}

function addAllToPlaylist() {

    myPlaylist.option("addTime", 0);

    showPageWait();

    var useWma = cMusic;  // *** support only add all mp3s
    var added = false;
    var cnt = { match: 0, gems: 0 };

    var playlist = [];
    if (match_all === true) { // <<<<<< is always False !!
        if (top_to_bottom === true) {  // CD's
            for (var i = 1; i <= num_fsongs; i++) {
                if (theFsongs[i].is_track_object === false) {
                    if (radio_check(i)) {
                        playlist.push(i);
                    }
                }
            }
        } else { // radio , video  old >> new
            for (var i = num_fsongs; i >= 1; i--) {
                if (theFsongs[i].is_track_object === false) {
                    if (radio_check(i)) {
                        playlist.push(i);
                    }
                }
            }
        }
    } else {
        var trackList = $('#dataItemList').children("li");
        var tplaylist = [];
        if (trackList.length > 0) {
            trackList.each(function (index) {
                var div1 = $(this);
                var idx = -1;
                if (div1.is(':visible')) {
                    for (var i = 1; i <= num_fsongs; i++) {
                        if (theFsongs[i].divobj !== undefined) {
                            if (theFsongs[i].divobj[0] === div1[0]) {
                                tplaylist.push(i); break;
                            }
                        }
                    }
                }
            });
        }
        if (list_is_sorted === true || top_to_bottom === true) {
            // if they've sorted things, then we need to put them on the playlist
            // in the same order as they were sorted by
            for (var n = 0; n < tplaylist.length; n++) {
                var i = tplaylist[n];
                playlist.push(i);
            }
        } else {
            // dirtier than this kan niet
            // if nothing has been sorted yet, then
            // list will have most recent tracks @ top & oldest tracks on the bottom
            // ...we want visitors to see the new stuff first
            // all nice, but.. we want them to start listening with the old songs and
            // move on to the new stuff.  Latest song is the one they should remember
            // SO... we need to reverse the order
            for (var n = tplaylist.length - 1; n >= 0; n--) {
                var i = tplaylist[n];
                playlist.push(i);
            }
        }
    }

    for (var n = 0; n < playlist.length; n++) {
        var i = playlist[n];
        var tmedia = theFsongs[i].mp3[useWma];
        var idx = -1;
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === i) && (myPlaylist.playlist[j].useWma === useWma)) {
                idx = j; added = true;
                break;
            }
        }
        if (idx < 0) {
            addToPlaylist(i, useWma, false);  // remove dummy below
            added = true;
        }
    }
    if (added) {
        var removeWait = 10;
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].title === "LATINCITA") {

                removedItem = myPlaylist.playlist[j];

                myPlaylist.remove(j);

            //  sleep(1500);  // addtime doesn't stop remove animation
                removeWait = 1200;
                break;
            }
        }
        if (myPlaylist.playlist.length > 0) {
            setTimeout(function () {
                myPlaylist.select(0);
            }, removeWait);
        }
    }
    myPlaylist.option("addTime", 'slow');

    // *** setWants is waiting 100 ms per added item, and then doing following

    init_playlist_drag(myPlaylist);

    recreateAllStars();

    clearClicked();

    hidePageWait();
}

function play_track(click_cel) {
    var idx = -1;
    var divobj = click_cel.parent();
    for (var i = 1; i <= num_fsongs; i++) {
        if (divobj[0] === theFsongs[i].divobj[0]) {
            idx = i; break;
        }
    }
    if (idx < 0) {
        alert("No clue what you just clicked.");
    } else {
        var useWma = cMusic;  // ???
        var removeDummy = true;

        addToPlaylist(idx, useWma, removeDummy);

        init_playlist_drag(myPlaylist);

        recreateStars(idx, useWma);
    }
}

function notify_add_error(n, useWma, message) {

    if ((n > 0) && (n <= num_fsongs)) {
        var sobj = theFsongs[n];
        if (sobj.load_error[useWma] === "") {
            if (useWma === cKaraoke) {
                if (sobj.played[cKaraoke] === cNotPlayed) {
                    message = "<NONE>";
                } else {
                    message = message + " (no karaoke)";
                }
            } else if (useWma === cReal) {
                if (sobj.played[cReal] === cNotPlayed) {
                    message = "<NONE>";
                } else {
                    message = message + " (no real-video)";
                }
            }
        } else {
            if (sobj.load_error[useWma] !== bNoError) {
                message = sobj.load_error[useWma];
            }
        }
    }
    var wma = useWma;
    if (useWma === cReal) {
        wma = "Real (" + useWma + ")";
    }
    if ((useWma === cKaraoke) || (useWma === cVideo)) {
        wma = "Karaoke/Video (" + useWma + ")";
    }
    if (useWma === cHdAudio) {
        wma = "HD-Audio (" + useWma + ")";
    }
    var errmsg = "ERROR: " + message + "\r\n\r\n[n = " + n + " useWma = " + wma + "]";
    if ((n > 0) && (n <= num_fsongs)) {
        errmsg = theFsongs[n].display_name + "\r\n\r\n>>> " + errmsg;
    }
    if (message === "<NONE>") {
        errmsg = "Video not requested yet from YouTube.";
    }
    if ((useWma === cReal) || (useWma === cKaraoke)) {
        edit_videolist(-1, n, useWma, undefined, false, errmsg);
    } else {
        alert(errmsg);
    }
}


function updatePlayList(n, useWma) {
    for (var j = 0; j < myPlaylist.playlist.length; j++) {
        if ((myPlaylist.playlist[j].id === n) && (myPlaylist.playlist[j].useWma === useWma)) {
            addToPlaylist(n, useWma, false);
        }
    }
    init_playlist_drag(myPlaylist);
}

function addToPlaylist(n, useWma, removeDummy) {

////  --- if "n" is a virtual songid, instead of an index to theFsongs[]
////      convert the songid into an "n"
//
      var songidx = -1; 
      var sobj2 = undefined;
//
//    if (n >= 20000) {
//        var ssongid2 = n + "";
//        for (i = 1; i <= num_rsongs; i++) {
//            if (theRsongs[i].songid === ssongid2) {
//                sobj2 = theRsongs[i]; break;
//            }
//        }
//    } else if (n >= 10000) {
//        var ssongid2 = n + "";
//        for (i = 1; i <= num_ksongs; i++) {
//            if (theKsongs[i].songid === ssongid2) {
//                sobj2 = theKsongs[i]; break;
//            }
//        }
//    }
//    if (sobj2 !== undefined) {
//        for (i = 1; i <= num_fsongs; i++) {
//            if (theFsongs[i].songid === sobj2.radioid) {
//                songidx = i;  break;
//            }
//        }
//    }

    //var songid_num = parseFloat(n + "");
    //if (songid_num > 20000) {
    //    useWma = cReal;
    //    songid_num -= 20000.0;
    //    n = songid_num;
    //} else if (songid_num > 10000) {
    //    useWma = cKaraoke;
    //    songid_num -= 10000.0;
    //    n = songid_num;
    //}

    if ((n <= 0) || (n > num_fsongs)) {
        notify_add_error(n,useWma,"invalid item number (n)");
        return;
    }
    var playing_track = false;

    var radioid = -1;    // $$$ test playing track in Search & track in Radios
    var trackidx = -1;
    if ((theFsongs[n].is_track === true) && (useWma === cMusic)) {
        //  ------------------ if song[n] is a track, find the radio that contains it
        if (songidx > 0) {
            trackidx = songidx;
        } else {
            trackidx = n;
        }
        radioid = theFsongs[n].radioid;
        songidx = -1;
        for (var i = 1; i <= num_fsongs; i++) {
            if (theFsongs[i].songid === radioid) {
                songidx = i; break;  // radio that track belongs to
            }
        }
        if (songidx < 0) {
            notify_add_error(n, useWma, "can't locate radio [" + radioid + "] for track (n)");
            return;
        } else {
            n = songidx;  // queue up radio "n"  &  request track "trackidx"
        }
        // theFsongs[trackidx] is track
        // theFsongs[songidx] is radio containing track
        // playing_track = true means track is on BANDS or RADIO
        if (theFsongs[trackidx].is_track_object === true) {
            playing_track = true;
        }
    } else if (useWma === cMusic) {
        //                     in the past,playing an entire show highlighted tracks as they were played
        //                     as of Feb 2021, this no longer happens !!!   Try seeing if passing a track_list fixes this...
        //  ------------------ song[n] is not a track, but it could be a radio
        var track_num = -1;
        for (var i = 1; i <= num_fsongs; i++) {
            if ((theFsongs[i].is_track === true) && (theFsongs[i].tracknum > 0)) {
                if (theFsongs[i].radioid === theFsongs[n].songid) {  // n is a RADIO and i is a track on that RADIO
                    if (radioid < 0) {
                        radioid = theFsongs[n].songid; // selected track IS a RADIO show
                        songidx = n;
                    }
                    if ((trackidx < 0) || (theFsongs[i].tracknum < track_num)) {  // since n is a radio, auto select 1st track on radio
                        trackidx = i;  // select the 1st track
                        track_num = theFsongs[i].tracknum;
                        if (theFsongs[trackidx].is_track_object === true) {
                            playing_track = true;
                        }
                    }
                    // theFsongs[trackidx] is track
                    // theFsongs[songidx] is radio containing track
                    // playing_track = true means track is on BANDS or RADIO
                }
            }
        }
        if (playing_track) {
            trackidx = -1;  // deselect 1st track & just play entire show
        }
    }

    var tmedia = theFsongs[n].mp3[useWma];
    if (tmedia === "" || extension(tmedia) === "" || tmedia === "<NONE>") {
        notify_add_error(n, useWma, "item (n) does not have a media-url");
        return;
    }
    tmedia_fav = Curr_Favorite(n, useWma);

    var already_added = false;
    var found_id = -1;
    var playlist_idx = -1;
    var track_to_play = -1;

    if ((trackidx >= 0) && (trackidx !== n)) {
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === trackidx) && (myPlaylist.playlist[j].useWma === useWma)) {
                already_added = true;  // already have this track
                found_id = trackidx;
                playlist_idx = j;
                break;
            }
        }
        // if we don't find track, go back and see if we can find track's radio and see if it already has this track
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].songid === radioid)  {
                var toffs = theFsongs[trackidx].offset;
                var tlist = myPlaylist.playlist[j].track_list;
                for (var tnum in tlist) {
                    if (tlist[tnum].offs === toffs) {
                        already_added = true;  // already have this track
                        found_id = myPlaylist.playlist[j].id;
                        playlist_idx = j;
                        track_to_play = tnum;
                        break;
                    }
                }
            }
        }
    }
    if (found_id < 0) {
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === n) && (myPlaylist.playlist[j].useWma === useWma)) {
                already_added = true;  // already have this one
                found_id = n;
                playlist_idx = j;
                break;
            }
        }
    }

    var track_list = [];
    var video_to_play = -1;
    var video_list = [];

    if (already_added === false) {
        if (playing_track) {
            // playing a track on RADIO/BANDS, find all the other tracks from the same show  >>> *** NOTE: give player all the tracks, but tell it to only play the one selected
            if (useWma === cMusic) {
                if (trackidx < 0) {
                    for (var i = num_fsongs; i >= 1; i--) {
                        if ((theFsongs[i].is_track === true) && (theFsongs[i].radioid === theFsongs[n].songid)) {
                            var track_num = theFsongs[i].tracknum;
                            var track_obj = {
                                offs: theFsongs[i].offset,
                                nxtoffs: theFsongs[i].nxtoffs,
                                duration: theFsongs[i].duration,
                                title: theFsongs[i].song,
                                artist: theFsongs[i].artist,
                                radioname: theFsongs[i].radioname,
                                radioid: theFsongs[i].radioid,
                                trackid: i
                            };
                            track_list[track_num] = track_obj;
                            if (i === trackidx) {
                                track_to_play = track_num;
                            }
                        }
                    }
                } else {
                     // if one track was selected, only add that one track to track-list
                    var track_num = theFsongs[trackidx].tracknum;
                    var track_obj = {
                        offs: theFsongs[trackidx].offset,
                        nxtoffs: theFsongs[trackidx].nxtoffs,
                        duration: theFsongs[trackidx].duration,
                        title: theFsongs[trackidx].song,
                        artist: theFsongs[trackidx].artist,
                        radioname: theFsongs[trackidx].radioname,
                        radioid: theFsongs[trackidx].radioid,
                        trackid: trackidx
                    };
                    track_list[1] = track_obj;
                    track_to_play = 1;
                }
            }
        } else {
            // playing anything except for tracks on RADIO/BANDS
            if (trackidx >= 0) {
                if ((useWma === cMusic) && (theFsongs[trackidx].tracknum > 0) && (theFsongs[trackidx].offset >= 0) && (theFsongs[trackidx].duration > 0)) {
                    var track_obj = {
                        offs: theFsongs[trackidx].offset,
                        nxtoffs: theFsongs[trackidx].nxtoffs,
                        duration: theFsongs[trackidx].duration,
                        title: theFsongs[trackidx].song,
                        artist: theFsongs[trackidx].artist, // theFsongs[trackidx].radioname
                        radioname: theFsongs[trackidx].radioname,
                        radioid: theFsongs[trackidx].radioid,
                        trackid: trackidx
                    };
                    track_list[1] = track_obj;
                    //  track_to_play = 1;  <<< just add single track to playlist
                }
            }
        }
    }
    var sobj2 = undefined;

    if ((useWma === cReal) || (useWma === cKaraoke)) {
        var video_num = 0;
        if (useWma === cReal) {
            for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
                if ((theRsongs[i].query === theFsongs[n].query) || (theRsongs[i].radioid === theFsongs[n].songid)) {
                    video_num++;
                    //--------- look for whatever user currently has queued up, don't force jump to favorite
                    //if ((tmedia_fav !== "") && (theRsongs[i].mp3[useWma] === tmedia_fav)) {
                    //    sobj2 = theRsongs[i];   // video stored in theFsongs[n].mp3[useWma]
                    //    video_to_play = video_num;
                    //}
                    if (theRsongs[i].mp3[useWma] === tmedia) {
                        if (video_to_play < 0) {
                            sobj2 = theRsongs[i];   // video stored in theFsongs[n].mp3[useWma]
                            video_to_play = video_num;
                        }
                    }
                    var video_obj = {
                        title: theRsongs[i].article_title, artist: theRsongs[i].artist, desc: theRsongs[i].critics_review, songid: theRsongs[i].songid,
                        poster: theRsongs[i].photo, media: theRsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
                    };
                    video_list[video_num] = video_obj;
                }
            }
        } else if (useWma === cKaraoke) {
            for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
                if ((theKsongs[i].query === theFsongs[n].query) || (theKsongs[i].radioid === theFsongs[n].songid)) {
                    video_num++;
                    //--------- look for whatever user currently has queued up, don't force jump to favorite
                    //if ((tmedia_fav !== "") && (theKsongs[i].mp3[useWma] === tmedia_fav)) {
                    //    sobj2 = theKsongs[i];   // video stored in theFsongs[n].mp3[useWma]
                    //    video_to_play = video_num;
                    //}
                    if (theKsongs[i].mp3[useWma] === tmedia) {
                        if (video_to_play < 0) {
                            sobj2 = theKsongs[i];  // video stored in theFsongs[n].mp3[useWma]
                            video_to_play = video_num;
                        }
                    }
                    var video_obj = {
                        title: theKsongs[i].article_title, artist: theKsongs[i].artist, desc: theKsongs[i].critics_review, songid: theKsongs[i].songid,
                        poster: theKsongs[i].photo, media: theKsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
                    };
                    video_list[video_num] = video_obj;
                }
            }
        }
        if (sobj2 === undefined) {
            if (useWma === cReal) {
                notify_add_error(n, useWma, "youtube did not return a 'real' version of this song");
            } else {
                notify_add_error(n, useWma, "youtube did not return a 'karaoke' of this song");
            }
            return;
        }
    }

    if (!already_added) {
        if ((trackidx >= 0) && (trackidx !== n) && (playing_track === false)) {
            showPlayState(trackidx, useWma, cAdded); 
        } else if (playing_track === false) {
            showPlayState(n, useWma, cAdded);  // only set color when we know song was added
        }
    }
    var added_id = -1;
    var added_photo = "";
    var entire_radio = false;

    if (!already_added) {

        var play_now = false;  // track_to_play handles play_now below

        if (sobj2 !== undefined) {
            added_id = n;
            added_photo = sobj2.photo;
            myPlaylist.add({
                title: sobj2.article_title,
                artist: sobj2.artist,
                m4v: tmedia,
                poster: sobj2.photo,
                useWma: useWma,
                entireRadio: entire_radio,
                songid: sobj2.songid,  // >= 10000 - see store_video
                id: n,
                track_list: track_list,
                video_list: video_list,
                image: {}
            }, play_now);
        } else if (songIsVideo(tmedia)) {
            added_id = n;
            added_photo = theFsongs[n].photo;
            myPlaylist.add({
                title: theFsongs[n].song,
                artist: theFsongs[n].artist,
                m4v: tmedia,
                poster: theFsongs[n].photo,
                useWma: useWma,
                entireRadio: entire_radio,
                songid: theFsongs[n].songid,
                id: n,
                track_list: track_list,
                video_list: video_list,
                image: {}
            }, play_now);     //                               vvvv - before 8.75, only did this when playing_track was false
        } else if ((trackidx >= 0) && (trackidx !== n)) { // && (playing_track === false)) {
            // *** could try to see if another track from this show is already added and just add this track to the list of tracks
            //     but for now, just queue up the track
            added_id = trackidx;
            added_photo = theFsongs[trackidx].photo;
            myPlaylist.add({
                title: theFsongs[trackidx].song,
                artist: theFsongs[trackidx].artist,
                mp3: tmedia,
                wav: theFsongs[trackidx].mp3[1],
                poster: theFsongs[trackidx].photo,
                useWma: useWma,
                entireRadio: entire_radio,
                songid: theFsongs[trackidx].songid,
                id: trackidx,
                track_list: track_list,
                video_list: video_list,
                image: {}
            }, play_now);
        } else {
            entire_radio = playing_track && (trackidx < 0);
            added_id = n;
            added_photo = theFsongs[n].photo;
            myPlaylist.add({
                title: theFsongs[n].song,
                artist: theFsongs[n].artist,
                mp3: tmedia,
                wav: theFsongs[n].mp3[1],
                poster: theFsongs[n].photo,
                useWma: useWma,
                entireRadio: entire_radio,
                songid: theFsongs[n].songid,
                id: n,
                track_list: track_list,
                video_list: video_list,
                image: {}
            }, play_now);
        }
    }

    if (added_photo) {
        fetchImage(added_photo);
    }

    if (already_added) {
        if (video_list.length > 0) {
            if (playlist_idx >= 0) {
                myPlaylist.playlist[playlist_idx].video_list = video_list;
                track_to_play = playlist_idx; // *** how do we queue up selected video ?
            }
        }
    }

    if ((useWma === cMusic) && (added_id >= 0)) {
        var imatches = (selIds.indexOf("|" + theFsongs[added_id].songid + "|") >= 0);
        if (!imatches) {
            if (selIds === "") {
                selIds = "|";
            }
            selIds = selIds + theFsongs[added_id].songid + "|";
        }
    }

//                                    CALLER needs to control removal, when adding multiple items, only perform
//                                    removal for first item, else memory could get trashed
    var removedDummy = false;
    var removeWait = 10;
    if (removeDummy) {
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].title === "LATINCITA") {

            //  sleep(500);

                removedItem = myPlaylist.playlist[j];

                if (myPlaylist.remove(j)) {  // *** remove occurs asynchronously via a slideUp, so may not happen until all code has finished

                    removedDummy = true;

                //  sleep(1500);
                    removeWait = 1200;
                }
                init_playlist_drag(myPlaylist);
                break;
            }
        }
    }

    if ((already_added || removedDummy) && (track_to_play < 0) && !isPlaying()) {
        if (found_id < 0) found_id = n;
        // TODO: skip this when adding all
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === found_id) && (myPlaylist.playlist[j].useWma === useWma)) {
            //  if (!isPlaying()) {
                    setTimeout(function () {
                        myPlaylist.select(j);
                    }, removeWait); // give myPlaylist.remove(j) time to get processed
            //  }
                break;
            }
        }
    } else {
        // only add to list, don't change current selection, and certainly don't stop something if it's playing
    }

//  init_playlist_drag(myPlaylist);   -- let caller do this

    if ((track_to_play >= 0) && !isPlaying()) {
        setTimeout(function () { myPlaylist.play_track(track_to_play); }, removeWait);
    }
}

function delFromPlaylist(n, useWma, idx) {

    if ((playing.id === n) && (playing.useWma === useWma)) {
        myPlaylist.stop();
    }

    if (idx >= 0) {
        myPlaylist.remove(idx);  // callback playlistEventHandler handles div & played
    } else if ((n > 0) && (n <= num_fsongs)) {
        showPlayState(n, useWma, cRemoved);
    }
    if (myPlaylist.playlist.length <= 0) {
        if (removedItem !== null) {
            myPlaylist.add(removedItem, false);
            myPlaylist.select(0);
        }
    }
    init_playlist_drag(myPlaylist);
    check_clean_selids(n);
}

function check_clean_selids(n) {

    if ((n > 0) && (n <= num_fsongs)) {
        var allGone = true;
        for (var i = 0; i < 2; i++) {
            if (theFsongs[n].played[i] > 0) { // > 0 is anything but DEAD
                allGone = false;
            }
        }
        if (allGone) {
            var pat = "|" + theFsongs[n].songid + "|";
            var p = selIds.indexOf(pat);
            if (p >= 0) {
                selIds = selIds.substr(0, p) + selIds.substr(p + pat.length);
            }
        }
    }
}

function clear_all_red() {
    for (var useWma = 0; useWma <= 4; useWma++) {
        for (var j = 1; j <= num_fsongs; j++) {
            var play_state = theFsongs[j].played[useWma];
            switch (play_state) {
                case cAdded:
                case cPlaying:
                case cPlayed:
                case cRemoved:
                    showPlayState(j, useWma, cNotPlayed); break;
            }
        }
    }
}

function randomSong(useWma) {

    var notPlayed = [];

    var widx = Math.floor(Math.random() * cWEIGHT_PATTERN.length);
    if (widx < 0) widx = 0;
    if (widx >= cWEIGHT_PATTERN.length) widx = cWEIGHT_PATTERN.length - 1;
    var weight_id = cWEIGHT_PATTERN[widx];
//  var weight_id = cWEIGHT_PATTERN[_.random(0, cWEIGHT_PATTERN.length - 1)];

    for (var istars = 6; istars >= 0; istars--) {
        for (var n = 1; n <= num_fsongs; n++) {
            if (theFsongs[n].played[useWma] === 0) {
                if (theFsongs[n].weight_id === weight_id) {
                    var song_id = theFsongs[n].songid;
                    var elem = star_data[song_id];
                    var stars = 0;
                    if (elem === undefined) {
                        stars = 0;
                    } else {
                        stars = elem.stars;
                        if (stars === undefined) {
                            stars = 0;
                        }
                    }
                    if (stars === istars) {
                        notPlayed.push(n);
                    }
                }
            }
        }
        if (notPlayed.length > 0) {
            break;
        }
    }

    var numNotPlayed = notPlayed.length;
    if (numNotPlayed <= 0) return -1;  // *** if we are going to just give up .. let the user know why !

    var idx = Math.floor(Math.random() * ((numNotPlayed - 1) - 0 + 1)) + 0;
    if (idx < 0) idx = 0;
    if (idx > (numNotPlayed - 1)) idx = numNotPlayed - 1;

    var id = notPlayed[idx];

    return id;
}


function playRandomSong() {

    var useWma = cMusic;  // *** for now only play random MP3s
    var notPlayed = [];
    var id = -1;
    var ntry = 0;
    var autoPlay = true;

    //  random_from_playlist = True ... there is a list of Selected of songs below
    //                       = False .. just select ANY random song

    //  *** somehow user expects something different to happen if isPlaying() === 1
    //  *** think about what this needs to be

    if (!random_from_playlist) {

        id = randomSong(useWma);

        if (id > 0) {
            notPlayed.push(id);
            ntry = 9;  // pick from non-played, not playlist or visible tracks
            autoPlay = false;
        }
    }

    if (notPlayed.length <= 0) {
        if (isPlaying() != 1) { // play anything, but if we are playing something, then don't do this
                                 // or else pressing [R] does nothing & user thinks button is broken
            ntry = 1; // play anything on playlist that has not yet been played
            for (var j = 0; j < myPlaylist.playlist.length; j++) {
                if (myPlaylist.playlist[j].useWma === useWma) {
                    var n = myPlaylist.playlist[j].id;
                    if ((n > 0) && (n <= num_fsongs)) {
                        if (theFsongs[n].played[useWma] === cAdded) {
                            notPlayed.push(n);
                        }
                    }
                }
            }
        //  if (notPlayed.length <= 2) {
        //      // if there is only one or two non-played songs, then random what?  just ignoreit
        //      notPlayed = [];
        //  }
        }
    }
    if (notPlayed.length <= 0) {
        ntry = 2; // add anything that is visible but is not yet on playlist to playlist and play it
        for (var n = 1; n <= num_fsongs; n++) {
            if ((theFsongs[n].played[useWma] === 0) && (songIsShown(n) === true)) {
                notPlayed.push(n);
            }
        }
    }
    if (notPlayed.length <= 0) {
        ntry = 3; // make anything that is not yet visible, visible, add it to playlist and play it
      //for (var n = 1; n <= num_fsongs; n++) {
      //    if ((theFsongs[n].meesterwerk === true) || (only_gemstones === false)) {
      //        if (radio_check(n)) {
      //            if ((theFsongs[n].played[useWma] === 0) && (songIsShown(n) === false)) {
      //                notPlayed.push(n);
      //            }
      //        }
      //    }
      //}
        if (random_from_playlist) {

            id = randomSong(useWma);

            if (id > 0) {
                notPlayed.push(id);
                ntry = 9;  // pick from non-played, not playlist or visible tracks
            }
        }
    }
    if (notPlayed.length <= 0) {
        ntry = 4; // ran out of options, just start replaying playlist
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].useWma === useWma) {
                var n = myPlaylist.playlist[j].id;
                if ((n > 0) && (n <= num_fsongs)) {
                    notPlayed.push(n);
                }
            }
        }
    }

    // ----------------- clear "played" and retry
    //  if (notPlayed.length <= 0) {
    //      for (var n = num_fsongs; n >= 1; n--) {
    //          if (theFsongs[n].played[useWma] > 1) {
    //              // already on playlist
    //              if (theFsongs[n].is_track === false) {
    //                  if ((theFsongs[n].meesterwerk === true) || (only_gemstones === false)) {
    //                      theFsongs[n].played[useWma] = 1;  // everything back to non-played
    //                      // div to dark red
    //                      var playBox = theFsongs[n].divobj.find(play_sel(useWma));
    //                      if (playBox && playBox.length > 0) {
    //                          playBox.removeClass("ui-added-x");
    //                          playBox.addClass("ui-played-x");
    //                          playBox.addClass("ui-added-x");   // if div was hidden, they might not have gotten added
    //                          playBox.removeClass("ui-played-x");
    //                      }
    //                  }
    //              }
    //          }
    //      }
    //  }

    var numNotPlayed = notPlayed.length;
    if (numNotPlayed <= 0) return;  // *** if we are going to just give up .. let the user know why !

    if (id <= 0) {
        var idx = Math.floor(Math.random() * ((numNotPlayed - 1) - 0 + 1)) + 0;
        if (idx < 0) idx = 0;
        if (idx > (numNotPlayed - 1)) idx = numNotPlayed - 1;

        id = notPlayed[idx];
    }
    var songId = theFsongs[id].songid;

    if (theFsongs[id].is_track_object === false) {
        if (theFsongs[id].divobj !== undefined) {
            if (songIsShown(id) === false) {
                $('#dataItemList').prepend(theFsongs[id].divobj); // display newest @ top, oldest @ bottom
                theFsongs[id].divobj.show();

                check_sortable();
            }
        }
    }

    //////////////////////// let's hope new code handles this properly
    //var playBox = theFsongs[n].divobj.find(play_sel(useWma));
    //if (playBox && playBox.length > 0) {
    //    if (theFsongs[n].played[useWma] === 0) {
    //        playBox.addClass("ui-added-x");
    //        playBox.addClass("ui-played-x");
    //        playBox.removeClass("ui-added-x");   // if div was hidden, they might not have gotten removed
    //        playBox.removeClass("ui-played-x");
    //    } else if (theFsongs[n].played[useWma] === 1) {
    //        playBox.removeClass("ui-added-x");
    //        playBox.addClass("ui-played-x");
    //        playBox.addClass("ui-added-x");   // if div was hidden, they might not have gotten added
    //        playBox.removeClass("ui-played-x");
    //    } else if (theFsongs[n].played[useWma] > 1) {
    //        playBox.addClass("ui-added-x");
    //        playBox.removeClass("ui-played-x");
    //        playBox.removeClass("ui-added-x");   // if div was hidden, they might not have gotten added
    //        playBox.addClass("ui-played-x");
    //    }
    //}
    format_track_list();

    find_matching_videos(id);  // random can select anything and make it visible

    addToPlaylist(id, useWma, true); // add oldest @ top, newest @ bottom

    init_playlist_drag(myPlaylist);

    recreateStars(id, useWma);

    if (autoPlay && !isPlaying()) {
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].id === id) {
                myPlaylist.select(j);
                setTimeout(function () { myPlaylist.play(j); }, 3000);
                break;
            }
        }
    }
    start_loader();
}

function setWants(user_wants) {

    var hgemstones = only_gemstones;
    only_gemstones = false;  // to make sure requested numbers are not filtered out ... turn off gemstones-only flag

    var useWma = cMusic;  // TODO: find a way to pass & save Wma

    var wants = "";

    if (current_page === "#MUSIC.radios") {
        wants = (user_wants["band"] || "") +
                (user_wants["bands"] || "") +
                (user_wants["orchestras"] || "") +
                (user_wants["orchestra"] || "") +
                (user_wants["radio"] || "") +
                (user_wants["show"] || "");
    } else if (current_page === "#MUSIC.bands") {
        wants = (user_wants["band"] || "") +
                (user_wants["bands"] || "") +
                (user_wants["orchestras"] || "") +
                (user_wants["orchestra"] || "") +
                (user_wants["radio"] || "") +
                (user_wants["show"] || "");
    } else if (current_page === "#MUSIC.karaoke") {
        wants = (user_wants["karaoke"] || "") + (user_wants["karaokes"] || "");
    } else if (current_page === "#VIDEOS.live") {
        wants = (user_wants["video"] || "");
    } else if (current_page === "#VIDEOS.solo") {
        wants = (user_wants["solo"] || "") + (user_wants["sola"] || "");
    } else if (current_page === "#VIDEOS.mtv") {
        wants = (user_wants["mtv"] || "") + (user_wants["tv"] || "");
    } else if (current_page === "#VIDEOS.prive") {
        wants = (user_wants["prive"] || "");
    } else {
        wants = (user_wants["id"] || "");
    }
    if (wants !== "") {
        var s = String(wants);
        s = s.split(';').join('|').trim();
        selIds = selIds + "|" + s + "|";
    }
    if ((user_wants["artist"] || "") !== "") {
        var s = String(user_wants["artist"]);
        s = removeAccents(s.split(';').join('|').trim().toLowerCase());
        selArtists = selArtists + "|" + s + "|";
    }
    if ((user_wants["title"] || "") !== "") {
        var s = String(user_wants["title"]);
        s = removeAccents(s.split(';').join('|').trim().toLowerCase());
        selSongs = selSongs + "|" + s + "|";
    }
    var shows = (user_wants["radio"] || "") +
                (user_wants["show"] || "") +
                (user_wants["band"] || "") +
                (user_wants["bands"] || "") +
                (user_wants["orchestra"] || "") +
                (user_wants["orchestras"] || "");
    if (shows !== "") {
        var s = String(shows);
        s = removeAccents(s.split(';').join('|').trim().toLowerCase());
        selShows = selShows + "|" + s + "|";
    }
    showSelection(false);

    myPlaylist.option("addTime", 0);

    showPageWait();

    var want_idx = -1;
    var wait_time = 0;
    var added = false;

    if (wants !== "") {
        var ss = s.split("|");
        for (var j in ss) {
            if (ss[j] !== "") {
                var sidx = -1;
                var eidx = -1;
                var want_id = ss[j];
                var want_obj = wantid_to_obj(want_id);
                useWma = want_obj.useWma;
                for (var n = 1; n <= num_fsongs; n++) {
                    if (theFsongs[n].songid === want_obj.songid) {
                        //               vvvvvvvv--- *** need to find a way to export/import tracks
                        if (theFsongs[n].is_track_object === false) {
                            sidx = n; break;
                        }
                    }
                }
                if ((useWma > 0) && (want_obj.video_id !== "")) {

                    eidx = store_video(sidx, 1, useWma, want_obj.video_id, "", "", "", "", "", true); // pre-store video
                    if (eidx <= 0) {                                                                  // NOTE: this could result in garbage being written to play-list
                        if (sidx >= 0) {
                            theFsongs[sidx].load_error[useWma] = "Unable to interpret id [" + want_id + "]";
                        }
                        sidx = -1;
                    }
                    var requested = youtube_fetchvideo(sidx, useWma, want_obj.video_id);  // runs asynchronously

                    if (!requested) {
                        if (sidx >= 0) {
                            if (theFsongs[sidx].load_error[useWma] === undefined || theFsongs[sidx].load_error[useWma] === "") {
                                theFsongs[sidx].load_error[useWma] = "Unable to request metadata for [" + want_id + "]";
                            }
                        }
                    }
                }
                if (sidx >= 0) {

                    addToPlaylist(sidx, useWma, false);  // remove dummy below

                    added = true;

                    wait_time += 500;  // give playlist time to processs add, if we did many many adds, wait a long time

                    if (want_idx === -1) {
                        want_idx = sidx;
                    }
                }
            }
        }
    }
    if (added) {
        var removeWait = 10;
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if (myPlaylist.playlist[j].title === "LATINCITA") {

                removedItem = myPlaylist.playlist[j];

                myPlaylist.remove(j);

                //  sleep(1500);  // addtime doesn't stop remove animation
                removeWait = 1200;

                wait_time += removeWait;
                break;
            }
        }
        if (myPlaylist.playlist.length > 0) {
            setTimeout(function () {
                myPlaylist.select(0);  // just select first item
            }, removeWait);
        }
    }
    myPlaylist.option("addTime", 'slow');

    //// addAllToPlaylist();  <<< where the heck did this come from ?
    //if (want_idx >= 0) {
    //    for (var j = 0; j < myPlaylist.playlist.length; j++) {
    //        if ((myPlaylist.playlist[j].id === want_idx) && (myPlaylist.playlist[j].useWma === useWma)) {
    //            myPlaylist.select(j); break;
    //        }
    //    }
    //}
    only_gemstones = hgemstones;

    init_playlist_drag(myPlaylist);

    // recreateStars doesn't seem to be doing anything.  It is called, and is replaceing the holders with stars, but when initialization is finished, holders are still there #%!%#%!#
    setTimeout(function () {
        recreateAllStars();
        clearClicked();
        hidePageWait();
    }, wait_time);
}

function wantid_to_obj(id) {
    var songid = "";
    var useWma = cMusic;
    var video_id = "";
    var video_url = "";

    if (id !== "") {
        var songid_num = parseFloat(id + "");
        if (songid_num > 20000) {
            // id = 20000 + id  url is on next line
            useWma = cReal;
            songid_num -= 20000.0;
            songid = songid_num + "";
        } else if (songid_num > 10000) {
            // id = 10000 + id  url is on next line
            useWma = cKaraoke;
            songid_num -= 10000.0;
            songid = songid_num + "";
        } else {
            // id = id + "." + useWma + "." + video_id
            var ss = id.split(".");
            var isongid = parseInt(ss[0]);
            if (isongid > 0) {
                songid = isongid + "";
            }
            if (ss.length > 1) {
                var iuseWma = parseInt(ss[1]);
                if ((iuseWma === cVideo) || (iuseWma === cKaraoke) || (iuseWma === cReal)) {
                    useWma = iuseWma;
                }
                if (ss.length > 2) {
                    var surl = ss[2];
                    video_id = videoId_from_url(surl);
                    video_url = url_from_videoId(video_id);
                }
            }
        }
    }
    if (video_id === "") {
        if ((useWma === cReal) || (useWma === cKaraoke)) {
            var sidx = -1;
            for (i = 1; i <= num_fsongs; i++) {
                if (theFsongs[i].songid === songid) {
                    sidx = i;
                    video_url = theFsongs[i].mp3[useWma];
                    video_id = videoId_from_url(video_url);
                    if (video_id !== "") {
                        break;
                    }
                }
            }
        }
    }
    var want_obj = {
        songid: songid,
        useWma: useWma,
        video_id: video_id,
        video_url: video_url
    };
    return want_obj;
}

function obj_to_wantid(songid, useWma, url, video_id) {
    var want_id = songid;
    if (useWma > 0) {
        want_id = want_id + "." + useWma;
        if (video_id !== "") {
            video_id = videoId_from_url(video_id);  // make sure http: is gone
        } else if (url !== "") {
            video_id = videoId_from_url(url);  // remove http:
        }
        want_id = want_id + "." + video_id;
    }
    return want_id;
}

function storePlaylist() {

    var export_list = $('#playList').val();
    var export_url = $('input[id=PlayListUrl]').val();
    var export_playlist = $('input[id=PlayListName]').val();

    if (export_playlist == "") {
        alert("Please enter a name for the play-list.");
        return;
    }

    var data = export_list.split(/\r\n|\n|\r/);

    var tracklist = { "nm": export_playlist, "data": data, "url": export_url };
    //  var jstr = JSON.stringify(tracklist);
    var jstr = JSON.stringify(tracklist);

    //alert("SAVE to " + export_playlist + "\n" + jstr);

    sendPlaylist(this_target, jstr, playListSaved);
}

function playListSaved(msg) {
    alert(msg);
//  alert("PlayList " + playList + " saved to LATINCITA.COM");
    $("#savePlayList").dialog("close");
}

function fetchPlayList() {

    var import_list = $('#playListIn').val();
    var import_url = $('input[id=PlayListUrlIn]').val();
    var import_playlist = $('input[id=PlayListNameIn]').val();

    if (import_playlist == "") {  // will be choosing name from a pick-list
        alert("Please enter a play-list name to load from LATINCITA.");
        return;
    }
    retrievePlayList(this_target, import_playlist, displayPlayList);
}

function fetchImage(photo) {

    retrieveImage(this_target, photo, setPlaylistPhoto);
}

function setPlaylistPhoto(photo, image_json) {
    if (!image_json || image_json == '{}') {
        console.log("filter_mobile.setPlaylistPhoto: photo [" + photo + "] not recognized.");
    } else {
        const img = JSON.parse(image_json);
        myPlaylist.setPlaylist_Image(photo,img);
    }
}


function displayPlayList(play_list_name,play_list_json) {

    //var nm = import_playlist;
    //var data = ["track \"1\"", "track 2", "track 3"];
    //var url = "https://www.latincita.com/tracklist?" + import_playlist;
    //var tracklist = { "nm": nm, "data": data, "url": url };
    ////  var jstr = JSON.stringify(tracklist);
    //var jstr = JSON.stringify(tracklist);

    currplaylist = play_list_name;

    const obj = JSON.parse(play_list_json);

    var playList = "";
    for (var j = 0; j < obj.data.length; j++) {
        playList = playList + obj.data[j] + "\r\n";
    }
    $('#playListIn').val(playList);
    $('#playListIn').attr("rows", "20");
    $('#playListIn').attr("max-height", "320px");
    $('input[id=PlayListUrlIn]').val(obj.url);
}

function importIt() {
    var import_list = $('#playListIn').val();
    var import_url = $('input[id=PlayListUrlIn]').val();

    var wants_something = "";
    var close_import = true;

    if ((import_list === "") && (import_url === "")) {
        // assume use hit [load] instead of [fetch]
        // fetching occurs async, so unless we can find a way to import after fetch has completed
        // juist force user to click [load] twice
        fetchPlayList();
        return;
    }

    if (import_list !== "") {
        import_list = import_list + "\r\n";
        var chrs = import_list.split('');
        var f = 0;
        var p = 0;
        var p2 = -1;
        var p0 = p;
        var track_num = 0;
        var artist = "";
        var song = "";
        var recdate = "";
        var songid = "";
        var prevsongid = "";
        var prevsong = "";
        var prevartist = "";
        var artist_list = "";
        var song_list = "";
        var songid_list = "";
        // f = 1  song
        // f = 2  artist
        // f = 3  date
        // f = 8  date-done
        // f = 4  id
        // f = 9  id-done
        // f = 5  comments
        while (p < chrs.length) {
            var ch = chrs[p]; p = p + 1;
            if (ch == '\t') {
                ch = " ";
            }
            if ((f === 0) && ((ch === '#') || ((ch === '/') && (chrs[p] === '/')) || ((ch === '-') && (chrs[p] === '-')))) {
                f = 5;
            } else if ((ch == '\n') || (ch == '\r')) {
                if (song.startsWith("http")) {
                    var title = "";    // *** should use YouTube API to fetch meta-data
                    var desc = "";
                    var poster = "";
                    var thumbnail = "";
                    var month_year = "<date unkown>";
                    var useWma = cReal; // just guessing
                    var youtube_query = "";
                    var mark_as_default = true; // ??

                    var video_url = song + "/" + artist;
                    var video_id = videoId_from_url(video_url);
                    // this is the cKaraoke or the cReal for prevsongid

                    var songid_num = parseFloat(prevsongid + "");
                    if (songid_num > 20000) {
                        // id = 20000 + id  url is on next line
                        useWma = cReal;
                        songid_num -= 20000.0;
                        songid = songid_num + "";
                    } else if (songid_num > 10000) {
                        // id = 10000 + id  url is on next line
                        useWma = cKaraoke;
                        songid_num -= 10000.0;
                        songid = songid_num + "";
                    } else {
                        // id = id + "." + useWma + "." + video_id
                        var ss = prevsongid.split(".");
                        var isongid = parseInt(ss[0]);
                        if (isongid > 0) {
                            songid = isongid + "";
                        }
                        if (ss.length > 1) {
                            var iuseWma = parseInt(ss[1]);
                            if ((iuseWma === cVideo) || (iuseWma === cKaraoke) || (iuseWma === cReal)) {
                                useWma = iuseWma;
                            }
                            if (ss.length > 2) {
                                var surl = ss[2];
                                video_id = videoId_from_url(surl);
                                video_url = url_from_videoId(video_id);
                            }
                        }
                    }

                    songidx = -1;
                    for (var i = 1; i <= num_fsongs; i++) {
                        if (theFsongs[i].songid === songid) {
                            songidx = i; break;
                        }
                    }
                    if (songidx >= 0) {
                        track_num++;

                        if (prevsong !== "") {  // YouTube title was provided on previous line, store it for setWants

                            var sobj = theFsongs[songidx];

                            title = prevsong;    // fetch real meta-data later in setWants
                            desc = "";
                            poster = sobj.photo;
                            thumbnail = sobj.photo;
                            month_year = "<date unkown>";
                            youtube_query = "";
                            mark_as_default = true; 

                            var eidx = store_video(songidx, track_num, useWma, video_id, title, desc, poster, month_year, youtube_query, mark_as_default);
                        } else {
                            // store_video in setWants
                        }
                        //if (eidx < 0) {
                        //    console.log("importIt: storing video failed: " + video_url);
                        //} else {
                        //    var sobj2 = undefined;
                        //    if (useWma === cReal) {
                        //        sobj2 = theRsongs[eidx];
                        //    } else if (useWma === cKaraoke) {
                        //        sobj2 = theKsongs[eidx];
                        //    }
                        //    if (sobj2 !== undefined) {
                                var songid2 = songid + "." + useWma + "." + video_id;
                                songid_list = songid_list.replace(prevsongid, songid2);
                        //}
                    } else {
                        console.log("importIt: songid not found: " + prevsongid);
                    }
                } else {
                    if ((songid === "") && (recdate !== "")) {
                        songid = recdate;
                    }
                    songid = songid.trim();
                    artist = artist.trim();
                    song = song.trim();
                    if (songid !== "") {
                        songid_list = songid_list + songid + ";";
                    } else {
                        if (artist !== "") {
                            artist_list = artist_list + artist + ";";
                        }
                        if (song !== "") {
                            song_list = song_list + song + ";";
                        }
                    }
                }
                prevsongid = songid;
                prevsong = song;
                prevartist = artist;
                songid = "";
                artist = "";
                song = "";
                recdate = "";
                f = 0;
                p0 = p;
            } else if ((ch === '/') && ((f === 0) || (f === 1) || (f === 8))) {
                if (f === 8) {
                    // recdate is actually part of song
                    song = ""; recdate = "";
                    for (var pp = p0; pp < (p - 1); pp++) {
                        var cch = chrs[pp];
                        song = song + cch;
                    }
                }
                f = 2; p0 = p;
            } else if (((ch === '(') || (ch === '[')) && ((f === 0) || (f === 1) || (f === 2) || (f === 3) || (f === 8) || (f === 9))) {
                if ((f === 0) || (f === 1) || (f === 2)) {
                    f = 3;
                } else if ((f === 3) || (f === 8)) {
                    f = 4; p2 = p - 2;
                } else if (f === 9) {
                //  v-p0     v-p2
                //  xxx (aaa) (bbb) (ccc)
                //  art  rec   sid   ^p
                    if (artist !== "") {
                        artist = "";
                        for (var pp = p0; pp <= p2; pp++) {
                            var cch = chrs[pp];
                            artist = artist + cch;
                        }
                    } else {
                        song = "";
                        for (var pp = p0; pp <= p2; pp++) {
                            var cch = chrs[pp];
                            song = song + cch;
                        }
                    }
                    recdate = songid;
                    songid = "";
                    f = 4; p2 = p - 2;
                }
            } else if (((ch === ')') || (ch === ']')) && ((f === 3) || (f === 4))) {
                if (f === 3) {
                    f = 8;
                } else {
                    f = 9;
                }
            } else {
                if (f === 0) {
                    f = 1;
                }
                if (f === 1) {
                    song = song + ch;
                } else if (f === 2) {
                    artist = artist + ch;
                } else if (f === 3) {
                    if (ch !== '#') {
                        recdate = recdate + ch;
                    }
                } else if (f === 4) {
                    if (ch !== '#') {
                        songid = songid + ch;
                    }
                }
            }
        }
        var user_wants = [];

        user_wants["id"] = songid_list;
        user_wants["artist"] = artist_list;
        user_wants["title"] = song_list;

        var wants_something = songid_list + artist_list + song_list;

     // alert(wants_something);

        if (wants_something !== "") {
            //  alert("song-ids: " + user_wants["id"] + "\r\n" + "artists: " + user_wants["artist"] + "\r\n" + "songs: " + user_wants["title"]);
            var url = window.location.href.toLowerCase();
            if (url.indexOf("music.search") > 0) {
                setWants(user_wants);
            } else {
                var wants_url = window.location.href;
                var p = wants_url.toLowerCase().indexOf("/mobile/");
                if (p < 0) p = wants_url.indexOf("?");
                if (p > 0) {
                    wants_url = wants_url.substr(0, p) + "?";
                    if (songid_list !== "") {
                        if (current_page === "#MUSIC.radios") {
                            wants_url += "Radio=" + songid_list;
                        } else if (current_page === "#MUSIC.bands") {
                            wants_url += "Band=" + songid_list;
                        } else if (current_page === "#VIDEOS.live") {
                            wants_url += "Video=" + songid_list;
                        } else if (current_page === "#VIDEOS.solo") {
                            wants_url += "Solo=" + songid_list;
                        } else if (current_page === "#VIDEOS.mtv") {
                            wants_url += "MTV=" + songid_list;
                        } else if (current_page === "#VIDEOS.prive") {
                            wants_url += "Prive=" + songid_list;
                        } else if (current_page === "#VIDEOS.karaoke") {
                            wants_url += "Karaoke=" + songid_list;
                        } else {
                            wants_url += "ID=" + songid_list;
                        }
                    }
                    if (artist_list !== "") {
                        wants_url += "Artist=" + artist_list;
                    }
                    if (song_list !== "") {
                        wants_url += "Song=" + song_list;
                    }
                }
                close_import = false;
                window.location = wants_url;
            }
        }
    }
    if (import_url !== "") {
        if (wants_something === "") {
            //  alert(import_url);
            //  just goto URL
            close_import = false;
            window.location = import_url;
        }
    }
    if (close_import) {
        setTimeout(function () {
            $("#loadPlayList").popup("close"); // *** doesn't seem to do anything
        }, 100);
    }
}

function importPlaylist() {

    retrievePlayListNames(this_target, displayPlayListNames);

    $('#playListIn').val("");
    $('#playListIn').attr("placeholder", "Paste here your play-list");
    $('#playListIn').attr("rows", "20");
    $('#playListIn').attr("max-height", "320px");
    $('input[id=PlayListNameIn]').val("");
    $('input[id=PlayListNameIn]').attr("placeholder", "Enter the name of a playlist saved to Latincita.com");
    $('input[id=PlayListUrlIn]').val("");
    $('input[id=PlayListUrlIn]').attr("placeholder", "Paste here a startup url");
    $("#fetchPlaylist").unbind('click').click(function (e) { fetchPlayList(); });
    $("#loadPlaylist").unbind('click').click(function (e) { importIt(); });
    $("#loadPlayList").popup("open");
}

function displayPlayListNames(play_list_names_json) {

    if (!play_list_names_json) {
        return;
    }

    //<div class="ui-field-contain">
    //    <label for="PlayListNameIn">Select Playlist...</label>
    //    <select name="PlayListNameIn" id="PlayListNameIn" data-mini="true">
    //        <option value="aaa">aaa</option>
    //        <option value="bbb">bbb</option>
    //    </select>
    //</div>
    //<div data-role="popup" id="sortMusic2V" >
    //    <ul id="sortMusicList2V" class="music-sort-li" data-role="listview" data-inset="true" style="min-width:210px;">
    //        <li data-role="list-divider">- second sort order -</li>
    //        <li id="slv2.1" data-icon="false"><a href="#">(2) Recorded (newest first)</a></li>
    //        <li id="slv2.2" data-icon="false"><a href="#">(2) Recorded (oldest first)</a></li>
    //        <li id="slv2.3" data-icon="false"><a href="#">(2) Released (latest top)</a></li>
    //        <li id="slv2.4" data-icon="false"><a href="#">(2) Released (oldest top)</a></li>
    //        <li id="slv2.6" data-icon="false"><a href="#">(2) Artist</a></li>
    //        <li id="slv2.7" data-icon="false"><a href="#">(2) Song</a></li>
    //        <li id="slv2.8" data-icon="false"><a href="#">(2) Stars (highest first)</a></li>
    //        <li id="slv2.9" data-icon="false"><a href="#">(2) Stars (lowest first)</a></li>
    //        <li id="slv2.A" data-icon="false"><a href="#">(2) ID (highest first)</a></li>
    //        <li id="slv2.B" data-icon="false"><a href="#">(2) ID (lowest first)</a></li>
    //        <li id="slv2.C" data-icon="false"><a href="#">(2) ---</a></li>
    //    </ul>
    //</div>

    const obj = JSON.parse(play_list_names_json);

    var curr_idx = -1;

    var playListNames = "";
    for (var j = 0; j < obj.length; j++) {
        if (playListNames !== "") {
            playListNames = playListNames +" | ";
        }
        playListNames = playListNames + obj[j];
        if (obj[j] === currplaylist) {
            curr_idx = j;
        }
    }

    if (curr_idx == -1) {
        curr_idx = 0;
    } else {
        curr_idx = curr_idx + 1;
    }

 // $('input[id=PlayListNameIn]').val(playListNames);
    $('input[id=PlayListNameIn]').val(currplaylist);

    var playlist_options = $.map(obj, function (name) {
                                var sel = "no";
                                if (name === currplaylist) {
                                    sel = "selected";
                                }
                                return $('<option>', { value: name, text: name, selected: sel });
                            });
    var no_val_opt = '<option>Select Playlist...</option>';

    // Replace all options:
    $('#PlayListNameList')
        .empty() // remove existing options
        .append(no_val_opt)
        .append(playlist_options)
        .prop('selectedIndex', curr_idx);

    $("#PlayListNameList").on("change", function () {
        // Get the selected option's text
        var selectedText = $(this).find("option:selected").text();
        var selectedVal = $(this).val();
        $('input[id=PlayListNameIn]').val(selectedText);
    });

}

function exportPlayList() {
    var want_ids = "";
    var nows = Date();
    var p = nows.indexOf(" GMT");
    if (p > 0) nows = nows.substr(0, p);
    //  var hplayList = "&#35; Latincita Playlist " + nows + "<br>";
    var playList = "# Latincita Playlist " + nows + "\r\n";
    for (var j = 0; j < myPlaylist.playlist.length; j++) {
        var n = myPlaylist.playlist[j].id;
        var useWma = myPlaylist.playlist[j].useWma;
        if ((n > 0) && (n <= num_fsongs)) {
            var sobj = theFsongs[n];
            var tmedia = sobj.mp3[useWma];
            var want_id = obj_to_wantid(sobj.songid, useWma, sobj.mp3[useWma], "");
            if (want_ids === "") {
                if (current_page === "#MUSIC.radios") {
                    want_ids = "Radio=" + want_id;
                } else if (current_page === "#MUSIC.bands") {
                    want_ids = "Band=" + want_id;
                } else if (current_page === "#MUSIC.karaoke") {
                    want_ids = "Karaoke=" + want_id;
                } else if (current_page === "#VIDEOS.live") {
                    want_ids = "Video=" + want_id;
                } else if (current_page === "#VIDEOS.solo") {
                    want_ids = "Solo=" + want_id;
                } else if (current_page === "#VIDEOS.mtv") {
                    want_ids = "MTV=" + want_id;
                } else if (current_page === "#VIDEOS.prive") {
                    want_ids = "Prive=" + want_id;
                } else {
                    want_ids = "ID=" + want_id;
                }
            } else {
                want_ids = want_ids + ";" + want_id;
            }
            //  var srch_song = sobj.song + "&nbsp;&#47;&nbsp;" + sobj.artist + "&nbsp;&#91;" + sobj.month_year + "&#93;&nbsp;&#40;&#35;" + sobj.songid + "&#41;";
            //  hplayList = hplayList + srch_song + "<br>";
            if ((useWma === cVideo) || (useWma === cKaraoke) || (useWma === cReal)) {
                var song = sobj.song;
                var artist = sobj.artist;
                var datum = "<date unkown>";
                var video_list = myPlaylist.playlist[j].video_list;
                if (video_list !== undefined) { // video_list doen't have an element 0
                    for (var video_num in video_list) {
                        if (video_list[video_num].media === tmedia) {
                            song = video_list[video_num].title;
                            song = song.replace(/[\]\[\/)(#]/g, "_");
                            break;
                        }
                    }
                }
            //  var srch_song = song + " / " + artist + " [" + datum + "] (#" + want_id + ")";
                var srch_song = song + " / " + artist + " (#" + want_id + ")";
                playList = playList + srch_song + "\r\n";
                playList = playList + tmedia + "\r\n";
            } else {
                var srch_song = sobj.song + " / " + sobj.artist + " [" + sobj.month_year + "] (#" + sobj.songid + ")";
                playList = playList + srch_song + "\r\n";
            }
        }
    }
    if (want_ids !== "") {
        //  alert("document.location: " + document.location + "\r\n" + "window.location: " + window.location + "\r\n" + "\r\n" + "window.location.href: " + window.location.href + "\r\n");
        var wants_url = window.location.href;
        var p2 = wants_url.toLowerCase().indexOf("/mobile/");
        if (p2 < 0) p2 = wants_url.indexOf("?");
        if (p2 > 0) {
            wants_url = wants_url.substr(0, p2) + "?" + want_ids;
            //  alert(tmedia);
            $('#playList').val(playList);
            $('#playList').attr("rows", "20");
            $('#playList').attr("max-height", "320px");
            //  $('#playList').textinput("option", "autogrow", false);  // stop it from growing huge
            $('input[id=PlayListName]').attr("placeholder", "Enter a playlist name to save to LATINCITA.com");
            $('input[id=PlayListUrl]').val(wants_url);
            $("#storePlaylist").unbind('click').click(function (e) { storePlaylist(); });
            $("#savePlayList").popup("open");
        }
    } else {
        alert("There is nothing on playlist that can be exported.");
    }
}

function edit_videolist(playlistidx, n, useWma, curr_video, requery, message) {

    if ((n <= 0) || (n > num_fsongs)) {
    //  notify_add_error(n, useWma, "invalid item number (n)");
        alert("edit_videolist: invalid item number: (n = " + n + ")");
        return;
    }  // "#close-edit-videolist"  "- Video List -" 
    if (useWma === cReal) {
        $("#close-edit-videolist").html("- Real Video List -");
    } else if (useWma === cKaraoke) {
        $("#close-edit-videolist").html("- Karaoke Video List -");
    } else {
        $("#close-edit-videolist").html("- Video List -");
    }
    var sobj = theFsongs[n];
    var sobj2 = undefined;
    var tmedia = sobj.mp3[useWma];
    var tmedia2 = "";
    var n2 = -1;
    if (curr_video !== undefined) {
        n2 = curr_video.videoid;
        if (useWma === cReal) {
            sobj2 = theRsongs[n2];
        } else if (useWma === cKaraoke) {
            sobj2 = theKsongs[n2];
        }
        tmedia2 = curr_video.media;
    }
    var video_grid = $('#VideoList');
    video_to_play = -1;
    video_useWma = useWma;

    theVideoList.length = 0;  // global
    num_videoOnList = 0;

    if (useWma === cReal) {
        for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
            if (theRsongs[i].query === sobj.query) {
                num_videoOnList++;
                if (tmedia2 === "") {
                    if (theRsongs[i].mp3[useWma] !== "") {
                        tmedia2 = theRsongs[i].mp3[useWma];
                    //  curr_video = ???
                    }
                }
                if ((theRsongs[i].mp3[useWma] === tmedia2) && (tmedia2 !== "")) {
                    sobj2 = theRsongs[i];
                    video_to_play = num_videoOnList;
                }
                var video_obj = {
                    title: theRsongs[i].article_title, artist: theRsongs[i].artist, desc: theRsongs[i].critics_review, date: theRsongs[i].month_year, songid: theRsongs[i].songid,
                    poster: theRsongs[i].photo, media: theRsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
                };
                theVideoList[num_videoOnList] = video_obj;
            }
        }
    } else if (useWma === cKaraoke) {
        for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
            if (theKsongs[i].query === theFsongs[n].query) {
                num_videoOnList++;
                if (tmedia2 === "") {
                    if (theKsongs[i].mp3[useWma] !== "") {
                        tmedia2 = theKsongs[i].mp3[useWma];
                        //  curr_video = ???
                    }
                }
                if ((theKsongs[i].mp3[useWma] === tmedia2) && (tmedia2 !== "")) {
                    sobj2 = theKsongs[i];
                    video_to_play = num_videoOnList;
                }
                var video_obj = {
                    title: theKsongs[i].article_title, artist: theKsongs[i].artist, desc: theKsongs[i].critics_review, date: theKsongs[i].month_year, songid: theKsongs[i].songid,
                    poster: theKsongs[i].photo, media: theKsongs[i].mp3[useWma], songidx: n, useWma: useWma, videoidx: i
                };
                theVideoList[num_videoOnList] = video_obj;
            }
        }
    }
    if (num_videoOnList <= 0) {
        var video_obj = {
            title: theFsongs[n].article_title, artist: theFsongs[n].artist, desc: "", date: "", songid: theFsongs[n].songid,
            poster: "", media: "", songidx: n, useWma: useWma, videoidx: -1
        };
        num_videoOnList++;
        theVideoList[num_videoOnList] = video_obj;
        video_to_play = num_videoOnList;
    }

    $("#VideoList > tbody").empty();
    for (var i = 1; i <= num_videoOnList; i++) {
        addToEditGrid(video_grid, i, theVideoList[i]);  // *** find some way to select row video_to_play in the list
    }

    //if (num_videoOnList <= 1) {
    //    // add dummy row at bottom
    //    var video_obj = {
    //        title: "", artist: "", desc: "",
    //        poster: "", media: "", songid: -2, useWma: useWma, videoid: -1
    //    };
    //    addToEditGrid(video_grid, -2, video_obj);
    //}

    video_grid.table("rebuild");

    if ((sobj2 === undefined) && (requery === false)) {
        var loaderror = "";
        if (useWma === cReal) {
            loaderror = "Youtube did not return a 'real' version of this song.";
        } else {
            loaderror = "Youtube did not return a 'karaoke' of this song.";
        }
        if (message === "") {
            message = loaderror;
        } else {
            message = loaderror + "\n\n" + message;
        }
    //  return;  --- let user manually retry or try with a different query
    }
    if (video_to_play >= 0) {
        selVideoInList(video_to_play);
    }
    //if (requery) {
    //    if (message !== "") {
    //        alert(message);
    //    }
    //    return;
    //}
    $('#VideoList tr').unbind('click').click(function () {
        var targObj = $(this);
        var idx = targObj.data("itemIndex");
        selVideoInList(idx,targObj);
    });
    $('#VideoList tr').unbind('dblclick').dblclick(function () {
        var targObj = $(this);
        var idx = targObj.data("itemIndex");
        selVideoInList(idx, targObj);
        selectVideo();
    });
 // $('input[id=NumToLoad]').attr("placeholder", "enter number to load");

    $("#selectVideo").unbind('click').click(function (e) { selectVideo(); $("#editVideoList").popup("close"); });
    $("#addAllVideos").unbind('click').click(function (e) { addAllVideos(); $("#editVideoList").popup("close"); });
    $("#loadNewVideos").unbind('click').click(function (e) { loadNewVideos(); });
 // $("#closeEditVideos").unbind('click').click(function (e) { $("#editVideoList").popup("close"); });

    if (requery === false) {
        $("#editVideoList").popup("open");
    }

    if (message !== "") {
        alert(message);
    }
}

function addAllVideos() {

    for (var j = 1; j <= num_videoOnList; j++) {
        var video_obj = theVideoList[j];
        var n = video_obj.songidx;   // theFsongs[n]
        var i = video_obj.videoidx;  // theKsongs[i]  |  theRsongs[i]
        var useWma = video_obj.useWma; // video_useWma;
        var already_added = false;
        var play_now = false;

        for (var k = 0; k < myPlaylist.playlist.length; k++) {
            if (myPlaylist.playlist[k].m4v === video_obj.media) {
                already_added = true;  // already have this one
                break;
            }
        }
        if (!already_added) {
            var video_list = [];
            var track_list = [];
            video_list[1] = video_obj;
            myPlaylist.add({
                title: video_obj.title,
                artist: theFsongs[n].artist,
                m4v: video_obj.media,
                poster: video_obj.poster,
                useWma: useWma,
                songid: video_obj.songid,  // ???
                id: n,
                track_list: track_list,
                video_list: video_list
            }, play_now);
        }
    }
    init_playlist_drag(myPlaylist);

    recreateAllStars();
}

function loadNewVideos() {

    var youtube_query = $('#VideoQuery').val();

    // don't forget number !

    if (video_to_play >= 0) {
        var video_obj = theVideoList[video_to_play];
        var n = video_obj.songidx;
        var i = video_obj.videoidx;
        var useWma = video_obj.useWma; // video_useWma;

        setTimeout(function () { youtube_requery(n, useWma, youtube_query); }, 10);
    }
    $(this).blur();
}

function selectVideo() {
    if (video_to_play >= 0) {
        var video_obj = theVideoList[video_to_play];
        var n = video_obj.songidx;
        var i = video_obj.videoidx;
        var useWma = video_obj.useWma; // video_useWma;

        if (useWma === cReal) {
            if ((i >= 1) && (i <= num_rsongs)) {
                theFsongs[n].mp3[useWma] = theRsongs[i].mp3[useWma];
            }
        } else if (useWma === cKaraoke) {
            if ((i >= 1) && (i <= num_ksongs)) {
                theFsongs[n].mp3[useWma] = theKsongs[i].mp3[useWma];
            }
        }
        var tmedia = theFsongs[n].mp3[useWma];

        $("#editVideoList").popup("close");

        if (myPlaylist.have_video(tmedia)) {
            myPlaylist.select_video(tmedia);
        } else {
            addToPlaylist(n, useWma, true);
            init_playlist_drag(myPlaylist);
        }
    }
}

function selVideoInList(idx,targObj) {
    if (idx) {
        var video_obj = theVideoList[idx];
        var n = video_obj.songidx;
        var i = video_obj.videoidx;
        var qry = "";
        var useWma = video_obj.useWma; // video_useWma;
        if (useWma === cReal) {
            if ((i >= 1) && (i <= num_rsongs)) {
                qry = theRsongs[i].youtube_query;
            }
        } else if (useWma === cKaraoke) {
            if ((i >= 1) && (i <= num_rsongs)) {
                qry = theKsongs[i].youtube_query;
            }
        }
        if (qry === "") {
            qry = theFsongs[n].query;
            if (useWma === cReal) {
                if (!qry.endsWith(" -karaoke")) {
                    qry = qry + " -karaoke";
                }
            } else if (useWma === cKaraoke) {
                if (!qry.startsWith("karaoke +")) {
                    qry = "karaoke +" + qry;
                }
            }
       }
        if (video_obj.desc === "") {
            $("#videoDescription").html("<YouTube returned no description>");
        } else {
            $("#videoDescription").html(video_obj.desc);
        }
        $('input[id=VideoURL]').val(video_obj.media);
        $('#VideoQuery').val(qry);

        //$('#VideoList tr:eq(2)').addClass('yel');
        //$("#VideoList tr[data-item-index='4']").addClass('grn');
        //$("#VideoList tr:contains('gFjdOjCPPlE')").addClass('blu');
        //$('#VideoList .blu').removeClass('blu').addClass('red');

        if (video_to_play >= 0) {
            $('#VideoList .jp-playlist-current').removeClass("jp-playlist-current");
        }
        if (targObj === undefined) {
            targObj = $("#VideoList tr[data-item-index='" + idx + "']");
        }

        video_to_play = idx;

        if (targObj.length > 0) {
            targObj.addClass("jp-playlist-current");
        }
    }
}

function addToEditGrid(grid,n,video_obj) {
    var sid = "_000000" + n;
    sid = sid.substring(sid.length - 7);
    var icon = "<img src=\"" + video_obj.poster + "\" alt=\"Video " + sid + "\" height=\"35\">";
    var videoid = video_obj.media;
    var publish_date = video_obj.date;
    var p = videoid.lastIndexOf("/");
    if (p > 0) {
        videoid = videoid.substr(p + 1);
        p = videoid.indexOf("?");
        if (p > 0) {
            videoid = videoid.substr(0, p);
        }
    }
    var html = '';
    if (n < 0) {
        sid = ""; icon = "";
    }
    html = "<tr id=\"VideoItem_" + sid + "\" data-item-index=\"" + n + "\" data-role=\"none\">" +
        "<th class=\"h\" style=\"text-align: center; vertical-align: middle\" data-role=\"none\">" +
        "&#91&nbsp;" + sid + "&nbsp;&#93;</th>" +
        "<td class=\"h c\" style=\"text-align: center\" data-role=\"none\">" + icon + "</td>" +
        "<td class=\"h c vl_text\" data-role=\"none\">" + video_obj.title  + "</td>" +
        "<td class=\"h c vl_text\" style=\"text-align: center; vertical-align: middle\" data-role=\"none\">" + publish_date + "</td>" +
        "</tr>";
    grid.append(html);
}

function chooseVideo(playlistidx, sidx, useWma, currVideo) {
    // called from playlistEventHandler(1142)
    // user is selecting a particular real/karaoke that they want to use as THE video
    // for a song or track
    // note: it's possible to add all the videos belonging to a song to the playlist as
    // individual items, in which case id > 10000 and song is  theKsongs[n].radioid
    // 1) we add this id + video to the list of chosen video's
    // 2) search through all theFsongs and for every song matching query or youtube_query
    //    store that id + video as well
    // 3) theFsongs[n].mp3[useWma] has to point to this video as well
    // 4) star_data[id] => elem.real_url / elem.karaoke_url needs to be adjusted
    // 5) each playlist item has a video_list, curr_video_num points to the 'current' video
    //    we need to adjust this as well
    //       if playlistItem.m4v points to one of the videos
    //       load_video_info will step along the list and stop when video is founf
    //       this sets curr_video_num, curr_video, prev_video, next_video & n_of_m
    //
    // selectVideo(2760) already does some of this: (3) & (5)...for currently playing video

    var video_url = "";
    if (currVideo !== undefined) {
        video_url = currVideo.media;
    }
    var sobj = undefined;
    var song_id = "";
    var query = "";

    if (sidx >= 1 && sidx <= num_fsongs) {
        sobj = theFsongs[sidx];
        song_id = sobj.songid;
        query = sobj.query;
    }

    if (video_url === "") {
        video_url = theFsongs[sidx].mp3[useWma];
    }

    if (song_id === "" || video_url === "") {
        alert("Can't determine what needs to be saved");
        return;
    }

    var slist = "";

    var elem = star_data[song_id];
    var currUrl = "";

    if (elem !== undefined) {
        if (useWma === cReal) {
            currUrl = elem.real_url;
        } else if (useWma === cKaraoke) {
            currUrl = elem.karaoke_url;
        }
    } else {
        elem = new Object();
    }
    if (currUrl !== video_url) {  // replace current favorite with this one
        if (useWma === cReal) {
            elem.real_url = video_url;
        } else if (useWma === cKaraoke) {
            elem.karaoke_url = video_url;
        }
        star_data[song_id] = elem;

        slist = song_id + ',' + video_url + ';';
    }

    theFsongs[sidx].mp3[useWma] = video_url;

    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].query === query) {
            if (i !== sidx) {

                var song_id2 = theFsongs[i].songid;
                var elem2 = star_data[song_id2];
                var currUrl2 = "";

                if (elem2 !== undefined) {
                    if (useWma === cReal) {
                        currUrl2 = elem2.real_url;
                    } else if (useWma === cKaraoke) {
                        currUrl2 = elem2.karaoke_url;
                    }
                } else {
                    elem2 = new Object();
                }
                if (currUrl2 === "") {
                    // this other song has no favorite, but it has the same query
                    // set its favorite equal to ours
                    if (useWma === cReal) {
                        elem2.real_url = video_url;
                    } else if (useWma === cKaraoke) {
                        elem2.karaoke_url = video_url;
                    }
                    star_data[song_id2] = elem2;

                    slist = slist + song_id2 + ',' + video_url + ';';

                    theFsongs[i].mp3[useWma] = video_url;
                //  theFsongs[i].load_error[useWma] = theFsongs[id].load_error[useWma];
                //  showPlayState(i, useWma, cEnabled
                }
            }
        }
        // ** think about this some more  ... when do we need to update songs on playlist ?
        if ((theFsongs[i].query === query) && (theFsongs[i].mp3[useWma] !== "") && (theFsongs[i].mp3[useWma] !== "<NONE>")) {
            if (i !== sidx) {
                updatePlayList(i, useWma);  // incase song was added to playlist ... update list of videos
            }
        }
    }

    if (slist !== "") {

        sendFavorites(this_target, slist, useWma);

        clr = "red";
        var heart = $('#v-choose-video');
        if (heart.length > 0) {
            heart.css("background-color", clr);
        }
    }

}

function rejectVideo(playlistidx, sidx, useWma, currVideo) {
    // this is used to get rid of stupid videos that the user hates
    // pull it out of all lists
    // delete it from the favorite's list on the server
    // if video is only one in a playlist item's video_list
    // then remove the entire row from the playlist

    var video_url = "";
    if (currVideo !== undefined) {
        video_url = currVideo.media;
    }
    var sobj = undefined;
    var song_id = "";
    var query = "";

    if (sidx >= 1 && sidx <= num_fsongs) {
        sobj = theFsongs[sidx];
        song_id = sobj.songid;
        query = sobj.query;
    }

    if (video_url === "") {
        video_url = theFsongs[sidx].mp3[useWma];
    }

    if (song_id === "" || video_url === "") {
        alert("Can't determine what needs to be removed.");
        return;
    }

    var video_list = undefined;
    var video_num = 0;
    var playlist_idx = -1;
    for (var j = myPlaylist.playlist.length - 1; j >= 0; j--) {
        if ((myPlaylist.playlist[j].id === sidx) && (myPlaylist.playlist[j].useWma === useWma)) {
            video_list = myPlaylist.playlist[j].video_list;  // current video list for the current song
            playlist_idx = j;
            var k = 1;
            var item_idx = -1;
            while (k < video_list.length) { // cant use "for (var video_num in video_list)" because of splice
                if (video_list[k].media === video_url) {
                    //  delete video from video_list
                    video_list.splice(k, 1);
                    item_idx = k;
                } else {
                    k++;
                }
            }
            var num_videos = video_list.length - 1;  // length is 1 greater than num videos, because we don't use index 0

            if (j === playlistidx) {
                var currUrl = Curr_Favorite(sidx, useWma);
                if (currUrl === video_url) {
                    var clr = "#dac8c9";
                    var heart = $('#v-choose-video');
                    if (heart.length > 0) {
                        heart.css("background-color", clr);
                    }
                }
                if (num_videos > 0) {
                    var teller = $("#v-n-of-m");
                    if (teller.length > 0) {
                        var teller_text = teller.text();
                        if (teller_text !== undefined && teller_text !== "") {
                            var p = teller_text.indexOf("/");
                            if (p > 0) {
                                teller_text = teller_text.replace(/([^0-9]*[0-9]+[^/]*[/][^0-9]*)([0-9]+)([^0-9]*)/, "$1" + num_videos + "$3");
                                teller.html(teller_text);
                            }
                        }
                    }
                }
                if ((item_idx > 0) && (num_videos > 0)) {
                    if ((item_idx >= (video_list.length - 1))) {
                        item_idx = video_list.length - 1;
                    }
                    if (item_idx > 0) {
                        var tmedia = video_list[item_idx].media;
                        myPlaylist.select_video(tmedia);
                    } else {
                        myPlaylist.select_video("");  // select NOTHING
                    }
                }
            }
            if (num_videos <= 0) {
                myPlaylist.remove(j);
                init_playlist_drag(myPlaylist);
            }
        }
    }

    if (song_id !== "") {

        var slist = song_id + ',' + video_url + ';';

        deleteFavorites(this_target, slist, useWma);  // checks to see if this video was a favorite, and if so remove it
    }
}

function unlockStars() {
    starsUnlocked = true;
    alert("Stars have been unlocked.");
}

function recreateAllStars() {
    insertStars(undefined);
    showAllStars(undefined);
}

function recreateStars(n, useWma) {
    var songID = theFsongs[n].songid;
    var tmedia = theFsongs[n].mp3[useWma];
    if (useWma === cReal) {
        for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
            if ((theRsongs[i].mp3[useWma] === tmedia) && (theRsongs[i].radioid === theFsongs[n].songid)) {
                songID = theRsongs[i].songid; break;
            }
        }
        if (songID === "") {  // video may have been stolen by another song
            for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
                if (theRsongs[i].mp3[useWma] === tmedia) { // && (theRsongs[i].radioid === theFsongs[n].songid)) 
                    songID = theRsongs[i].songid; break;
                }
            }
        }
    } else if (useWma === cKaraoke) {
        for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
            if ((theKsongs[i].mp3[useWma] === tmedia) && (theKsongs[i].radioid === theFsongs[n].songid)) {
                songID = theKsongs[i].songid; break;
            }
        }
        if (songID === "") {  // video may have been stolen by another song
            for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
                if (theKsongs[i].mp3[useWma] === tmedia) { // && (theRsongs[i].radioid === theFsongs[n].songid)) 
                    songID = theKsongs[i].songid; break;
                }
            }
        }
    } else {
        songID = theFsongs[n].songid;
    }
    insertStars(songID);
    showAllStars(songID);
}

function insertStars(thisSongID) {
    var tsid = 0.0;
    var tisid = 0;
    if (thisSongID === undefined) {
        tsid = 0.0;
    } else if (thisSongID === "") {
        tsid = 0.0;
    } else {
        tsid = parseFloat(thisSongID + "");
        tisid = Math.trunc(tsid);
    }
    $(".star_holder").each(
        function () {
            var SongID = $(this).data("id");

            var is_menu = false;
            var sid = 0.0;
            var siid = 0;

            var doit = true;
            if (SongID === undefined) {
                doit = false;
            } else if (SongID === "") {
                doit = false;
            } else {
                sid = parseFloat(SongID + "");
                siid = Math.trunc(sid);

                if (sid >= 32000) {
                    is_menu = true; // menu
                    doit = true;
                } else if (tsid === 0.0) {
                    doit = true; // do all songs
                } else if (tsid === sid) {
                    doit = true;
                } else if (tisid === siid) {
                    doit = true;  // SongID = (20000 + thisSongID) + 0.0001
                } else if ((siid === (10000 + tisid)) || (tisid === (10000 + siid))) {
                    doit = true;  // SongID = (20000 + thisSongID) + 0.0001
                } else if ((siid === (20000 + tisid)) || (tisid === (20000 + siid))) {
                    doit = true;  // SongID = (20000 + thisSongID) + 0.0001
                } else {
                    doit = false;
                }
            }
            if (doit) {
                var useWma = cMusic;
                if (is_menu) {
                    useWma = 1000;
                } else if (sid >= 20000) {
                    useWma = cReal;
                } else if (sid >= 10000) {
                    useWma = cKaraoke;
                }

                var star_div = "";
                var star_id = "stars";
                var star_class = "stars";
                var starbox_class = "starbox";
                if ($(this).hasClass("jp-playlist-stars")) {
                    star_class = star_class + " jp-playlist-stars";
                    if (useWma === cVideo || useWma === cKaraoke) {
                        star_class = star_class + " jps-k";
                    } else if (useWma === cReal) {
                        star_class = star_class + " jps-r";
                    } else {
                        star_class = star_class + " jps-m";
                    }
                    star_id = star_id + "_p_" + SongID;
                } else if ($(this).hasClass("tracklist-stars")) {
                    star_class = star_class + " tracklist-stars";
                    starbox_class = starbox_class + " tracklist-starbox";
                    star_id = star_id + "_t_" + SongID;
                } else {
                    if ((current_page === "#MUSIC.radios") || (current_page === "#MUSIC.bands") ||
                         current_page.startsWith("#VIDEOS")) {
                        star_class = star_class + " songlist-stars-m"; // 8 px margin (only one button)
                    } else {
                        star_class = star_class + " songlist-stars"; // 86 px margin (three buttons)
                    }
                    starbox_class = starbox_class + " songlist-starbox";
                    star_id = star_id + "_s_" + SongID;
                }

                star_list[star_id] = SongID;

                var sss = "*";
                var star_clr = "gray_star";
                if (useWma === cVideo || useWma === cKaraoke) {
                    sss = "="; star_clr = "karaoke_star";
                } else if (useWma === cReal) {
                    sss = "o"; star_clr = "real_star";
                }

                star_div = star_div + '<div id="' + star_id + '" class="' + star_class + ' noselect" data-songid="' + SongID + '" data-role="none" >\n';
                star_div = star_div + '    <div class="' + starbox_class + ' noselect" data-role="none">\n';
                star_div = star_div + '        <span class="star1 noselect ' + star_clr + '" data-role="none">' + sss + '</span>\n';
                star_div = star_div + '        <span class="star2 noselect ' + star_clr + '" data-role="none">' + sss + '</span>\n';
                star_div = star_div + '        <span class="star3 noselect ' + star_clr + '" data-role="none">' + sss + '</span>\n';
                star_div = star_div + '        <span class="star4 noselect ' + star_clr + '" data-role="none">' + sss + '</span>\n';
                if (sss === "*") {
                    star_div = star_div + '        <span class="star5 noselect ' + star_clr + '" data-role="none">' + sss + '</span>\n';
                }
                star_div = star_div + '    </div>\n';
                star_div = star_div + '</div>\n';
                $(this).replaceWith(star_div);

                if (is_menu === false) {
                    $("#" + star_id).click(function (e) {
                        var event = e || window.event;
                        // seems to block any events sent to children and propagated to $(this)
                        // event.stopPropagation ? event.stopPropagation() : (event.cancelBubble = true);
                        var star_div = $(this);
                        increment_stars(star_div);
                    });
                }
            }
        });
}

function sendNewStars() {
    var fifteenSecsAgo = new Date();
    fifteenSecsAgo.setTime(fifteenSecsAgo.getTime() - 15 * 1000);
    if (laststartime > fifteenSecsAgo) {
        setTimeout(function () {
            sendNewStars();
        }, 100);
    } else {
        if (changedStars !== '' && changedStars !== ';') {
            var sarr = changedStars.split(';');
            changedStars = ';';
            var slist = '';
            for (var i = 0; i < sarr.length; i++) {
                var songID = sarr[i];
                if (songID !== '') {
                    var val = star_data[songID].stars;
                    if (current_page.startsWith("#VIDEOS")) {
                        var songid_num = parseFloat(songID + "");
                        songid_num = 30000 + songid_num;   // videos start as ID 30000
                        slist = slist + songid_num + ',' + val + ';';
                    } else {
                        slist = slist + songID + ',' + val + ';';
                    }
                }
            }
            if (slist !== '') {
                sendStars(this_target, slist);
            }
        }
    }
}

function showAllStars(thisSongID) {
    var tsid = 0.0;
    var tisid = 0;
    if (thisSongID === undefined) {
        tsid = 0.0;
    } else if (thisSongID === "") {
        tsid = 0.0;
    } else {
        tsid = parseFloat(thisSongID + "");
        tisid = Math.trunc(tsid);
    }
    $('.stars').each(function () {
        var stardiv = $(this);
        var SongID = stardiv.data('songid');

        var is_menu = false;
        var sid = 0.0;
        var siid = 0;

        var doit = true;
        if (SongID === undefined) {
            doit = false;
        } else if (SongID === "") {
            doit = false;
        } else {
            sid = parseFloat(SongID + "");
            siid = Math.trunc(sid);

            if (sid >= 32000) {
                is_menu = true; // menu
            } else if (tsid === 0.0) {
                doit = true; // do all songs
            } else if (tsid === sid) {
                doit = true;
            } else if (tisid === siid) {
                doit = true;  // SongID = (20000 + thisSongID) + 0.0001
            } else if ((siid === (10000 + tisid)) || (tisid === (10000 + siid))) {
                doit = true;  // SongID = (20000 + thisSongID) + 0.0001
            } else if ((siid === (20000 + tisid)) || (tisid === (20000 + siid))) {
                doit = true;  // SongID = (20000 + thisSongID) + 0.0001
            } else {
                doit = false;
            }
        }
        if (is_menu) {
            var stars = 100;
            switch (siid) {
                case 32006: stars = 6; break;
                case 32005: stars = 5; break;
                case 32004: stars = 4; break;
                case 32003: stars = 3; break;
                case 32002: stars = 2; break;
                case 32001: stars = 1; break;
                case 32000: stars = 0; break;
            }
            if (stars < 100) {
                show_stars(stardiv, stars);
            }
        } else if (doit) {
            if (star_data[SongID] === undefined) {
                show_stars(stardiv, 0);
            } else {
                var stars = star_data[SongID].stars;
                show_stars(stardiv, stars);
            }
        }
    });
}

function increment_stars(star_div) {
    if (starsUnlocked === false) {
        return;
    }
    var songID = star_div.data('songid');
    if (star_data[songID] === undefined) {
        return;
    }
    var val = star_data[songID].stars + 0;
    if (val === 6) {
        val = 0;
    } else {
        val = val + 1;
    }
    star_data[songID].stars = val;
    for (tid in star_list) {
        if (star_list[tid] === songID) {
            var sdiv = $("#" + tid);
            if (sdiv.length > 0) {
                show_stars(sdiv, val);
            }
        }
    }
//  show_stars(star_div, val);
    if (changedStars.indexOf(";" + songID + ";") >= 0) {
        // already have this song
    } else {
        changedStars = changedStars + songID + ";";
    }
    //alert("song " + songId + " -> " + val);
    laststartime = new Date().getTime();
    setTimeout(function () {
        sendNewStars();
    }, 500);
}

function show_stars(star_div, stars) {
    if (stars === undefined) {
        return;
    }
    if (stars === "") {
        return;
    }
    if (stars > 5) {
        for (i = 1; i <= 5; i++) {
            var star = star_div.find('.star' + i);
            var redi = "red_star" + i;
            var llli = "lll_star" + i;
            star.removeClass('gray_star').removeClass('red_star').removeClass(redi).addClass('lll_star').addClass(llli);
        }
    } else {
        for (i = 1; i <= 5; i++) {
            var star = star_div.find('.star' + i);
            var redi = "red_star" + i;
            var llli = "lll_star" + i;
            if (stars < i) {
                star.removeClass('red_star').removeClass(redi).removeClass('lll_star').removeClass(llli).addClass('gray_star');
            } else {
                star.removeClass('gray_star').removeClass('lll_star').removeClass(llli).addClass('red_star').addClass(redi);
            }
        }
    }
}

function songMatchesFilter(i, cnt) {

    if (match_all) { // <<<<<< is always False !!
        return true;
    }
    var matchesSelId = false;

    var gmatches = false;
    var lmatches = false;
    var amatches = false;
    var smatches = false;
    var imatches = false;
    var rmatches = false;
    var tmatches = false;
    var mmatches = false;
    var vmatches = false;
    var gempty = false;
    var lempty = false;
    var aempty = false;
    var sempty = false;
    var iempty = false;
    var rempty = false;
    var tempty = false;
    var mempty = false;
    var vempty = false;

    if (!String.prototype.trim) {
        String.prototype.trim = function () {
            return this.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
        };
    }

    if (selIds === '' || selIds === '|' || selIds === '||') {
        imatches = true; iempty = true;
    } else {
        if (theFsongs[i].songid === '') {
            imatches = false;
        } else {
            imatches = (selIds.indexOf("|" + theFsongs[i].songid + "|") >= 0);
            if (imatches) {
                matchesSelId = true;
            } else { // try ignoring usewma & video_id in selIds
                imatches = (selIds.indexOf("|" + theFsongs[i].songid + ".") >= 0);
                if (imatches) {
                    matchesSelId = true;
                }
            }
        }
    }
    if (selGenres === '' || selGenres === '|' || selGenres === '||') {
        gmatches = true; gempty = true;
    } else {
        if (theFsongs[i].genre === '') {
            gmatches = false;
        } else {
            gmatches = (selGenres.indexOf("|" + theFsongs[i].genre.trim().toLowerCase() + "|") >= 0);
        }
    }
    if (selLang === '' || selLang === '|' || selLang === '||') {
        lmatches = true; lempty = true;
    } else {
        if (theFsongs[i].language === '') {
            lmatches = false;
        } else {
            lmatches = (selLang.indexOf("|" + theFsongs[i].language.trim().toLowerCase() + "|") >= 0);
        }
    }
    if (selArtists === '' || selArtists === '|' || selArtists === '||') {
        amatches = true; aempty = true;
    } else {
        if (theFsongs[i].sartist === '') {
            amatches = false;
        } else {
            amatches = (selArtists.indexOf("|" + theFsongs[i].sartist + "|") >= 0);
        }
    }
    if (selSongs === '' || selSongs === '|' || selSongs === '||') {
        smatches = true; sempty = true;
    } else {
        if (theFsongs[i].ssong === '') {
            smatches = false;
        } else {
            smatches = (selSongs.indexOf("|" + theFsongs[i].ssong + "|") >= 0);
        }
    }
    if (selShows === '' || selShows === '|' || selShows === '||') {
        tmatches = true; tempty = true;
    } else {
        if (theFsongs[i].article_title === '') {
            tmatches = false;
        } else {
            // TODO:  figure out a way to match sub-string
            tmatches = (selShows.indexOf("|" + theFsongs[i].article_title + "|") >= 0);
        }
    }
    if (selGems === '' || selGems === '|' || selGems === '||') {
        mmatches = true; mempty = true;
    } else {
        if (theFsongs[i].songid === '') {
            mmatches = false;
        } else {
            // TODO:  figure out a way to match sub-string
            mmatches = (selGems.indexOf("|" + theFsongs[i].songid + "|") >= 0);
        }
    }
    if (selStars === '' || selStars === '|' || selStars === '||') {
        rmatches = true; rempty = true;
    } else {
        if (theFsongs[i].songid === '') {
            rmatches = false;
        } else {
            var song_id = theFsongs[i].songid;
            var elem = star_data[song_id];
            var stars = 0;
            if (elem === undefined) {
                stars = 0;
            } else {
                stars = elem.stars;
                if (stars === undefined) {
                    stars = 0;
                }
            }
            rmatches = (selStars.indexOf("|" + stars + "|") >= 0);
        }
    }
    if (selLiveStudio === '' || selLiveStudio === '|' || selLiveStudio === '||') {
        vmatches = true; vempty = true;
    } else {
        vmatches = false;
        if (theFsongs[i].is_live) {
            var want_live = selLiveStudio.indexOf("LIVE") >= 0;
            if (want_live) {
                vmatches = true;
            }
        } else {
            var want_studio = selLiveStudio.indexOf("STUDIO") >= 0;
            if (want_studio) {
                vmatches = true;
            }
        }
    }

    var matches = false;
    if (iempty && gempty && lempty && aempty && sempty && tempty && mempty && vempty) {
        matches = false;
    } else {
        if (mmatches && !mempty) {
            matches = true; // gemstone (songid) selection always matches
        } else {
            if (tmatches && !tempty) {
                matches = true;  // radio-show  (article_title)
            } else {
                if (imatches && !iempty) {
                    matches = true;  // SongID (loaded on playlist)
                } else {
                    // normal filter - entire combination has to match
                    if (!gempty) {
                        matches = gmatches;  // start with everything in selected Genre
                    }
                    if ((matches || gempty) && !sempty) {
                        matches = smatches;  // must (also) match Song-Name
                    }
                    if ((matches || (gempty && sempty)) && !lempty) {
                        matches = lmatches;  // must (also) match language
                    }
                    if ((matches || (gempty && sempty && lempty)) && !aempty) {
                        matches = amatches;  // must (also) match artist
                    }
                }
            }
        }
    }

    if (!rempty) {
        if (matches) {
            if (!rmatches) {
                matches = false; // filter out anything with the wrong number of stars
            }
        } else {
            if (iempty && gempty && lempty && aempty && sempty && tempty && mempty && vempty) {
                if (rmatches) {
                    matches = true;  // only matching on stars
                }
            }
        }
    }

    if (!vempty) {
        if (matches) {
            if (!vmatches) {
                matches = false; // filter out non live/studio
            }
        } else {
            if (iempty && gempty && lempty && aempty && sempty && tempty && mempty && rempty) {
                if (vmatches) {
                    matches = true;  // only matching on live/studio
                }
            }
        }
    }

    //  var onlyGemstones = $('#chkGemstones').is(':checked')
    if (matches) {
        if (radio_check(i) === false) {
            if (!matchesSelId) {
                return false;
            }
        }
        cnt.match++;
        if (theFsongs[i].meesterwerk === true) {
            cnt.gems++;
        }
        if (only_gemstones) {
            if (!matchesSelId) {
                if (theFsongs[i].meesterwerk !== true) {
                    return false;
                }
            }
        }
    }
    return matches;
}

function getFilters(whichFilters) {

    if (!String.prototype.trim) {
        String.prototype.trim = function () {
            return this.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
        };
    }
    if (whichFilters === undefined) {
        whichFilters = "";
    }
    if (whichFilters === "") {
        whichFilters = "ALL";
    }

    var labels = "|";
    if ((whichFilters === "Genre") || (whichFilters === "ALL")) {
        $("#pnlGenre input:checked").each(
            function () {
                var label = $('label[for=' + this.id + ']').html();
                labels += label.trim().toLowerCase() + "|";
            });
    }
    selGenres = labels;
    if (selGenres === "|") selGenres = "";

    labels = "|";
    if ((whichFilters === "Language") || (whichFilters === "ALL")) {
        $("#pnlLanguage input:checked").each(
            function () {
                var label = $('label[for=' + this.id + ']').html();
                labels += label.trim().toLowerCase() + "|";
            });
    }
    selLang = labels;
    if (selLang === "|") selLang = "";

    labels = "|";
    if ((whichFilters === "Artist") || (whichFilters === "ALL")) {
        $("#pnlArtist input:checked").each(
            function () {
                var label = $('label[for=' + this.id + ']').html();
                labels += label.trim().toLowerCase() + "|";
            });
    }
    selArtists = labels;
    if (selArtists === "|") selArtists = "";

    labels = "|";
    if ((whichFilters === "Song") || (whichFilters === "ALL")) {
        $("#pnlSong input:checked").each(
            function () {
                var label = $('label[for=' + this.id + ']').html();
                labels += label.trim().toLowerCase() + "|";
            });
    }
    selSongs = labels;
    if (selSongs === "|") selSongs = "";

    labels = "|";
    if ((whichFilters === "Gemstones") || (whichFilters === "ALL")) {
        $("#pnlGemstones input:checked").each(
            function () {
                var track = $('label[for=' + this.id + ']').html();
                var p = track.indexOf("(#");   // ...... (#123)   123 is songid
                if (p > 0) {
                    var ids = track.substring(p, track.length);
                    p = ids.indexOf(")");
                    if (p > 0) {
                        var label = ids.substring(2, p);
                        labels += label.trim() + "|";
                    }
                }
            });
    }
    selGems = labels;
    if (selGems === "|") selGems = "";

    labels = "|";
    if ((whichFilters === "Stars") || (whichFilters === "ALL")) {
        $("#pnlStars input:checked").each(
            function () {
                var stars = 100;
                switch (this.id) { // <<< let's hope this is data-id
                    case "checkbox-606": stars = 6; break;
                    case "checkbox-605": stars = 5; break;
                    case "checkbox-604": stars = 4; break;
                    case "checkbox-603": stars = 3; break;
                    case "checkbox-602": stars = 2; break;
                    case "checkbox-601": stars = 1; break;
                    case "checkbox-600": stars = 0; break;
                }
                if (stars < 100) {
                    labels += stars + "|";
                }
            });
    }
    selStars = labels;
    if (selStars === "|") selStars = "";

    labels = "|";
    if ((whichFilters === "LiveStudio") || (whichFilters === "ALL")) {
        $("#pnlLive input:checked").each(
            function () {
                var live_studio = "";
                switch (this.id) { // <<< let's hope this is data-id
                    case "checkbox-701": live_studio = "LIVE"; break;
                    case "checkbox-702": live_studio = "STUDIO"; break;
                }
                if (live_studio !== "") {
                    labels += live_studio + "|";
                }
            });
    }
    selLiveStudio = labels;
    if (selLiveStudio === "|") selLiveStudio = "";

}

function genreClicked() {
    $("#pnlGenre :checkbox").checkboxradio('refresh');
    clearOtherClicked("Genre");
    getFilters();
    showSelection(false);
}
function langClicked() {
    $("#pnlLanguage :checkbox").checkboxradio('refresh');
    clearOtherClicked("Language");
    getFilters();
    showSelection(false);
}
function artistClicked() {
    $("#pnlArtist :checkbox").checkboxradio('refresh');
    clearOtherClicked("Artist");
    getFilters();
    showSelection(false);
}
function starSelClicked() {
    $("#pnlStars :checkbox").checkboxradio('refresh');
//  clearOtherClicked("Gemstones");    // try to combine stars w/ others
    getFilters();
    showSelection(false);
}
function liveClicked() {
    $("#pnlLive :checkbox").checkboxradio('refresh');
//  clearOtherClicked("Genre");    // live must be able to be combined with everything
    getFilters();
    showSelection(false);
}
function songClicked() {
    $("#pnlSong :checkbox").checkboxradio('refresh');
    clearOtherClicked("Song");
    getFilters();
    showSelection(false);
}
function idClicked() {
    $("#pnlGemstones :checkbox").checkboxradio('refresh');
    clearOtherClicked("Gemstones");
    getFilters();
    showSelection(false);
}

function check_only_gemstones() {
    var chk = $('#chkGemstones');
    if (chk && chk.length > 0) {
        chk.prop('checked', true).checkboxradio('refresh');
    }
}

function gemstonesClicked() {

    only_gemstones = $('#chkGemstones').is(':checked');

 // $('#chkGemstones input').refresh();
 // $('#chkGemstones').refresh();

    clearOtherClicked("Gemstones");
    getFilters();
    showSelection(false);
}

function clearOtherClicked(exceptWhich) {
    if (exceptWhich !== "Genre" && exceptWhich !== "Language") {
        $("#pnlGenre :checkbox").removeAttr('checked').checkboxradio('refresh');
    }
    if (exceptWhich !== "Language" && exceptWhich !== "Genre") {
        $("#pnlLanguage :checkbox").removeAttr('checked').checkboxradio('refresh');
    }
    if (exceptWhich !== "Artist") {
        $("#pnlArtist :checkbox").removeAttr('checked').checkboxradio('refresh');
    }
    if (exceptWhich !== "Song") {
        $("#pnlSong :checkbox").removeAttr('checked').checkboxradio('refresh');
    }
    if (exceptWhich !== "Gemstones") {
        $("#pnlGemstones :checkbox").removeAttr('checked').checkboxradio('refresh');
    }
//  if (exceptWhich !== "Stars") {
//      $("#pnlStars :checkbox").removeAttr('checked').checkboxradio('refresh');
//  }
}

function clearClicked() {
    $("#pnlGenre :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlLanguage :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlArtist :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlSong :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlGemstones :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlStars :checkbox").removeAttr('checked').checkboxradio('refresh');
    $("#pnlLive :checkbox").removeAttr('checked').checkboxradio('refresh');
    // selIds = ""; --- use clearPlaylist to clear selIds
    selGenres = "";
    selLang = "";
    selArtists = "";
    selSongs = "";
    selShows = "";
    selGems = "";
    selStars = "";
    selLiveStudio = "";
    //  selIds = "";  -- add/ remove from playlist to fill/clear selIds-
    showSelection(false);
    //  initPlaylist();  --- don't clear playlist !

    if (myPlaylist.playlist.length <= 1) {
        var num_sortable = 0;
        for (var i = 1; i <= num_fsongs; i++) {
            if (theFsongs[i].divobj !== undefined) {
                if (theFsongs[i].is_track_object === false) {
                    if (theFsongs[i].divobj.is(':visible') === true) {
                        num_sortable++;
                        if (num_sortable >= 2) {
                            break;
                        }
                    }
                }
            }
        }
        if (num_sortable <= 1) {
            random_from_playlist = false;  // nothing left to select from, go back to random from all non-played
        }
    }
}

function selectAllClicked() {
    selGenres = "";
    selLang = "";
    selArtists = "";
    selSongs = "";
    selGems = "";
    selShows = "";
    selStars = "";
    selLiveStudio = "";
    //  selIds = "";  -- add/ remove from playlist to fill/clear selIds-
    if (false) {
        // old method --- select everything
        $("#pnlGenre :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlLanguage :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlArtist :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlSong :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlGemstones :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlStars :checkbox").prop('checked', true).checkboxradio('refresh');
        $("#pnlLive :checkbox").removeAttr('checked').checkboxradio('refresh');
        getFilters();
        showSelection(false);
    } else {
        // new method --- select NOTHING & tell showSelection to show everything
        $("#pnlGenre :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlLanguage :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlArtist :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlSong :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlGemstones :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlStars :checkbox").removeAttr('checked').checkboxradio('refresh');
        $("#pnlLive :checkbox").removeAttr('checked').checkboxradio('refresh');

        setTimeout(function () {
            console.log("filter_mobile::close_panel");
            $("#MusicSearch").panel("close");
        }, 100);

        showSelection(true);
    }
}


function catchFilterClicks() {
    $("#pnlGenre :checkbox").click(genreClicked);
    $("#pnlLanguage :checkbox").click(langClicked);
    $("#pnlArtist :checkbox").click(artistClicked);
    $("#pnlSong :checkbox").click(songClicked);
    $("#pnlGemstones :checkbox").click(idClicked);
    $("#pnlStars :checkbox").click(starSelClicked);
    $("#pnlLive :checkbox").click(liveClicked);
    $("#hdrAllFilters").click(selectAllClicked);
    $("#hdrClearFilters").click(clearClicked);
    $('#chkGemstones').click(gemstonesClicked);
    $('#MusicSearchHdr').click(function () {
        //////////////////////////////////////// doen't work - says panel not initialized
        //    setTimeout(function () {
        //        $("#MusicSearch").popup("close");
        //    }, 100);
        $("#MusicSearch").panel("close");
    });
}

function catchPlaylistClicks() {
    $("#ClearList").click(removeAllFromPlaylist);
    $("#SaveList").click(exportPlayList);
    $("#LoadList").click(importPlaylist);
    $("#AddAlltoList").click(addAllToPlaylist);
    $("#v-random").click(playRandomSong);
//  $("#v-n-of-m").click(edit_videolist);
}

function trackHeaderClick(targ) {
    if (targ) {
        var e = $(targ);
        if (e.length > 0) {
            var btn = e.closest(".radio-track-list").prev(".ui-table-columntoggle-btn");
            if (btn.length > 0) {
                btn.click();
            }
        }
    }
}

function decodeHtmlNumeric(str) {
    return str.replace(/&#([0-9]{1,7});/g, function (g, m1) {
        return String.fromCharCode(parseInt(m1, 10));
    }).replace(/&#[xX]([0-9a-fA-F]{1,6});/g, function (g, m1) {
        return String.fromCharCode(parseInt(m1, 16));
    });
}

function cleanString(str) {
    strAccents = str;
    strAccents = decodeHtmlNumeric(strAccents);
    strAccents = strAccents.split('');
    strAccentsOut = new Array();
    strAccentsLen = strAccents.length;
    var badChars = "~`!@#$%^&*()_-+={[}]|\:;\"'<,>.?/";
    var accents = 'ÀÁÂÃÄÅàáâãäåÒÓÔÕÕÖØòóôõöøÈÉÊËèéêëðÇçÐÌÍÎÏìíîïÙÚÛÜùúûüÑñŠšŸÿýŽžÆßæ';
    var accentsOut = ['A', 'A', 'A', 'A', 'A', 'A', 'a', 'a', 'a', 'a', 'a', 'a', 'O', 'O', 'O', 'O', 'O', 'O', 'O', 'o', 'o', 'o', 'o', 'o', 'o', 'E', 'E', 'E', 'E', 'e', 'e', 'e', 'e', 'e', 'C', 'c', 'D', 'I', 'I', 'I', 'I', 'i', 'i', 'i', 'i', 'U', 'U', 'U', 'U', 'u', 'u', 'u', 'u', 'N', 'n', 'S', 's', 'Y', 'y', 'y', 'Z', 'z', 'AE', 'ss', 'ae'];
    for (var y = 0; y < strAccentsLen; y++) {
        var code = strAccents[y].charCodeAt(0);
        if (accents.indexOf(strAccents[y]) !== -1) {
            strAccentsOut[y] = accentsOut[accents.indexOf(strAccents[y])];
        } else if ((code < 32) || (code > 127)) {
            strAccentsOut[y] = " ";
        } else {
            strAccentsOut[y] = strAccents[y];
        }
    }
    strAccentsOut = strAccentsOut.join('');
    strAccentsOut = strAccentsOut.replace(/  */g, " ").replace(/ *$/, "");
    return strAccentsOut;
}


function showDetails(sobj,useWma) {

    var isNonMusic = false;
    if (sobj.mp3[useWma] !== "") {
        var isMusic = (sobj.mp3[useWma].indexOf(".mp3") > 0) || (sobj.mp3[useWma].indexOf(".wav") > 0);
        isNonMusic = !isMusic;
    }
    var tmedia = sobj.mp3[useWma];
    var sobj2 = undefined;

    if ((useWma === cReal) || (useWma === cKaraoke)) {
        if (useWma === cReal) {
            for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
                if (theRsongs[i].radioid === sobj.songid) {
                    if (theRsongs[i].mp3[useWma] === tmedia) {
                        sobj2 = theRsongs[i]; break;
                    }
                }
            }
        } else if (useWma === cKaraoke) {
            for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
                if (theKsongs[i].radioid === sobj.songid) {
                    if (theKsongs[i].mp3[useWma] === tmedia) {
                        sobj2 = theKsongs[i]; break;
                    }
                }
            }
        }
        if (sobj2 === undefined) {
        //  if (useWma === cReal) {
        //      notify_add_error(n, useWma, "youtube did not return a 'real' version of this song");
        //  } else {
        //      notify_add_error(n, useWma, "youtube did not return a 'karaoke' of this song");
        //  }
        //  return;
        }
    }
    if (sobj2 !== undefined) {
        sobj = sobj2;
    }

    var srch_artist = cleanString(sobj.artist);
    var srch_song = cleanString(sobj.song + " " + sobj.artist);
    srch_artist = srch_artist.replace(/ /g, "+");
    srch_song = srch_song.replace(/ /g, "+");

    var artist_url = "http://www.youtube.com/results?search_query=" + srch_artist;
    var song_url = "http://www.youtube.com/results?search_query=" + srch_song;

    if (sobj2 !== undefined) {
        song_url = sobj2.mp3[useWma];
    }

    var artist_wiki_url = "http://wikipedia.org/w/index.php?search=" + srch_artist;
    var artist_wiki_nl_url = "http://nl.wikipedia.org/w/index.php?search=" + srch_artist;
    var artist_wiki_es_url = "http://es.wikipedia.org/w/index.php?search=" + srch_artist;
    var artist_google_url = "http://www.google.com/search?q=%22" + srch_artist + "%22";
    artist_google_url = "http://www.google.com/search?q=(" + srch_artist + ")";

    //                                    vvvvvv -- was webhp which everyone is calling a 'virus'
    var text_url = "http://www.google.com/search?q=(letras+OR+lyrics+OR+text)+AND+(" + srch_song + ")&"; // deze pagina is gewijzigd in internet explorer om scripting op meerdere sites te helpen voorkomen
    var song_wiki_url = "http://wikipedia.org/w/index.php?search=" + srch_song;
    var song_wiki_nl_url = "http://nl.wikipedia.org/w/index.php?search=" + srch_song;
    var song_wiki_es_url = "http://es.wikipedia.org/w/index.php?search=" + srch_song;
    var song_google_url = "http://www.google.com/search?q=%22" + srch_song + "%22";
    song_google_url = "http://www.google.com/search?q=(" + srch_song + ")";

    var x = x;

    $('#WikiEng').unbind('click').click(function () {
        window.open(artist_wiki_url);
    }).show();
    $('#WikiNed').unbind('click').click(function () {
        window.open(artist_wiki_nl_url);
    }).show();
    $('#WikiEs').unbind('click').click(function () {
        window.open(artist_wiki_es_url);
    }).show();
    $('#YouTube').unbind('click').click(function () {
        window.open(artist_url);
    }).show();
    $('#Google').unbind('click').click(function () {
        window.open(artist_google_url);
    }).show();
    $('#WikiEngSong').unbind('click').click(function () {
        window.open(song_wiki_url);
    }).show();
    $('#WikiNedSong').unbind('click').click(function () {
        window.open(song_wiki_nl_url);
    }).show();
    $('#WikiEsSong').unbind('click').click(function () {
        window.open(song_wiki_es_url);
    }).show();
    $('#GoogleSong').unbind('click').click(function () {
        window.open(song_google_url);
    }).show();
    $('#YouTubeSong').unbind('click').click(function () {
        window.open(song_url);
    }).show();
    $('#Lyrics').unbind('click').click(function () {
        window.open(text_url);
    }).show();
    $('#Translate').unbind('click').click(function () {
        window.open("http://translate.google.com");
    }).show();

    var mySong = {
        mp3: sobj.mp3[0],
        wma: sobj.mp3[1],
        artwork: sobj.photo,
        download_mp3: function () {
            downloadFile(this.mp3);
        },
        download_wma: function () {
            downloadFile(this.wma);
        },
        download_foto: function () {
            downloadFile(this.artwork);
        }
    };

    //  $("#watch_song_title").html(sobj.song + " (" + sobj.artist + ")");
    $('input[id=theArtistName]').val(sobj.artist);
    $('input[id=theSongName]').val(sobj.song);

    $("#theRecordedOn").html(sobj.month_year);
    $("#theRecordedOnN").html("~ " + sobj.month_year + " ~");
    if (isNonMusic) {
        //  $("#theMp3").html("<a href=\"" + sobj.mp3[useWma] + "\">DOWNLOAD VIDEO</a>");
        $("#theMp3").unbind('click').click($.proxy(mySong.download_mp3, mySong));
    } else {
        //  $("#theMp3").html("<a href=\"" + sobj.mp3[useWma] + "\">DOWNLOAD MP3</a>");
        $("#theMp3").unbind('click').click($.proxy(mySong.download_mp3, mySong));
    }
    if (sobj.mp3[1] && (sobj.mp3[1] !== "")) {
        //  var p1 = sobj.mp3[useWma].lastIndexOf("/");
        //  var p2 = sobj.mp3[1].lastIndexOf("\\");
        //  var wavUrl = sobj.mp3[useWma].substr(0, p1 + 1) + sobj.mp3[1].substr(p2 + 1, sobj.mp3[1].length - (p2 + 1));
        //  $("#theWav").html("<a href=\"" + encodeURI(wavUrl) + "\">DOWNLOAD High Quality</a>").show();
        $("#theWav").unbind('click').click($.proxy(mySong.download_wma, mySong));
    } else {
        $("#theWav").hide();
    }
    var artwork = sobj.photo;
    var pp = artwork.lastIndexOf("-P.");
    if (pp) {
        mySong.artwork = artwork.substr(0, pp) + artwork.substr(pp + 2, artwork.length - pp - 2);
    }
    //  $("#thePic").html("<a href=\"" + artwork + "\">ARTWORK</a>");
    $("#thePic").unbind('click').click($.proxy(mySong.download_foto, mySong));

    var wants_url = window.location.href;
    var p2 = wants_url.toLowerCase().indexOf("/mobile/");
    if (p2 < 0) p2 = wants_url.indexOf("?");
    if (p2 > 0) {
        wants_url = wants_url.substr(0, p2);
    }
    var tmedia2 = wants_url + "?ID=" + sobj.songid;
    if (current_page === "#MUSIC.radios") {
        tmedia2 = wants_url + "?Radio=" + sobj.songid;
    }
    if (current_page === "#MUSIC.bands") {
        tmedia2 = wants_url + "?Band=" + sobj.songid;
    }
    if (current_page === "#MUSIC.karaoke") {
        tmedia2 = wants_url + "?Karaoke=" + sobj.songid;
    }
    if (current_page === "#VIDEOS.live") {
        tmedia2 = wants_url + "?Video=" + sobj.songid;
    }
    if (current_page === "#VIDEOS.solo") {
        tmedia2 = wants_url + "?Solo=" + sobj.songid;
    }
    if (current_page === "#VIDEOS.mtv") {
        tmedia2 = wants_url + "?MTV=" + sobj.songid;
    }
    if (current_page === "#VIDEOS.prive") {
        tmedia2 = wants_url + "?Prive=" + sobj.songid;
    }
    //  $("#SongUrl").html(tmedia);
    $('input[id=SongUrl]').val(tmedia2);
    $("#SongUrl").show();

    $("#SongId").html(sobj.songid);
    $("#CriticsReview").html(sobj.critics_review);
    if (sobj.critics_review === "") {
        $("#CriticsReview").hide();
        $("#infReview").hide();
    } else {
        $("#CriticsReview").show();
        $("#infReview").show();
    }
    $("#DiaryText").html(sobj.diary_text);
    if (sobj.diary_text === "") {
        $("#DiaryText").hide();
        $("#infDiary").hide();
    } else {
        $("#DiaryText").show();
        $("#infDiary").show();
    }
    //  $("#SongNameMarquee").innerHTML = this.mp3[useWma];
    var radio_name = "";
    if (!sobj.article_title || sobj.article_title === "") {
        title = sobj.song + " // " + sobj.artist;
    } else {
        title = sobj.article_title;
    }
    if (sobj.radioname !== "") {
        title = title + " --- " + sobj.radioname;
        radio_name = sobj.radioname;
    }
    $("#SongNameMarquee").html("<marquee onMouseover=\"this.scrollAmount=3\" onMouseout=\"this.scrollAmount=6\">" + title + "</marquee>");
    $("#radioShow").html(radio_name);
    if (radio_name === "") {
        $("#obj_with_show").hide();
        $("#obj_no_show").show();
    } else {
        $("#obj_with_show").show();
        $("#obj_no_show").hide();
    }

}

function showVideoDetails(sobj,useWma) {

    var isNonMusic = true;

    var tmedia = sobj.mp3[useWma];
    var real_song = sobj.mp3[cMusic];  // VIDEO:  sobj.mp3[0] is youtube  sobj.mp3[1] is ""
    var sobj2 = undefined;             //         when pressing INFO on player usewma is 0

    if ((useWma === cReal) || (useWma === cKaraoke)) {
        if (useWma === cReal) {
            for (var i = 1; i <= num_rsongs; i++) { // find first youtube for this song
                if (theRsongs[i].query === sobj.query) {
                    if (theRsongs[i].mp3[useWma] === tmedia) {
                        sobj2 = theRsongs[i]; break;
                    }
                }
            }
        } else if (useWma === cKaraoke) {
            for (var i = 1; i <= num_ksongs; i++) { // find first youtube for this song
                if (theKsongs[i].query === theFsongs[n].query) {
                    if (theKsongs[i].mp3[useWma] === tmedia) {
                        sobj2 = theKsongs[i]; break;
                    }
                }
            }
        }
        if (sobj2 === undefined) {
            //  if (useWma === cReal) {
            //      notify_add_error(n, useWma, "youtube did not return a 'real' version of this song");
            //  } else {
            //      notify_add_error(n, useWma, "youtube did not return a 'karaoke' of this song");
            //  }
            //  return;
        }
    }
    if (sobj2 !== undefined) {
        sobj = sobj2;  // swap sobj with video-object that contains media
        tmedia = sobj.mp3[useWma];
    }
    var dat = "";
    if (sobj.month_year) {
        if (sobj.rel_mo_yr) {
            if (sobj.month_year === sobj.rel_mo_yr) {
                dat = sobj.month_year;
            } else {
                dat = sobj.rel_mo_yr + " (released on)  ...  " + sobj.month_year + " (recorded on)";
            }
        } else {
            dat = sobj.month_year + " (recorded on)";
        }
    } else {
        if (sobj.rel_mo_yr) {
            dat = sobj.rel_mo_yr + " (released on)";
        }
    }
    $("#theArtistNameV").html(sobj.artist);
    $("#theSongNameV").html(sobj.song);
    $("#theRecordedOnV").html(dat);
    $("#SongIdV").html(sobj.songid);
    $("#CriticsReviewV").html(sobj.critics_review);
    if (sobj.critics_review === "") {
        $("#CriticsReviewV").hide();
        $("#infReviewV").hide();
    } else {
        $("#CriticsReviewV").show();
        $("#infReviewV").show();
    }
    $("#DiaryTextV").html(sobj.diary_text);
    if (sobj.diary_text === "") {
        $("#DiaryTextV").hide();
        $("#infDiaryV").hide();
    } else {
        $("#DiaryTextV").show();
        $("#infDiaryV").show();
    }
    var mySong = {
        mp3: tmedia,
        wma: real_song,
        artwork: sobj.photo,
        download_mp3: function () {
            downloadFile(this.mp3);
        },
        download_latincita: function () {
            downloadFile(this.wma);
        },
        download_foto: function () {
            downloadFile(this.artwork);
        }
    };
    var artwork = sobj.photo;
    var pp = artwork.lastIndexOf("-P.");
    if (pp) {
        mySong.artwork = artwork.substr(0, pp) + artwork.substr(pp + 2, artwork.length - pp - 2);
    }
    mySong.artwork = mySong.artwork.replace(/[/][Mm][Oo][Bb][Ii][Ll][Ee][/]/, "/");
    //  $("#thePic").html("<a href=\"" + artwork + "\">ARTWORK</a>");
    $("#thePicY").unbind('click').click($.proxy(mySong.download_foto, mySong));
    $("#thePicV").unbind('click').click($.proxy(mySong.download_foto, mySong));

    if (tmedia.indexOf("youtu.be") > 0 || tmedia.indexOf(".youtube.") > 0) {
        var url = tmedia;
        if (tmedia.indexOf("/v/") > 0) {
            url = tmedia.replace(/[/]v[/]/, "/embed/");
        }
        $("#theMp3V-wrapper").hide();
        $("#theMp3Y").html("<a id=\"theMp3Y\" href=\"" + url + "\" data-role=\"button\" data-icon=\"action\" target=\"_blank\">Download Video</a>");
    //  $("#theMp3Y").html("<a id=\"thePicY\" href=\"#\" data-role=\"button\" data-icon=\"action\">Download Artwork</a>");
        $("#theMp3Y-wrapper").show();
    } else {
        $("#theMp3Y-wrapper").hide();
        $("#theMp3V").unbind('click').click($.proxy(mySong.download_mp3, mySong));
        $("#theMp3V-wrapper").show();
    }
    //  $("#SongNameMarquee").innerHTML = this.mp3[useWma];
    if (!sobj.article_title || sobj.article_title === "") {
        title = sobj.song + " // " + sobj.artist;
    } else {
        title = sobj.article_title;
    }
    $("#SongNameMarqueeV").html("<marquee onMouseover=\"this.scrollAmount=3\" onMouseout=\"this.scrollAmount=6\">" + title + "</marquee>");
    if (tmedia.startsWith("/")) {
        var wants_url = window.location.href;
        var p2 = wants_url.toLowerCase().indexOf("/mobile/");
        if (p2 < 0) p2 = wants_url.indexOf("?");
        if (p2 > 0) {
            wants_url = wants_url.substr(0, p2);
        }
        tmedia = wants_url + tmedia;
    }
    $('input[id=VideoURLV]').val(tmedia);

}

function showMe2(targ) {
    var targObj = $(targ);
    if (targObj.length > 0) {
        var idx = targObj.data("itemIndex");
        if (idx) {
            var id = "#TrackItem" + idx;
            n = -1;
            for (var i = 1; i <= num_fsongs; i++) {
                if (theFsongs[i].boxid === id) {
                    n = i; break;
                }
            }
            if (n > 0) {
                showDetails(theFsongs[i], cMusic);
                $("#songDetails").popup("open");
            } else {
                alert(id + " not found.");
            }
        }
    }
}

function showMe(targ) {
    if (targ) {
        var id = targ.alt;
        if (!id) {
            var targObj = $(targ);
            if (targObj.length > 0) {
                id = targObj.data("id");
            }
        }
        if (id) {
            id = "#SongListItem" + id;
            n = -1;
            for (var i = 1; i <= num_fsongs; i++) {
                if (theFsongs[i].boxid === id) {
                    n = i; break;
                }
            }
            if (n > 0) {
                showDetails(theFsongs[i], cMusic);
                $("#songDetails").popup("open");
            } else {
                alert(id + " not found.");
            }
        }
    }
}

function showMeVideo(targ) {
    if (targ) {
        var id = targ.alt;
        if (!id) {
            var targObj = $(targ);
            if (targObj.length > 0) {
                id = targObj.data("id");
            }
        }
        if (id) {
            id = "#SongListItem" + id;
            n = -1;
            for (var i = 1; i <= num_fsongs; i++) {
                if (theFsongs[i].boxid === id) {
                    n = i; break;
                }
            }
            if (n > 0) {
                showVideoDetails(theFsongs[i], cMusic); // <<< seems to want useWma = 0 and not 1
                $("#videoDetails").popup("open");
            } else {
                alert(id + " not found.");
            }
        }
    }
}

function handleSongNum(songId, useWma, action) {  // not used

    var n = -1;
    var idx = -1;

    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].songid === songId) {
            n = i; break;
        }
    }
    if (n > 0) {
        for (var i = 0; i < myPlaylist.playlist.length; i++) {
            if ((myPlaylist.playlist[i].id === n) && (myPlaylist.playlist[i].useWma === useWma)) {
                idx = i; break;
            }
        }
        if (idx < 0) {
            if (theFsongs[n].played[useWma] === 0) {
                playMe(theFsongs[n].boxid);
            }
            if (action === "play") {
                if (theFsongs[n].played[useWma] === cAdded) {
                    playMe(theFsongs[n].boxid);
                }
            }
        }
    }
}

function play_url(s) {

    var useWma = cMusic;
    var songidx = -1;
    var wait_time = 0;
    var play_now = true;  // true = play tracks immediatly, instead of just adding them to the playlist
    var id = "";

    if (s.endsWith("</p>")) {
        s = s.substring(0, s.length - 4);
    }
    s = s.trim();

//  alert(s);

    if (s.startsWith("https://www.latincita.com/?ID=") || s.startsWith("https://www.latincita.com?ID=")) {
        var p = s.indexOf("=");
        var id = s.substr(p + 1);
    //  alert('Play ID: ' + id);
        useWma = cMusic;
        wait_time = 1500;
        for (var i = 1; i <= num_fsongs; i++) {
            if (theFsongs[i].songid === id) {
                songidx = i; break;
            }
        }
        if (songidx < 1) {
            alert("Can't find songid " + id);
            return;
        }

    } else if (s.startsWith("https://www.youtube.com/@")) {
        // Youtube Channel
    //  window.location.href = s;
        window.open(s, '_blank');
        return;

    } else if (s.startsWith("https://www.youtube") || s.startsWith("https://youtu")) {
    //  alert('Play Video: ' + s);

        var video_url = s;
        useWma = cVideo;
        wait_time = 3000;

        for (var i = 1; i <= num_fsongs; i++) {
            if (theFsongs[i].mp3[useWma] !== "") {
                if (theFsongs[i].mp3[useWma] === video_url) {
                    id = theFsongs[i].songid;
                    songidx = i; break;
                }
            }
        }
        if (songidx < 1) {

            retrieveVideoData(this_target, video_url, displayVideoData);   // <<< processing continues in callback

            return;
        }

    } else if (s.startsWith("https://www.latincita.com")) {
    //  alert('Redirect to: ' + s);
        s = s.substring("https://www.latincita.com".length);
    //  window.location.href = s;
        window.open(s, '_blank');
        return;
    } else {
    //  window.location.href = s;
        window.open(s, '_blank');
        return;
    }

    if (theFsongs[songidx].is_track_object === false) {
        if (theFsongs[songidx].divobj !== undefined) {
            if (songIsShown(songidx) === false) {
                $('#dataItemList').prepend(theFsongs[songidx].divobj); // display newest @ top, oldest @ bottom
                theFsongs[songidx].divobj.show();

                check_sortable();
            }
        }
    }

    for (var j = 0; j < myPlaylist.playlist.length; j++) {
        if ((myPlaylist.playlist[j].id === songidx) && (myPlaylist.playlist[j].useWma === useWma)) {
            if (isPlaying()) {
                myPlaylist.stop();
            }
            myPlaylist.select(j);
        //  if user clicks a second time on something loaded already, then just start playing it
            setTimeout(function () { myPlaylist.play(j); }, wait_time);
            return;
        }
    }
    if (useWma === cMusic) {
        find_matching_videos(songidx);  // random can select anything and make it visible
    }

    addToPlaylist(songidx, useWma, true);

    format_track_list();  // round corners @ top and bottom of list & init drag

    init_playlist_drag(myPlaylist);

    recreateStars(songidx, useWma);

    if (play_now || !isPlaying()) {
        if (isPlaying()) {
            myPlaylist.stop();
        }
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === songidx) && (myPlaylist.playlist[j].useWma === useWma)) {
                myPlaylist.select(j);
                setTimeout(function () { myPlaylist.play(j); }, wait_time);
                break;
            }
        }
    }

}

function displayVideoData(video_url, video_data) {

    var songidx = -1;
    var wait_time = 3000;
    var play_now = true;  // true = play tracks immediatly, instead of just adding them to the playlist
    var useWma = cVideo;

    if (! video_data) {
        var track = new fTrackObject();

        var box_id = "";
        var song_id = "";
        var artist =  "Latincita";
        var song = "";
        var video_song = "";
        var genre = "";
        var lang = "";
        var month_year = "";    // recorded on
        var rel_mo_yr = "";     // released on
        var mesterwerk = "True";
        var purpose = "";

        var article_title = "";
        var critics_review = "";
        var diary_text = "";
        var mp3 = video_url;   // song_url
        var wma = "";
        var photo = "";
        var tracknum = "";
        var radio_id = "";
        var radio_name = "";
        var soffset = "";
        var snxtoffset = "";
        var sduration = "";
        var stars = 0;
        var real_url = "";
        var karaoke_url = "";
        var version_of_song = "";

        var isTrackObj = false
        var isTrack = isTrackObj;  // or offset > 0

        track.Set(box_id, song_id, radio_id, radio_name, tracknum, soffset, snxtoffset, sduration, artist, song, video_song, genre, lang, purpose, version_of_song, month_year, rel_mo_yr, mesterwerk,
                  article_title, critics_review, diary_text, mp3, wma, photo, stars, isTrackObj, isTrack);

        if (song === "") {
            track.display_name = video_url;
        }
        num_fsongs++;
        theFsongs[num_fsongs] = track;

        songidx = num_fsongs;
    } else {
        songidx = addDummyTrack(video_data);
    }

    // NOTE: this routine is only called for new videos that have not been played yet

    theFsongs[songidx].mp3[useWma] = video_url; // track.Set stores it in cMusic

    var sobj = theFsongs[songidx];

    for (var j = 0; j < myPlaylist.playlist.length; j++) {
        if ((myPlaylist.playlist[j].id === songidx) && (myPlaylist.playlist[j].useWma === useWma)) {
            if (isPlaying()) {
                myPlaylist.stop();
            }
            myPlaylist.select(j);
        //  if user clicks a second time on something loaded already, then just start playing it
            setTimeout(function () { myPlaylist.play(j); }, wait_time);
            return;
        }
    }
    if (useWma === cMusic) {
        find_matching_videos(songidx);  // random can select anything and make it visible
    }

    addToPlaylist(songidx, useWma, true);

    format_track_list();  // round corners @ top and bottom of list

    init_playlist_drag(myPlaylist);

    recreateStars(songidx, useWma);

    if (play_now || !isPlaying()) {
        if (isPlaying()) {
            myPlaylist.stop();
        }
        for (var j = 0; j < myPlaylist.playlist.length; j++) {
            if ((myPlaylist.playlist[j].id === songidx) && (myPlaylist.playlist[j].useWma === useWma)) {
                myPlaylist.select(j);
                setTimeout(function () { myPlaylist.play(j); }, wait_time);
                break;
            }
        }
    }
}


function playReal(bid) {
    var useWma = cReal;
    playSub(bid, useWma);
}

function playKaraoke(bid) {
    var useWma = cKaraoke;
    playSub(bid, useWma);
}

function viewMe(bid) {
    var useWma = cVideo;  // <<< cVideo was 1 == cKaraoke
    playSub(bid, useWma);
}

function playMe(bid) {
    var useWma = cMusic;
    playSub(bid, useWma);
}

function playSub(bid, useWma) {

    var n = -1;
    for (var i = 1; i <= num_fsongs; i++) {
        if (theFsongs[i].boxid === bid) {
            n = i; break;
        }
    }
    if (n > 0) {
        var playable = true;
        switch (theFsongs[n].played[useWma]) {
            case cNotPlayed:
                if ((useWma === cReal) || (useWma === cKaraoke)) {
                    playable = false;
                    notify_add_error(n, useWma, "Video's for this song have not yet been requested.");
                }
                break;
            case cDead:
                playable = false;
                notify_add_error(n, useWma, "YouTube did not return any video's for this song."); break;
            case cQueued:
                playable = false;
                notify_add_error(n, useWma, "Video's for this song are still waiting to be retrieved."); break;
            case cWaiting:
                playable = false;
                notify_add_error(n, useWma, "YouTube has not yet returned any video's for this song."); break;
            case cDisabled:
                playable = false;  // not allowed to play
                break;
        }
        if (!playable) {
            return;
        }
        //alert(theFsongs[n].mp3[useWma]);
        //$("#jpId").jPlayer({
        //    ready: function () { // The $.jPlayer.event.ready event
        //        $(this).jPlayer("setMedia", { // Set the media
        //            mp3: theFsongs[n].mp3[useWma]
        //        }).jPlayer("play"); // Attempt to auto play the media
        //    },
        //    supplied: "mp3"
        //});
        var tmedia = "";
        var actie = "";
        var event = -999;
        if (theFsongs[n].mp3[useWma] !== "") {
            if ((theFsongs[n].played[useWma] === cNotPlayed) ||
                (theFsongs[n].played[useWma] === cEnabled)) {
                actie = "add";  event = cAdded;
            } else if (theFsongs[n].played[useWma] === cAdded) {
                actie = "play";  // clicking cAdded button, color doesn't change until "played" is received by callback
            } else if (theFsongs[n].played[useWma] === cPlaying) {
                actie = "stop";  // clicking cPlaying button, color doesn't change until "stopped" is received by callback
            } else if (theFsongs[n].played[useWma] === cPlayed) {
                actie = "del";  event = cRemoved;
            } else { //                                cRemoved
                actie = "add"; event = cAdded;
            }
            if (actie === "play") {
                //  clear_all_red();  -- don't do this, want to see what we've akready played
            }
            if (event !== -999) {
                showPlayState(n, useWma, event);
            }
            if (actie === "add") {
                //  theFsongs[n].played[useWma] = cAdded;     -- done in addToPlaylist
            } else if (actie === "play") {
                //  theFsongs[n].played[useWma] = cPlaying;   -- done in playlistEventHandler
            } else if (actie === "stop") {
                //  theFsongs[n].played[useWma] = cPlayed;    -- done in playlistEventHandler
            } else {
                //  theFsongs[n].played[useWma] = cRemoved;   -- done in delFromPlaylist
            }
            tmedia = theFsongs[n].mp3[useWma];
            if (tmedia === "" || extension(tmedia) === "") {
                notify_add_error(n, useWma, "song (n) is missing");
            } else {
                if (actie === "add") {
                    addToPlaylist(n, useWma, true);
                    init_playlist_drag(myPlaylist);
                    recreateStars(n, useWma);
                }
                var idx = -1;
                for (var i = myPlaylist.playlist.length - 1; i >= 0; i--) { // does this work from MUSIC if item is a track ?
                    if ((myPlaylist.playlist[i].id === n) && (myPlaylist.playlist[i].useWma === useWma)) {
                        idx = i; break;
                    }
                }
                if (actie === "del") {
                    delFromPlaylist(n, useWma, idx);
                    init_playlist_drag(myPlaylist);
                    //  if (idx >= 0) {
                    //      myPlaylist.remove(idx);  --done in delFromPlaylist
                    //  }
                }
                if (actie === "play") {
                    if (idx >= 0) {
                        myPlaylist.select(idx);
                        setTimeout(function () { myPlaylist.play(idx); }, 3000);
                    }
                }
                if (actie === "stop") {
                    if (idx >= 0) {
                        myPlaylist.select(idx);
                        setTimeout(function () { myPlaylist.pause(); }, 100);
                    }
                }
            }
        } else {
            notify_add_error(n, useWma, "item (n) is missing");
        }
    } else {
        notify_add_error(n, useWma, "item (n) is not found");
    }
}

function showPlayState(sidx, useWma, event) {

    if (defaultTitle !== '') {
        var currTitle = document.title;
        var newTitle = currTitle;
        if (event === cPlaying) {
            //  display track-name in window title
            var sobj = theFsongs[sidx];
            if (sobj) {
                newTitle = 'Latincita: ' + sobj.song;
            } else {
                newTitle = defaultTitle;
            }
        } else {
            //  display raw title
            newTitle = defaultTitle;
        }
        if (currTitle !== newTitle) {
            $(document).prop('title', newTitle);
        }
    }

    var playButton = undefined;

    if (sidx >= 1 && sidx <= num_fsongs) {
        if (event === cEnabled) {  // don't let YouTube results clear user's playstate
            if ((useWma === cReal) || (useWma === cKaraoke)) {
                switch (theFsongs[sidx].played[useWma]) {
                    case cAdded:
                    case cPlaying:
                    case cPlayed:
                    case cRemoved:
                        event = theFsongs[sidx].played[useWma]; break;
                }
            }
        } else if (event === cRemoved) { // don't let playlistEventHandler to toggle cNotPlayed that was just set by removeall
            switch (theFsongs[sidx].played[useWma]) {
                case cNotPlayed:
                    event = cNotPlayed; break;
            }
        }
        if (theFsongs[sidx].divobj !== undefined) {
            if (theFsongs[sidx].is_track_object === false) {
                playButton = theFsongs[sidx].divobj.find(play_sel(useWma));
            }
        }
    }

    if (playButton && playButton.length > 0) {
    //  playButton.addClass("ui-added-x");
        playButton.removeClass("ui-added-x");
    //  playButton.addClass("ui-playing-x");
        playButton.removeClass("ui-playing-x");
    //  playButton.addClass("ui-played-x");
        playButton.removeClass("ui-played-x");
    //  playButton.addClass("ui-removed-x");
        playButton.removeClass("ui-removed-x");
    //  playButton.addClass("ui-disabled");
        playButton.removeClass("ui-disabled");
    //  playButton.addClass("ui-disabled-x");
        playButton.removeClass("ui-disabled-x");
    //  playButton.addClass("ui-died-x");
        playButton.removeClass("ui-died-x");
    //  playButton.addClass("ui-notqueried-x");
        playButton.removeClass("ui-notqueried-x");
    //  playButton.addClass("ui-queued-xx");
        playButton.removeClass("ui-queued-x");
    //  playButton.addClass("ui-waiting-xx");
        playButton.removeClass("ui-waiting-x");

        switch (event) {
            case cAdded:
                playButton.addClass("ui-added-x"); break;
            case cPlaying:
                playButton.addClass("ui-playing-x"); break;
            case cPlayed:
                playButton.addClass("ui-played-x"); break;
            case cRemoved:
                playButton.addClass("ui-removed-x"); break;
            case cDisabled:
                playButton.addClass("ui-disabled"); break;
            case cEnabled:
            //  playButton.removeClass("ui-disabled"); 
                break;
            case cNotPlayed:
            //  playButton.addClass("ui-disabled-x");
                if ((useWma === cReal) || (useWma === cKaraoke)) {
                    playButton.addClass("ui-notqueried-x");
                }
                break;
            case cQueued:
                playButton.addClass("ui-disabled-x");
                playButton.addClass("ui-queued-x");
                break;
            case cWaiting:
                playButton.addClass("ui-disabled-x");
                playButton.addClass("ui-waiting-x");
                break;
            case cDead:
                playButton.addClass("ui-disabled-x");
                playButton.addClass("ui-died-x");
                break;
        }
    //  if (event === cDisabled || event === cEnabled) {
    //      theFsongs[id].played[useWma] = cNotPlayed;
    //  } else {
            theFsongs[sidx].played[useWma] = event;
    //  }
    //  console.log("showPlayState [" + sidx + "," + useWma + "] {" + theFsongs[sidx].query + "} -> " + event);
    }

}

// ----------------------------------   [ filter_mobile ]