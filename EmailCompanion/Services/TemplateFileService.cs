namespace EmailCompanion.Services;

public class TemplateFileInfo
{
    public required string FileName { get; set; }
    public required string RelativePath { get; set; }
    public required string FullPath { get; set; }
    public string? DeclaredModelType { get; set; }
    public DateTime LastModified { get; set; }
    public long SizeBytes { get; set; }
}

public class TemplateFileService
{
    private readonly AppConfigService _configService;

    public TemplateFileService(AppConfigService configService)
    {
        _configService = configService;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configService.TemplateFolderFullPath) &&
        Directory.Exists(_configService.TemplateFolderFullPath);

    public List<TemplateFileInfo> GetTemplates()
    {
        var root = _configService.TemplateFolderFullPath;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return [];

        return Directory.GetFiles(root, "*.cshtml", SearchOption.AllDirectories)
            .Select(fullPath =>
            {
                var info = new FileInfo(fullPath);
                var content = File.ReadAllText(fullPath);
                return new TemplateFileInfo
                {
                    FileName = info.Name,
                    RelativePath = Path.GetRelativePath(root, fullPath),
                    FullPath = fullPath,
                    DeclaredModelType = TemplateParserService.ExtractDeclaredModelType(content),
                    LastModified = info.LastWriteTimeUtc,
                    SizeBytes = info.Length
                };
            })
            .OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ReadTemplate(string fullPath)
    {
        var root = _configService.TemplateFolderFullPath;
        if (string.IsNullOrWhiteSpace(root))
            return null;

        var normalizedRoot = Path.GetFullPath(root);
        var normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!normalizedPath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(normalizedPath))
            return null;

        return File.ReadAllText(normalizedPath);
    }
}
