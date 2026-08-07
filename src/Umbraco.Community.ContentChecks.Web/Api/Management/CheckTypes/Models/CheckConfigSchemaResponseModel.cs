namespace Umbraco.Community.ContentChecks.Web.Api.Management.CheckTypes.Models;

/// <summary>
/// The config schema for one registered check type.
/// </summary>
public class CheckConfigSchemaResponseModel
{
    /// <summary>
    /// The field definitions for the check's configuration.
    /// </summary>
    public IReadOnlyList<CheckConfigFieldResponseModel> Fields { get; set; } = [];
}
