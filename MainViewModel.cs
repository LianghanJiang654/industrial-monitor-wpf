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
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.IO;

namespace FactorialApp
{
    public class Recipe
    {
        public string Name { get; set; } = "";
        public string TemperatureThreshold { get; set; } = "26";
        public string TargetX { get; set; } = "0";
        public string TargetY { get; set; } = "0";
        public string TargetZ { get; set; } = "0";
        public string VisionImageName { get; set; } = "normal";
        public string VisionThresholdMode { get; set; } = "fixed";
        public string VisionThreshold { get; set; } = "128";
        public string VisionMinArea { get; set; } = "1000";
        public string VisionMaxArea { get; set; } = "500000";
        public string VisionPositionTolerance { get; set; } = "5";
        public string VisionAngleTolerance { get; set; } = "3";
        public string VisionAreaTolerancePercent { get; set; } = "10";
        public string ProductCode { get; set; } = "PRODUCT-A";
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
        private readonly ICameraService _cameraService = new SimulatedCameraService();



        private string _productCode = "PRODUCT-A";
        public string ProductCode
        {
            get => _productCode;
            set { _productCode = value; OnPropertyChanged(nameof(ProductCode)); }
        }

        private string _activeRecipeName = "DEFAULT";
        public string ActiveRecipeName
        {
            get => _activeRecipeName;
            set { _activeRecipeName = value; OnPropertyChanged(nameof(ActiveRecipeName)); }
        }

        private ImageSource? _visionImage;
        public ImageSource? VisionImage
        {
            get => _visionImage;
            set { _visionImage = value; OnPropertyChanged(nameof(VisionImage)); }
        }

        private bool _plcReady = true;
        public bool PlcReady
        {
            get => _plcReady;
            set { _plcReady = value; OnPropertyChanged(nameof(PlcReady)); }
        }

        private string _handshakeState = "READY";
        public string HandshakeState
        {
            get => _handshakeState;
            set { _handshakeState = value; OnPropertyChanged(nameof(HandshakeState)); }
        }

        private string _lastAlarmCode = "-";
        public string LastAlarmCode
        {
            get => _lastAlarmCode;
            set { _lastAlarmCode = value; OnPropertyChanged(nameof(LastAlarmCode)); }
        }

        private bool _cameraConnected;
        public bool CameraConnected
        {
            get => _cameraConnected;
            set { _cameraConnected = value; OnPropertyChanged(nameof(CameraConnected)); OnPropertyChanged(nameof(CameraStatusText)); }
        }

        private bool _cameraLive;
        public bool CameraLive
        {
            get => _cameraLive;
            set { _cameraLive = value; OnPropertyChanged(nameof(CameraLive)); OnPropertyChanged(nameof(CameraStatusText)); }
        }

        private double _cameraExposureUs = 5000;
        public double CameraExposureUs
        {
            get => _cameraExposureUs;
            set
            {
                _cameraExposureUs = value;
                _cameraService.ExposureUs = value;
                OnPropertyChanged(nameof(CameraExposureUs));
            }
        }

        private double _cameraGain = 1;
        public double CameraGain
        {
            get => _cameraGain;
            set
            {
                _cameraGain = value;
                _cameraService.Gain = value;
                OnPropertyChanged(nameof(CameraGain));
            }
        }

        private string _cameraFaultMode = "none";
        public string CameraFaultMode
        {
            get => _cameraFaultMode;
            set
            {
                _cameraFaultMode = value;
                _cameraService.FaultMode = value;
                OnPropertyChanged(nameof(CameraFaultMode));
            }
        }

        public string[] CameraFaultModes { get; } =
        {
            "none",
            "offline",
            "timeout",
            "no_image"
        };

        public string CameraStatusText =>
            CameraConnected
                ? (CameraLive ? "CONNECTED / LIVE" : "CONNECTED")
                : "DISCONNECTED";

        private string _systemAlarmText = "-";
        public string SystemAlarmText
        {
            get => _systemAlarmText;
            set { _systemAlarmText = value; OnPropertyChanged(nameof(SystemAlarmText)); }
        }

        private bool _hasVisionAlarm;
        public bool HasVisionAlarm
        {
            get => _hasVisionAlarm;
            set { _hasVisionAlarm = value; OnPropertyChanged(nameof(HasVisionAlarm)); }
        }

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
            set { _cycleCount = value; OnPropertyChanged(nameof(CycleCount)); OnPropertyChanged(nameof(YieldPercent)); }
        }

        private int _passCount;
        public int PassCount
        {
            get => _passCount;
            set { _passCount = value; OnPropertyChanged(nameof(PassCount)); OnPropertyChanged(nameof(YieldPercent)); }
        }

