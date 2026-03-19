using System.Text.Json;

namespace EmailCompanion.Services;

public class AppConfig
{
    public string? RepoRootPath { get; set; }
    public string? TemplateSubfolder { get; set; } = AppConfigService.DefaultTemplateSubfolder;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? DefaultSenderEmail { get; set; }
    public string? DefaultRecipientEmail { get; set; }
}

public class AppConfigService
{
    public const string DefaultTemplateSubfolder = "EmailTemplates";

    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EmailCompanion");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
    private static readonly string? WorkspaceRepoRoot = FindWorkspaceRepoRoot();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly Lock _lock = new();
    private AppConfig _config;

    public AppConfigService()
    {
        _config = Load();
    }

    public AppConfig Config
    {
        get
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }

    public string? TemplateFolderFullPath
    {
        get => GetTemplateFolderFullPath();
    }

    public string? GetEffectiveRepoRootPath(AppConfig? config = null)
    {
        var activeConfig = config ?? Config;
        if (!string.IsNullOrWhiteSpace(activeConfig.RepoRootPath))
            return activeConfig.RepoRootPath;

        return WorkspaceRepoRoot;
    }

    public string? GetTemplateFolderFullPath(AppConfig? config = null)
    {
        var activeConfig = config ?? Config;
        var repoRoot = GetEffectiveRepoRootPath(activeConfig);
        if (string.IsNullOrWhiteSpace(repoRoot))
            return null;

        var subfolder = GetTemplateSubfolder(activeConfig);
        return Path.GetFullPath(Path.Combine(repoRoot, subfolder));
    }

    public static string GetTemplateSubfolder(AppConfig config)
    {
        return string.IsNullOrWhiteSpace(config.TemplateSubfolder)
            ? DefaultTemplateSubfolder
            : config.TemplateSubfolder;
    }

    private static string? FindWorkspaceRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NotificationCompanion.sln")) ||
                Directory.Exists(Path.Combine(current.FullName, DefaultTemplateSubfolder)))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public void Save(AppConfig config)
    {
        lock (_lock)
        {
            _config = config;
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
        }
    }

    private static AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}
