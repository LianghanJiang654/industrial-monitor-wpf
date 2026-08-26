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
            set { _temperature = value; OnPropertyChanged(nameof(Temperature)); OnPropertyChanged(nameof(TemperatureColor)); }
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

        // ===== 多轴 XYZ =====
        private string _axisXPosition = "0";
        private string _axisYPosition = "0";
        private string _axisZPosition = "0";
        private string _targetX = "0";
        private string _targetY = "0";
        private string _targetZ = "0";

        public string AxisXPosition
        {
            get { return _axisXPosition; }
            set { _axisXPosition = value; OnPropertyChanged(nameof(AxisXPosition)); }
        }

        public string AxisYPosition
        {
            get { return _axisYPosition; }
            set { _axisYPosition = value; OnPropertyChanged(nameof(AxisYPosition)); }
        }

        public string AxisZPosition
        {
            get { return _axisZPosition; }
            set { _axisZPosition = value; OnPropertyChanged(nameof(AxisZPosition)); }
        }

        public string TargetX
        {
            get { return _targetX; }
            set { _targetX = value; OnPropertyChanged(nameof(TargetX)); }
        }

        public string TargetY
        {
            get { return _targetY; }
            set { _targetY = value; OnPropertyChanged(nameof(TargetY)); }
        }

        public string TargetZ
        {
            get { return _targetZ; }
            set { _targetZ = value; OnPropertyChanged(nameof(TargetZ)); }
        }

        public ICommand MoveXCommand { get; }
        public ICommand MoveYCommand { get; }
        public ICommand MoveZCommand { get; }
        public ICommand JogStartCommand { get; }
        public ICommand JogStopCommand { get; }

        public ObservableCollection<string> LogEntries { get; set; } = new ObservableCollection<string>();

        public string MotorStatus
        {
            get { return _motorStatus; }
            set { _motorStatus = value; OnPropertyChanged(nameof(MotorStatus)); }
        }

        private string _temperatureThreshold = "26";
        public string TemperatureThreshold
        {
            get { return _temperatureThreshold; }
            set { _temperatureThreshold = value; OnPropertyChanged(nameof(TemperatureThreshold)); OnPropertyChanged(nameof(TemperatureColor)); }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ToggleMotorCommand { get; }
        public ICommand AcknowledgeAlarmCommand { get; }
        public ObservableCollection<AlarmItem> Alarms { get; set; } = new ObservableCollection<AlarmItem>();

        public MainViewModel()
        {
            InitializeDatabase();
            StartCommand = new RelayCommand(ExecuteStart);
            StopCommand = new RelayCommand(ExecuteStop);
            ToggleMotorCommand = new RelayCommand(ExecuteToggleMotor);
            AcknowledgeAlarmCommand = new RelayCommand<AlarmItem>(ExecuteAcknowledgeAlarm);

            MoveXCommand = new RelayCommand(() => ExecuteMoveAxis("X", TargetX));
            MoveYCommand = new RelayCommand(() => ExecuteMoveAxis("Y", TargetY));
            MoveZCommand = new RelayCommand(() => ExecuteMoveAxis("Z", TargetZ));
            JogStartCommand = new RelayCommand<string>(ExecuteJogStart);
            JogStopCommand = new RelayCommand<string>(ExecuteJogStop);

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

            var alarmTableCommand = connection.CreateCommand();
            alarmTableCommand.CommandText = @"
    CREATE TABLE IF NOT EXISTS Alarms (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Timestamp TEXT,
        Message TEXT,
        IsAcknowledged INTEGER DEFAULT 0
    )";
            alarmTableCommand.ExecuteNonQuery();

            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = "SELECT Timestamp, Message FROM Logs ORDER BY Id DESC LIMIT 50";
            using var reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                string timestamp = reader.GetString(0);
                string message = reader.GetString(1);
                LogEntries.Add($"[{timestamp}] {message}");
            }

            var selectAlarmsCommand = connection.CreateCommand();
            selectAlarmsCommand.CommandText = "SELECT Id, Timestamp, Message, IsAcknowledged FROM Alarms ORDER BY Id DESC LIMIT 50";
            using var alarmReader = selectAlarmsCommand.ExecuteReader();
            while (alarmReader.Read())
            {
                Alarms.Add(new AlarmItem
                {
                    Id = alarmReader.GetInt32(0),
                    Timestamp = alarmReader.GetString(1),
                    Message = alarmReader.GetString(2),
                    IsAcknowledged = alarmReader.GetInt32(3) == 1
                });
            }
        }

        private void ExecuteMoveAxis(string axis, string target)
        {
            ReadRegister($"MOVE {axis} {target}");
            AddLog($"Axis {axis} move command sent, target: {target}");
        }

        private void ExecuteJogStart(string parameter)
        {
            string[] parts = parameter.Split(',');
            string axis = parts[0];
            string direction = parts[1];
            ReadRegister($"JOG {axis} {direction}");
        }

        private void ExecuteJogStop(string axis)
        {
            ReadRegister($"JOG {axis} 0");
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
                if (double.TryParse(tempResult, out double tv) && int.TryParse(_temperatureThreshold, out int threshold) && tv > threshold)
                {
                    RaiseAlarm($"Temperature exceeded threshold: {tempResult} > {threshold}");
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

                AxisXPosition = ReadRegister("READPOS X");
                AxisYPosition = ReadRegister("READPOS Y");
                AxisZPosition = ReadRegister("READPOS Z");

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

        private void RaiseAlarm(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            using var connection = new SqliteConnection(dbPath);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Alarms (Timestamp, Message, IsAcknowledged) VALUES ($timestamp, $message, 0)";
            command.Parameters.AddWithValue("$timestamp", timestamp);
            command.Parameters.AddWithValue("$message", message);
            command.ExecuteNonQuery();

            var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";
            long newId = (long)idCommand.ExecuteScalar();

            Alarms.Insert(0, new AlarmItem
            {
                Id = (int)newId,
                Timestamp = timestamp,
                Message = message,
                IsAcknowledged = false
            });
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

        private void ExecuteAcknowledgeAlarm(AlarmItem alarm)
        {
            if (alarm == null) return;

            alarm.IsAcknowledged = true;

            using var connection = new SqliteConnection(dbPath);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Alarms SET IsAcknowledged = 1 WHERE Id = $id";
            command.Parameters.AddWithValue("$id", alarm.Id);
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
                    client.Connect("10.24.48.31", 5001);

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