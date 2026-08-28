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
                    AppConfig.VisionServerIp,
                    AppConfig.VisionServerPort);

            DataContext = new MainViewModel(visionService);
        }
    }
}
