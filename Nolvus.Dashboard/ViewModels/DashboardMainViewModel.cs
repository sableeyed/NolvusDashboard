using Nolvus.Dashboard.ViewModels;
using Nolvus.Dashboard.Frames;

namespace Nolvus.Dashboard.ViewModels;

public class DashboardMainViewModel : ViewModelBase
{
    private object _currentView;
    public object CurrentView
    {
        get => _currentView;
        set { _currentView = value; Notify(); }
    }

    public DashboardMainViewModel()
    {
        // For now, always start in "fresh install" state
        CurrentView = new StartFrame();
    }
}
