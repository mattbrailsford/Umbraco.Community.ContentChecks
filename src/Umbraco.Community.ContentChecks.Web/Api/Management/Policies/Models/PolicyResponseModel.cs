namespace Umbraco.Community.ContentChecks.Web.Api.Management.Policies.Models;

/// <summary>
/// A check policy, as returned by the Management API.
/// </summary>
public sealed class PolicyResponseModel
{
    /// <summary>
    /// The policy's durable identity.
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
    public required IReadOnlyList<PolicyRuleModel> Rules { get; init; }
}
