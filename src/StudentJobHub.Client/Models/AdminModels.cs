namespace StudentJobHub.Client.Models;

public class AdminOverviewModel
{
    public int TotalUsers { get; set; }

    public int SuspendedUsers { get; set; }

    public int TotalJobs { get; set; }

    public int OpenJobs { get; set; }

    public int TotalServices { get; set; }

    public int TotalApplications { get; set; }

    public int NewUsersThisMonth { get; set; }
}

public class AdminUserModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? University { get; set; }

    public List<string> Roles { get; set; } = new();

    public bool IsSuspended { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdminJobModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string PostedByName { get; set; } = string.Empty;

    public string PostedByEmail { get; set; } = string.Empty;

    public bool IsOpen { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class AdminServiceModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderEmail { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }
}
