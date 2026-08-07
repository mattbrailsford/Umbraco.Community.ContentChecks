using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Umbraco.Community.ContentChecks.Web.Api.Common.Configuration;

/// <summary>
/// Rewrites every enum's generated schema from a bare integer to a string with its real member
/// names. .NET's native OpenAPI schema generation doesn't look at the JSON serializer options this
/// API registers (<see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>, applied
/// via the named JSON options every controller uses), so left alone it advertises every
/// <c>ContentCheckStatus</c>/<c>ContentCheckRuleMode</c>/etc. field as a plain number — a schema
/// that doesn't match what the API actually sends or accepts. Umbraco.AI hit the identical gap on
/// its own (Swashbuckle-based) pipeline and fixed it the same way, for the same reason.
/// </summary>
internal sealed class ContentChecksEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (enumType.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            schema.Enum = Enum.GetNames(enumType).Select(name => (JsonNode)name).ToList();
        }

        return Task.CompletedTask;
    }
}
