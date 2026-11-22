using Boxbox.Presentation.OpenWorld;
using Godot;

namespace Boxbox.Presentation.Hud;

public partial class StreetMinimap : CanvasLayer
{
    private Camera3D _camera = null!;
    private SubViewport _view = null!;
    private TextureRect _pointer = null!;
    private TextureRect _mission = null!;
    public Vector3? MissionPosition { get; set; }

    public PlayerController? Player { get; set; }

    public override void _Ready()
    {
        _view = GetNode<SubViewport>("Panel/Map/View");
        _view.World3D = GetViewport().World3D;
        _camera = GetNode<Camera3D>("Panel/Map/View/Camera");
        _pointer = GetNode<TextureRect>("Panel/Pointer");
        _mission = GetNode<TextureRect>("Panel/Mission");
        _mission.SelfModulate = HudColors.MissionPink;
    }

    public override void _Process(double delta)
    {
        if (!Visible || Player is not { } player)
        {
            return;
        }

        var focus = player.FocusPosition;
        _camera.GlobalPosition = focus + Vector3.Up * 55f;
        _camera.LookAt(focus, Vector3.Forward);
        _mission.Visible = MissionPosition.HasValue;
        if (MissionPosition is { } destination)
        {
            var position = _camera.UnprojectPosition(destination);
            _mission.Position = new Vector2(
                Mathf.Clamp(position.X - 3.5f, 4, 173),
                Mathf.Clamp(position.Y - 3.5f, 4, 173));
        }
        var heading = player.FocusHeading;
        if (new Vector2(heading.X, heading.Z).LengthSquared() > 0.01f)
        {
            _pointer.Rotation = Mathf.Atan2(heading.X, -heading.Z);
        }
    }
}
