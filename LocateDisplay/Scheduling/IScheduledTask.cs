using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocateDisplay.Scheduling
{
    public interface IScheduledTask
    {
        TimeSpan Interval { get; }
        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}
