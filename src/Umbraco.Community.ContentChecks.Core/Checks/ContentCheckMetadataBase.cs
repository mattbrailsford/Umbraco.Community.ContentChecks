using System.Reflection;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks;

/// <summary>
/// The shared foundation under both <see cref="PropertyContentCheckBase{TConfig}"/> and
/// <see cref="Markup.MarkupContentCheckBase{TConfig}"/> — the attribute-reading/lazy-schema/
/// <see cref="ResolveConfig"/> plumbing has nothing to do with property-vs-markup. Split out of
/// what used to be a single <c>ContentCheckBase&lt;TConfig&gt;</c> once a markup branch needed
/// the same plumbing under a different <c>EvaluateAsync</c> shape
/// (docs/markup-checks-exploration.md §4). Direct port of Umbraco.AI's
/// <c>AIGuardrailEvaluatorBase&lt;TConfig&gt;</c>, same as before this split.
/// </summary>
/// <typeparam name="TConfig">The configuration type for this check.</typeparam>
public abstract class ContentCheckMetadataBase<TConfig> : IContentCheck
{
    private readonly Lazy<ContentCheckConfigSchema?> _configSchema;
    private readonly IContentCheckConfigResolver _configResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckMetadataBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown if the derived class is missing the required attribute.</exception>
    protected ContentCheckMetadataBase(IContentCheckInfrastructure infrastructure)
    {
        SchemaBuilder = infrastructure.SchemaBuilder;
        _configResolver = infrastructure.ConfigResolver;

        var attribute = GetType().GetCustomAttribute<ContentCheckAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"The check '{GetType().FullName}' is missing the required {nameof(ContentCheckAttribute)}.");

        Alias = attribute.Alias;
        Name = attribute.Name;
        Cost = attribute.Cost;
        Kind = attribute.Kind;

        _configSchema = new Lazy<ContentCheckConfigSchema?>(
            () => ConfigType is not null ? SchemaBuilder.BuildForType(ConfigType, Alias) : null);
    }

    /// <inheritdoc />
    public string Alias { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public ContentCheckCost Cost { get; }

    /// <inheritdoc />
    public ContentCheckKind Kind { get; }

    /// <inheritdoc />
    public Type? ConfigType => typeof(TConfig);

    /// <summary>
    /// The schema builder for generating UI schemas.
    /// </summary>
    protected IContentCheckConfigSchemaBuilder SchemaBuilder { get; }

    /// <inheritdoc />
    public ContentCheckConfigSchema? GetConfigSchema() => _configSchema.Value;

    /// <summary>
    /// Resolves the strongly-typed check configuration from the supplied <see cref="ContentCheckConfig"/>,
    /// validating it against the schema through the config resolver.
    /// </summary>
    /// <param name="config">The check's resolved configuration wrapper.</param>
    /// <returns>The resolved configuration, or <c>default</c> if none is present.</returns>
    protected TConfig? ResolveConfig(ContentCheckConfig config)
    {
        if (config.Config is not { } configElement)
        {
            return default;
        }

        return (TConfig?)_configResolver.ResolveModel(typeof(TConfig), configElement, GetConfigSchema());
    }

    /// <summary>
    /// Builds the default <typeparamref name="TConfig"/> instance — the same instance the
    /// config schema reads its per-field default values from, so an unconfigured rule behaves
    /// exactly as its schema already advertises, not some separately-invented fallback.
    /// </summary>
    protected static TConfig CreateDefaultConfig()
        => (TConfig)Activator.CreateInstance(typeof(TConfig))!;
}
