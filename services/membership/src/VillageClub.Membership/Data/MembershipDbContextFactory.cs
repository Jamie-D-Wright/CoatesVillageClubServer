using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VillageClub.Membership.Data;

/// <summary>
/// Design-time factory for EF Core migrations and tooling.
/// This allows EF Core tools to create the DbContext without running the full application.
/// </summary>
public class MembershipDbContextFactory : IDesignTimeDbContextFactory<MembershipDbContext>
{
    public MembershipDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembershipDbContext>();
        
        // Try to get connection string from environment variable (set by user or automation)
        var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
        
        // Fallback to local development connection string if not set
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True";
        }
        
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
        
        return new MembershipDbContext(optionsBuilder.Options);
    }
}
