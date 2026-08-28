using System;
using System.Threading;
using System.Threading.Tasks;

namespace FactorialApp
{
    public class SimulatedCameraService : ICameraService
    {
        public bool IsConnected { get; private set; }
        public bool IsLive { get; private set; }

        public double ExposureUs { get; set; } = 5000;
        public double Gain { get; set; } = 1;

        // none / offline / timeout / no_image
        public string FaultMode { get; set; } = "none";

        public event Action? StateChanged;

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(250, cancellationToken);

            if (FaultMode == "offline")
            {
                IsConnected = false;
                IsLive = false;
                RaiseStateChanged();
                throw new InvalidOperationException("CAMERA OFFLINE");
            }

            IsConnected = true;
            RaiseStateChanged();
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            IsLive = false;
            IsConnected = false;
            RaiseStateChanged();
        }

        public async Task StartLiveAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("CAMERA NOT CONNECTED");

            await Task.Delay(100, cancellationToken);
            IsLive = true;
            RaiseStateChanged();
        }

        public async Task StopLiveAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(80, cancellationToken);
            IsLive = false;
            RaiseStateChanged();
        }

        public async Task TriggerAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("CAMERA NOT CONNECTED");

            if (FaultMode == "offline")
                throw new InvalidOperationException("CAMERA OFFLINE");

            if (FaultMode == "timeout")
            {
                await Task.Delay(5000, cancellationToken);
                return;
            }

            if (FaultMode == "no_image")
                throw new InvalidOperationException("CAMERA NO IMAGE");

            await Task.Delay(120, cancellationToken);
        }
    }
}
