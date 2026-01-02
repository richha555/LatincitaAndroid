using LatincitaAndroid.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static Android.Renderscripts.ScriptGroup;

namespace LatincitaAndroid.ViewModel;

public partial class RadioProgramsViewModel : BaseViewModel
{
    public ObservableCollection<RadioProgram> RadioPrograms { get; } = new();

    //[ObservableProperty]
    //private RadioProgram currentRadioProgram;

    //[ObservableProperty]
    //private TrackObject currentTrack;

    public AllLatincitaService AllLatincitaService { get; }
    public RadioProgramsService RadioProgramsService { get; }
    public RandomService RandomService { get; }
    public ProgramListService ProgramListService { get; }
    IConnectivity connectivity;
    public RadioProgramsViewModel(AllLatincitaService AllLatincitaService, RadioProgramsService RadioProgramsService, RandomService RandomService, ProgramListService ProgramListService, IConnectivity connectivity)
    {
        Title = "Radio Program Viewer";
        this.connectivity = connectivity;
        this.AllLatincitaService = AllLatincitaService;
        this.RadioProgramsService = RadioProgramsService;
        this.RandomService = RandomService;
        this.ProgramListService = ProgramListService;

        //RadioPrograms = ProgramListService.RadioPrograms;
        //CurrentRadioProgram = ProgramListService.CurrentRadioProgram;
        //CurrentTrack = ProgramListService.CurrentTrack;
    }

    [RelayCommand]
    async Task GoToDetails(RadioProgram RadioProgram)
    {
        if (RadioProgram == null)
            return;

        //  TrackObject _track = await AllLatincitaService.get_track(RadioProgram);

        ProgramListService.SetProgram(RadioProgram);
    //  ProgramListService.SetTrack(_track);

        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
        {
        //  ["RadioProgram"] = RadioProgram //,
        //  ["TrackObject"] = _track
        });
    }

    [ObservableProperty]
    bool isRefreshing;

    [RelayCommand]
    async Task GetRadioProgramsAsync()
    {
        if (IsBusy)
            return;

        try
        {
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Load All Radio Programs",
                    $"No connectivity!  Please check internet and try again.", "OK");
                return;
            }

            bool have_radio = false;
            have_radio = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.RADIO);

            if (have_radio) {
                Debug.WriteLine("Skipping loading Radio-Programs as they are already loaded.");
                //await Shell.Current.DisplayAlert("Load All Radio Programs",
                //    $"Radio-Programs appear to be already loaded - skipping load.", "OK");
                return;
            }

            IsBusy = true;
            var _RadioPrograms = await RadioProgramsService.GetRadioPrograms();

            //if (_RadioPrograms.Count != 0)
            //    this.RadioPrograms.Clear();
        //  ProgramListService.ClearList();

            //foreach (var RadioProgram in _RadioPrograms)
            //    this.RadioPrograms.Add(RadioProgram);
            ProgramListService.AddToList(_RadioPrograms);

            //if (CurrentRadioProgram != null) {
            //    TrackObject _track = await AllLatincitaService.get_track(CurrentRadioProgram);

            //    ProgramListService.SetTrack(_track);
            //} else {
            //    ProgramListService.SetTrack(null);
            //}

        } catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get RadioPrograms: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }

    }

    [RelayCommand]
    async Task GetRandomAsync()
    {
        if (IsBusy)
            return;

        try
        {
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("No connectivity!",
                    $"Please check internet and try again.", "OK");
                return;
            }

            ///////////////////////////// CODE IS DUPLICATED IN RadioProgramDetailsViewModel

            IsBusy = true;
            var _Random = await RandomService.GetRandom();

            if (_Random == null) {
                return;
            }

            ProgramListService.AddRandom(_Random);

        } catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get Random Track: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
