using System.Text.Json;
using Umbraco.Community.ContentChecks.Core.Checks;
using Umbraco.Community.ContentChecks.Core.Policies;

namespace Umbraco.Community.ContentChecks.Persistence.Checks;

/// <summary>
/// Default implementation of <see cref="IContentCheckResultFactory"/>.
/// </summary>
internal sealed class ContentCheckResultFactory : IContentCheckResultFactory
{
    /// <inheritdoc />
    public ContentCheckStoredResult BuildDomain(ContentCheckResultEntity entity) => new()
    {
        ContentKey = entity.ContentKey,
        ContentStamp = entity.ContentStamp,
        Culture = entity.Culture,
        Segment = entity.Segment,
        Path = JsonSerializer.Deserialize<List<ContentCheckLocationStep>>(entity.PathJson) ?? [],
        CheckAlias = entity.CheckAlias,
        RequirementLevel = entity.RequirementLevel,
        NotEvaluatedPolicy = entity.NotEvaluatedPolicy,
        Result = JsonSerializer.Deserialize<ContentCheckResult>(entity.ResultJson)
            ?? new ContentCheckResult { Status = ContentCheckStatus.NotEvaluated, Message = string.Empty },
        RanAtUtc = entity.RanAtUtc,
    };

    /// <inheritdoc />
    public ContentCheckResultEntity BuildEntity(ContentCheckStoredResult result) => new()
    {
        Key = Guid.NewGuid(),
        ContentKey = result.ContentKey,
        ContentStamp = result.ContentStamp,
        Culture = result.Culture,
        Segment = result.Segment,
        PathJson = JsonSerializer.Serialize(result.Path),
        CheckAlias = result.CheckAlias,
        RequirementLevel = result.RequirementLevel,
        NotEvaluatedPolicy = result.NotEvaluatedPolicy,
        ResultJson = JsonSerializer.Serialize(result.Result),
        RanAtUtc = result.RanAtUtc,
    };
}
