using System;
using System.Threading;
using System.Threading.Tasks;

namespace FactorialApp
{
    public interface IPlcService
    {
        bool Start { get; }
        bool Trigger { get; }
        bool Busy { get; }
        bool Done { get; }
        bool Pass { get; }
        bool Fail { get; }

        event Action? StateChanged;

        Task StartCycleAsync(CancellationToken cancellationToken = default);
        void Reset();
    }
}