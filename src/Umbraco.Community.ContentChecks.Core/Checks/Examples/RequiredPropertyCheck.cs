using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks.Examples;

/// <summary>
/// Configuration for <see cref="RequiredPropertyCheck"/>.
/// </summary>
public class RequiredPropertyCheckConfig
{
    /// <summary>
    /// The alias of the property that must have a value.
    /// </summary>
    [ContentCheckField(
        Label = "Property alias",
        Description = "The property that must have a value.",
        SortOrder = 1)]
    public string PropertyAlias { get; set; } = string.Empty;
}

/// <summary>
/// Proving check A (<c>Free</c> + <c>Deterministic</c>): fails when a configured property has no
/// value. Exercises <see cref="ContentCheckStatus.NotApplicable"/> (via <see cref="IContentCheckSnapshot.HasProperty"/>),
/// <see cref="ContentCheckStatus.Fail"/>/<see cref="ContentCheckStatus.Pass"/>, and <see cref="ContentCheckTarget"/>.
/// </summary>
[ContentCheck("required-property", "Required property")]
public sealed class RequiredPropertyCheck : PropertyContentCheckBase<RequiredPropertyCheckConfig>
{
    /// <inheritdoc />
    public override string Description => "Fails when a given property has no value.";

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredPropertyCheck"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    public RequiredPropertyCheck(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    protected override Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        RequiredPropertyCheckConfig config,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(config.PropertyAlias))
        {
            return Task.FromResult(new ContentCheckResult
            {
                Status = ContentCheckStatus.NotApplicable,
                Message = "No property alias configured.",
            });
        }

        var target = new ContentCheckTarget
        {
            PropertyAlias = config.PropertyAlias,
            Culture = context.Culture,
            Segment = context.Segment,
        };

        if (!context.Content.HasProperty(config.PropertyAlias))
        {
            return Task.FromResult(new ContentCheckResult
            {
                Status = ContentCheckStatus.NotApplicable,
                Message = $"This content type has no '{config.PropertyAlias}' property.",
                Targets = [target],
            });
        }

        var value = context.Content.GetValue<string>(config.PropertyAlias, context.Culture, context.Segment);

        return Task.FromResult(string.IsNullOrWhiteSpace(value)
            ? new ContentCheckResult
            {
                Status = ContentCheckStatus.Fail,
                Message = $"'{config.PropertyAlias}' has no value.",
                Targets = [target],
            }
            : new ContentCheckResult
            {
                Status = ContentCheckStatus.Pass,
                Message = $"'{config.PropertyAlias}' has a value.",
                Targets = [target],
            });
    }
}
