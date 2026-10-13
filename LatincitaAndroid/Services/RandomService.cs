using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace LatincitaAndroid.Services;
public class RandomService
{
    HttpClient httpClient;
    public RandomService()
    {
        this.httpClient = new HttpClient();
    }

    public string played_list = "";

    PlayListItem RandomTrack;

    public async Task<PlayListItem> GetRandom()
    {
        ////#if ANDROID
        //        var handler = new AndroidMessageHandler();
        //        handler.ServerCertificateCustomValidationCallback =
        //            (req, cert, chain, errors) =>
        //                req.RequestUri.Host == "www.latincita.com";

        //        var client = new HttpClient(handler);
        //        client.DefaultRequestVersion = HttpVersion.Version11;
        //        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        //        client.Timeout = TimeSpan.FromSeconds(30);

        //    //#else
        //    //      var client = new HttpClient();
        //    //#endif

        // Online
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var url = "https://www.latincita.com/api/all/random";
        if (this.played_list != "") {
            url += "?played_list=" + this.played_list;
        }
        Debug.WriteLine("| >>> URL: " + url);

        var errmsg = "";
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
        //  RandomList = await response.Content.ReadFromJsonAsync(RadioProgramContext.Default.ListRadioProgram);

            var json = await response.Content.ReadAsStringAsync();
            try {
                json = json.Replace("\n", "").Replace("\r", "");
                if (json.StartsWith("\"")) {
                    json = json.Substring(1);
                    if (json.EndsWith("\"")) {
                        json = json.Substring(0, json.Length - 1);
                    }
                }
                json = json.Replace("\\r\\n", "\n");
                json = json.Replace("\\r", "\n");
                json = json.Replace("\\n", "\n");
                json = json.Replace("\\\"", "\"");
                json = json.Replace("\\\\", "\\");
                RandomTrack = JsonSerializer.Deserialize<PlayListItem>(json);

                if (!string.IsNullOrWhiteSpace(RandomTrack.played_list)) {
                    this.played_list = RandomTrack.played_list;  // update played-list
                }

// {
//   "title": "Latincita - Papa Don't Preach - Madonna",
//   "artist": "Madonna",
//   "m4v": "https://www.latincita.com/best_of_latincita/Radio%2016%20October.mp3",
//   "poster": "https://www.latincita.com/Images/Mobile/Radio%20-%2016%20October-P.JPG",
//   "useWma": 0,
//   "entireRadio": false,
//   "songid": "1064",
//   "id": 582,
//   "track_list": [
//          {
//             "offs": 4832,
//             "nxtoffs": -1,
//             "duration": 7200,
//             "title": "Papa Don't Preach",
//             "artist": "Madonna",
//             "radioname": "Radio 16 October",
//             "radioid": "248"
//          }
//   ],
//   "image": {
//     "Title": "Radio - 16 October",
//     "FullTitle": "Radio - 16 October",
//     "ImageRelativeURL": "~/Images/Radio%20-%2016%20October.jpg",
//     "ImageFullURL": "http://www.latincita.com/Images/Radio%20-%2016%20October.jpg",
//     "Width": 3241,
//     "Height": 3243
//   },
//   "played_list": "1736,1064,185,47"
// }
            } catch(Exception ex) {
                errmsg = $"Error: {ex.Message}\nJSON: {json}"; 
            }
            if (errmsg != "") {
                throw new InvalidDataException(errmsg);
            }
        //  RandomTrack = await response.Content.ReadFromJsonAsync(PlayListItemContext.Default.PlayListItem);
        }

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        return RandomTrack;
    }
}
