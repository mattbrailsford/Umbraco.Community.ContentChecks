using Umbraco.AI.Core.Tools;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.AI.Tools;

/// <summary>
/// Tool that lists every configured content-check policy, so an agent can discover what will be
/// checked before it starts generating or editing content -- without needing arguments, since
/// the full policy list is the point.
/// </summary>
[AITool("list_content_check_policies", "List Content Check Policies", ScopeId = ContentChecksToolScope.ScopeId)]
public sealed class ListContentCheckPoliciesTool : AIToolBase
{
    private readonly IContentCheckPolicyService _policyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListContentCheckPoliciesTool"/> class.
    /// </summary>
    public ListContentCheckPoliciesTool(IContentCheckPolicyService policyService)
        => _policyService = policyService;

    /// <inheritdoc />
    public override string Description =>
        "Lists every configured content-check policy -- which content type each applies to, and " +
        "the rules it runs. Use this to learn what will be checked before generating or editing " +
        "content, so you can aim for a pass on the first run_content_checks call rather than " +
        "discovering the rules by failing them.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var policies = await _policyService.GetAllPoliciesAsync(cancellationToken);

        return policies
            .Select(policy => new ContentCheckPolicySummary(
                Key: policy.Key,
                Alias: policy.Alias,
                Name: policy.Name,
                ContentTypeKey: policy.ContentTypeKey,
                Rules: policy.Rules
                    .Select(rule => new ContentCheckPolicyRuleSummary(
                        RuleKey: rule.Key,
                        CheckAlias: rule.CheckAlias,
                        Name: rule.Name,
                        RequirementLevel: rule.RequirementLevel.ToString()))
                    .ToList()))
            .ToList();
    }
}

/// <summary>
/// One policy, as returned by the list_content_check_policies tool.
/// </summary>
public record ContentCheckPolicySummary(
    Guid Key,
    string Alias,
    string Name,
    Guid ContentTypeKey,
    IReadOnlyList<ContentCheckPolicyRuleSummary> Rules);

/// <summary>
/// One rule within a policy, as returned by the list_content_check_policies tool.
/// </summary>
public record ContentCheckPolicyRuleSummary(
    Guid RuleKey,
    string CheckAlias,
    string? Name,
    string RequirementLevel);
