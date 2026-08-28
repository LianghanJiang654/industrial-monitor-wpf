using System;
using System.Threading;
using System.Threading.Tasks;

namespace FactorialApp
{
    public interface ICameraService
    {
        bool IsConnected { get; }
        bool IsLive { get; }
        double ExposureUs { get; set; }
        double Gain { get; set; }
        string FaultMode { get; set; }

        event Action? StateChanged;

        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task StartLiveAsync(CancellationToken cancellationToken = default);
        Task StopLiveAsync(CancellationToken cancellationToken = default);
        Task TriggerAsync(CancellationToken cancellationToken = default);
    }
}