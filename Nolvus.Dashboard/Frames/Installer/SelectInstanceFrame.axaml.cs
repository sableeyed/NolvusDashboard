using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Core;
using Nolvus.Components.Controls;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class SelectInstanceFrame : DashboardFrame
    {

        public SelectInstanceFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();


            //UI Components
            //BtnContinue.Click += BtnCancel_Click;
            //BtnCancel.Click += BtnCancel_Click;
            NolvusListBox.SelectionChanged += NolvusListBox_SelectedIndexChanged;
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

        private int LgIndex(List<LgCode> Lgs)
        {
            INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

            if (Instance != null)
            {
                var Index = Lgs.FindIndex(x => x.Code == Instance.Settings.LgCode);

                return Index == -1 ? 0 : Index;
            }

            return 0;            
        }

        private void SetDataSource(IEnumerable<INolvusVersionDTO> Source)
        {
            //NolvusListBox.Items = Source;
            NolvusListBox.SelectedIndex = InstanceIndex(Source);

            //PicLoading.IsVisible = false;
            NolvusListBox.IsVisible = true;
        }

        private async Task LoadAvailableLists(IEnumerable<INolvusVersionDTO> Lists)
        {
            var materialized = Lists.Select(x =>
            {
                x.ImageObject = ServiceSingleton.Lib.SetImageOpacity(ServiceSingleton.Lib.GetImageFromUrl(x.Image), 0.50F);
                return x;
            }).ToList();

            SetDataSource(materialized);
            await Task.CompletedTask;
        }

        private void SwitchInstance(INolvusVersionDTO NolvusInstance)
        {
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

                //NolvusListBox.SelectedIndexChanged += NolvusListBox_SelectedIndexChanged;                
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance selection loading", ex.Message, ex.StackTrace);
            }
        }

        private void BtnContinue_Click(object sender, EventArgs e)
        {
            // INolvusVersionDTO InstanceToInstall = NolvusListBox.SelectedItem as INolvusVersionDTO;

            // if (InstanceToInstall.Maintenance)
            // {
            //     NolvusMessageBox.ShowMessage("Maintenance", "The nolvus instance " + InstanceToInstall.Name + " is under maintenance. Unable to install.", MessageBoxType.Error);
            // }
            // else
            // {
            //     if (ServiceSingleton.Instances.InstanceExists(InstanceToInstall.Name))
            //     {
            //         NolvusMessageBox.ShowMessage("Invalid Instance", "The nolvus instance " + InstanceToInstall.Name + " is already installed!", MessageBoxType.Error);
            //     }
            //     else
            //     {
            //         if (!InstanceToInstall.IsBeta || NolvusMessageBox.ShowConfirmation("Disclaimer", string.Format("{0} is in BETA state.\n\n\nDon't Install it if :\n\n- You are expecting the full polished version.\n\n- You want to do a full playthrough.\n\n\nInstall it only if :\n\n- You want to help us reporting bugs.\n\n- You want to give us some feedbacks.\n\n\nDo you want to continue?", InstanceToInstall.Name), 390, 470) == DialogResult.Yes)
            //         {
            //             INolvusInstance WorkingInstance = ServiceSingleton.Instances.WorkingInstance;

            //             WorkingInstance.Settings.LgCode = (DrpDwnLg.SelectedItem as LgCode).Code;
            //             WorkingInstance.Settings.LgName = (DrpDwnLg.SelectedItem as LgCode).Name;

            //             ServiceSingleton.Dashboard.LoadFrame<PathFrame>();
            //         }
            //     }
            // }
        }  

        // private void BtnCancel_Click(object sender, EventArgs e)
        // {
        //     ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        // }

        private void NolvusListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SwitchInstance(NolvusListBox.SelectedItem as INolvusVersionDTO);            
        }          
    }
}