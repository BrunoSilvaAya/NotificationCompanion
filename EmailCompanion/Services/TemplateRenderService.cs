using System.Globalization;
using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RazorLight;

namespace EmailCompanion.Services;

public class RenderResult
{
    public bool Success { get; set; }
    public string Html { get; set; } = "";
    public string? Error { get; set; }
}

public partial class TemplateRenderService
{
    [GeneratedRegex(@"^\s*@model\s+.*$", RegexOptions.Multiline)]
    private static partial Regex ModelDirectiveRegex();

    public async Task<RenderResult> RenderAsync(string templateFullPath, List<TemplateField> fields)
    {
        try
        {
            var templateDir = Path.GetDirectoryName(templateFullPath)!;
            var templateContent = await File.ReadAllTextAsync(templateFullPath);

            // Strip @model directives since we use ExpandoObject
            templateContent = ModelDirectiveRegex().Replace(templateContent, "");

            var engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templateDir)
                .UseMemoryCachingProvider()
                .Build();

            var modelToken = BuildModel(fields);
            NormalizeModelForTemplate(templateFullPath, modelToken);
            var model = new TemplateDynamicValue(modelToken);
            var cacheKey = $"{templateFullPath}_{DateTime.UtcNow.Ticks}";

            var html = await engine.CompileRenderStringAsync(cacheKey, templateContent, model);

            return new RenderResult { Success = true, Html = html };
        }
        catch (Exception ex)
        {
            var errorHtml = $"<pre style=\"color:#e74c3c;white-space:pre-wrap;font-family:monospace;padding:1rem;\">{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre>";
            return new RenderResult { Success = false, Html = errorHtml, Error = ex.Message };
        }
    }

    private static JObject BuildModel(List<TemplateField> fields)
    {
        var root = new JObject();

        foreach (var field in fields)
        {
            var segments = field.Path.Split('.');
            var current = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var isLast = i == segments.Length - 1;

                if (isLast)
                {
                    current[segment] = ToJToken(ParseFieldValue(field));
                }
                else
                {
                    if (current[segment] is JObject existingObject)
                    {
                        current = existingObject;
                    }
                    else
                    {
                        var nested = new JObject();
                        current[segment] = nested;
                        current = nested;
                    }
                }
            }
        }

        return root;
    }

    private static void NormalizeModelForTemplate(string templateFullPath, JObject root)
    {
        if (!templateFullPath.EndsWith(Path.Combine("LotusOne", "ClientVendorEmail.cshtml"), StringComparison.OrdinalIgnoreCase))
            return;

        var header = EnsureObject(root, "Header");
        EnsureBoolean(header, "IncludeShiftsLogo", false);
        EnsureBoolean(header, "IncludeLotusOneLogo", true);

        EnsureString(root, "ImageBaseUrl", "https://example.com/assets/");

        var body = EnsureObject(root, "Body");
        EnsureString(body, "Title", "Client Vendor Email");
        EnsureString(body, "Date", "March 18, 2026");
        EnsureString(body, "Description", "Sample summary for the recipient.");
        EnsureString(body, "LowerDescription", "Additional information for the vendor appears here.");

        var signature = EnsureObject(body, "Signature");
        EnsureString(signature, "Name", "Aya Healthcare");
        EnsureString(signature, "Info", "LotusOne Team<br />support@example.com");

        var tables = EnsureArray(body, "Tables");
        if (tables.Count == 0)
        {
            tables.Add(CreateDefaultTable());
        }
        else
        {
            foreach (var item in tables.OfType<JObject>())
            {
                NormalizeTable(item);
            }
        }

        var buttons = EnsureArray(body, "Buttons");
        if (buttons.Count == 0)
        {
            buttons.Add(CreateDefaultButton());
        }
        else
        {
            foreach (var item in buttons.OfType<JObject>())
            {
                NormalizeButton(item);
            }
        }

        var footer = EnsureObject(root, "Footer");
        EnsureBoolean(footer, "ShowQuestions", true);
        EnsureBoolean(footer, "ShowAutomatedNotification", true);
        EnsureBoolean(footer, "ShowCopyright", true);
        EnsureString(footer, "QuestionsText", "Contact your LotusOne representative for assistance.");
    }

    private static void NormalizeTable(JObject table)
    {
        EnsureString(table, "Title", "Summary");
        EnsureObject(table, "Data");
        EnsureObject(table, "SectionedData");
        EnsureArray(table, "CardSection");
        EnsureObject(table, "GridTable");
    }

    private static void NormalizeButton(JObject button)
    {
        EnsureString(button, "Text", "Open Details");
        EnsureString(button, "Url", "https://example.com/details");
        EnsureString(button, "BackgroundColor", "#5F7D8C");
        EnsureString(button, "TextColor", "#FFFFFF");
        EnsureString(button, "BorderColor", "#5F7D8C");
        EnsureString(button, "MsoWidth", "160px");
    }

    private static JObject CreateDefaultTable()
    {
        var table = new JObject();
        NormalizeTable(table);
        table["Data"] = new JObject
        {
            ["Client"] = "Contoso Health",
            ["Status"] = "Active"
        };
        return table;
    }

    private static JObject CreateDefaultButton()
    {
        var button = new JObject();
        NormalizeButton(button);
        return button;
    }

    private static JObject EnsureObject(JObject parent, string propertyName)
    {
        if (parent[propertyName] is JObject existing)
            return existing;

        var created = new JObject();
        parent[propertyName] = created;
        return created;
    }

    private static JArray EnsureArray(JObject parent, string propertyName)
    {
        if (parent[propertyName] is JArray existing)
            return existing;

        var created = new JArray();
        parent[propertyName] = created;
        return created;
    }

    private static void EnsureString(JObject parent, string propertyName, string defaultValue)
    {
        if (parent[propertyName] is JValue value && value.Type == JTokenType.String && !string.IsNullOrWhiteSpace(value.Value<string>()))
            return;

        if (parent[propertyName] is not null && parent[propertyName]!.Type != JTokenType.Null && parent[propertyName]!.Type != JTokenType.String)
            return;

        parent[propertyName] = defaultValue;
    }

    private static void EnsureBoolean(JObject parent, string propertyName, bool defaultValue)
    {
        if (parent[propertyName] is JValue value)
        {
            if (value.Type == JTokenType.Boolean)
                return;

            if (value.Type == JTokenType.String && bool.TryParse(value.Value<string>(), out var parsed))
            {
                parent[propertyName] = parsed;
                return;
            }
        }

        parent[propertyName] = defaultValue;
    }

    private static JToken ToJToken(object? value)
    {
        return value switch
        {
            null => JValue.CreateNull(),
            JToken token => token,
            _ => JToken.FromObject(value)
        };
    }

    private static object? ParseFieldValue(TemplateField field)
    {
        if (field.IsCollection)
            return ParseCollectionValue(field.Value);

        return field.ValueType switch
        {
            TemplateValueType.Boolean => ParseBooleanValue(field.Value),
            TemplateValueType.Integer => ParseIntegerValue(field.Value),
            TemplateValueType.Decimal => ParseDecimalValue(field.Value),
            TemplateValueType.Date => ParseDateValue(field.Value),
            TemplateValueType.DateTime => ParseDateTimeValue(field.Value),
            TemplateValueType.Json => ParseJsonValue(field.Value),
            _ => ParseScalarValue(field.Value)
        };
    }

    private static object? ParseBooleanValue(string value)
    {
        return bool.TryParse(value, out var parsed) ? parsed : ParseScalarValue(value);
    }

    private static object? ParseIntegerValue(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longParsed))
            return longParsed;

        return ParseScalarValue(value);
    }

    private static object? ParseDecimalValue(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : ParseScalarValue(value);
    }

    private static object? ParseDateValue(string value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.Date
            : ParseScalarValue(value);
    }

    private static object? ParseDateTimeValue(string value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : ParseScalarValue(value);
    }

    private static object? ParseJsonValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        try
        {
            return JToken.Parse(value);
        }
        catch
        {
            return ParseScalarValue(value);
        }
    }

    private static object ParseCollectionValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new JArray();

        var trimmed = value.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                return JArray.Parse(trimmed);
            }
            catch
            {
                return new List<object> { value };
            }
        }

        return new List<object> { value };
    }

    private static object? ParseScalarValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var trimmed = value.Trim();

        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                return JToken.Parse(trimmed);
            }
            catch
            {
                // Fall back to heuristic parsing below.
            }
        }

        if (bool.TryParse(trimmed, out var boolValue))
            return boolValue;

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return intValue;

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            return longValue;

        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
            return decimalValue;

        return value;
    }

    private sealed class TemplateDynamicValue : DynamicObject, IEnumerable
    {
        private readonly JToken _token;

        public TemplateDynamicValue(JToken token)
        {
            _token = token;
        }

        public int Count => _token switch
        {
            JArray array => array.Count,
            JObject obj => obj.Count,
            _ => 0
        };

        public T? ToObject<T>()
        {
            return _token.ToObject<T>();
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (binder.Name == "ToObject")
            {
                // Extract generic type arguments from the C# runtime binder via reflection
                var typeArgs = binder.GetType()
                    .GetProperty("TypeArguments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(binder) as IList<Type>;

                if (typeArgs is { Count: 1 })
                {
                    result = _token.ToObject(typeArgs[0]);
                    return true;
                }

                // Non-generic ToObject(Type) overload
                if (args is { Length: 1 } && args[0] is Type targetType)
                {
                    result = _token.ToObject(targetType);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder.Name == nameof(Count))
            {
                result = Count;
                return true;
            }

            if (_token is JObject obj && obj.TryGetValue(binder.Name, StringComparison.OrdinalIgnoreCase, out var value))
            {
                result = Wrap(value);
                return true;
            }

            result = null;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            if (indexes.Length == 1)
            {
                if (_token is JObject obj && indexes[0] is string key)
                {
                    result = Wrap(obj[key]);
                    return true;
                }

                if (_token is JArray array && indexes[0] is int index && index >= 0 && index < array.Count)
                {
                    result = Wrap(array[index]);
                    return true;
                }
            }

            result = null;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            var targetType = binder.Type;

            if (_token.Type == JTokenType.Null)
            {
                result = null;
                return true;
            }

            try
            {
                if (targetType == typeof(string))
                {
                    result = _token.Type == JTokenType.String
                        ? _token.Value<string>()
                        : _token.ToString();
                    return true;
                }

                result = _token.ToObject(targetType);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public IEnumerator GetEnumerator()
        {
            if (_token is JArray array)
            {
                foreach (var item in array)
                {
                    yield return Wrap(item);
                }
            }
        }

        public override string ToString()
        {
            return _token.Type == JTokenType.String
                ? _token.Value<string>() ?? string.Empty
                : _token.ToString();
        }

        private static object? Wrap(JToken? token)
        {
            if (token is null || token.Type == JTokenType.Null)
                return null;

            return token switch
            {
                JValue value => value.Value,
                JObject or JArray => new TemplateDynamicValue(token),
                _ => token.ToObject<object?>()
            };
        }
    }
}
