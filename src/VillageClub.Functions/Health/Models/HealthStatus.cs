using System;

namespace VillageClub.Functions.Health.Models
{
    public class HealthStatus
    {
        public string Status { get; set; } = "Healthy";
        public TimeSpan Uptime { get; set; }
        public DateTime LastCheckTime { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }

        public static class States
        {
            public const string Healthy = "Healthy";
            public const string Degraded = "Degraded";
            public const string Unhealthy = "Unhealthy";
        }

        public HealthStatus()
        {
            LastCheckTime = DateTime.UtcNow;
        }
    }
}