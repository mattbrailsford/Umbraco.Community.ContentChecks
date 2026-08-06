using System.Text.Json;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Policies;

/// <summary>
/// Default implementation of <see cref="IContentCheckPolicyFactory"/>.
/// </summary>
internal sealed class ContentCheckPolicyFactory : IContentCheckPolicyFactory
{
    /// <inheritdoc />
    public ContentCheckPolicy BuildDomain(ContentCheckPolicyEntity entity) => new()
    {
        Key = entity.Key,
        Alias = entity.Alias,
        Name = entity.Name,
        ContentTypeKey = entity.ContentTypeKey,
        StartNodeKey = entity.StartNodeKey,
        ParentFolderKey = entity.ParentFolderKey,
        Rules = JsonSerializer.Deserialize<List<ContentCheckPolicyRule>>(entity.RulesJson) ?? [],
    };

    /// <inheritdoc />
    public ContentCheckPolicyEntity BuildEntity(ContentCheckPolicy policy) => new()
    {
        Key = policy.Key,
        Alias = policy.Alias,
        Name = policy.Name,
        ContentTypeKey = policy.ContentTypeKey,
        StartNodeKey = policy.StartNodeKey,
        ParentFolderKey = policy.ParentFolderKey,
        RulesJson = JsonSerializer.Serialize(policy.Rules),
    };

    /// <inheritdoc />
    public void UpdateEntity(ContentCheckPolicyEntity entity, ContentCheckPolicy policy)
    {
        entity.Alias = policy.Alias;
        entity.Name = policy.Name;
        entity.ContentTypeKey = policy.ContentTypeKey;
        entity.StartNodeKey = policy.StartNodeKey;
        entity.ParentFolderKey = policy.ParentFolderKey;
        entity.RulesJson = JsonSerializer.Serialize(policy.Rules);
    }

    /// <inheritdoc />
    public ContentCheckPolicyFolder BuildDomain(ContentCheckPolicyFolderEntity entity) => new()
    {
        Key = entity.Key,
        Name = entity.Name,
        ParentFolderKey = entity.ParentFolderKey,
    };

    /// <inheritdoc />
    public ContentCheckPolicyFolderEntity BuildEntity(ContentCheckPolicyFolder folder) => new()
    {
        Key = folder.Key,
        Name = folder.Name,
        ParentFolderKey = folder.ParentFolderKey,
    };

    /// <inheritdoc />
    public void UpdateEntity(ContentCheckPolicyFolderEntity entity, ContentCheckPolicyFolder folder)
    {
        entity.Name = folder.Name;
        entity.ParentFolderKey = folder.ParentFolderKey;
    }
}
