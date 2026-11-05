using MudBlazor;

namespace FoodTrace.Components.Layout;

public static class TemaFoodTrace
{
    private static readonly string[] Familia = ["Inter", "Segoe UI", "sans-serif"];

    public static MudTheme Tema { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#176B52",
            PrimaryDarken = "#10513E",
            Secondary = "#234B5A",
            Tertiary = "#D99A29",
            TertiaryContrastText = "#182B25",
            Success = "#237A48",
            Warning = "#9A5B00",
            Error = "#B42318",
            Info = "#175CD3",
            Background = "#F7F8FA",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#182B25",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#182B25",
            DrawerIcon = "#53665E",
            TextPrimary = "#182B25",
            TextSecondary = "#53665E",
            ActionDefault = "#53665E",
            LinesDefault = "#DDE5E0",
            LinesInputs = "#73857B",
            TableLines = "#DDE5E0",
            Divider = "#DDE5E0"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = Familia, FontSize = "0.9375rem" },
            H1 = new H1Typography { FontFamily = Familia, FontSize = "2rem", FontWeight = "700" },
            H2 = new H2Typography { FontFamily = Familia, FontSize = "1.75rem", FontWeight = "700" },
            H3 = new H3Typography { FontFamily = Familia, FontSize = "1.5rem", FontWeight = "700" },
            H4 = new H4Typography { FontFamily = Familia, FontSize = "1.375rem", FontWeight = "600" },
            H5 = new H5Typography { FontFamily = Familia, FontSize = "1.25rem", FontWeight = "600" },
            H6 = new H6Typography { FontFamily = Familia, FontSize = "1.125rem", FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontFamily = Familia },
            Subtitle2 = new Subtitle2Typography { FontFamily = Familia },
            Body1 = new Body1Typography { FontFamily = Familia, FontSize = "0.9375rem" },
            Body2 = new Body2Typography { FontFamily = Familia, FontSize = "0.875rem" },
            Button = new ButtonTypography { FontFamily = Familia, TextTransform = "none", FontWeight = "600" },
            Caption = new CaptionTypography { FontFamily = Familia },
            Overline = new OverlineTypography { FontFamily = Familia }
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" }
    };
}
