using Godot;

namespace Boxbox.Presentation.OpenWorld;

public sealed partial class PedestrianSpawner : Node3D
{
    private const int Maximum = 8;
    private const float ActiveRadius = 32f;
    private const float MinimumSpawnSpacing = 1.8f;
    private readonly List<PedestrianAgent> _agents = [];
    private double _cooldown;
    private uint _sequence;
    public PlayerController? Focus { get; set; }
    [Export] public bool EnableReciprocalAvoidance { get; set; } = true;

    public override void _Process(double delta)
    {
        if (Focus is null) return;
        foreach (var existing in _agents)
            if (IsInstanceValid(existing) && existing.GlobalPosition.DistanceTo(Focus.FocusPosition) > ActiveRadius)
                existing.QueueFree();
        _agents.RemoveAll(a => !IsInstanceValid(a) || a.IsQueuedForDeletion());
        // Keep the on/off control useful for agents already spawned. This
        // is deliberately not coupled to their model or skeleton scale.
        foreach (var existing in _agents)
            existing.UseReciprocalAvoidance = EnableReciprocalAvoidance;
        _cooldown -= delta;
        if (_cooldown > 0 || _agents.Count >= Maximum) return;
        _cooldown = 1.5;
        if (!TryFindOffscreenSpawn(Focus.FocusPosition, out var spawn)) return;
        _sequence++;
        var agent = new PedestrianAgent
        {
            Position = spawn,
            UseReciprocalAvoidance = EnableReciprocalAvoidance
        };
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
            // A fresh pedestrian must not materialize on top of an existing one.
            if (_agents.Any(agent => IsInstanceValid(agent) && !agent.IsQueuedForDeletion() &&
                agent.GlobalPosition.DistanceSquaredTo(candidate) < MinimumSpawnSpacing * MinimumSpawnSpacing)) continue;
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
