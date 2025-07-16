using System.Runtime.InteropServices;

namespace SpaceWar.Services;

public static class Prefab
{
    static readonly Dictionary<StringName, PackedScene> cache = new();

    public static PackedScene Load(StringName scene)
    {
        ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(cache, scene, out var exists);

        if (!exists)
            value = GD.Load<PackedScene>($"res://prefabs/{scene}.tscn");

        return value;
    }

    public static T Instantiate<T>(StringName scene) where T : Node
    {
        var prefab = Load(scene);
        ArgumentNullException.ThrowIfNull(prefab);
        return prefab.Instantiate<T>();
    }
}
