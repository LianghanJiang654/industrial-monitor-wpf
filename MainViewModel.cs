

   using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
namespace FactorialApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _temperature = "--";
        private string _humidity = "--";
        private string _pressure = "--";
        private string _motorStatus = "Stopped";
        private CancellationTokenSource? cts;

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
        }

        public ICommand ShowMonitorCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public Brush TemperatureColor
        {
            get
            {
                if (int.TryParse(_temperature, out int tempValue) && int.TryParse(_temperatureThreshold, out int threshold))
                {
                    return tempValue > threshold ? Brushes.Red : Brushes.White;
                }
                return Brushes.White;
            }
        }
        
        public string Temperature
        {
            get { return _temperature; }
            set { _temperature = value; OnPropertyChanged(nameof(Temperature));OnPropertyChanged(nameof(TemperatureColor)); }
        }
        public ISeries[] TemperatureSeries { get; set; }
        private ObservableCollection<double> temperatureHistory = new ObservableCollection<double>();
        public string Humidity
        {
            get { return _humidity; }
            set { _humidity = value; OnPropertyChanged(nameof(Humidity)); }
        }

        public string Pressure
        {
            get { return _pressure; }
            set { _pressure = value; OnPropertyChanged(nameof(Pressure)); }
        }
        private string _axisPosition = "0";
        private string _targetPosition = "100";

        public string AxisPosition
        {
            get { return _axisPosition; }
            set { _axisPosition = value; OnPropertyChanged(nameof(AxisPosition)); }
        }
        public ObservableCollection<string> LogEntries { get; set; } = new ObservableCollection<string>();
        public string TargetPosition
        {
            get { return _targetPosition; }
            set { _targetPosition = value; OnPropertyChanged(nameof(TargetPosition)); }
        }

        public ICommand MoveAxisCommand { get; }
        public string MotorStatus
        {
            get { return _motorStatus; }
            set { _motorStatus = value; OnPropertyChanged(nameof(MotorStatus)); }
        }
        private string _temperatureThreshold = "26";

        public string TemperatureThreshold
        {
            get { return _temperatureThreshold; }
            set { _temperatureThreshold = value; OnPropertyChanged(nameof(TemperatureThreshold)); OnPropertyChanged(nameof(TemperatureColor));}
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ToggleMotorCommand { get; }

       
        
        public MainViewModel()
        {
            StartCommand = new RelayCommand(ExecuteStart);
            StopCommand = new RelayCommand(ExecuteStop);
            ToggleMotorCommand = new RelayCommand(ExecuteToggleMotor);

            TemperatureSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = temperatureHistory,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = 3 }
                }
            };
            ShowMonitorCommand = new RelayCommand(() => CurrentView = new MonitorView { DataContext = this });
            ShowSettingsCommand = new RelayCommand(() => CurrentView = new SettingsView { DataContext = this });

            CurrentView = new MonitorView { DataContext = this };
            MoveAxisCommand = new RelayCommand(ExecuteMoveAxis);
        }
        private void ExecuteMoveAxis()
        {
            string command = "MOVE 4 " + TargetPosition;
            ReadRegister(command);
            AddLog("Axis move command sen, target:" + TargetPosition);
        }
        private async void ExecuteStart()
        {
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            while (!token.IsCancellationRequested)
            {
                string tempResult = ReadRegister("READ 0");
                Temperature = tempResult;
                if (double.TryParse(tempResult, out double tempValue))
                {
                    temperatureHistory.Add(tempValue);
                    if (temperatureHistory.Count > 20)
                    {
                        temperatureHistory.RemoveAt(0);
                    }
                }
                Humidity = ReadRegister("READ 1");
                Pressure = ReadRegister("READ 2");
                
                Pressure = ReadRegister("READ 2");
                AxisPosition = ReadRegister("READ 4");
                
                await Task.Delay(2000, token).ContinueWith(t => { });
            }
        }

        private void ExecuteStop()
        {
            cts?.Cancel();
        }
        
        private void ExecuteToggleMotor()
        {
            string command = MotorStatus == "Stopped" ? "WRITE 3 1" : "WRITE 3 0";
            string result = ReadRegister(command);
            MotorStatus = MotorStatus == "Stopped" ? "Running" : "Stopped";
            AddLog("Motor toggled to " + MotorStatus);
        }

        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogEntries.Insert(0, $"[{timestamp}] {message}");
            if (LogEntries.Count > 50)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
        }
        
        private string ReadRegister(string command)
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    TcpClient client = new TcpClient();
                    client.Connect("192.168.2.130", 5001);

                    NetworkStream stream = client.GetStream();
                    byte[] messageBytes = Encoding.ASCII.GetBytes(command);
                    stream.Write(messageBytes, 0, messageBytes.Length);

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    client.Close();
                    return response;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        return "Error: " + ex.Message;
                    }
                    Thread.Sleep(500);
                }
            }

            return "Error: Max retries exceeded";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}