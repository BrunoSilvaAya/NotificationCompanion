namespace EmailCompanion.Services;

public enum TemplateValueType
{
    Text,
    MultilineText,
    Email,
    Url,
    Integer,
    Decimal,
    Boolean,
    Date,
    DateTime,
    Json,
    Collection
}

public class TemplateField
{
    public string Path { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsCollection { get; set; }
    public bool IsConditional { get; set; }
    public bool HasStringCast { get; set; }
    public TemplateValueType ValueType { get; set; } = TemplateValueType.Text;
    public string? Placeholder { get; set; }
    public string? SampleValue { get; set; }

    public bool UsesTextArea =>
        IsCollection ||
        ValueType is TemplateValueType.MultilineText or TemplateValueType.Json or TemplateValueType.Collection;
}

public sealed class TemplateFieldDefinition
{
    public required string Path { get; init; }
    public TemplateValueType ValueType { get; init; } = TemplateValueType.Text;
    public string? Placeholder { get; init; }
    public string? SampleValue { get; init; }
}

public sealed class TemplateDescriptor
{
    public string? DeclaredModelType { get; init; }
    public List<TemplateField> Fields { get; init; } = [];
}