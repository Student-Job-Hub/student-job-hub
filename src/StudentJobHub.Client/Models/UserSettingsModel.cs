namespace StudentJobHub.Client.Models;

public class UserSettingsModel
{
    // Preferences
    public bool IsDarkMode { get; set; } = false;
    public bool CompactView { get; set; } = false;
    public string PreferredCurrency { get; set; } = "GHS (GH₵)";
    public string DefaultJobFilter { get; set; } = "All";

    // Notifications
    public bool EmailNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;
    public bool ApplicationStatusAlerts { get; set; } = true;
    public bool DirectMessageAlerts { get; set; } = true;
    public bool WeeklyDigest { get; set; } = false;

    // Privacy & Security
    public string ProfileVisibility { get; set; } = "CampusOnly"; // Public, CampusOnly, Private
    public bool ShowEmailToEmployers { get; set; } = true;
    public bool ShowPhoneNumber { get; set; } = false;
    public bool ShareUniversityAffiliation { get; set; } = true;
}
