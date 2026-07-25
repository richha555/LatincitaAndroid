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

    [ObservableProperty]
    private string pageTitle = "Latincita Android";

    [ObservableProperty]
    private string selectedButton = "";

    //[ObservableProperty]
    //private RadioProgram currentRadioProgram;

    //[ObservableProperty]
    //private TrackObject currentTrack;

    public AllLatincitaService AllLatincitaService { get; }
    public RadioProgramsService RadioProgramsService { get; }
    public CdService CdService { get; }
    public FavoriteService FavoriteService { get; }
    public RandomService RandomService { get; }
    public ProgramListService ProgramListService { get; }
    public AudioPlaybackService AudioPlaybackService { get; }

    IConnectivity connectivity;
    public RadioProgramsViewModel(AllLatincitaService AllLatincitaService, RadioProgramsService RadioProgramsService, CdService CdService, FavoriteService FavoriteService, RandomService RandomService, ProgramListService ProgramListService, AudioPlaybackService AudioPlaybackService, IConnectivity connectivity)
    {
        //  Title = "Radio Program Viewer";
        this.connectivity = connectivity;
        this.AllLatincitaService = AllLatincitaService;
        this.RadioProgramsService = RadioProgramsService;
        this.CdService = CdService;
        this.FavoriteService = FavoriteService;
        this.RandomService = RandomService;
        this.ProgramListService = ProgramListService;
        this.AudioPlaybackService = AudioPlaybackService;

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

        Debug.WriteLine("| SELECT PROGRAM: " + RadioProgram.ArticleTitle);

        await Task.Run(() =>
        {
            ProgramListService.SetProgram(RadioProgram);  // make sure this does not run in UI thread !!
            //  ProgramListService.SetTrack(_track);
        });

        Debug.WriteLine("| < GOTO DETAIL PAGE >");

        await Shell.Current.GoToAsync(nameof(DetailsPage), true, new Dictionary<string, object>
        {
        //  ["AudioPlaybackService"] = this.AudioPlaybackService,
        //  ["RadioProgram"] = RadioProgram //,
        //  ["TrackObject"] = _track
        });
    }

    [ObservableProperty]
    bool isRefreshing;


    [RelayCommand]
    async Task RefreshProgramsAsync()
    {
        if (IsBusy)
            return;

        return; // it's just a pain.  stip it.

    //  this.IsRefreshing = true;  <<< set by caller

        string curr_sel = this.SelectedButton;

        ProgramListService.ClearList();

        switch (curr_sel) {
            case "RADIO":
                await GetRadioProgramsAsync();
                break;
            case "CD":
                await GetCdsAsync();
                break;
            case "FAVORITES":
                await GetFavoritesAsync();
                break;
            case "RANDOM":
                await GetRandomAsync();
                break;
        }
        this.IsRefreshing = false;
    }


    [RelayCommand]
    async Task GetRadioProgramsAsync()
    {
        if (IsBusy)
            return;

        this.SelectedButton = "RADIO";

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("Load All Radio Programs",
                    $"No connectivity!  Please check internet and try again.", "OK");
                return;
            }

            this.PageTitle = "Latincta Android Radio";

            bool have_radio = false;
            have_radio = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.RADIO);

            if (have_radio) {
                Debug.WriteLine("| Skipping loading Radio-Programs as they are already loaded.");
                //await Shell.Current.DisplayAlert("Load All Radio Programs",
                //    $"Radio-Programs appear to be already loaded - skipping load.", "OK");
                return;
            }

            IsBusy = true;
            var _RadioPrograms = await RadioProgramsService.GetRadioPrograms();

            //if (_RadioPrograms.Count != 0)
            //    this.RadioPrograms.Clear();
            ProgramListService.ClearList();

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
            Debug.WriteLine($"| Unable to get RadioPrograms: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }

    }


    [RelayCommand]
    async Task GetCdsAsync()
    {
        if (IsBusy)
            return;

        this.SelectedButton = "CD";

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet) {
                await Shell.Current.DisplayAlert("Load All CD's",
                    $"No connectivity!  Please check internet and try again.", "OK");
                return;
            }

            this.PageTitle = "Latincta Android CD's";

            bool have_cds = false;
            have_cds = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.CD);

            if (have_cds) {
                Debug.WriteLine("| Skipping loading CD's as they are already loaded.");
                //await Shell.Current.DisplayAlert("Load All Radio Programs",
                //    $"Radio-Programs appear to be already loaded - skipping load.", "OK");
                return;
            }

            IsBusy = true;
            var _RadioPrograms = await CdService.GetCds();

            //if (_RadioPrograms.Count != 0)
            //    this.RadioPrograms.Clear();
            ProgramListService.ClearList();

            //foreach (var RadioProgram in _RadioPrograms)
            //    this.RadioPrograms.Add(RadioProgram);
            ProgramListService.AddToList(_RadioPrograms);

            //if (CurrentRadioProgram != null) {
            //    TrackObject _track = await AllLatincitaService.get_track(CurrentRadioProgram);

            //    ProgramListService.SetTrack(_track);
            //} else {
            //    ProgramListService.SetTrack(null);
            //}

        } catch (Exception ex) {
            Debug.WriteLine($"| Unable to get CD's: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        } finally {
            IsBusy = false;
            IsRefreshing = false;
        }

    }




    [RelayCommand]
    async Task GetFavoritesAsync()
    {
        if (IsBusy)
            return;

        this.SelectedButton = "FAVORITES";

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet) {
                await Shell.Current.DisplayAlert("Load All Favorites",
                    $"No connectivity!  Please check internet and try again.", "OK");
                return;
            }

            this.PageTitle = "Latincta Android Favorites";

            bool have_favorites = false;
            have_favorites = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.FAVORITE);

            if (have_favorites) {
                Debug.WriteLine("| Skipping loading Favorites as they are already loaded.");
                //await Shell.Current.DisplayAlert("Load All Radio Programs",
                //    $"Radio-Programs appear to be already loaded - skipping load.", "OK");
                return;
            }

            IsBusy = true;
            var _RadioPrograms = await FavoriteService.GetFavorites();

            //if (_RadioPrograms.Count != 0)
            //    this.RadioPrograms.Clear();
            ProgramListService.ClearList();

            //foreach (var RadioProgram in _RadioPrograms)
            //    this.RadioPrograms.Add(RadioProgram);
            ProgramListService.AddToList(_RadioPrograms);

            //if (CurrentRadioProgram != null) {
            //    TrackObject _track = await AllLatincitaService.get_track(CurrentRadioProgram);

            //    ProgramListService.SetTrack(_track);
            //} else {
            //    ProgramListService.SetTrack(null);
            //}

        } catch (Exception ex) {
            Debug.WriteLine($"| Unable to get Favorites: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        } finally {
            IsBusy = false;
            IsRefreshing = false;
        }

    }


    [RelayCommand]
    async Task GetRandomAsync()
    {
        if (IsBusy)
            return;

        this.SelectedButton = "RANDOM";

        try {
            if (connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert("No connectivity!",
                    $"Please check internet and try again.", "OK");
                return;
            }

            ///////////////////////////// CODE IS DUPLICATED IN RadioProgramDetailsViewModel

            bool have_random = false;
            have_random = ProgramListService.RadioPrograms.Any(x => x.Type == RadioProgramType.RANDOM);

            this.PageTitle = "Latincta Android Music";

            IsBusy = true;
            var _Random = await RandomService.GetRandom();

            if (_Random == null) {
                return;
            }

            if (!have_random) {
                ProgramListService.ClearList();
            }

            ProgramListService.AddRandom(_Random);

        } catch (Exception ex)
        {
            Debug.WriteLine($"| Unable to get Random Track: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
