using System.ComponentModel.DataAnnotations;

namespace VillageClub.Functions.Configuration
{
    public class ServiceSettings
    {
        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        [Required]
        public string LogLevel { get; set; } = "Information";

        [Required]
        public string ApplicationName { get; set; } = string.Empty;

        [Required]
        public string EnvironmentName { get; set; } = string.Empty;

        public void Validate()
        {
            var validLogLevels = new[] { "Debug", "Information", "Warning", "Error", "Critical" };
            if (!validLogLevels.Contains(LogLevel))
            {
                throw new ValidationException($"LogLevel must be one of: {string.Join(", ", validLogLevels)}");
            }

            if (string.IsNullOrWhiteSpace(ApplicationName))
            {
                throw new ValidationException("ApplicationName must not be empty");
            }

            if (string.IsNullOrWhiteSpace(EnvironmentName))
            {
                throw new ValidationException("EnvironmentName must not be empty");
            }
        }
    }
}