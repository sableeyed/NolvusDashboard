using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Nolvus.Core.Interfaces;

namespace Nolvus.Core.Frames
{
    public partial class DashboardFrame : IDashboardFrame
    {
        protected FrameParameters Parameters;
        IDashboard DashBoardInstance;

        public DashboardFrame(){}

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

        private void OnFrameLoadedSync(object sender, EventArgs e)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(async () => await OnLoadedAsync());
            }
            else
            {
                _ = OnLoadedAsync();
            }
        }

        private void OnFrameLoaded(object Sender, EventArgs e)
        {
            OnLoaded();
        }

        protected virtual Task OnLoadAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnLoad() { }

        protected virtual Task OnLoadedAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnLoaded() { }

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

        public static async Task<T> CreateAsync<T>(object[] Args) where T : DashboardFrame
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var frame = Activator.CreateInstance(typeof(T)) as T;
                return await frame.InitializeAsync<T>();
            });
        }

        public static async Task<T> Create<T>(object[] Args) where T : DashboardFrame
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var frame = (T)Activator.CreateInstance(typeof(T), Args);
                return frame.Initialize<T>();
            }

            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                var frame = (T)Activator.CreateInstance(typeof(T), Args);
                return frame.Initialize<T>();
            }).Result;
        }

        public void Close()
        {
            DashBoardInstance.OnFrameLoaded -= OnFrameLoaded;
            DashBoardInstance.OnFrameLoadedAsync -= OnFrameLoadedSync;
        }
    }
}