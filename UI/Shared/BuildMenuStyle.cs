// Local adaptation of godot-tower-defense's UIButtonFactory panel geometry and HUD palette.
using Godot;

public static class BuildMenuStyle
{
    public static readonly Color Accent = new("#69cddc"), Text = new("#e0f2f5"), Muted = new("#97b4bf");

    // Reuse the reference's restrained cyan edge, four-pixel corners and inset spacing.
    public static StyleBoxFlat Box(Color fill, Color edge) => new()
    {
        BgColor = fill, BorderColor = edge,
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8,
        CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
    };

    // Styling stays independent of gameplay actions, including locked/disabled tiles.
    public static void Apply(Button button)
    {
        Color fill = new("#1b3645");
        button.FocusMode = Control.FocusModeEnum.All;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", Text);
        button.AddThemeColorOverride("font_disabled_color", Muted);
        button.AddThemeStyleboxOverride("normal", Box(fill, fill.Lightened(.30f)));
        button.AddThemeStyleboxOverride("hover", Box(fill.Lightened(.20f), Accent));
        button.AddThemeStyleboxOverride("pressed", Box(fill.Lightened(.32f), Accent));
        button.AddThemeStyleboxOverride("disabled", Box(new Color("#132633"), Muted.Darkened(.4f)));
        var focus = Box(Colors.Transparent, Accent); focus.DrawCenter = false;
        button.AddThemeStyleboxOverride("focus", focus);
    }
}
