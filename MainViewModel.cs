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
using System.Text.Json;
using System.IO;

namespace FactorialApp
{
    public class Recipe
    {
        public string Name { get; set; }
        public string TemperatureThreshold { get; set; }
        public string TargetX { get; set; }
        public string TargetY { get; set; }
        public string TargetZ { get; set; }
    }
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _temperature = "--";
        private string _humidity = "--";
        private string _pressure = "--";
        private string _motorStatus = "Stopped";
        private CancellationTokenSource? cts;
        private string dbPath = "Data Source=monitor.db";
        private object _currentView;
        private IDeviceService deviceService;
        private readonly IVisionService _visionService;
        private readonly SimulatedPlcService _plcService = new SimulatedPlcService();

        private bool _plcStart;
        public bool PlcStart
        {
            get => _plcStart;
            set { _plcStart = value; OnPropertyChanged(nameof(PlcStart)); }
        }

        private bool _plcTrigger;
        public bool PlcTrigger
        {
            get => _plcTrigger;
            set { _plcTrigger = value; OnPropertyChanged(nameof(PlcTrigger)); }
        }

        private bool _plcBusy;
        public bool PlcBusy
        {
            get => _plcBusy;
            set { _plcBusy = value; OnPropertyChanged(nameof(PlcBusy)); }
        }

        private bool _plcDone;
        public bool PlcDone
        {
            get => _plcDone;
            set { _plcDone = value; OnPropertyChanged(nameof(PlcDone)); }
        }

        private bool _plcPass;
        public bool PlcPass
        {
            get => _plcPass;
            set { _plcPass = value; OnPropertyChanged(nameof(PlcPass)); }
        }

        private bool _plcFail;
        public bool PlcFail
        {
            get => _plcFail;
            set { _plcFail = value; OnPropertyChanged(nameof(PlcFail)); }
        }

        private CancellationTokenSource? _autoCycleCts;

        private bool _isAutoRunning;
        public bool IsAutoRunning
        {
            get => _isAutoRunning;
            set { _isAutoRunning = value; OnPropertyChanged(nameof(IsAutoRunning)); }
        }

        private int _cycleCount;
        public int CycleCount
        {
            get => _cycleCount;
            set { _cycleCount = value; OnPropertyChanged(nameof(CycleCount)); }
        }

        private int _passCount;
        public int PassCount
        {
            get => _passCount;
            set { _passCount = value; OnPropertyChanged(nameof(PassCount)); }
        }

        private int _failCount;
        public int FailCount
        {
            get => _failCount;
            set { _failCount = value; OnPropertyChanged(nameof(FailCount)); }
        }

        private string _visionThreshold = "128";
        private string _visionMinArea = "1000";
        private string _visionMaxArea = "500000";

        public string VisionThreshold
        {
            get => _visionThreshold;
            set
            {
                _visionThreshold = value;
                OnPropertyChanged(nameof(VisionThreshold));
            }
        }

        public string VisionMinArea
        {
            get => _visionMinArea;
            set
            {
                _visionMinArea = value;
                OnPropertyChanged(nameof(VisionMinArea));
            }
        }

        public string VisionMaxArea
        {
            get => _visionMaxArea;
            set
            {
                _visionMaxArea = value;
                OnPropertyChanged(nameof(VisionMaxArea));
            }
        }

        public ObservableCollection<VisionMark> VisionMarks { get; set; } =
            new ObservableCollection<VisionMark>();

        private string _visionStatus = "Ready";
        public string VisionStatus
        {
            get => _visionStatus;
            set
            {
                _visionStatus = value;
                OnPropertyChanged(nameof(VisionStatus));
            }
        }
        

        public enum SystemState
        {
            Idle,
            Running,
            Alarm
        }
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
        public ObservableCollection<Recipe> Recipes { get; set; } = new ObservableCollection<Recipe>();
        public ICommand SaveRecipeCommand { get; }
        public ICommand LoadRecipeCommand { get; }

        private string _newRecipeName = "";
        public string NewRecipeName
        {
            get { return _newRecipeName; }
            set { _newRecipeName = value; OnPropertyChanged(nameof(NewRecipeName)); }
        }

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
        private SystemState _currentState = SystemState.Idle;
        public SystemState CurrentState
        {
            get { return _currentState; }
            set { _currentState = value; OnPropertyChanged(nameof(CurrentState)); OnPropertyChanged(nameof(StateText)); }
        }

