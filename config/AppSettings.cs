using System.Collections.Generic;

namespace WebAPI.config;

public class AppSettings
{
    public bool DebugMode { get; set; } = false;
    public string SsoUrl { get; set; } = string.Empty;
    public string EndpointSearchContact { get; set; } = string.Empty;
    public string EndpointReportCall { get; set; } = string.Empty;
    public bool OnlyUsersContacts { get; set; } = true;
    public List<Company> Companies { get; set; } = new List<Company>();
}

public class Company
{
    public long Id { get; set; }
    public string Url { get; set; }
}