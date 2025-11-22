using Avalonia.Controls;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Controls
{
    public partial class InstancesPanel : UserControl
    {
        public InstancesPanel()
        {
            InitializeComponent();
        }

        public IDashboardFrame ContainerFrame { get; set; }

        public void LoadInstances(List<INolvusInstance> instances)
        {
            InstancesHost.Children.Clear();

            foreach (INolvusInstance instance in instances)
            {
                var panel = new InstancePanel(this);

                panel.LoadInstance(instance);

                InstancesHost.Children.Add(panel);
            }
        }
    }
}
