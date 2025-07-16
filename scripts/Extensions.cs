using System.Text.RegularExpressions;
using Backdash.Serialization;

public static partial class Extensions
{
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex AlphaNumRegex();

    public static string ToAlphaNumeric(this string text) =>
        AlphaNumRegex().Replace(text.Trim(), "_").ToLower();

    public static Vector2 RoundTo(this Vector2 vector, int digits = 2) =>
        new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

    public static int Left(this Rect2I rect) => rect.Position.X;
    public static int Right(this Rect2I rect) => rect.Position.X + rect.Size.X;
    public static int Top(this Rect2I rect) => rect.Position.Y;
    public static int Bottom(this Rect2I rect) => rect.Position.Y + rect.Size.Y;

    public static void Write(this BinaryBufferWriter writer, in Vector2 rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
    }

    public static void Write(this BinaryBufferWriter writer, in Vector2I rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
    }

    public static void Write(this BinaryBufferWriter writer, in Rect2I rect)
    {
        writer.Write(rect.Position);
        writer.Write(rect.Size);
    }

    public static void Read(this BinaryBufferReader reader, ref Vector2 vector)
    {
        vector.X = reader.ReadFloat();
        vector.Y = reader.ReadFloat();
    }

    public static void Read(this BinaryBufferReader reader, ref Rect2I rect)
    {
        rect.Position = reader.ReadVector2I();
        rect.Size = reader.ReadVector2I();
    }

    public static Vector2I ReadVector2I(this BinaryBufferReader reader) =>
        new(reader.ReadInt32(), reader.ReadInt32());
}
