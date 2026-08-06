using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.Core.Checks.Examples;

/// <summary>
/// Configuration for <see cref="StubRubricCheck"/>.
/// </summary>
public class StubRubricCheckConfig
{
    /// <summary>
    /// Free-text description of what good content looks like.
    /// </summary>
    [ContentCheckField(
        Label = "Rubric",
        Description = "Free-text description of what good content looks like.",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string Rubric { get; set; } = string.Empty;

    /// <summary>
    /// The minimum score required to pass, from 0 to 1.
    /// </summary>
    [ContentCheckField(
        Label = "Threshold",
        Description = "Minimum score to pass, from 0 to 1.",
        SortOrder = 2)]
    public double Threshold { get; set; } = 0.7;
}

/// <summary>
/// Proving check B (<c>Metered</c> + <c>Nondeterministic</c>), throwaway: a stub rubric check
/// with a hand-rolled fake judge that returns a score. Exists purely to prove the contract
/// survives scores, thresholds, and timing with no Umbraco.AI dependency — the fake "judge"
/// here scores by rubric text length, which is meaningless as an actual rubric. Delete this once
/// AI Integration ships a real one.
/// </summary>
[ContentCheck("stub-rubric", "Stub rubric (throwaway)", Cost = ContentCheckCost.Metered, Kind = ContentCheckKind.Nondeterministic)]
public sealed class StubRubricCheck : ContentCheckBase<StubRubricCheckConfig>
{
    /// <inheritdoc />
    public override string Description =>
        "Throwaway stand-in for a real AI-judged rubric check. Delete once AI Integration ships one.";

    /// <summary>
    /// Initializes a new instance of the <see cref="StubRubricCheck"/> class.
    /// </summary>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    public StubRubricCheck(IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
    {
    }

    /// <inheritdoc />
    protected override async Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        StubRubricCheckConfig config,
        CancellationToken cancellationToken)
    {
        // Stands in for "an LLM call happened" -- a real judge would call out to an Umbraco.AI
        // profile here instead, which is exactly why this check is Metered + Nondeterministic.
        await Task.Delay(1, cancellationToken);
        var score = Math.Clamp(config.Rubric.Length / 100.0, 0, 1);

        return new ContentCheckResult
        {
            Status = score >= config.Threshold ? ContentCheckStatus.Pass : ContentCheckStatus.Fail,
            Message = $"Stub judge scored {score:0.00} against a threshold of {config.Threshold:0.00}.",
            Score = score,
            Threshold = config.Threshold,
        };
    }
}
