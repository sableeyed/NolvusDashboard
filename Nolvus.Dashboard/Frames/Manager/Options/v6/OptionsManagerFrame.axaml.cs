using Avalonia.Controls;
using Avalonia.Input;
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
        private INolvusOptionDTO SelectedOption
        {
            get
            {
                return (DrpDwnLstOption.SelectedItem as INolvusOptionDTO)!;
            }
        }

        private INolvusOption CurrentOption
        {
            get
            {
                return ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption(SelectedOption.Name);
            }
        }

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

        private void UnlinkEvents()
        {
            DrpDwnLstNewValue.SelectionChanged -= DrpDwnLstNewValue_SelectedIndexChanged;
            TglBtnNewValue.IsCheckedChanged -= TglBtnNewValue_ToggleStateChanged;
        }

        private void LinkEvents()
        {
            DrpDwnLstNewValue.SelectionChanged += DrpDwnLstNewValue_SelectedIndexChanged;
            TglBtnNewValue.IsCheckedChanged += TglBtnNewValue_ToggleStateChanged;
        }

        private void LockMessage(bool Lock)
        {
            DrpDwnLstCategory.IsEnabled = !Lock;
            DrpDwnLstOption.IsEnabled = !Lock;

            PnlLock.IsVisible = Lock;

            BtnChange.IsEnabled = Lock;
        }

        private string GetNewValue()
        {
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

        private void InitNewValue()
        {
            if (SelectedOption.DataType == "bool")
            {
                TglBtnNewValue.IsChecked = CurrentOption.Value == "TRUE";
            }
            else if (SelectedOption.DataType == "list")
            {
                DrpDwnLstNewValue.SelectedIndex = ValueIndex((DrpDwnLstNewValue.ItemsSource as IEnumerable<INolvusOptionChoiceDTO>)!, CurrentOption.Name);
            }
        }

        private async Task LoadOptionChoices()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            var Values = await ApiManager.Service.Installer.GetOptionChoices(SelectedOption.Name, Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstNewValue.ItemsSource = null;
            DrpDwnLstNewValue.ItemsSource = Values;
        }

        private string GetFormatedValue(bool Current)
        {
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
            var Instance = ServiceSingleton.Instances.WorkingInstance;
            var Categories = await ApiManager.Service.Installer.GetOptionCategories(Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstCategory.ItemsSource = Categories;

            DrpDwnLstCategory.SelectedIndex = 0;
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

            TglBtnNewValue.IsVisible = false;
            DrpDwnLstNewValue.IsVisible = false;

            UnlinkEvents();

            LblCurrentValue.Text = CurrentOption.FormattedValue;

            if (SelectedOption.DataType == "bool")
            {
                TglBtnNewValue.IsVisible = true;
            }
            else
            {
                DrpDwnLstNewValue.IsVisible = true;
                await LoadOptionChoices();
            }

            InitNewValue();

            LinkEvents();
        }

        private void TglBtnNewValue_ToggleStateChanged(object? sender, RoutedEventArgs e)
        {
            if (CurrentOption.Value != GetNewValue())
            {
                LockMessage(true);
            }
            else
            {
                LockMessage(false);
            }
        }

        private void DrpDwnLstNewValue_SelectedIndexChanged(object? sender, SelectionChangedEventArgs e)
        {
            // Swapping the choice list raises this with nothing selected in Avalonia.
            if (DrpDwnLstNewValue.SelectedItem == null)
                return;

            if (CurrentOption.Value != GetNewValue())
            {
                LockMessage(true);
            }
            else
            {
                LockMessage(false);
            }
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
                        ServiceSingleton.Logger.Log("New game required, about to backup game save files");
                        await Instance.BackupGameSaveFiles();
                    }

                    ServiceSingleton.Instances.PrepareInstanceForOption();

                    await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>(new FrameParameters(new FrameParameter() { Key = "ModsToInstall", Value = ModsToUpdate.Cast<IInstallableElement>().ToList() }));
                }
                else
                {
                    var owner = TopLevel.GetTopLevel(this) as Window;
                    NolvusMessageBox.Show(owner, "Info", string.Format("{0} doesn't have any mod associated with it for {1}!\n\nThis Option Could be linked to an older or newer Nolvus version.\n\nCheck for Nolvus list update.", CurrentOption.Name, Instance.FullName), MessageBoxType.Error);
                }
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during option change initialization", ex.Message, ex.StackTrace);
            }
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private async void BtnChange_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            if (!ModOrganizer.IsRunning)
            {
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
                            if (!SelectedOption.NewGameRequired || (SelectedOption.NewGameRequired && await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", string.Format("Changing {0} will require a new game!\n\nTo prevent issues, your save files will be removed from the saves directory and will be backed up!\n\nAre you sure you want to continue?", CurrentOption.FriendlyName)) == true))
                            {
                                DisableButtons();
                                ServiceSingleton.Dashboard.DisableSettings();

                                DoChangeOption(CurrentOption, GetNewValue(), SelectedOption.NewGameRequired);
                            }
                        }
                    }
                    finally
                    {
                        EnableButtons();
                        ServiceSingleton.Dashboard.EnableSettings();
                    }
                }
            }
            else
            {
                NolvusMessageBox.Show(owner, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it first.", MessageBoxType.Error);
            }
        }

        private void LnkLblReInit_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            LockMessage(false);
            UnlinkEvents();
            InitNewValue();
            LinkEvents();
        }
    }
}
