public partial class BackgroundManager : Node2D
{
    [Export] CompressedTexture2D bigStar;
    [Export] CompressedTexture2D smallStar;

    [Export] int density = 80;
    [Export] float bigStarPercent = 0.05f;

    public override void _Ready()
    {
        var bounds = GetViewportRect();
        for (var i = 0; i < density; i++)
        {
            Sprite2D star = new();

            star.Position = new(
                GD.Randf() * bounds.Size.X,
                GD.Randf() * bounds.Size.Y
            );

            star.Texture =
                GD.Randf() > 1 - bigStarPercent
                    ? bigStar
                    : smallStar;

            AddChild(star);
        }
    }
}
