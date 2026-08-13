using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.ContentChecks.Core.Checks;

namespace Umbraco.Community.ContentChecks.Core.Policies;

/// <summary>
/// Default implementation of <see cref="IGuardResolutionService"/>. Nearest-ancestor
/// start node wins, same shape as <see cref="ContentCheckResolutionService"/>'s policy scoping —
/// not yet re-verified against that service directly, see docs/guard-spec.md's open
/// questions. Rule matching is user rule beats role rule beats catch-all rule; ties within the same
/// specificity (e.g. two role rules both matching the user's groups) resolve to whichever rule is
/// listed first on the guard.
/// </summary>
internal sealed class ContentCheckGuardResolutionService : IGuardResolutionService
{
    private readonly IGuardRepository _repository;
    private readonly IEntityService _entityService;
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCheckGuardResolutionService"/> class.
    /// </summary>
    /// <param name="repository">Looks up every configured guard.</param>
    /// <param name="entityService">Resolves a start node key to its path id.</param>
    /// <param name="userService">Resolves the current user's group keys for role-row matching.</param>
    public ContentCheckGuardResolutionService(
        IGuardRepository repository, IEntityService entityService, IUserService userService)
    {
        _repository = repository;
        _entityService = entityService;
        _userService = userService;
    }

    /// <inheritdoc />
    public async Task<ContentCheckResponse> ResolveResponseAsync(
        IContent content,
        RequirementLevel requirementLevel,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default)
    {
        var guards = await _repository.GetAllAsync(cancellationToken);

        var guard = guards
            .Where(s => IsWithinStartNode(content, s.StartNodeKey))
            .OrderByDescending(s => StartNodeDepth(s.StartNodeKey))
            .FirstOrDefault();

        if (guard is null)
        {
            return ContentCheckResponse.Warn;
        }

        var roleKeys = await GetRoleKeysAsync(currentUserKey);

        var rule = guard.Rules.FirstOrDefault(r => r.UserKey is not null && r.UserKey == currentUserKey)
            ?? guard.Rules.FirstOrDefault(r => r.UserKey is null && r.RoleKey is not null && roleKeys.Contains(r.RoleKey.Value))
            ?? guard.Rules.FirstOrDefault(r => r.UserKey is null && r.RoleKey is null);

        if (rule?.BlockAt is null)
        {
            return ContentCheckResponse.Warn;
        }

        return requirementLevel >= rule.BlockAt.Value ? ContentCheckResponse.Block : ContentCheckResponse.Warn;
    }

    private async Task<IReadOnlySet<Guid>> GetRoleKeysAsync(Guid? currentUserKey)
    {
        if (currentUserKey is null)
        {
            return new HashSet<Guid>();
        }

        var user = await _userService.GetAsync(currentUserKey.Value);
        return user is null
            ? new HashSet<Guid>()
            : user.Groups.Select(g => g.Key).ToHashSet();
    }

    private bool IsWithinStartNode(IContent content, Guid? startNodeKey)
    {
        if (startNodeKey is null)
        {
            return true;
        }

        var startNodeId = _entityService.Get(startNodeKey.Value)?.Id;
        return startNodeId is not null && content.Path.Split(',').Contains(startNodeId.Value.ToString());
    }

    /// <summary>
    /// A rough "how specific is this guard" score for picking the nearest match when more than
    /// one guard's start node covers the content — deeper path id, more specific.
    /// </summary>
    private int StartNodeDepth(Guid? startNodeKey)
    {
        if (startNodeKey is null)
        {
            return -1;
        }

        var entity = _entityService.Get(startNodeKey.Value);
        return entity?.Path.Split(',').Length ?? -1;
    }
}
