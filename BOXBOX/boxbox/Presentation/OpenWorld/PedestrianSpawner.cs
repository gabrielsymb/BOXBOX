using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class PedestrianSpawner : Node3D
{
    private const int Maximum = 8;
    private const float ActiveRadius = 32f;
    private readonly List<PedestrianAgent> _agents = [];
    private double _cooldown;
    private uint _sequence;
    public PlayerController? Focus { get; set; }

    public override void _Process(double delta)
    {
        if (Focus is null) return;
        foreach (var existing in _agents)
            if (IsInstanceValid(existing) && existing.GlobalPosition.DistanceTo(Focus.FocusPosition) > ActiveRadius)
                existing.QueueFree();
        _agents.RemoveAll(a => !IsInstanceValid(a));
        _cooldown -= delta;
        if (_cooldown > 0 || _agents.Count >= Maximum) return;
        _cooldown = 1.5;
        if (!TryFindOffscreenSpawn(Focus.FocusPosition, out var spawn)) return;
        _sequence++;
        var agent = new PedestrianAgent { Position = spawn };
        agent.Initialize(Focus, WorldAssetCatalog.Current.Pick(WorldAssetCategory.NpcCharacters, _sequence), _sequence * 2654435761u);
        AddChild(agent); _agents.Add(agent);
    }

    private bool TryFindOffscreenSpawn(Vector3 focus, out Vector3 spawn)
    {
        var camera = GetViewport().GetCamera3D();
        var viewportRect = GetViewport().GetVisibleRect().Grow(80f);
        for (uint attempt = 1; attempt <= 16; attempt++)
        {
            var candidateSeed = (_sequence + attempt) * 2654435761u;
            var candidate = SidewalkGraph.FindSpawn(focus, candidateSeed);
            var distance = candidate.DistanceTo(focus);
            if (distance is < 19f or > ActiveRadius - 1f) continue;
            if (camera is not null && !camera.IsPositionBehind(candidate) &&
                viewportRect.HasPoint(camera.UnprojectPosition(candidate))) continue;
            spawn = candidate;
            _sequence += attempt - 1;
            return true;
        }
        spawn = default;
        return false;
    }
}
