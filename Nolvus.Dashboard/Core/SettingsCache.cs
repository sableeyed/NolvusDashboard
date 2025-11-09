namespace Nolvus.Dashboard.Core
{
    public static class SettingsCache
    {
        // 1.0 = 100%, 1.25 = 125%, etc
        public static double UiScaleMultiplier { get; set; } = 1.0;
        public static string GameDirectory { get; set; } = string.Empty;
        public static string NexusApiKey { get; set; } = string.Empty;
        public static string NolvusUser { get; set; } = string.Empty;
        public static string NolvusPassword { get; set; } = string.Empty;
    }
}
