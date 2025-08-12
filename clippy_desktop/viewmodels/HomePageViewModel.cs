using Clippy.db.functions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Clippy.viewmodels
{
    public class ViewModelBase : ObservableObject
    {

    }


    public partial class HomePageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ClipItem> _clips = new();
        [ObservableProperty]
        private ObservableCollection<Device> _devices = new([App.GetCurrentDevice()]);
        [ObservableProperty]
        private User? _user = null;
        [ObservableProperty]
        private string _search = "";
        [ObservableProperty]
        private ClipItem? _bufferedClip = null;
        [ObservableProperty]
        private int _selectedDeviceIndex = 0;
        [ObservableProperty]
        private ObservableCollection<ClipItem> _clipItems = new(); 
        [ObservableProperty]
        private ObservableCollection<ClipItem> _searchClips = new();
        private SemaphoreSlim clipsLoadLocker = new SemaphoreSlim(1);
        public HomePageViewModel()
        {
            LoadClips();
        }
        public void LoadClips()
        {
            try
            {
                clipsLoadLocker.Wait();
                var result = ClipItemDBUtility.FindClipItems(queryParam: new AndQuery
                {
                    Queries = [
                    new QueryParam{
                        Key="device",
                        EQUALITY=DBEQUALITY.EQUAL,
                        Value=Devices[SelectedDeviceIndex].Id
                    },
                ],
                },
            orderKey: "created_at",
            orderDescending: false
            );
                ClipItems = new(result.Entries);
                Clips = ClipItems;
            }
            finally
            {
                clipsLoadLocker.Release();
            }

        }
        partial void OnSelectedDeviceIndexChanged(int value)
        {
            if (value != SelectedDeviceIndex)
            {
                Search = "";
                App.InitializeClipObserver(Devices[value]);
                LoadClips();
            }
        }
        void search()
        {
            if (Search.Trim().Length > 2)
            {
                SearchClips = new(ClipItems.Where(e => e.ExType == ClipType.TEXT && e.Data.ToLower().Contains(Search.ToLower().Trim())));
                Clips = SearchClips;
            }
            else
            {
                Clips = ClipItems;
            }
        }
        [RelayCommand]
        public void SearchClip()
        {
            search();
        }
        partial void OnSearchChanged(string value)
        {
            search();
        }
        [RelayCommand]
        public async Task DeleteClipItem(ClipItem? clip)
        {
            if (clip != null)
            {
                if (ClipItemDBUtility.DeleteClipItem(clip))
                {
                    ClipItems.Remove(clip);
                    try
                    {
                        if (App.firebaseClient != null && MainWindow.context.CurrentUser != null)
                        {
                            await FirebaseUtil.DeleteClip(App.firebaseClient, MainWindow.context.CurrentUser, clip.Id, clip.Device);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

            }

        }
        [RelayCommand]
        public void CopyToClipboard(ClipItem? clip)
        {
            if (clip != null)
            {
                BufferedClip = clip;
                if (clip.ExType == ClipType.IMAGE)
                {
                    var file = Directory.GetFiles(ClipboardHelper.GetImageClipsDirectory(), "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false }).FirstOrDefault(e => e.Contains(clip.Data));
                    if (file != null)
                    {
                        var bmp = System.Drawing.Bitmap.FromFile(file);
                        ClipboardHelper.SetImageToClipboard((System.Drawing.Bitmap)bmp);
                    }
                    _ = Utils.ExecuteOnNewThread(() =>
                    {
                        var bufferedId = BufferedClip.Id;
                        Thread.Sleep(10000);
                        if (bufferedId == BufferedClip?.Id)
                        {
                            BufferedClip = null;
                        }
                    });
                }
                else
                {
                    ClipboardHelper.SetClipboardText(clip.Data);
                }
            }
        }
        [RelayCommand]
        public void GoToProfile()
        {
            if (MainWindow.context.CurrentPage != Page.PROFILE)
                MainWindow.context.CurrentPage = Page.PROFILE;
        }
    }
}
