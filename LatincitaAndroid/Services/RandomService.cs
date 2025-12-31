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

        var errmsg = "";
        var response = await httpClient.GetAsync("https://www.latincita.com/api/AllLatincita/Random");
        if (response.IsSuccessStatusCode)
        {
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
