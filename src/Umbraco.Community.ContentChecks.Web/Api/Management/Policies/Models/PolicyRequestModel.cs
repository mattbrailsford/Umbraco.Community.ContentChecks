namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// A check policy to create or update. Shared between create and update: create takes the
/// <c>Key</c> from this body (the client generates it, the same convention Umbraco's own backoffice
/// uses for new entities); update takes the <c>Key</c> from the route instead and ignores this one.
/// </summary>
public sealed class PolicyRequestModel
{
    /// <summary>
    /// The policy's durable identity. Only read on create; a route key always wins on update.
    /// </summary>
    public required Guid Key { get; init; }

    /// <summary>
    /// The unique alias of the policy.
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the policy.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The content type this policy attaches to, by its durable Key.
    /// </summary>
    public required Guid ContentTypeKey { get; init; }

    /// <summary>
    /// Restricts the policy to content underneath this node. <c>null</c> means everywhere the
    /// content type reaches.
    /// </summary>
    public Guid? StartNodeKey { get; init; }

    /// <summary>
    /// The folder this policy is organized under in the backoffice tree. <c>null</c> means at the root.
    /// </summary>
    public Guid? ParentFolderKey { get; init; }

    /// <summary>
    /// The rules this policy runs.
    /// </summary>
    public IReadOnlyList<PolicyRuleModel> Rules { get; init; } = [];
}
