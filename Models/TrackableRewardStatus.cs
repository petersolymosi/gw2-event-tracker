namespace Gw2EventTracker.Models {

    public sealed class TrackableRewardStatus {
        public string DisplayName { get; set; } = string.Empty;
        public string TrackType { get; set; } = string.Empty;
        public string ApiId { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

}
