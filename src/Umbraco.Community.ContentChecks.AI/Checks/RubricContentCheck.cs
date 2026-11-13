#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental -- see docs/ai-integration-spec.md.
using Umbraco.AI.Core.Decision;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Config;

namespace Umbraco.Community.ContentChecks.AI.Checks;

/// <summary>
/// Configuration for <see cref="RubricContentCheck"/>.
/// </summary>
public class RubricContentCheckConfig
{
    /// <summary>
    /// The alias of the property whose value is judged against <see cref="Rubric"/>.
    /// </summary>
    [ContentCheckField(
        Label = "Property alias",
        Description = "The property whose value is judged against the rubric.",
        SortOrder = 1)]
    public string PropertyAlias { get; set; } = string.Empty;

    /// <summary>
    /// Free-text description of what good content looks like. Deliberately free text, not a
    /// guided/weighted-criteria form — see docs/ai-integration-spec.md's build checklist.
    /// </summary>
    [ContentCheckField(
        Label = "Rubric",
        Description = "Free-text description of what good content looks like.",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":6}]",
        SortOrder = 2)]
    public string Rubric { get; set; } = string.Empty;

    /// <summary>
    /// The minimum score, from 0 to 1, required to pass.
    /// </summary>
    [ContentCheckField(
        Label = "Threshold",
        Description = "Minimum score to pass, from 0 to 1.",
        SortOrder = 3)]
    public double Threshold { get; set; } = 0.7;

    /// <summary>
    /// The Umbraco.AI profile to judge with. <c>null</c> uses the default decision profile.
    /// </summary>
    /// <remarks>
    /// Deliberately configured separately from any profile used to generate the content being
    /// judged (finding G1, docs/overview.md) — the UI should warn if the two resolve to the same
    /// profile, an AI marking its own homework.
    /// </remarks>
    [ContentCheckField(
        Label = "Judge profile",
        Description = "The AI profile to use as the judge. Leave empty to use the default decision profile. Use a different profile than whatever generated this content.",
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        SortOrder = 4)]
    public Guid? ProfileId { get; set; }
}

/// <summary>
/// The real AI-judged check (<c>Metered</c> + <c>Nondeterministic</c>): a free-text rubric judged
/// by an Umbraco.AI profile via the (experimental) Decision API, returning a calibrated pass
/// probability and a pass/fail verdict against a configured threshold. Replaced the throwaway
/// <c>StubRubricCheck</c> proving check, since deleted — see docs/ai-integration-spec.md.
/// </summary>
/// <remarks>
/// Deliberately mirrors Umbraco.AI's own <c>DecisionGuardrailEvaluator</c>/<c>DecisionJudgeGrader</c>
/// pattern rather than a hand-rolled chat prompt: a fixed <c>TrueCriteria</c>/<c>FalseCriteria</c>
/// pair (never admin-editable, so "yes" always means "passes" regardless of how the admin's own
/// rubric happens to be worded), the rubric itself only ever appears inside <c>Instructions</c>,
/// and the content being judged is passed as <c>state</c>. The trade-off is that the Decision API
/// returns a calibrated probability, not free text, so there's no judge reasoning or per-target
/// concerns to surface to an editor here — the Message is a generic score-vs-threshold line.
/// </remarks>
[ContentCheck("rubric", "Rubric (AI-judged)", Cost = ContentCheckCost.Metered, Kind = ContentCheckKind.Nondeterministic)]
public sealed class RubricContentCheck : ContentCheckBase<RubricContentCheckConfig>
{
    private readonly IAIDecisionService _decisionService;

    /// <inheritdoc />
    public override string Description =>
        "Judges a property's value against a free-text rubric using an AI profile, returning a pass/fail score.";

    /// <summary>
    /// Initializes a new instance of the <see cref="RubricContentCheck"/> class.
    /// </summary>
    /// <param name="decisionService">Used to put the judge question to the configured AI profile.</param>
    /// <param name="infrastructure">The check infrastructure dependencies.</param>
    public RubricContentCheck(IAIDecisionService decisionService, IContentCheckInfrastructure infrastructure)
        : base(infrastructure)
        => _decisionService = decisionService;

    /// <inheritdoc />
    protected override async Task<ContentCheckResult> EvaluateAsync(
        ContentCheckContext context,
        RubricContentCheckConfig config,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.PropertyAlias) || string.IsNullOrWhiteSpace(config.Rubric))
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.NotApplicable,
                Message = "No property alias or rubric configured.",
            };
        }

        var target = new ContentCheckTarget
        {
            PropertyAlias = config.PropertyAlias,
            Culture = context.Culture,
            Segment = context.Segment,
        };

        if (!context.Content.HasProperty(config.PropertyAlias))
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.NotApplicable,
                Message = $"This content type has no '{config.PropertyAlias}' property.",
                Targets = [target],
            };
        }

        var value = context.Content.GetValue<string>(config.PropertyAlias, context.Culture, context.Segment);
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.NotApplicable,
                Message = $"'{config.PropertyAlias}' has no value to judge.",
                Targets = [target],
            };
        }

        // TrueCriteria/FalseCriteria are fixed, never admin-editable -- the yes/no meaning must
        // not depend on how an admin words their rubric (admin-authored rubrics often lean toward
        // describing what's bad, which would otherwise make it ambiguous whether "yes" means
        // "passes" or "fails"). The rubric itself only ever appears inside Instructions. Mirrors
        // Umbraco.AI's own DecisionGuardrailEvaluator/DecisionJudgeGrader for the same reason.
        var question = new AIBinaryDecisionQuestion
        {
            Instructions = $"""
                You are judging one piece of website content (property '{config.PropertyAlias}')
                against this editorial rubric:

                {config.Rubric}
                """,
            TrueCriteria = "The content meets every criterion in the rubric.",
            FalseCriteria = "The content fails at least one criterion in the rubric.",
        };

        try
        {
            var response = await _decisionService.AskAsync(
                configure: builder =>
                {
                    builder.WithAlias("content-check-rubric-judge");

                    if (config.ProfileId is { } profileId)
                    {
                        builder.WithProfile(profileId);
                    }
                },
                question: question,
                state: value,
                cancellationToken: cancellationToken);

            var score = Math.Clamp(response.Answer.TrueProbability, 0, 1);

            return new ContentCheckResult
            {
                Status = score >= config.Threshold ? ContentCheckStatus.Pass : ContentCheckStatus.Fail,
                Message = $"Scored {score:0.00} against a threshold of {config.Threshold:0.00}.",
                Score = score,
                Threshold = config.Threshold,
                Targets = [target],
            };
        }
        catch (OperationCanceledException)
        {
            // A cancelled request isn't a judgment -- let it propagate rather than reporting a
            // passed/failed verdict, unlike the error handling below. Matches Umbraco.AI's own
            // DecisionGuardrailEvaluator/DecisionJudgeGrader.
            throw;
        }
        catch (Exception ex)
        {
            // Deliberately the opposite of Umbraco.AI's own Decision-judge evaluators, which fail
            // closed on error (safe default for chat safety). A provider outage here must never
            // present as a content failure, or an AI outage would block every publish behind this
            // rule -- see docs/core-contract-spec.md's "fail-open on error" note.
            return new ContentCheckResult
            {
                Status = ContentCheckStatus.NotEvaluated,
                NotEvaluatedReason = ContentCheckNotEvaluatedReason.Error,
                Message = $"Rubric judgment failed: {ex.Message}",
                Targets = [target],
            };
        }
    }
}
