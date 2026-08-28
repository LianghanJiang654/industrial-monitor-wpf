using System.Windows;

namespace FactorialApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            IVisionService visionService =
                new TcpVisionService(
                    "192.168.2.130",
                    5001
                );

            DataContext = new MainViewModel(visionService);
        }
    }
}