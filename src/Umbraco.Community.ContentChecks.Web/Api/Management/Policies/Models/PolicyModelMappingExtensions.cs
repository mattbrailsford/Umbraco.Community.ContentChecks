using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// Direct field-copy mapping between <see cref="ContentCheckPolicy"/> and its API models. Plain
/// static methods, not <c>IUmbracoMapper</c> map definitions — the shapes are simple enough that
/// registering a mapper (and the DI wiring that comes with it) wouldn't buy anything here.
/// </summary>
public static class PolicyModelMappingExtensions
{
    /// <summary>
    /// Maps a domain policy to its response model.
    /// </summary>
    public static PolicyResponseModel ToResponseModel(this ContentCheckPolicy policy) => new()
    {
        Key = policy.Key,
        Alias = policy.Alias,
        Name = policy.Name,
        ContentTypeKey = policy.ContentTypeKey,
        StartNodeKey = policy.StartNodeKey,
        ParentFolderKey = policy.ParentFolderKey,
        Rules = policy.Rules.Select(r => new PolicyRuleModel
        {
            CheckAlias = r.CheckAlias,
            Config = r.Config.Config,
            Mode = r.Mode,
            NotEvaluatedPolicy = r.NotEvaluatedPolicy,
        }).ToList(),
    };

    /// <summary>
    /// Maps a request model to a domain policy, using <paramref name="key"/> as its Key — the
    /// caller decides whether that comes from the request body (create) or the route (update).
    /// </summary>
    public static ContentCheckPolicy ToDomain(this PolicyRequestModel request, Guid key) => new()
    {
        Key = key,
        Alias = request.Alias,
        Name = request.Name,
        ContentTypeKey = request.ContentTypeKey,
        StartNodeKey = request.StartNodeKey,
        ParentFolderKey = request.ParentFolderKey,
        Rules = request.Rules.Select(r => new ContentCheckPolicyRule
        {
            CheckAlias = r.CheckAlias,
            Config = new() { Config = r.Config },
            Mode = r.Mode,
            NotEvaluatedPolicy = r.NotEvaluatedPolicy,
        }).ToList(),
    };
}
