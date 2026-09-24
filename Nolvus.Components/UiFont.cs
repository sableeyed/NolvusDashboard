using Avalonia.Media;

namespace Nolvus.Components
{
    /// <summary>
    /// Sets up a global font system for the Dashboard. Upstream uses Segoe UI, which is Microsoft's and cannot be shipped
    /// with the port, so Segoe UI is used where it is installed and Open Sans otherwise, bundled in Assets/Fonts/OpenSans.
    /// </summary>
    public static class UiFont
    {
        public const string FamilyName = "Segoe UI, avares://Nolvus.Components/Assets/Fonts/OpenSans#Open Sans";

        public static readonly FontFamily Family = new FontFamily(FamilyName);
    }
}
