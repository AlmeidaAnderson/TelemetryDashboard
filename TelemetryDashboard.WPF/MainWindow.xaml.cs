using System.Windows;
using TelemetryDashboard.ViewModels;

namespace TelemetryDashboard
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Clean up resources
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Dispose();
            }

            base.OnClosing(e);
        }
    }
}