        private int _failCount;
        public int FailCount
        {
            get => _failCount;
            set { _failCount = value; OnPropertyChanged(nameof(FailCount)); OnPropertyChanged(nameof(YieldPercent)); }
        }

        private string _visionImageName = "multi_marks";

        public string VisionImageName
        {
            get => _visionImageName;
            set
            {
                _visionImageName = value;
                OnPropertyChanged(nameof(VisionImageName));
            }
        }

        public string[] VisionImageNames { get; } =
        {
            "multi_marks",
            "normal",
            "dark",
            "bright",
            "uneven",
            "position_ng",
            "angle_ng"
        };
        
        private string _visionThresholdMode = "fixed";

        public string VisionThresholdMode
        {
            get => _visionThresholdMode;
            set
            {
                _visionThresholdMode = value;
                OnPropertyChanged(nameof(VisionThresholdMode));
            }
        }

        public string[] VisionThresholdModes { get; } =
        {
            "fixed",
            "otsu",
            "adaptive"
        };
        
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

        private string _visionPositionTolerance = "5";
        public string VisionPositionTolerance { get => _visionPositionTolerance; set { _visionPositionTolerance = value; OnPropertyChanged(nameof(VisionPositionTolerance)); } }

        private string _visionAngleTolerance = "3";
        public string VisionAngleTolerance { get => _visionAngleTolerance; set { _visionAngleTolerance = value; OnPropertyChanged(nameof(VisionAngleTolerance)); } }

        private string _visionAreaTolerancePercent = "10";
        public string VisionAreaTolerancePercent { get => _visionAreaTolerancePercent; set { _visionAreaTolerancePercent = value; OnPropertyChanged(nameof(VisionAreaTolerancePercent)); } }

        private string _lastNgReason = "-";
        public string LastNgReason { get => _lastNgReason; set { _lastNgReason = value; OnPropertyChanged(nameof(LastNgReason)); } }

        public double YieldPercent => CycleCount == 0 ? 0.0 : (double)PassCount / CycleCount * 100.0;

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
        public ICommand CameraConnectCommand { get; }
        public ICommand CameraDisconnectCommand { get; }
        public ICommand CameraLiveCommand { get; }
        public ICommand CameraStopLiveCommand { get; }
        public ICommand ResetVisionAlarmCommand { get; }
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

