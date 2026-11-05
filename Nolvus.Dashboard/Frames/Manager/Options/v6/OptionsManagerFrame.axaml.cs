using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;

namespace Nolvus.Dashboard.Frames.Manager.Options.v6
{
    public partial class OptionsManagerFrame : DashboardFrame
    {
        public OptionsManagerFrame(IDashboard Dashboard, FrameParameters Params) : base(Dashboard, Params)
        {
            InitializeComponent();

            TglBtnNewValue.IsVisible = false;
            DrpDwnLstNewValue.IsVisible = false;

            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Options Manager]");
            ServiceSingleton.Dashboard.Info("Select an option to change");
        }

        private int ValueIndex(IEnumerable<INolvusOptionChoiceDTO> Values, string Option)
        {
            var Index = Values.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue(Option));

            return Index == -1 ? 0 : Index;
        }

        private string GetNewValue()
        {
            var SelectedOption = (DrpDwnLstOption.SelectedItem as INolvusOptionDTO);

            if (SelectedOption.DataType == "bool")
            {
                return TglBtnNewValue.IsChecked == true ? "TRUE" : "FALSE";
            }
            else if (SelectedOption.DataType == "list")
            {
                return (DrpDwnLstNewValue.SelectedItem as INolvusOptionChoiceDTO).Value;
            }

            return string.Empty;
        }

        private string GetFormatedValue(bool Current)
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var SelectedOption = (DrpDwnLstOption.SelectedItem as INolvusOptionDTO);
            var CurrentOption = Instance.OptionsManager.GetOption(SelectedOption.Name);

            if (SelectedOption.DataType == "bool")
            {
                if (Current)
                {
                    return CurrentOption.Value == "TRUE" ? "Yes" : "No";
                }
                else
                {
                    return TglBtnNewValue.IsChecked == true ? "Yes" : "No";
                }
            }
            else if (SelectedOption.DataType == "list")
            {
                if (Current)
                {
                    return CurrentOption.Value;
                }
                else
                {
                    return (DrpDwnLstNewValue.SelectedItem as INolvusOptionChoiceDTO).Value;
                }
            }

            return string.Empty;
        }

        protected override async Task OnLoadedAsync()
        {
            var Categories = await ApiManager.Service.Installer.GetOptionCategories();

            DrpDwnLstCategory.ItemsSource = Categories;

            DrpDwnLstCategory.SelectedIndex = 0;
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private async void DrpDwnLstCategory_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;
            var Options = await ApiManager.Service.Installer.GetOptionsByCategory(Instance.SettingsManager.Variant, (DrpDwnLstCategory.SelectedItem as INolvusOptionCategoryDTO).Name, Instance.Id);

            DrpDwnLstOption.ItemsSource = null;

            DrpDwnLstOption.ItemsSource = Options;

            DrpDwnLstOption.SelectedIndex = 0;
        }

        private async void DrpDwnLstOption_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Swapping the option list raises this with nothing selected in Avalonia.
            if (DrpDwnLstOption.SelectedItem == null)
                return;

            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var SelectedOption = (DrpDwnLstOption.SelectedItem as INolvusOptionDTO);
            var CurrentOption = Instance.OptionsManager.GetOption(SelectedOption.Name);

            string CurrentValue = string.Empty;

            TglBtnNewValue.IsVisible = false;
            DrpDwnLstNewValue.IsVisible = false;

            if (SelectedOption.DataType == "bool")
            {
                TglBtnNewValue.IsVisible = true;

                TglBtnNewValue.IsChecked = false;

                if (CurrentOption.Value == "TRUE")
                {
                    TglBtnNewValue.IsChecked = true;
                }

                CurrentValue = CurrentOption.Value == "TRUE" ? "Yes" : "No";
            }
            else if (SelectedOption.DataType == "list")
            {
                DrpDwnLstNewValue.IsVisible = true;

                var Values = await ApiManager.Service.Installer.GetOptionChoices(SelectedOption.Name, Instance.SettingsManager.Variant, Instance.Id);

                DrpDwnLstNewValue.ItemsSource = null;
                DrpDwnLstNewValue.ItemsSource = Values;

                DrpDwnLstNewValue.SelectedIndex = ValueIndex(Values, SelectedOption.Name);

                CurrentValue = CurrentOption.Value;
            }
            else
            {
                CurrentValue = CurrentOption.Value;
            }

            LblCurrentValue.Text = CurrentValue;
        }

        private async void DoChangeOption(INolvusOption CurrentOption, string NewValue, bool BackupSaveFiles)
        {
            try
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                var ModsToUpdate = await ServiceSingleton.OptionManager.PrepareModsToUpdate(CurrentOption.Name, CurrentOption.Value, GetNewValue());

                if (ModsToUpdate.Count > 0)
                {
                    Instance.StatusManager.AddField("Option", CurrentOption.Name);
                    Instance.StatusManager.AddField("OldOption", CurrentOption.Value);
                    Instance.StatusManager.AddField("NewOption", NewValue);

                    Instance.OptionsManager.SetOptionValue(CurrentOption.Name, NewValue);

                    if (BackupSaveFiles)
                    {
                        await Instance.BackupGameSaveFiles();
                    }

                    ServiceSingleton.Instances.PrepareInstanceForOption();

                    await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>(new FrameParameters(new FrameParameter() { Key = "ModsToInstall", Value = ModsToUpdate.Cast<IInstallableElement>().ToList() }));
                }
                else
                {
                    var owner = TopLevel.GetTopLevel(this) as Window;
                    NolvusMessageBox.Show(owner, "Info", string.Format("{0} doesn't have any mod associated with it for {1}!\n\nThis Option Could be linked to an older or new Nolvus version.\n\nCheck for Nolvus list update.", CurrentOption.Name, Instance.FullName), MessageBoxType.Error);
                }
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during option change initialization", ex.Message, ex.StackTrace);
            }
        }

        private async void BtnChange_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            if (!ModOrganizer.IsRunning)
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                var SelectedOption = (DrpDwnLstOption.SelectedItem as INolvusOptionDTO);
                var CurrentOption = Instance.OptionsManager.GetOption(SelectedOption.Name);

                if (GetNewValue() == CurrentOption.Value)
                {
                    NolvusMessageBox.Show(owner, "Error", string.Format("New value ({0}) for {1} didn't change!", CurrentOption.Value, CurrentOption.Name), MessageBoxType.Error);
                }
                else
                {
                    try
                    {
                        bool? Result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", string.Format("You are about to change {0} from {1} to {2}.\n\nWarning!!!\n\nIf you customized the list, all added mods will be placed at the end of the loadorder and left unchecked.\n\nAre you sure you want to continue?", CurrentOption.FriendlyName, GetFormatedValue(true), GetFormatedValue(false)));

                        if (Result == true)
                        {
                            if (!SelectedOption.NewGameRequired)
                            {
                                DisableButtons();
                                ServiceSingleton.Dashboard.DisableSettings();

                                DoChangeOption(CurrentOption, GetNewValue(), false);
                            }
                            else if (await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", string.Format("Changing {0} will require a new game!\n\nTo prevent issues, your save files will be removed from the saves directory and will be backed up!\n\nAre you sure you want to continue?", CurrentOption.FriendlyName)) == true)
                            {
                                DisableButtons();
                                ServiceSingleton.Dashboard.DisableSettings();

                                DoChangeOption(CurrentOption, GetNewValue(), true);
                            }
                        }
                    }
                    finally
                    {
                        ServiceSingleton.Dashboard.EnableSettings();
                    }
                }
            }
            else
            {
                NolvusMessageBox.Show(owner, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it first.", MessageBoxType.Error);
            }
        }
    }
}
