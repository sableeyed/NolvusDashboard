using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class OptionsFrame : DashboardFrame
    {
        public OptionsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
            TglNudity.IsCheckedChanged += OnNudityChanged;
            TglLeveling.IsCheckedChanged += OnLevelingChanged;
            TglGore.IsCheckedChanged += OnGoreChanged;
            TglController.IsCheckedChanged += OnControllerChanged;

        }

        private int AnimsIndex(List<string> Anims)
        {            
            var Index = Anims.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Options.CombatAnimation);

            return Index == -1 ? 0 : Index;                            
        }

        private int UIsIndex(List<string> UIs)
        {
            var Index = UIs.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Options.UI);

            return Index == -1 ? 0 : Index;
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            TglNudity.IsChecked = false;

            if (Instance.Options.Nudity == "TRUE")
            {
                TglNudity.IsChecked = true;
            }

            TglLeveling.IsChecked = false;

            if (Instance.Options.AlternateLeveling == "TRUE")
            {
                TglLeveling.IsChecked = true;
            }

            TglAltStart.IsChecked = false;

            if (Instance.Options.AlternateStart == "TRUE")
            {
                TglAltStart.IsChecked = true;
            }

            TglStances.IsChecked = false;

            if (Instance.Options.StancesPerksTree == "TRUE")
            {
                TglStances.IsChecked = true;
            }

            TglGore.IsChecked = false;

            if (Instance.Options.Gore == "TRUE")
            {
                TglGore.IsChecked = true;
            }

            if (Instance.Options.Controller == "TRUE")
            {
                TglController.IsChecked = true;
            }

            List<string> CombatAnims = new List<string>();

            CombatAnims.Add("Conventional");
            CombatAnims.Add("Fantasy");

            DrpCombat.ItemsSource = CombatAnims;

            DrpCombat.SelectedIndex = AnimsIndex(CombatAnims);

            List<string> UIs = new List<string>();

            UIs.Add("Untarnished UI");
            UIs.Add("Edge UI");

            DrpUI.ItemsSource = UIs;

            DrpUI.SelectedIndex = UIsIndex(UIs);

            ServiceSingleton.Dashboard.Info("Options");
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v6.PerformanceFrame>();
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? result = await NolvusMessageBox.ShowConfirmation(owner!, "Confirmation", "The options you selected can not be changed after installation. Are you sure you want to continue?");
            if (result == true)
            {
                ServiceSingleton.Dashboard.LoadFrame<v6.DifficultyFrame>();
            }
        }

        private void OnNudityChanged(object? sender, RoutedEventArgs e)
        {
            if (TglNudity.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Nudity = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Nudity = "FALSE";
            }
        }

        private void OnLevelingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglLeveling.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateLeveling = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateLeveling = "FALSE";
            }
        }

        private void OnAlternateStartChanged(object? sender, RoutedEventArgs e)
        {
            //Removed in 3.7.11
        }

        private void OnStancesChanged(object? sender, RoutedEventArgs e)
        {
            //Removed in 3.7.11
        }

        private void OnGoreChanged(object? sender, RoutedEventArgs e)
        {
            if (TglGore.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Gore = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Gore = "FALSE";
            }
        }

        private void OnAnimationsChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Options.CombatAnimation = DrpCombat.SelectedValue!.ToString()!;
        }

        private void OnUIChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Options.UI = DrpUI.SelectedValue!.ToString()!;
        }

        private void OnControllerChanged(object? sender, RoutedEventArgs e)
        {
            if (TglController.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Controller = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Controller = "FALSE";
            }
        }

    }
}