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
public class RadioProgramsService
{
    HttpClient httpClient;
    public RadioProgramsService()
    {
        this.httpClient = new HttpClient();
    }

    List<RadioProgram> RadioProgramsList;
    public async Task<List<RadioProgram>> GetRadioPrograms()
    {
        if (RadioProgramsList?.Count > 0)
            return RadioProgramsList;

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

        // Online
        var response = await httpClient.GetAsync("https://www.latincita.com/api/RadioPrograms");
        if (response.IsSuccessStatusCode)
        {
            RadioProgramsList = await response.Content.ReadFromJsonAsync(RadioProgramContext.Default.ListRadioProgram);
        }

        foreach (RadioProgram radio in RadioProgramsList)
            radio.Type = RadioProgramType.RADIO;

        // Offline
        /*using var stream = await FileSystem.OpenAppPackageFileAsync("Monkeydata.json");
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        MonkeyList = JsonSerializer.Deserialize(contents, MonkeyContext.Default.ListMonkey);*/

        return RadioProgramsList;
    }
}
