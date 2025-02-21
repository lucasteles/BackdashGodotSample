using System.Text.RegularExpressions;
using Backdash.Serialization;

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

    public static void Write(this BinaryBufferWriter writer, in Vector2 rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
    }

    public static void Write(this BinaryBufferWriter writer, in Rect2 rect)
    {
        writer.Write(rect.Position);
        writer.Write(rect.Size);
    }

    public static Vector2 ReadVector2(this BinaryBufferReader reader) =>
        new(reader.ReadFloat(), reader.ReadFloat());

    public static Rect2 ReadRec2(this BinaryBufferReader reader) =>
        new(
            position: reader.ReadVector2(),
            size: reader.ReadVector2()
        );
}
