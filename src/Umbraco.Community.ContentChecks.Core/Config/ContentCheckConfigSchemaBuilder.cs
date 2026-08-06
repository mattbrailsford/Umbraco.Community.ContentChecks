using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// Default implementation of <see cref="IContentCheckConfigSchemaBuilder"/>. Trimmed port of
/// Umbraco.AI's <c>AIEditableModelSchemaBuilder</c>: drops the sensitive-field/masked-editor
/// inference and the app-settings config-variable substitution this package has no use for (no
/// check configuration is a secret — an AI-judged check references an Umbraco.AI profile by
/// alias; the profile owns the credential).
/// </summary>
internal sealed class ContentCheckConfigSchemaBuilder : IContentCheckConfigSchemaBuilder
{
    /// <inheritdoc />
    public ContentCheckConfigSchema BuildForType<TConfig>(string checkAlias)
        where TConfig : class
        => BuildForType(typeof(TConfig), checkAlias);

    /// <inheritdoc />
    public ContentCheckConfigSchema BuildForType(Type configType, string checkAlias)
    {
        var properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var configInstance = Activator.CreateInstance(configType);

        var fields = properties
            .Select(property => BuildFieldForProperty(property, checkAlias, configInstance))
            .ToList();

        return new ContentCheckConfigSchema(configType, fields);
    }

    private static ContentCheckField BuildFieldForProperty(PropertyInfo property, string checkAlias, object? configInstance)
    {
        var attr = property.GetCustomAttribute<ContentCheckFieldAttribute>();
        var key = ToCamelCase(property.Name);
        var checkKey = ToCamelCase(checkAlias);

        object? defaultValue = null;
        if (configInstance is not null && property.CanRead)
        {
            try
            {
                defaultValue = property.GetValue(configInstance);
            }
            catch
            {
                // If we can't read the property value, just leave it as null.
            }
        }

        return new ContentCheckField
        {
            Key = key,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label ?? $"#ccFields_{checkKey}{property.Name}Label",
            Description = attr?.Description ?? $"#ccFields_{checkKey}{property.Name}Description",
            EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType),
            EditorConfig = attr?.EditorConfig is { Length: > 0 } editorConfig
                ? JsonSerializer.Deserialize<JsonElement>(editorConfig, JsonSerializerOptions.Web)
                : null,
            DefaultValue = defaultValue,
            ValidationRules = InferValidationAttributes(property),
            SortOrder = attr?.SortOrder ?? 0,
            Group = string.IsNullOrWhiteSpace(attr?.Group)
                ? null
                : attr.Group.StartsWith('#') ? attr.Group : $"#ccFieldGroups_{ToCamelCase(attr.Group)}Label",
        };
    }

    /// <summary>
    /// Picks the editor to render a field with when its <see cref="ContentCheckFieldAttribute"/>
    /// does not name one explicitly.
    /// </summary>
    private static string InferEditorUiAlias(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
        {
            return "Umb.PropertyEditorUi.TextBox";
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(long))
        {
            return "Umb.PropertyEditorUi.Integer";
        }

        if (underlyingType == typeof(bool))
        {
            return "Umb.PropertyEditorUi.Toggle";
        }

        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
        {
            return "Umb.PropertyEditorUi.Decimal";
        }

        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            return "Umb.PropertyEditorUi.DatePicker";
        }

        return "Umb.PropertyEditorUi.TextBox";
    }

    private static IEnumerable<ValidationAttribute> InferValidationAttributes(PropertyInfo property)
    {
        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>().ToList();

        // Non-nullable reference/value types get an implicit Required, unless already declared.
        // Value types are skipped — they always have a default value, so Required adds nothing.
        if (!IsNullable(property)
            && !property.PropertyType.IsValueType
            && validationAttributes.OfType<RequiredAttribute>().Any() is false)
        {
            validationAttributes.Add(new RequiredAttribute());
        }

        return validationAttributes;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return true;
        }

        if (property.PropertyType.IsValueType)
        {
            return false;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        return nullability.WriteState == NullabilityState.Nullable;
    }

    private static string ToCamelCase(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
