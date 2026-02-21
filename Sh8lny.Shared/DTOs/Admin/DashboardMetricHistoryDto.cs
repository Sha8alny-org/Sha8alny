namespace Sh8lny.Shared.DTOs.Admin;

/// <summary>
/// DTO for a historical dashboard metric snapshot.
/// Used for trend analysis on the admin dashboard.
/// </summary>
public class DashboardMetricHistoryDto
{
    /// <summary>
    /// The date this snapshot represents.
    /// </summary>
    public DateTime MetricDate { get; set; }

    // User Statistics
    public int TotalStudents { get; set; }
    public int TotalCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int BannedUsers { get; set; }

    // Project Statistics
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int ClosedProjects { get; set; }

    // Application Statistics
    public int TotalApplications { get; set; }
    public int CompletedApplications { get; set; }

    // Financial Statistics
    public decimal TotalTransactionVolume { get; set; }
    public int TotalTransactions { get; set; }

    // Recent Activity
    public int NewUsersLast30Days { get; set; }
    public int NewProjectsLast30Days { get; set; }
}
