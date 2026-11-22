using Boxbox.Presentation.Hud;
using Godot;

namespace Boxbox.Presentation.Dialogue;

public partial class SubtitleOverlay : CanvasLayer
{
    private readonly Queue<DialogueLine> _pending = new();
    private Control _panel = null!;
    private Label _speaker = null!;
    private Label _subtitle = null!;
    private TextureRect _portrait = null!;
    private AudioStreamPlayer _voice = null!;
    private Godot.Timer _timer = null!;
    private DialogueLine? _current;
    private bool _english;

    public event Action? Finished;

    public override void _Ready()
    {
        _panel = GetNode<Control>("Panel");
        _speaker = GetNode<Label>("Panel/Content/Words/Speaker");
        _speaker.AddThemeColorOverride("font_color", HudColors.MissionPink);
        _subtitle = GetNode<Label>("Panel/Content/Words/Subtitle");
        _portrait = GetNode<TextureRect>("Panel/Content/Portrait");
        _voice = GetNode<AudioStreamPlayer>("Voice");
        _timer = GetNode<Godot.Timer>("Timer");
        _timer.Timeout += () =>
        {
            if (_pending.Count == 0)
            {
                Stop();
                Finished?.Invoke();
                return;
            }
            Next();
        };
    }

    public void SetLanguage(bool english)
    {
        _english = english;
        if (_current is { } line)
        {
            _subtitle.Text = Text(line);
        }
    }

    public void Play(params DialogueLine[] lines)
    {
        Stop();
        foreach (var line in lines)
        {
            _pending.Enqueue(line);
        }
        Next();
    }

    public void Stop()
    {
        _pending.Clear();
        _current = null;
        _timer.Stop();
        _voice.Stop();
        _panel.Hide();
    }

    private string Text(DialogueLine line) => _english ? line.English : line.Portuguese;

    private void Next()
    {
        if (!_pending.TryDequeue(out var line))
        {
            Stop();
            return;
        }

        _current = line;
        _speaker.Text = line.Speaker;
        _subtitle.Text = Text(line);
        _portrait.Texture = line.Portrait;
        _portrait.Visible = line.Portrait is not null;
        _panel.Show();
        var readingTime = Math.Clamp(Math.Max(line.Portuguese.Length, line.English.Length) / 15.0 + 0.45, 2.0, 7.0);
        var duration = Math.Max(readingTime, line.Voice?.GetLength() ?? 0);
        if (line.Voice is { } voice)
        {
            _voice.Stream = voice;
            _voice.Play();
        }
        _timer.Start(duration);
    }
}
