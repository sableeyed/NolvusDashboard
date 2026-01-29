using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Core;
using Nolvus.Components.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Nolvus.Package.Mods;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp;
using Avalonia.Threading;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Instance;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class SelectInstanceFrame : DashboardFrame
    {

        public SelectInstanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnContinue.Click += BtnContinue_Click;
            BtnCancel.Click += BtnCancel_Click;
            NolvusListBox.SelectionChanged += NolvusListBox_SelectionChanged;
        }

        private int InstanceIndex(IEnumerable<INolvusVersionDTO> Versions)
        {
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            if (Instance != null)
            {
                var Index = Versions.ToList().FindIndex(x => x.Name == Instance.Name);

                return Index == -1 ? Versions.ToList().Count - 1 : Index;
            }
            return 0;
        }

        private async Task LoadAvailableLists(IEnumerable<INolvusVersionDTO> Lists)
        {
            var items = await Task.Run(() =>
            {
                return Lists.Select(dto =>
                {
                    try
                    {
                        var img = ServiceSingleton.Lib.GetImageFromUrl(dto.Image);
                        dto.ImageObject = img;

                        if (img != null)
                        {
                            //OOP
                            //img.Mutate(ctx => ctx.Resize(new SixLabors.ImageSharp.Size(220, 180)));
                            using var ms = new MemoryStream();
                            img.Save(ms, new PngEncoder());
                            ms.Position = 0;

                            dto.AvaloniaImage = new Bitmap(ms);
                        }
                        else
                        {
                            dto.AvaloniaImage = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        dto.AvaloniaImage = null;
                    }
                    return dto;
                }).ToList();
            });
            NolvusListBox.ItemsSource = items;
            NolvusListBox.IsVisible = true;
        }

        private void SwitchInstance(INolvusVersionDTO NolvusInstance)
        {
            if (NolvusInstance == null)
                return; //They have not installed yet?
            if (ServiceSingleton.Instances.WorkingInstance == null || ServiceSingleton.Instances.WorkingInstance.Name != NolvusInstance.Name)
            {
                ServiceSingleton.Instances.WorkingInstance = new NolvusInstance(NolvusInstance);                
            }
        }

        private void LoadLanguages()
        {
            var languages = new List<LgCode>
            {
                new LgCode { Code = "EN", Name = "English" },
                new LgCode { Code = "FR", Name = "French" },
                new LgCode { Code = "IT", Name = "Italian" },
                new LgCode { Code = "DE", Name = "German" },
                new LgCode { Code = "ES", Name = "Spanish" },
                new LgCode { Code = "RU", Name = "Russian" },
                new LgCode { Code = "PL", Name = "Polish" },
            };

            DrpDwnLg.ItemsSource = languages;
            var inst = ServiceSingleton.Instances.WorkingInstance;

            if (inst != null)
                DrpDwnLg.SelectedItem = languages.FirstOrDefault(x => x.Code == inst.Settings.LgCode) ?? languages[0];
            else
                DrpDwnLg.SelectedIndex = 0;
        }


        protected override async Task OnLoadedAsync()
        {
            try
            {
                ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Instance Auto Installer]");
                ServiceSingleton.Dashboard.Info("Instance Prerequisites");

                BtnCancel.IsVisible = !Parameters.IsEmpty && Parameters["Cancel"] != null;

                LoadLanguages();

                await LoadAvailableLists(await ApiManager.Service.Installer.GetNolvusVersions());

                SwitchInstance(NolvusListBox.SelectedItem as INolvusVersionDTO);

                //NolvusListBox.SelectionChanged += NolvusListBox_SelectedIndexChanged;                
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance selection loading", ex.Message, ex.StackTrace);
            }
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            var InstanceToInstall = NolvusListBox.SelectedItem as INolvusVersionDTO;
            if (InstanceToInstall == null || owner == null)
                return;

            if (InstanceToInstall.Maintenance)
            {
                await NolvusMessageBox.Show(owner, "Maintenance", string.Format("The nolvus instance {0} is under maintenance for the following reason : \n\n{1}\n\n Unable to install.", InstanceToInstall.Name, InstanceToInstall.MaintenanceReason), MessageBoxType.Error);
                return;
            }

            if (InstanceToInstall.IsBeta)
            {
                bool? beta = await NolvusMessageBox.ShowConfirmation(owner, "Disclaimer", string.Format("{0} is in BETA state.\n\n\nDon't Install it if :\n\n- You are expecting the full polished version.\n\n- You want to do a full playthrough.\n\n\nInstall it only if :\n\n- You want to help us reporting bugs.\n\n- You want to give us some feedbacks.\n\n\nDo you want to continue?", InstanceToInstall.Name), 390, 470);
                if (beta != true)
                    return;
            }

            if (ServiceSingleton.Instances.InstanceExists(InstanceToInstall.Name))
            {
                bool? proceed = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", string.Format("The nolvus instance {0} already exists, Do you wan to proceed anyway?", InstanceToInstall.Name));
                if (proceed != true)
                    return;
                
                var TagSelection = NolvusInstanceTag.EnterTag("Instance Tag");

                bool? ok = await TagSelection.ShowDialog<bool?>(owner);
                if (ok == true)
                {
                    ServiceSingleton.Instances.WorkingInstance.Tag = TagSelection.InstanceTag;
                    Continue();
                    return;
                }
            }

            Continue();
        }

        private void Continue()
        {
            INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;
            if (WorkingInstance != null)
            {
                if (DrpDwnLg.SelectedItem is LgCode selectedLg)
                {
                    WorkingInstance.Settings.LgCode = selectedLg.Code;
                    WorkingInstance.Settings.LgName = selectedLg.Name;
                }
            }
            ServiceSingleton.Dashboard.LoadFrame<PathFrame>();
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void NolvusListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SwitchInstance(NolvusListBox.SelectedItem as INolvusVersionDTO);            
        }    

    }
}