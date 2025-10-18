namespace VillageClub.Contracts.Enums;

/// <summary>
/// User roles in the Village Club system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular club member (can view events, use bar)
    /// </summary>
    Member,

    /// <summary>
    /// Volunteer (can sign up for shifts, report stock issues)
    /// </summary>
    Volunteer,

    /// <summary>
    /// Committee member (full administrative access)
    /// </summary>
    Committee
}

/// <summary>
/// Specific committee roles for granular permissions
/// </summary>
public enum CommitteeRole
{
    /// <summary>
    /// Committee treasurer (financial approvals)
    /// </summary>
    Treasurer,

    /// <summary>
    /// Committee chairman
    /// </summary>
    Chairman,

    /// <summary>
    /// Committee clerk/secretary
    /// </summary>
    Clerk,

    /// <summary>
    /// Bar manager (stock management, bar operations)
    /// </summary>
    BarManager,

    /// <summary>
    /// General committee member
    /// </summary>
    General
}

/// <summary>
/// User account status
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Active account
    /// </summary>
    Active,

    /// <summary>
    /// Inactive account (soft deleted)
    /// </summary>
    Inactive,

    /// <summary>
    /// Suspended account (disciplinary action)
    /// </summary>
    Suspended
}