        public string StateText => CurrentState switch
        {
            SystemState.Idle => "IDLE",
            SystemState.Running => "RUNNING",
            SystemState.Alarm => "ALARM",
            _ => "UNKNOWN"
        };

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
        public ICommand DetectVisionCommand { get; }
        public ICommand StartPlcCycleCommand { get; }
        public ICommand AutoStartCommand { get; }
        public ICommand AutoStopCommand { get; }
        public ObservableCollection<AlarmItem> Alarms { get; set; } = new ObservableCollection<AlarmItem>();

        public MainViewModel(IVisionService visionService)
        {
            _visionService = visionService;

            LoadRecipesFromFile();
            deviceService = new SimulatedDeviceService();
            InitializeDatabase();
            StartCommand = new RelayCommand(ExecuteStart);
            StopCommand = new RelayCommand(ExecuteStop);
            ToggleMotorCommand = new RelayCommand(ExecuteToggleMotor);
            AcknowledgeAlarmCommand = new RelayCommand<AlarmItem>(ExecuteAcknowledgeAlarm);

            DetectVisionCommand = new RelayCommand(async () => await ExecuteDetectVision());
            _plcService.StateChanged += UpdatePlcState;
            StartPlcCycleCommand = new RelayCommand(async () => await ExecutePlcCycle());
            AutoStartCommand = new RelayCommand(async () => await ExecuteAutoCycle());
            AutoStopCommand = new RelayCommand(ExecuteAutoStop);
            UpdatePlcState();
            SaveRecipeCommand = new RelayCommand(ExecuteSaveRecipe);
            LoadRecipeCommand = new RelayCommand<Recipe>(ExecuteLoadRecipe);

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
  
        private void ExecuteSaveRecipe()
        {
            if (string.IsNullOrWhiteSpace(NewRecipeName))
            {
                AddLog("Recipe name cannot be empty");
                return;
            }

            Recipe recipe = new Recipe
            {
                Name = NewRecipeName,
                TemperatureThreshold = TemperatureThreshold,
                TargetX = TargetX,
                TargetY = TargetY,
                TargetZ = TargetZ
            };

            Recipes.Add(recipe);
            SaveRecipesToFile();
            AddLog($"Recipe '{NewRecipeName}' saved");
        }

        private void ExecuteLoadRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            TemperatureThreshold = recipe.TemperatureThreshold;
            TargetX = recipe.TargetX;
            TargetY = recipe.TargetY;
            TargetZ = recipe.TargetZ;
            AddLog($"Recipe '{recipe.Name}' loaded");
        }

        private void SaveRecipesToFile()
        {
            string json = JsonSerializer.Serialize(Recipes);
            File.WriteAllText("recipes.json", json);
        }

        private void LoadRecipesFromFile()
        {
            if (File.Exists("recipes.json"))
            {
                string json = File.ReadAllText("recipes.json");
                var loaded = JsonSerializer.Deserialize<ObservableCollection<Recipe>>(json);
                if (loaded != null)
                {
                    foreach (var r in loaded)
                    {
                        Recipes.Add(r);
                    }
                }
            }
        }
        private void UpdatePlcState()
        {
            PlcStart = _plcService.Start;
            PlcTrigger = _plcService.Trigger;
            PlcBusy = _plcService.Busy;
            PlcDone = _plcService.Done;
            PlcPass = _plcService.Pass;
            PlcFail = _plcService.Fail;
        }

        private async Task<VisionResult?> RunVisionDetectAsync()
        {
            if (!int.TryParse(VisionThreshold, out int threshold))
            {
                VisionStatus = "Invalid Threshold";
                AddLog("Vision parameter error: Threshold is invalid");
                return null;
            }

            if (!double.TryParse(VisionMinArea, out double minArea))
            {
                VisionStatus = "Invalid Min Area";
                AddLog("Vision parameter error: Min Area is invalid");
                return null;
            }

            if (!double.TryParse(VisionMaxArea, out double maxArea))
            {
                VisionStatus = "Invalid Max Area";
                AddLog("Vision parameter error: Max Area is invalid");
                return null;
            }

            if (threshold < 0 || threshold > 255)
            {
                VisionStatus = "Threshold must be 0-255";
                AddLog("Vision parameter error: Threshold must be 0-255");
                return null;
            }

            if (minArea < 0)
            {
                VisionStatus = "Min Area must be >= 0";
                AddLog("Vision parameter error: Min Area must be >= 0");
                return null;
            }

            if (maxArea <= minArea)
            {
                VisionStatus = "Max Area must be > Min Area";
                AddLog("Vision parameter error: Max Area must be greater than Min Area");
                return null;
            }

            AddLog($"Vision Params -> Threshold={threshold}, MinArea={minArea}, MaxArea={maxArea}");

            return await _visionService.DetectAsync(
                threshold,
                minArea,
                maxArea
            );
        }

