//completely untested

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Nolvus.Core.Interfaces;

namespace Nolvus.Core.Frames
{
    public partial class DashboardFrame : UserControl, IDashboardFrame
    {
        protected FrameParameters Parameters;
        protected IDashboard DashBoardInstance;

        public DashboardFrame()
        {
            InitializeComponent();
        }

        public DashboardFrame(IDashboard dashboard, FrameParameters parameters)
        {
            InitializeComponent();

            DashBoardInstance = dashboard;
            DashBoardInstance.OnFrameLoaded += OnFrameLoaded;
            DashBoardInstance.OnFrameLoadedAsync += OnFrameLoadedSync;

            Parameters = parameters ?? new FrameParameters();
        }

        private void OnFrameLoadedSync(object sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(async () => await OnLoadedAsync());
        }

        private void OnFrameLoaded(object sender, EventArgs e)
        {
            OnLoaded();
        }

        protected virtual Task OnLoadAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnLoad()
        {
        }

        protected virtual Task OnLoadedAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnLoaded()
        {
        }

        protected virtual async Task<T> InitializeAsync<T>() where T : DashboardFrame
        {
            await OnLoadAsync();
            return (T)this;
        }

        protected virtual T Initialize<T>() where T : DashboardFrame
        {
            OnLoad();
            return (T)this;
        }

        public static Task<T> CreateAsync<T>() where T : DashboardFrame, new()
        {
            var instance = new T();
            return instance.InitializeAsync<T>();
        }

        public static Task<T> CreateAsync<T>(object[] args) where T : DashboardFrame
        {
            var instance = Activator.CreateInstance(typeof(T), args) as T;
            if (instance == null)
                throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");

            return instance.InitializeAsync<T>();
        }

        public static T Create<T>(object[] args) where T : DashboardFrame
        {
            var instance = Activator.CreateInstance(typeof(T), args) as T;
            if (instance == null)
                throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}");

            return instance.Initialize<T>();
        }

        public void Close()
        {
            Dispatcher.UIThread.Post(() =>
            {
                DashBoardInstance.OnFrameLoaded -= OnFrameLoaded;
                DashBoardInstance.OnFrameLoadedAsync -= OnFrameLoadedSync;

                this.DataContext = null;
                this.Content = null;

                (this as IDisposable)?.Dispose();
            });
        }
    }
}