            _cameraService.StateChanged += UpdateCameraState;
            CameraConnectCommand = new RelayCommand(async () => await ExecuteCameraConnect());
            CameraDisconnectCommand = new RelayCommand(async () => await ExecuteCameraDisconnect());
            CameraLiveCommand = new RelayCommand(async () => await ExecuteCameraLive());
            CameraStopLiveCommand = new RelayCommand(async () => await ExecuteCameraStopLive());
            ResetVisionAlarmCommand = new RelayCommand(ExecuteResetVisionAlarm);
            DetectVisionCommand = new RelayCommand(async () => await ExecuteDetectVision());
            _plcService.StateChanged += UpdatePlcState;
            StartPlcCycleCommand = new RelayCommand(async () => await ExecutePlcCycle());
            AutoStartCommand = new RelayCommand(async () => await ExecuteAutoCycle());
            AutoStopCommand = new RelayCommand(ExecuteAutoStop);
            UpdatePlcState();
            UpdateCameraState();
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
                TargetZ = TargetZ,
                VisionImageName = VisionImageName,
                VisionThresholdMode = VisionThresholdMode,
                VisionThreshold = VisionThreshold,
                VisionMinArea = VisionMinArea,
                VisionMaxArea = VisionMaxArea,
                VisionPositionTolerance = VisionPositionTolerance,
                VisionAngleTolerance = VisionAngleTolerance,
                VisionAreaTolerancePercent = VisionAreaTolerancePercent,
                ProductCode = ProductCode
            };

            Recipes.Add(recipe);
            SaveRecipesToFile();
            ActiveRecipeName = recipe.Name;
            AddLog($"Recipe '{NewRecipeName}' saved");
        }

        private void ExecuteLoadRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            TemperatureThreshold = recipe.TemperatureThreshold;
            TargetX = recipe.TargetX;
            TargetY = recipe.TargetY;
            TargetZ = recipe.TargetZ;
            VisionImageName = recipe.VisionImageName;
            VisionThresholdMode = recipe.VisionThresholdMode;
            VisionThreshold = recipe.VisionThreshold;
            VisionMinArea = recipe.VisionMinArea;
            VisionMaxArea = recipe.VisionMaxArea;
            VisionPositionTolerance = recipe.VisionPositionTolerance;
            VisionAngleTolerance = recipe.VisionAngleTolerance;
            VisionAreaTolerancePercent = recipe.VisionAreaTolerancePercent;
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
        private void UpdateVisionImage(VisionResult? result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.AnnotatedImageBase64))
                return;

            try
            {
                byte[] bytes = Convert.FromBase64String(result.AnnotatedImageBase64);

                using MemoryStream stream = new MemoryStream(bytes);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                VisionImage = bitmap;
            }
            catch (Exception ex)
            {
                AddLog($"Image display error: {ex.Message}");
            }
        }

        private void UpdateCameraState()
        {
            CameraConnected = _cameraService.IsConnected;
            CameraLive = _cameraService.IsLive;
        }

        private string GetAlarmCode(string message)
        {
            string upper = (message ?? "").ToUpperInvariant();

            if (upper.Contains("OFFLINE") || upper.Contains("NOT CONNECTED"))
                return "E001";

            if (upper.Contains("TIMEOUT"))
                return "E002";

            if (upper.Contains("NO IMAGE"))
                return "E003";

            if (upper.Contains("COUNT NG"))
                return "V101";

            if (upper.Contains("POSITION NG"))
                return "V102";

            if (upper.Contains("ANGLE NG"))
                return "V103";

            if (upper.Contains("AREA NG"))
                return "V104";

            return "E999";
        }

        private void SetVisionAlarm(string message)
        {
            string code = GetAlarmCode(message);
            HasVisionAlarm = true;
            LastAlarmCode = code;
            SystemAlarmText = $"{code}  {message}";
            AddLog($"ALARM {code}: {message}");
        }

        private void ClearVisionAlarm()
        {
            HasVisionAlarm = false;
            LastAlarmCode = "-";
            SystemAlarmText = "-";
        }

        private async Task ExecuteCameraConnect()
        {
            try
            {
                _cameraService.FaultMode = CameraFaultMode;
                await _cameraService.ConnectAsync();
                UpdateCameraState();
                ClearVisionAlarm();
                AddLog("Camera connected");
            }
            catch (Exception ex)
            {
                UpdateCameraState();
                SetVisionAlarm(ex.Message);
            }
        }

        private async Task ExecuteCameraDisconnect()
        {
            try
            {
                await _cameraService.DisconnectAsync();
                UpdateCameraState();
                AddLog("Camera disconnected");
            }
            catch (Exception ex)
            {
                SetVisionAlarm(ex.Message);
            }
        }

        private async Task ExecuteCameraLive()
        {
            try
            {
                _cameraService.FaultMode = CameraFaultMode;
                await _cameraService.StartLiveAsync();
                UpdateCameraState();
                ClearVisionAlarm();
                AddLog("Camera live view started");
            }
            catch (Exception ex)
            {
                UpdateCameraState();
                SetVisionAlarm(ex.Message);
            }
        }

        private async Task ExecuteCameraStopLive()
        {
            try
            {
                await _cameraService.StopLiveAsync();
                UpdateCameraState();
                AddLog("Camera live view stopped");
            }
            catch (Exception ex)
            {
                SetVisionAlarm(ex.Message);
            }
        }

        private void ExecuteResetVisionAlarm()
        {
            ClearVisionAlarm();
            LastNgReason = "-";
            AddLog("Vision alarm reset");
        }

        private async Task<bool> PrepareCameraTriggerAsync(CancellationToken cancellationToken)
        {
            if (!CameraConnected)
            {
                SetVisionAlarm("CAMERA NOT CONNECTED");
                VisionStatus = "CAMERA NOT CONNECTED";
                return false;
            }

            try
            {
                _cameraService.FaultMode = CameraFaultMode;

                using CancellationTokenSource timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

                try
                {
                    await _cameraService.TriggerAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    SetVisionAlarm("VISION TIMEOUT - Camera trigger exceeded 2s");
                    VisionStatus = "VISION TIMEOUT";
                    return false;
                }

                ClearVisionAlarm();
                return true;
            }
            catch (Exception ex)
            {
                SetVisionAlarm(ex.Message);
                VisionStatus = ex.Message;
                return false;
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
            if (!int.TryParse(VisionThreshold, out int threshold) || threshold < 0 || threshold > 255) { VisionStatus = "Invalid Threshold"; AddLog("Vision parameter error: Threshold must be 0-255"); return null; }
            if (!double.TryParse(VisionMinArea, out double minArea) || minArea < 0) { VisionStatus = "Invalid Min Area"; AddLog("Vision parameter error: Min Area is invalid"); return null; }
            if (!double.TryParse(VisionMaxArea, out double maxArea) || maxArea <= minArea) { VisionStatus = "Invalid Max Area"; AddLog("Vision parameter error: Max Area must be > Min Area"); return null; }
            if (!double.TryParse(VisionPositionTolerance, out double posTol) || posTol < 0) { VisionStatus = "Invalid Position Tol"; AddLog("Vision parameter error: Position Tolerance is invalid"); return null; }
            if (!double.TryParse(VisionAngleTolerance, out double angleTol) || angleTol < 0) { VisionStatus = "Invalid Angle Tol"; AddLog("Vision parameter error: Angle Tolerance is invalid"); return null; }
            if (!double.TryParse(VisionAreaTolerancePercent, out double areaTol) || areaTol < 0) { VisionStatus = "Invalid Area Tol"; AddLog("Vision parameter error: Area Tolerance is invalid"); return null; }
            AddLog($"Vision Params -> Image={VisionImageName}, Mode={VisionThresholdMode}, Threshold={threshold}, MinArea={minArea}, MaxArea={maxArea}, PosTol={posTol}px, AngleTol={angleTol}deg, AreaTol={areaTol}%");
            return await _visionService.DetectAsync(VisionImageName, VisionThresholdMode, threshold, minArea, maxArea, posTol, angleTol, areaTol);
        }

        private async Task ExecuteDetectVision()
        {
            VisionStatus = "Triggering camera...";
            AddLog("Manual vision trigger started");

            bool cameraReady = await PrepareCameraTriggerAsync(CancellationToken.None);
            if (!cameraReady)
                return;

            VisionStatus = "Detecting...";
            AddLog("Vision detection started");

            VisionResult? result = await RunVisionDetectAsync();
            UpdateVisionImage(result);

            VisionMarks.Clear();

            if (result == null)
            {
                VisionStatus = "VISION ERROR";
                LastNgReason = "No result";
                SetVisionAlarm("VISION ERROR - No result");
                return;
            }

            foreach (VisionMark mark in result.Marks)
                VisionMarks.Add(mark);

            if (!result.Success)
            {
                VisionStatus = "VISION ERROR";
                LastNgReason = result.Message ?? "Vision service error";
                SetVisionAlarm(LastNgReason);
                return;
            }

            if (result.InspectionPass)
            {
                VisionStatus = $"PASS - {result.Count} mark(s)";
                LastNgReason = "-";
                ClearVisionAlarm();
                AddLog($"Vision result = PASS ({result.Count} mark(s))");
            }
            else
            {
                VisionStatus = result.Message ?? "NG";
                LastNgReason = result.Message ?? "Unknown NG";
                LastAlarmCode = GetAlarmCode(LastNgReason);
                AddLog($"Vision result = FAIL [{LastAlarmCode}]: {LastNgReason}");
            }

            for (int i = 0; i < VisionMarks.Count; i++)
            {
                VisionMark mark = VisionMarks[i];
                AddLog(
                    $"Mark {i + 1}: X={mark.X:F2}, Y={mark.Y:F2}, " +
                    $"Angle={mark.Angle:F2}, Area={mark.Area:F2}"
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
            PlcReady = false;
            HandshakeState = "START";

            _plcService.Reset();
            UpdatePlcState();

            await _plcService.StartCycleAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            AddLog("PLC Trigger received -> Camera Trigger");
            HandshakeState = "TRIGGER";

            VisionStatus = "Triggering camera...";
            bool cameraReady = await PrepareCameraTriggerAsync(cancellationToken);

            if (!cameraReady)
            {
                _plcService.SetVisionResult(false);
                HandshakeState = "DONE / FAIL";
                PlcReady = true;
                LastNgReason = VisionStatus;
                return false;
            }

            AddLog("Camera image ready -> Vision Detect");
            HandshakeState = "BUSY";
            VisionStatus = "Detecting...";

            VisionResult? result = await RunVisionDetectAsync();
            UpdateVisionImage(result);
            cancellationToken.ThrowIfCancellationRequested();

            bool visionOk =
                result != null &&
                result.Success &&
                result.InspectionPass;

            _plcService.SetVisionResult(visionOk);
            HandshakeState = visionOk ? "DONE / PASS" : "DONE / FAIL";
            PlcReady = true;

            VisionMarks.Clear();

            if (result != null)
            {
                foreach (VisionMark mark in result.Marks)
                    VisionMarks.Add(mark);
            }

            if (visionOk)
            {
                VisionStatus = $"PASS - {result!.Count} mark(s)";
                LastNgReason = "-";
                ClearVisionAlarm();
                AddLog("Vision result = PASS");
            }
            else
            {
                string reason = result?.Message ?? "No vision result";
                VisionStatus = reason;
                LastNgReason = reason;

                if (result == null || !result.Success)
                {
                    SetVisionAlarm(reason);
                }
                else
                {
                    LastAlarmCode = GetAlarmCode(reason);
                    AddLog($"Vision result = FAIL [{LastAlarmCode}]: {reason}");
                }
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