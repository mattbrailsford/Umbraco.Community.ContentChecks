using Umbraco.Community.ContentChecks.Core.Policies.Elements;

namespace Umbraco.Community.ContentChecks.Tests.Unit.Policies;

/// <summary>
/// A <see cref="IContentCheckElementSource"/> test double that returns different element
/// instances depending on the raw value it's given -- needed to exercise cascading recursion
/// through more than one level, where the "nested" property's raw value differs from the root's.
/// </summary>
internal sealed class FakeContentCheckElementSourceByValue : IContentCheckElementSource
{
    private readonly IReadOnlyDictionary<object, IReadOnlyList<ContentCheckElementInstance>> _instancesByValue;

    public FakeContentCheckElementSourceByValue(string propertyEditorAlias, IReadOnlyDictionary<object, IReadOnlyList<ContentCheckElementInstance>> instancesByValue)
    {
        PropertyEditorAlias = propertyEditorAlias;
        _instancesByValue = instancesByValue;
    }

    public string PropertyEditorAlias { get; }

    public IEnumerable<Guid> GetAllowedElementTypeKeys(object? dataTypeConfiguration) => [];

    public IEnumerable<ContentCheckElementInstance> GetElementInstances(object? rawValue)
        => rawValue is not null && _instancesByValue.TryGetValue(rawValue, out var instances) ? instances : [];
}
