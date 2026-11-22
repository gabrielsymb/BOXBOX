using Godot;

namespace Boxbox.Presentation.Dialogue;

public partial class DialogueLine : Resource
{
    [Export] public string Speaker { get; set; } = "";
    [Export] public string Portuguese { get; set; } = "";
    [Export] public string English { get; set; } = "";
    [Export] public AudioStream? Voice { get; set; }
    [Export] public Texture2D? Portrait { get; set; }
}
