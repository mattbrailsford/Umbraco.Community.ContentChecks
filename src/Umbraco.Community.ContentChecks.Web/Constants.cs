namespace Umbraco.Community.ContentChecks.Web;

/// <summary>
/// Defines constants for the Content Checks Management API.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Defines the root namespace for the application.
    /// </summary>
    public const string AppNamespaceRoot = "Umbraco.Community.ContentChecks";

    /// <summary>
    /// Defines constants for the Content Checks Management API.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>
        /// The API root path.
        /// </summary>
        public const string BackofficePath = "/content-checks/management/api";

        /// <summary>
        /// The API title.
        /// </summary>
        public const string ApiTitle = "Umbraco Content Checks Management API";

        /// <summary>
        /// The API name.
        /// </summary>
        public const string ApiName = "content-checks-management";

        /// <summary>
        /// The namespace prefix for the Content Checks Management API.
        /// </summary>
        public const string ApiNamespacePrefix = $"{AppNamespaceRoot}.Web.Api.Management";

        /// <summary>
        /// Defines constants for different feature areas within the Management API.
        /// </summary>
        public static class Feature
        {
            /// <summary>
            /// Defines constants for check policy features.
            /// </summary>
            public static class Policies
            {
                /// <summary>
                /// The route segment for policy features.
                /// </summary>
                public const string RouteSegment = "policies";

                /// <summary>
                /// The Swagger group name for policy features.
                /// </summary>
                public const string GroupName = "Policies";
            }

            /// <summary>
            /// Defines constants for guard features.
            /// </summary>
            public static class Guards
            {
                /// <summary>
                /// The route segment for guard features.
                /// </summary>
                public const string RouteSegment = "guards";

                /// <summary>
                /// The Swagger group name for guard features.
                /// </summary>
                public const string GroupName = "Guards";
            }

            /// <summary>
            /// Defines constants for policy folder features.
            /// </summary>
            public static class Folders
            {
                /// <summary>
                /// The route segment for folder features.
                /// </summary>
                public const string RouteSegment = "folders";

                /// <summary>
                /// The Swagger group name for folder features.
                /// </summary>
                public const string GroupName = "Folders";
            }

            /// <summary>
            /// Defines constants for the combined policy/folder tree — children-of-root,
            /// children-of-folder, and ancestors (breadcrumb). Moving a policy into or out of a
            /// folder isn't a tree endpoint; it's the <c>ParentFolderKey</c> field on the policy's
            /// own update call.
            /// </summary>
            public static class Tree
            {
                /// <summary>
                /// The route segment for tree features.
                /// </summary>
                public const string RouteSegment = "tree";

                /// <summary>
                /// The Swagger group name for tree features.
                /// </summary>
                public const string GroupName = "Tree";
            }

            /// <summary>
            /// Defines constants for registered check-type features — list, with each type's
            /// config schema, so the UI renders every check's config form from this, no per-type
            /// frontend code.
            /// </summary>
            public static class CheckTypes
            {
                /// <summary>
                /// The route segment for check-type features.
                /// </summary>
                public const string RouteSegment = "check-types";

                /// <summary>
                /// The Swagger group name for check-type features.
                /// </summary>
                public const string GroupName = "Check Types";
            }

            /// <summary>
            /// Defines constants for the Applicable Checks feature — resolution's own per-item
            /// output (Policy Resolution spec §7), not the schema-level Effective Checks service.
            /// </summary>
            public static class ApplicableChecks
            {
                /// <summary>
                /// The route segment for Applicable Checks features.
                /// </summary>
                public const string RouteSegment = "applicable-checks";

                /// <summary>
                /// The Swagger group name for Applicable Checks features.
                /// </summary>
                public const string GroupName = "Applicable Checks";
            }

            /// <summary>
            /// Defines constants for the evaluation (live-run) feature — one mechanism for one
            /// content item or several (Management API &amp; UI spec's "Live-run mechanism").
            /// </summary>
            public static class Evaluation
            {
                /// <summary>
                /// The route segment for evaluation features.
                /// </summary>
                public const string RouteSegment = "evaluation";

                /// <summary>
                /// The Swagger group name for evaluation features.
                /// </summary>
                public const string GroupName = "Evaluation";

                /// <summary>
                /// The most content items one run can be started with — recorded as pending
                /// real-world tuning. Keeps this endpoint from quietly becoming an uncapped sweep.
                /// </summary>
                public const int MaxBatchSize = 50;
            }

            /// <summary>
            /// Defines constants for the check results feature — the last completed run's
            /// persisted results for a content item, read without needing a live run in progress.
            /// </summary>
            public static class CheckResults
            {
                /// <summary>
                /// The route segment for check results features.
                /// </summary>
                public const string RouteSegment = "check-results";

                /// <summary>
                /// The Swagger group name for check results features.
                /// </summary>
                public const string GroupName = "Check Results";
            }
        }
    }
}
