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

public class CdService
{
    HttpClient httpClient;
    public CdService()
    {
        this.httpClient = new HttpClient();
    }

    List<RadioProgram> CdList;
    public async Task<List<RadioProgram>> GetCds()
    {
        if (CdList?.Count > 0)
            return CdList;

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

        var url = "https://www.latincita.com/api/cd";
        Debug.WriteLine("| >>> URL: " + url);

        // Online
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode) {
            CdList = await response.Content.ReadFromJsonAsync(RadioProgramContext.Default.ListRadioProgram);
        }

        foreach (RadioProgram radio in CdList)
            radio.Type = RadioProgramType.CD;

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        return CdList;
    }
}
