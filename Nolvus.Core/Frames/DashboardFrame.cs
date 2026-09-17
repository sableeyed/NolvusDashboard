using Avalonia.Controls;
using Avalonia.LogicalTree;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Core.Frames
{
    public partial class DashboardFrame : UserControl, IDashboardFrame
    {
        public FrameParameters Parameters { get; private set; }
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

        // OnLoadedAsync is raised from an event, so there is nobody to hand an exception back to.
        // Discarding the task left a frame that threw while loading on screen half built, with
        // nothing anywhere to say so - log it at least.
        private async void OnFrameLoadedSync(object sender, EventArgs e)
        {
            try
            {
                await OnLoadedAsync();
            }
            catch (Exception ex)
            {
                // Null conditional : this runs in an async void, so an exception thrown while
                // reporting an exception would go unhandled, and the logger is resolved from a
                // service registry that hands back null when nothing is registered.
                ServiceSingleton.Logger?.Log($"[FRAME] {GetType().Name} failed while loading : {ex}");
            }
        }

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