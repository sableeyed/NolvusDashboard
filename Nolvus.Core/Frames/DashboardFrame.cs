using Avalonia.Controls;
using Avalonia.LogicalTree;
using Nolvus.Core.Interfaces;

namespace Nolvus.Core.Frames
{
    public partial class DashboardFrame : UserControl, IDashboardFrame
    {
        protected FrameParameters Parameters;
        IDashboard DashBoardInstance;

        public DashboardFrame(IDashboard Dashboard, FrameParameters Params)
        {
            DashBoardInstance = Dashboard;
            DashBoardInstance.OnFrameLoaded += OnFrameLoaded;
            DashBoardInstance.OnFrameLoadedAsync += OnFrameLoadedSync;

            Parameters = Params;

            if (Parameters == null)
            {
                Parameters = new FrameParameters();
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            foreach (var btn in this.GetLogicalDescendants().OfType<Button>())
                btn.IsEnabled = enabled;
        }

        public void EnableButtons()  => SetButtonsEnabled(true);
        public void DisableButtons() => SetButtonsEnabled(false);

        protected virtual void OnLoad() { }
        protected virtual Task OnLoadAsync() => Task.CompletedTask;
        protected virtual void OnLoaded() { }
        protected virtual Task OnLoadedAsync() => Task.CompletedTask;


        public static Task<T> CreateAsync<T>(object[] args) where T : DashboardFrame
        {
            var instance = Activator.CreateInstance(typeof(T), args) as T;
            if (instance == null)
                throw new InvalidOperationException($"Unable to create instance of frame {typeof(T).Name}");

            return instance.InitializeAsync<T>();
        }

        public static T Create<T>(object[] args) where T : DashboardFrame
        {
            var instance = Activator.CreateInstance(typeof(T), args) as T;
            if (instance == null)
                throw new InvalidOperationException($"Unable to create instance of frame {typeof(T).Name}");

            return instance.Initialize<T>();
        }

        private void OnFrameLoaded(object sender, EventArgs e) => OnLoaded();

        private void OnFrameLoadedSync(object sender, EventArgs e) => _ = OnLoadedAsync();

        public virtual T Initialize<T>() where T : DashboardFrame
        {
            OnLoad();
            return (T)this;
        }

        public virtual async Task<T> InitializeAsync<T>() where T : DashboardFrame
        {
            await OnLoadAsync();
            return (T)this;
        }

        public void Close()
        {
            DashBoardInstance.OnFrameLoaded -= OnFrameLoaded;
            DashBoardInstance.OnFrameLoadedAsync -= OnFrameLoadedSync;
        }
    }
}