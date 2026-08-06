using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Umbraco.Community.ContentChecks.Core.Config;

/// <summary>
/// Default implementation of <see cref="IContentCheckConfigResolver"/>. Trimmed port of
/// Umbraco.AI's <c>AIEditableModelResolver</c>: JSON round-trip plus DataAnnotations validation
/// via the schema, with the app-settings config-variable substitution (<c>$Key:Path</c>) this
/// package has no use for cut entirely.
/// </summary>
internal sealed class ContentCheckConfigResolver : IContentCheckConfigResolver
{
    /// <inheritdoc />
    public TConfig? ResolveModel<TConfig>(object? data, ContentCheckConfigSchema? schema = null)
        where TConfig : class, new()
        => (TConfig?)ResolveModel(typeof(TConfig), data, schema);

    /// <inheritdoc />
    public object? ResolveModel(Type configType, object? data, ContentCheckConfigSchema? schema = null)
    {
        if (data is null)
        {
            return null;
        }

        object? deserialized = data is JsonElement jsonElement
            ? jsonElement.Deserialize(configType, JsonSerializerOptions.Web)
            : JsonSerializer.Deserialize(
                JsonSerializer.Serialize(data, JsonSerializerOptions.Web),
                configType,
                JsonSerializerOptions.Web);

        if (deserialized is not null)
        {
            ValidateModel(deserialized, schema);
        }

        return deserialized;
    }

    private static void ValidateModel(object model, ContentCheckConfigSchema? schema)
    {
        if (schema is null)
        {
            return;
        }

        var modelType = model.GetType();
        var validationErrors = new List<string>();

        foreach (var field in schema.Fields)
        {
            if (string.IsNullOrEmpty(field.PropertyName))
            {
                continue;
            }

            var property = modelType.GetProperty(field.PropertyName);
            if (property is null)
            {
                continue;
            }

            var value = property.GetValue(model);

            foreach (var validationRule in field.ValidationRules)
            {
                var validationContext = new ValidationContext(model)
                {
                    MemberName = field.PropertyName,
                    DisplayName = field.Label,
                };

                var validationResult = validationRule.GetValidationResult(value, validationContext);
                if (validationResult != ValidationResult.Success)
                {
                    validationErrors.Add(validationResult?.ErrorMessage ?? $"Validation failed for {field.Label}");
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Validation failed for config '{schema.Type.Name}':\n{string.Join('\n', validationErrors)}");
        }
    }
}
