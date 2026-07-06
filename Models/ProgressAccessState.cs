namespace Gw2EventTracker.Models {

    public enum ProgressAccessState {
        /// <summary>No GW2 API key or module subtoken yet.</summary>
        NoApiKey,
        /// <summary>API key exists but this module's optional permissions are not enabled.</summary>
        MissingModulePermissions,
        /// <summary>API data is available for completion tracking.</summary>
        Ready,
        /// <summary>Permissions are present but the last fetch failed.</summary>
        FetchFailed
    }

}
