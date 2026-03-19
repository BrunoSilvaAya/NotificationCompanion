using System.Text.RegularExpressions;

namespace EmailCompanion.Services;

public static partial class TemplateParserService
{
    [GeneratedRegex(@"^\s*@model\s+([^\r\n]+)$", RegexOptions.Multiline)]
    private static partial Regex ModelDirectiveRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9_])Model\??\.([A-Za-z_][\w]*(?:\??\.[A-Za-z_][\w]*)*)", RegexOptions.None)]
    private static partial Regex ModelReferenceRegex();

    [GeneratedRegex(@"@foreach\s*\(\s*var\s+\w+\s+in\s+Model\??\.([A-Za-z_][\w]*(?:\.[\w]+)*)\s*\)", RegexOptions.None)]
    private static partial Regex ForeachRegex();

    [GeneratedRegex(@"@if\s*\(\s*(?:\(+\s*\w+\s*\)\s*)*Model\??\.([A-Za-z_][\w]*(?:\??\.[A-Za-z_][\w]*)*)", RegexOptions.None)]
    private static partial Regex ConditionalRegex();

    [GeneratedRegex(@"\(string\)\s*Model\??\.([A-Za-z_][\w]*(?:\??\.[A-Za-z_][\w]*)*)", RegexOptions.None)]
    private static partial Regex StringCastRegex();

    public static string? ExtractDeclaredModelType(string templateContent)
    {
        var match = ModelDirectiveRegex().Match(templateContent);
        if (!match.Success)
            return null;

        return match.Groups[1].Value.Trim();
    }

    public static List<TemplateField> Parse(string templateContent)
    {
        var fields = new Dictionary<string, TemplateField>(StringComparer.OrdinalIgnoreCase);

        // Extract foreach collections
        foreach (Match match in ForeachRegex().Matches(templateContent))
        {
            var path = NormalizePath(match.Groups[1].Value);
            AddOrUpdateField(fields, path, field => field.IsCollection = true);
        }

        // Extract conditional references
        foreach (Match match in ConditionalRegex().Matches(templateContent))
        {
            var path = NormalizePath(match.Groups[1].Value);
            AddOrUpdateField(fields, path, field => field.IsConditional = true);
        }

        foreach (Match match in StringCastRegex().Matches(templateContent))
        {
            var path = NormalizePath(match.Groups[1].Value);
            AddOrUpdateField(fields, path, field => field.HasStringCast = true);
        }

        // Extract all Model.Property references, including usages inside Razor code blocks.
        foreach (Match match in ModelReferenceRegex().Matches(templateContent))
        {
            var path = NormalizePath(match.Groups[1].Value);
            AddOrUpdateField(fields, path);
        }

        // Remove entries that are collection-runtime properties (e.g. Body.Buttons.Count)
        // and entries whose path is a strict prefix of another field's path (e.g. "Body" when "Body.Title" exists).
        var keys = fields.Keys.ToList();
        foreach (var key in keys)
        {
            var lastDot = key.LastIndexOf('.');
            if (lastDot >= 0)
            {
                var lastSegment = key[(lastDot + 1)..];
                if (lastSegment.Equals("Count", StringComparison.OrdinalIgnoreCase) ||
                    lastSegment.Equals("Length", StringComparison.OrdinalIgnoreCase))
                {
                    fields.Remove(key);
                    continue;
                }
            }

            var prefix = key + ".";
            if (fields.Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                // Only remove if this was parsed as a bare parent (no schema sample value).
                var field = fields[key];
                if (string.IsNullOrWhiteSpace(field.SampleValue) && !field.IsCollection)
                    fields.Remove(key);
            }
        }

        return fields.Values
            .OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("?.", ".").Replace("?", "");
    }

    private static void AddOrUpdateField(
        IDictionary<string, TemplateField> fields,
        string path,
        Action<TemplateField>? update = null)
    {
        if (!fields.TryGetValue(path, out var field))
        {
            field = new TemplateField
            {
                Path = path,
                DisplayName = FormatDisplayName(path)
            };
            fields[path] = field;
        }

        update?.Invoke(field);
    }

    private static string FormatDisplayName(string path)
    {
        return path.Replace(".", " > ");
    }
}
