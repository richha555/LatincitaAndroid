using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
//using UIKit;

namespace LatincitaAndroid.Services;

public class FavoriteService
{
    HttpClient httpClient;
    public FavoriteService()
    {
        this.httpClient = new HttpClient();
    }

    List<RadioProgram> FavoriteList;
    public async Task<List<RadioProgram>> GetFavorites()
    {
        if (FavoriteList?.Count > 0)
            return FavoriteList;

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
            new MediaTypeWithQualityHeaderValue("application/json"));

        var url = "https://www.latincita.com/api/play";
        Debug.WriteLine("| >>> URL: " + url);

        // Online
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode) {
            FavoriteList = await response.Content.ReadFromJsonAsync(RadioProgramContext.Default.ListRadioProgram);
        }

        foreach (RadioProgram radio in FavoriteList)
            radio.Type = RadioProgramType.FAVORITE;

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        return FavoriteList;
    }
}
