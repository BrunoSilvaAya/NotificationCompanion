using System.Globalization;

namespace EmailCompanion.Services;

public class TemplateSchemaService
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateFieldDefinition>> _schemasByModelType =
        new Dictionary<string, IReadOnlyDictionary<string, TemplateFieldDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Aya.Core.Dto.Contracts.ContractRequestEmailInfoDto"] = CreateFieldMap(
            [
                new TemplateFieldDefinition { Path = "Type", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "NurseName", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "StatusName", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "NurseContractsNotes", ValueType = TemplateValueType.MultilineText },
                new TemplateFieldDefinition { Path = "DraftedBy", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "FacilityName", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "DatesText", ValueType = TemplateValueType.MultilineText },
                new TemplateFieldDefinition { Path = "RequestId", ValueType = TemplateValueType.Integer },
                new TemplateFieldDefinition { Path = "Note", ValueType = TemplateValueType.MultilineText }
            ]),
            ["Aya.Core.DTO.Travelers.TravelerExtensionRequestNotificationTemplateDto"] = CreateFieldMap(
            [
                new TemplateFieldDefinition { Path = "Facility", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "WorkerName", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "JobDetailsUrl", ValueType = TemplateValueType.Url },
                new TemplateFieldDefinition { Path = "JobId", ValueType = TemplateValueType.Integer },
                new TemplateFieldDefinition { Path = "Dates", ValueType = TemplateValueType.MultilineText },
                new TemplateFieldDefinition { Path = "Profession", ValueType = TemplateValueType.Text },
                new TemplateFieldDefinition { Path = "Specialty", ValueType = TemplateValueType.Text }
            ]),
            ["Aya.Core.DTO.Offer.ContractsTeamCandidateOfferEmailInfoDto"] = CreateFieldMap(
            [
                new TemplateFieldDefinition { Path = "Id", ValueType = TemplateValueType.Integer }
            ])
        };

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, TemplateFieldDefinition>> _schemasByTemplatePath =
        new Dictionary<string, IReadOnlyDictionary<string, TemplateFieldDefinition>>(StringComparer.OrdinalIgnoreCase)
        {
            ["LotusOne\\ClientVendorEmail.cshtml"] = CreateFieldMap(
            [
                new TemplateFieldDefinition
                {
                    Path = "ImageBaseUrl",
                    ValueType = TemplateValueType.Url,
                    SampleValue = "https://example.com/assets/"
                },
                new TemplateFieldDefinition { Path = "Header.IncludeShiftsLogo", ValueType = TemplateValueType.Boolean, SampleValue = "false" },
                new TemplateFieldDefinition { Path = "Header.IncludeLotusOneLogo", ValueType = TemplateValueType.Boolean, SampleValue = "true" },
                new TemplateFieldDefinition { Path = "Body.Title", ValueType = TemplateValueType.Text, SampleValue = "Client Vendor Email" },
                new TemplateFieldDefinition { Path = "Body.Date", ValueType = TemplateValueType.Text, SampleValue = "March 18, 2026" },
                new TemplateFieldDefinition { Path = "Body.Description", ValueType = TemplateValueType.MultilineText, SampleValue = "Sample summary for the recipient." },
                new TemplateFieldDefinition
                {
                    Path = "Body.Tables",
                    ValueType = TemplateValueType.Collection,
                    SampleValue = "[\n  {\n    \"Title\": \"Summary\",\n    \"Data\": {\n      \"Client\": \"Contoso Health\",\n      \"Status\": \"Active\"\n    },\n    \"SectionedData\": {},\n    \"CardSection\": [],\n    \"GridTable\": {}\n  }\n]"
                },
                new TemplateFieldDefinition
                {
                    Path = "Body.Buttons",
                    ValueType = TemplateValueType.Collection,
                    SampleValue = "[\n  {\n    \"Text\": \"Open Details\",\n    \"Url\": \"https://example.com/details\",\n    \"BackgroundColor\": \"#5F7D8C\",\n    \"TextColor\": \"#FFFFFF\",\n    \"BorderColor\": \"#5F7D8C\",\n    \"MsoWidth\": \"160px\"\n  }\n]"
                },
                new TemplateFieldDefinition { Path = "Body.LowerDescription", ValueType = TemplateValueType.MultilineText, SampleValue = "Additional information for the vendor appears here." },
                new TemplateFieldDefinition { Path = "Body.Signature.Name", ValueType = TemplateValueType.Text, SampleValue = "Aya Healthcare" },
                new TemplateFieldDefinition { Path = "Body.Signature.Info", ValueType = TemplateValueType.MultilineText, SampleValue = "LotusOne Team<br />support@example.com" },
                new TemplateFieldDefinition { Path = "Footer.ShowQuestions", ValueType = TemplateValueType.Boolean, SampleValue = "true" },
                new TemplateFieldDefinition { Path = "Footer.QuestionsText", ValueType = TemplateValueType.MultilineText, SampleValue = "Contact your LotusOne representative for assistance." },
                new TemplateFieldDefinition { Path = "Footer.ShowAutomatedNotification", ValueType = TemplateValueType.Boolean, SampleValue = "true" },
                new TemplateFieldDefinition { Path = "Footer.ShowCopyright", ValueType = TemplateValueType.Boolean, SampleValue = "true" }
            ])
        };

    public TemplateDescriptor Describe(string relativePath, string templateContent)
    {
        var declaredModelType = TemplateParserService.ExtractDeclaredModelType(templateContent);
        var parsedFields = TemplateParserService.Parse(templateContent);
        var schema = ResolveSchema(relativePath, declaredModelType);

        var fields = MergeFields(parsedFields, schema);
        return new TemplateDescriptor
        {
            DeclaredModelType = declaredModelType,
            Fields = fields
        };
    }

    public string CreateSampleValue(TemplateField field)
    {
        if (!string.IsNullOrWhiteSpace(field.SampleValue))
            return field.SampleValue;

        var lastSegment = field.Path.Contains('.')
            ? field.Path[(field.Path.LastIndexOf('.') + 1)..]
            : field.Path;

        return field.ValueType switch
        {
            TemplateValueType.Boolean => "true",
            TemplateValueType.Integer => lastSegment.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ? "12345" : "42",
            TemplateValueType.Decimal => "123.45",
            TemplateValueType.Email => "sample@example.com",
            TemplateValueType.Url => "https://example.com/details",
            TemplateValueType.Date => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TemplateValueType.DateTime => DateTime.Today.AddHours(9).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            TemplateValueType.Json => "{\n  \"sample\": true\n}",
            TemplateValueType.Collection => "[\n  {\n    \"name\": \"Sample item\",\n    \"value\": \"Sample value\"\n  }\n]",
            TemplateValueType.MultilineText => $"Sample {lastSegment}",
            _ => $"Sample {lastSegment}"
        };
    }

    public string GetPlaceholder(TemplateField field)
    {
        if (!string.IsNullOrWhiteSpace(field.Placeholder))
            return field.Placeholder;

        return field.ValueType switch
        {
            TemplateValueType.Boolean => "true or false",
            TemplateValueType.Integer => "42",
            TemplateValueType.Decimal => "123.45",
            TemplateValueType.Email => "sample@example.com",
            TemplateValueType.Url => "https://example.com",
            TemplateValueType.Date => "yyyy-MM-dd",
            TemplateValueType.DateTime => "yyyy-MM-ddTHH:mm",
            TemplateValueType.Json => "{ \"sample\": true }",
            TemplateValueType.Collection => "[{ \"name\": \"Sample\" }]",
            _ => ""
        };
    }

    private IReadOnlyDictionary<string, TemplateFieldDefinition> ResolveSchema(string relativePath, string? declaredModelType)
    {
        var templateKey = relativePath.Replace('/', '\\');
        var merged = new Dictionary<string, TemplateFieldDefinition>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(declaredModelType) &&
            _schemasByModelType.TryGetValue(declaredModelType, out var modelSchema))
        {
            foreach (var entry in modelSchema)
            {
                merged[entry.Key] = entry.Value;
            }
        }

        if (_schemasByTemplatePath.TryGetValue(templateKey, out var templateSchema))
        {
            foreach (var entry in templateSchema)
            {
                merged[entry.Key] = entry.Value;
            }
        }

        return merged;
    }

    private List<TemplateField> MergeFields(
        IEnumerable<TemplateField> parsedFields,
        IReadOnlyDictionary<string, TemplateFieldDefinition> schema)
    {
        var merged = new Dictionary<string, TemplateField>(StringComparer.OrdinalIgnoreCase);

        foreach (var parsedField in parsedFields)
        {
            var field = CloneField(parsedField);
            ApplyDefinition(field, schema.GetValueOrDefault(field.Path));
            merged[field.Path] = field;
        }

        foreach (var definition in schema.Values)
        {
            if (merged.ContainsKey(definition.Path))
                continue;

            var field = new TemplateField
            {
                Path = definition.Path,
                DisplayName = definition.Path.Replace('.', ' ')
                    .Replace("  ", " ")
                    .Trim()
                    .Replace(" ", " > "),
                ValueType = definition.ValueType,
                Placeholder = definition.Placeholder,
                SampleValue = definition.SampleValue
            };
            merged[field.Path] = field;
        }

        return merged.Values
            .OrderBy(field => field.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static TemplateField CloneField(TemplateField source)
    {
        return new TemplateField
        {
            Path = source.Path,
            DisplayName = source.DisplayName,
            Value = source.Value,
            IsCollection = source.IsCollection,
            IsConditional = source.IsConditional,
            HasStringCast = source.HasStringCast,
            ValueType = source.ValueType,
            Placeholder = source.Placeholder,
            SampleValue = source.SampleValue
        };
    }

    private void ApplyDefinition(TemplateField field, TemplateFieldDefinition? definition)
    {
        if (field.IsCollection)
            field.ValueType = TemplateValueType.Collection;

        if (definition is not null)
        {
            field.ValueType = field.IsCollection ? TemplateValueType.Collection : definition.ValueType;
            field.Placeholder = definition.Placeholder;
            field.SampleValue = definition.SampleValue;
        }
        else if (!field.IsCollection)
        {
            field.ValueType = InferValueType(field);
        }

        field.Placeholder ??= GetPlaceholder(field);
        field.SampleValue ??= CreateSampleValue(field);
    }

    private static TemplateValueType InferValueType(TemplateField field)
    {
        if (field.IsCollection)
            return TemplateValueType.Collection;

        var lastSegment = field.Path.Contains('.')
            ? field.Path[(field.Path.LastIndexOf('.') + 1)..]
            : field.Path;

        if (field.HasStringCast)
            return IsMultilineName(lastSegment) ? TemplateValueType.MultilineText : TemplateValueType.Text;

        if (IsBooleanName(lastSegment) || field.IsConditional)
            return TemplateValueType.Boolean;

        if (lastSegment.Contains("Email", StringComparison.OrdinalIgnoreCase))
            return TemplateValueType.Email;

        if (lastSegment.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Uri", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Link", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateValueType.Url;
        }

        if (lastSegment.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.EndsWith("At", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.EndsWith("Utc", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Time", StringComparison.OrdinalIgnoreCase))
        {
            return lastSegment.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
                   lastSegment.EndsWith("At", StringComparison.OrdinalIgnoreCase) ||
                   lastSegment.EndsWith("Utc", StringComparison.OrdinalIgnoreCase)
                ? TemplateValueType.DateTime
                : TemplateValueType.Date;
        }

        if (lastSegment.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.EndsWith("Count", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.EndsWith("Number", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.EndsWith("Port", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateValueType.Integer;
        }

        if (lastSegment.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Rate", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
            lastSegment.Contains("Total", StringComparison.OrdinalIgnoreCase))
        {
            return TemplateValueType.Decimal;
        }

        if (IsMultilineName(lastSegment))
        {
            return TemplateValueType.MultilineText;
        }

        return TemplateValueType.Text;
    }

    private static bool IsBooleanName(string name)
    {
        return name.StartsWith("Is", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Has", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Can", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Should", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Include", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Allow", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Show", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Use", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Was", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("Were", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMultilineName(string name)
    {
        return name.Contains("Note", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Message", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Description", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Body", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Text", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Info", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, TemplateFieldDefinition> CreateFieldMap(IEnumerable<TemplateFieldDefinition> definitions)
    {
        return definitions.ToDictionary(definition => definition.Path, StringComparer.OrdinalIgnoreCase);
    }
}