using Godot;

namespace Boxbox.Presentation.OpenWorld;

public static class ThreadedSceneCache
{
    private static readonly Dictionary<string, PackedScene> Loaded = [];
    private static readonly HashSet<string> Requested = [];

    public static void Request(IEnumerable<string> paths)
    {
        foreach (var path in paths.Distinct())
        {
            if (Loaded.ContainsKey(path) || !Requested.Add(path)) continue;
            var error = ResourceLoader.LoadThreadedRequest(path, "PackedScene", useSubThreads: true);
            if (error != Error.Ok)
                GD.PushError($"Unable to queue threaded world asset: {path} ({error})");
        }
    }

    public static void Poll()
    {
        foreach (var path in Requested.Where(path => !Loaded.ContainsKey(path)).ToArray())
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path);
            if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                if (ResourceLoader.LoadThreadedGet(path) is PackedScene scene) Loaded[path] = scene;
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Failed)
            {
                GD.PushError($"Threaded world asset failed: {path}");
            }
        }
    }

    public static bool IsReady => Requested.Count > 0 && Requested.All(Loaded.ContainsKey);

    public static PackedScene Get(string path) => Loaded.TryGetValue(path, out var scene)
        ? scene
        : throw new InvalidOperationException($"World asset is not ready: {path}");

    public static void Clear()
    {
        foreach (var scene in Loaded.Values)
            scene.Dispose();
        Loaded.Clear();
        Requested.Clear();
    }
}
