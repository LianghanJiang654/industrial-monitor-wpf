using System;
using System.Threading;
using System.Threading.Tasks;

namespace FactorialApp
{
    public class SimulatedPlcService : IPlcService
    {
        public bool Start { get; private set; }
        public bool Trigger { get; private set; }
        public bool Busy { get; private set; }
        public bool Done { get; private set; }
        public bool Pass { get; private set; }
        public bool Fail { get; private set; }

        public event Action? StateChanged;

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        public async Task StartCycleAsync(CancellationToken cancellationToken = default)
        {
            Reset();

            Start = true;
            RaiseStateChanged();

            await Task.Delay(500, cancellationToken);

            Busy = true;
            Trigger = true;
            RaiseStateChanged();

            await Task.Delay(800, cancellationToken);

            Trigger = false;
            RaiseStateChanged();

            // 这里暂时只模拟 PLC 流程。
            // 下一步 Vision Handshake 时，会由视觉结果决定 Pass / Fail。
        }

        public void SetVisionResult(bool success)
        {
            Busy = false;
            Done = true;
            Pass = success;
            Fail = !success;
            RaiseStateChanged();
        }

        public void Reset()
        {
            Start = false;
            Trigger = false;
            Busy = false;
            Done = false;
            Pass = false;
            Fail = false;
            RaiseStateChanged();
        }
    }
}