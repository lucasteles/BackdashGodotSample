using System.Runtime.CompilerServices;

namespace SpaceWar.Services;

public static class Prefab
{
    static readonly ConditionalWeakTable<string, PackedScene> cache = new();

    public static PackedScene Load(string name) =>
        cache.GetValue(name, _ => GD.Load<PackedScene>($"res://prefabs/{name}.tscn"));

    public static T Instantiate<T>(string name) where T : Node
    {
        var prefab = Load(name);
        ArgumentNullException.ThrowIfNull(prefab);
        return prefab.Instantiate<T>();
    }
}
