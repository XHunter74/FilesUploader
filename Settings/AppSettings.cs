using System.ComponentModel.DataAnnotations;

namespace xhunter74.CollectionManager.API.Settings;

public class AppSettings
{
    public const string ConfigSection = "AppSettings";

    [Required(AllowEmptyStrings = false)]
    [Range(1, 60, ErrorMessage = "ScanInterval must be a positive integer value from 1 to 60.")]
    public int ScanIntervalMinutes { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string ScanFolder { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string Container { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string AzureConnectionString { get; set; }
}