        private async Task ExecuteDetectVision()
        {
            VisionStatus = "Detecting...";
            AddLog("Vision detection started");

            VisionResult? result = await RunVisionDetectAsync();

            if (result == null)
            {
                if (VisionStatus == "Detecting...")
                    VisionStatus = "Failed";

                AddLog("Vision detection failed: no result");
                return;
            }

            if (!result.Success)
            {
                VisionStatus = "Failed";
                AddLog($"Vision detection failed: {result.Message}");
                return;
            }

            VisionMarks.Clear();

            foreach (VisionMark mark in result.Marks)
            {
                VisionMarks.Add(mark);
            }

            VisionStatus = $"OK - {VisionMarks.Count} mark(s)";
            AddLog($"Vision OK - detected {VisionMarks.Count} mark(s)");

            for (int i = 0; i < VisionMarks.Count; i++)
            {
                VisionMark mark = VisionMarks[i];
                AddLog(
                    $"Mark {i + 1}: X={mark.X:F2}, Y={mark.Y:F2}, Angle={mark.Angle:F2}, Area={mark.Area:F2}"
                );
            }
        }

        private async Task ExecutePlcCycle()
        {
            if (IsAutoRunning)
            {
                AddLog("Manual PLC cycle ignored: Auto mode is running");
                return;
            }

            await RunOneVisionCycleAsync(CancellationToken.None);
        }

        private async Task<bool> RunOneVisionCycleAsync(CancellationToken cancellationToken)
        {
            AddLog("PLC cycle started");

            _plcService.Reset();
            UpdatePlcState();

            await _plcService.StartCycleAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            AddLog("PLC Trigger received -> Vision Detect");
            VisionStatus = "Detecting...";

            VisionResult? result = await RunVisionDetectAsync();
            cancellationToken.ThrowIfCancellationRequested();

            bool visionOk =
                result != null &&
                result.Success &&
                result.Marks.Count > 0;

            _plcService.SetVisionResult(visionOk);

            if (visionOk)
            {
                VisionMarks.Clear();

                foreach (VisionMark mark in result!.Marks)
                {
                    VisionMarks.Add(mark);
                }

                VisionStatus = $"OK - {result.Count} mark(s)";
                AddLog("Vision result = PASS");
            }
            else
            {
                VisionStatus = "Failed";
                AddLog($"Vision result = FAIL: {result?.Message ?? "no result"}");
            }

            return visionOk;
        }

        private async Task ExecuteAutoCycle()
        {
            if (IsAutoRunning)
            {
                AddLog("Auto cycle is already running");
                return;
            }

            _autoCycleCts = new CancellationTokenSource();
            CancellationToken token = _autoCycleCts.Token;

            CycleCount = 0;
            PassCount = 0;
            FailCount = 0;
            IsAutoRunning = true;

            AddLog("AUTO START");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool passed = await RunOneVisionCycleAsync(token);

                    CycleCount++;

                    if (passed)
                        PassCount++;
                    else
                        FailCount++;

                    AddLog(
                        $"Auto Cycle {CycleCount} finished - " +
                        $"Pass={PassCount}, Fail={FailCount}"
                    );

                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException)
            {
                AddLog("AUTO STOP");
            }
            finally
            {
                IsAutoRunning = false;
                _plcService.Reset();
                UpdatePlcState();
            }
        }

        private void ExecuteAutoStop()
        {
            if (!IsAutoRunning)
            {
                AddLog("Auto cycle is not running");
                return;
            }

            _autoCycleCts?.Cancel();
        }

        private async void ExecuteStart()
        {
            if (CurrentState == SystemState.Alarm)
            {
                AddLog("Cannot start: system is in Alarm state. Please acknowledge alarm first.");
                return;
            }

            CurrentState = SystemState.Running;
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
            if (CurrentState != SystemState.Alarm)
            {
                CurrentState = SystemState.Idle;
            }
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
            CurrentState = SystemState.Alarm;
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
            bool hasUnacknowledgedAlarms = false;
            foreach (var a in Alarms)
            {
                if (!a.IsAcknowledged)
                {
                    hasUnacknowledgedAlarms = true;
                    break;
                }
            }

            if (!hasUnacknowledgedAlarms && CurrentState == SystemState.Alarm)
            {
                CurrentState = SystemState.Idle;
            }
        }
        private string ReadRegister(string command)
        {
            return deviceService.ReadRegister(command);
        }
        

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}