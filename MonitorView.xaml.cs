using System.Windows.Controls;
using System.Windows.Input;

namespace FactorialApp
{
    public partial class MonitorView : UserControl
    {
        public MonitorView()
        {
            InitializeComponent();
        }

        private MainViewModel? VM => DataContext as MainViewModel;

        private void JogX_PlusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("X,1");
        }

        private void JogX_MinusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("X,-1");
        }

        private void JogX_Up(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStopCommand.Execute("X");
        }

        private void JogY_PlusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("Y,1");
        }

        private void JogY_MinusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("Y,-1");
        }

        private void JogY_Up(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStopCommand.Execute("Y");
        }

        private void JogZ_PlusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("Z,1");
        }

        private void JogZ_MinusDown(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStartCommand.Execute("Z,-1");
        }

        private void JogZ_Up(object sender, MouseButtonEventArgs e)
        {
            VM?.JogStopCommand.Execute("Z");
        }
    }
}