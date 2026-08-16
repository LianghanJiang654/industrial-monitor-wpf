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
using Microsoft.Data.Sqlite;
namespace FactorialApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _temperature = "--";
        private string _humidity = "--";
        private string _pressure = "--";
        private string _motorStatus = "Stopped";
        private CancellationTokenSource? cts;
        private string dbPath = "Data Source=monitor.db";
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(nameof(CurrentView)); }
        }
        public ISeries[] HumiditySeries { get; set; }
        public ISeries[] PressureSeries { get; set; }
        private ObservableCollection<double> humidityHistory = new ObservableCollection<double>();
        private ObservableCollection<double> pressureHistory = new ObservableCollection<double>();
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
            InitializeDatabase();
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
            HumiditySeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = humidityHistory,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 }
                }
            };

            PressureSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = pressureHistory,
                    Fill = null,
                    Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 3 }
                }
            };
        }
        
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(dbPath);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
        CREATE TABLE IF NOT EXISTS Logs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Timestamp TEXT,
            Message TEXT
        )";
            command.ExecuteNonQuery();

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT Timestamp, Message FROM Logs ORDER BY Id DESC LIMIT 50";
            using var reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                string timestamp = reader.GetString(0);
                string message = reader.GetString(1);
                LogEntries.Add($"[{timestamp}] {message}");
            }
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
                string humResult = ReadRegister("READ 1");
                Humidity = humResult;
                if (double.TryParse(humResult, out double humValue))
                {
                    humidityHistory.Add(humValue);
                    if (humidityHistory.Count > 20) humidityHistory.RemoveAt(0);
                }

                string presResult = ReadRegister("READ 2");
                Pressure = presResult;
                if (double.TryParse(presResult, out double presValue))
                {
                    pressureHistory.Add(presValue);
                    if (pressureHistory.Count > 20) pressureHistory.RemoveAt(0);
                }
                
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

            using var connection = new SqliteConnection(dbPath);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Logs (Timestamp, Message) VALUES ($timestamp, $message)";
            command.Parameters.AddWithValue("$timestamp", timestamp);
            command.Parameters.AddWithValue("$message", message);
            command.ExecuteNonQuery();
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