using Godot;

namespace Boxbox.Presentation.Grocery;

public partial class ShopShell : Node3D
{
    [Export] public Material ExteriorMaterial { get; set; } = null!;

    public override void _Ready()
    {
        if (GetNodeOrNull<Node3D>("Nando") is { } attendant)
        {
            Boxbox.Presentation.OpenWorld.CharacterVertexPalette.Apply(attendant);
            Boxbox.Presentation.OpenWorld.VisualShadowSupport.NormalizeHeight(attendant, 1.65f);
            Boxbox.Presentation.OpenWorld.VisualShadowSupport.AlignLowestPointToGround(attendant, 0.02f);
            var animation = attendant.FindChildren("*", "AnimationPlayer", true, false)
                .OfType<AnimationPlayer>().FirstOrDefault();
            if (animation is not null && animation.HasAnimation("Idle"))
            {
                animation.GetAnimation("Idle").LoopMode = Animation.LoopModeEnum.Linear;
                animation.Play("Idle");
            }
        }

        foreach (var child in GetChildren())
        {
            if (child.IsInGroup("shop_shell"))
            {
                foreach (var mesh in child.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                {
                    for (var surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
                    {
                        var material = mesh.GetActiveMaterial(surface);
                        if (material is BaseMaterial3D { Transparency: not BaseMaterial3D.TransparencyEnum.Disabled })
                        {
                            continue;
                        }

                        mesh.SetSurfaceOverrideMaterial(surface, ExteriorMaterial);
                    }
                }
            }
        }
    }
}
