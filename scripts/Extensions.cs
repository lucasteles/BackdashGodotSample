using System.Text.RegularExpressions;

public static partial class Extensions
{
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex ClearTextRegex();

    public static string NormalizeText(this string text) =>
        ClearTextRegex().Replace(text.Trim(), "_").ToLower();

    public static Vector2 RoundTo(this Vector2 vector, int digits = 2) =>
        new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

    public static float Left(this Rect2 rect) => rect.Position.X;
    public static float Right(this Rect2 rect) => rect.Position.X + rect.Size.X;
    public static float Top(this Rect2 rect) => rect.Position.Y;
    public static float Bottom(this Rect2 rect) => rect.Position.Y + rect.Size.Y;
}
