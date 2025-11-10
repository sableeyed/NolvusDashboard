using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Frames
{
    public partial class StartFrame : DashboardFrame
    {
        public StartFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();
        }

        protected override void OnLoad()
        {
            // Called BEFORE the frame becomes visible
            // Good for quick UI setup work
        }

        protected override Task OnLoadAsync()
        {
            // Called BEFORE the frame becomes visible (async)
            // Good for reading files, verifying install paths, etc.
            return Task.CompletedTask;
        }

        protected override void OnLoaded()
        {
            // Called AFTER the frame is shown to the user
        }

        protected override Task OnLoadedAsync()
        {
            // Called AFTER frame is visible (async)
            // This is where original installer logic will go later.
            return Task.CompletedTask;
        }
    }
}
