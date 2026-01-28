using MudBlazor;

namespace Fuzz.Web.Theme;

public static class FuzzTheme
{
    public static MudTheme DarkOrangeTheme => new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#FF6B35",
            Secondary = "#1A1A2E",
            Tertiary = "#16213E",
            Background = "#0F0F23",
            Surface = "#1A1A2E",
            AppbarBackground = "#0F0F23",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#1A1A2E",
            DrawerText = "#E0E0E0",
            DrawerIcon = "#FF6B35",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#B0B0B0",
            ActionDefault = "#FF6B35",
            ActionDisabled = "#555555",
            ActionDisabledBackground = "#2A2A3E",
            Divider = "#333344",
            DividerLight = "#222233",
            TableLines = "#333344",
            LinesDefault = "#333344",
            LinesInputs = "#444455",
            TextDisabled = "#666666",
            Info = "#3498db",
            Success = "#2ecc71",
            Warning = "#f39c12",
            Error = "#e74c3c",
            Dark = "#0F0F23",
            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
            GrayDefault = "#555555",
            GrayLight = "#777777",
            GrayLighter = "#999999",
            GrayDark = "#333333",
            GrayDarker = "#1A1A1A",
            OverlayDark = "rgba(15, 15, 35, 0.8)",
            OverlayLight = "rgba(255, 255, 255, 0.1)",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#FF6B35",
            Secondary = "#1A1A2E",
            Tertiary = "#16213E",
            Background = "#0F0F23",
            Surface = "#1A1A2E",
            AppbarBackground = "#0F0F23",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#1A1A2E",
            DrawerText = "#E0E0E0",
            DrawerIcon = "#FF6B35",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#B0B0B0",
            ActionDefault = "#FF6B35",
            ActionDisabled = "#555555",
            ActionDisabledBackground = "#2A2A3E",
            Divider = "#333344",
            DividerLight = "#222233",
            TableLines = "#333344",
            LinesDefault = "#333344",
            LinesInputs = "#444455",
            TextDisabled = "#666666",
            Info = "#3498db",
            Success = "#2ecc71",
            Warning = "#f39c12",
            Error = "#e74c3c",
            Dark = "#0F0F23",
            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
            GrayDefault = "#555555",
            GrayLight = "#777777",
            GrayLighter = "#999999",
            GrayDark = "#333333",
            GrayDarker = "#1A1A1A",
            OverlayDark = "rgba(15, 15, 35, 0.8)",
            OverlayLight = "rgba(255, 255, 255, 0.1)",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px"
        }
    };
}
