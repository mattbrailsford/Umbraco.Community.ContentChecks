using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

/// <summary>
/// A minimal <see cref="IContentCheckElementSource"/> test double -- bypasses the real Block
/// List/Grid parsing entirely, since that's Umbraco's own well-tested code; tests here only need
/// to exercise the resolution/effective-checks logic that calls a source.
/// </summary>
internal sealed class FakeContentCheckElementSource : IContentCheckElementSource
{
    private readonly IReadOnlyList<Guid> _allowedElementTypeKeys;
    private readonly IReadOnlyList<ContentCheckElementInstance> _elementInstances;

    public FakeContentCheckElementSource(
        string propertyEditorAlias,
        IReadOnlyList<Guid>? allowedElementTypeKeys = null,
        IReadOnlyList<ContentCheckElementInstance>? elementInstances = null)
    {
        PropertyEditorAlias = propertyEditorAlias;
        _allowedElementTypeKeys = allowedElementTypeKeys ?? [];
        _elementInstances = elementInstances ?? [];
    }

    public string PropertyEditorAlias { get; }

    public IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration) => _allowedElementTypeKeys;

    public IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue) => _elementInstances;
}
