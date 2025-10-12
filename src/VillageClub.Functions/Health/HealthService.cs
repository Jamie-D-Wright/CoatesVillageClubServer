using System.Diagnostics;
using VillageClub.Functions.Health.Models;

namespace VillageClub.Functions.Health
{
    public class HealthService
    {
        private readonly DateTime _startTime;
        private const long MEMORY_THRESHOLD_BYTES = 1_500_000_000; // 1.5GB

        public HealthService()
        {
            _startTime = DateTime.UtcNow;
        }

        public HealthStatus GetHealthStatus()
        {
            var process = Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64;
            var threadCount = process.Threads.Count;

            var status = new HealthStatus
            {
                Uptime = DateTime.UtcNow - _startTime,
                MemoryUsage = memoryUsage,
                ThreadCount = threadCount,
                Status = DetermineStatus(memoryUsage)
            };

            return status;
        }

        private string DetermineStatus(long memoryUsage)
        {
            if (memoryUsage >= MEMORY_THRESHOLD_BYTES)
            {
                return HealthStatus.States.Degraded;
            }

            return HealthStatus.States.Healthy;
        }
    }
}