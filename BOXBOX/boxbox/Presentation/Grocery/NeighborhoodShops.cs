using System.Globalization;
using Boxbox.Application.BusinessDay;
using Boxbox.Presentation.Dialogue;
using Godot;

namespace Boxbox.Presentation.Grocery;

public partial class NeighborhoodShops : Node3D
{
    private Node3D _grocery = null!;
    private Node3D _supplier = null!;
    private Camera3D _camera = null!;
    private CashHud _cashHud = null!;
    private GroceryDesk _desk = null!;
    private Control _window = null!;
    private SubtitleOverlay _dialogue = null!;
    private readonly Dictionary<SupplierConversation, DialogueLine> _lines = [];
    private Button _travel = null!;
    private Button _interact = null!;
    private TextureButton _close = null!;
    private GroceryNeighborhoodProgress _progress = new();
    private Button _coffee = null!;
    private Button _talk = null!;

    public override void _Ready()
    {
        _grocery = GetNode<Node3D>("Grocery");
        _supplier = GetNode<Node3D>("Supplier");
        _camera = GetNode<Camera3D>("Camera");

        _cashHud = GetNode<CashHud>("CashHud");
        _dialogue = GetNode<SubtitleOverlay>("Dialogue");
        _lines.Add(SupplierConversation.Welcome, GD.Load<DialogueLine>("res://Content/Dialogue/wholesaler-welcome.tres"));
        _lines.Add(SupplierConversation.CoffeeOffer, GD.Load<DialogueLine>("res://Content/Dialogue/wholesaler-coffee.tres"));
        _lines.Add(SupplierConversation.RegularCoffeeOffer, GD.Load<DialogueLine>("res://Content/Dialogue/wholesaler-regular.tres"));
        _dialogue.Finished += () =>
        {
            _progress.CompleteSupplierConversation();
            RefreshActions();
        };
        var layer = GetNode<CanvasLayer>("Interface");
        var hud = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(hud);
        hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var signs = new VBoxContainer { Position = new Vector2(38, 32) };
        signs.AddThemeConstantOverride("separation", 12);
        hud.AddChild(signs);
        _travel = ActionButton(signs, "wall-door-rotate", "", Travel);
        _interact = ActionButton(signs, "cash-register", "", Interact);
        _talk = ActionButton(signs, "talk", "", Talk);
        _coffee = ActionButton(signs, "mug", "", BuyCoffee);

        _window = new Control { Visible = false };
        hud.AddChild(_window);
        _window.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
        _window.OffsetLeft = -615;
        _window.OffsetRight = -24;
        _window.OffsetTop = -350;
        _window.OffsetBottom = 350;
        _desk = GD.Load<PackedScene>("res://Presentation/Grocery/GroceryDesk.tscn").Instantiate<GroceryDesk>();
        _desk.Embedded = true;
        _window.AddChild(_desk);
        _close = new TextureButton
        {
            TextureNormal = GD.Load<Texture2D>("res://Assets/Grocery/cross.png"),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            ZIndex = 1
        };
        _window.AddChild(_close);
        _close.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _close.OffsetLeft = -44;
        _close.OffsetRight = -10;
        _close.OffsetTop = 12;
        _close.OffsetBottom = 46;
        _close.Pressed += () => SetWindow(false);

        _desk.LanguageChanged += RefreshActions;
        _desk.CashChanged += RefreshCash;
        _desk.OrderConfirmed += () =>
        {
            _progress.RecordConfirmedOrder();
            _progress.TryGoBack();
            RefreshLocation();
        };
        _desk.ReplayRequested += () =>
        {
            _progress = new GroceryNeighborhoodProgress();
            RefreshLocation();
        };

        RefreshLocation();
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (_window.Visible && @event.IsActionPressed("ui_cancel"))
        {
            SetWindow(false);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Travel()
    {
        if (_window.Visible)
        {
            SetWindow(false);
            return;
        }

        if (_progress.AtSupplier)
        {
            if (_progress.TryGoBack())
            {
                RefreshLocation();
            }
        }
        else if (_progress.TryVisitSupplier(out var greeting))
        {
            RefreshLocation();
            Play(greeting);
        }
    }

    private void RefreshLocation()
    {
        _grocery.Visible = !_progress.AtSupplier;
        _supplier.Visible = _progress.AtSupplier;
        _dialogue.Stop();
        RefreshActions();
        SetWindow(false);
    }

    private void Talk()
    {
        Play(_progress.BeginSupplierConversation());
        RefreshActions();
    }

    private void Play(SupplierConversation conversation)
    {
        if (_lines.TryGetValue(conversation, out var line))
        {
            _dialogue.Play(line);
        }
    }

    private void RefreshActions()
    {
        string Text(string pt, string en) => _desk.IsEnglish ? en : pt;
        _travel.Text = _progress.AtSupplier ? Text("Voltar", "Back") : Text("Ir ao atacado", "Visit the wholesaler");
        _interact.Text = _progress.AtSupplier ? Text("Fazer pedido", "Place an order") : _progress.DayStarted
            ? Text("Ver expediente", "View the business day")
            : Text("Abrir pelo caixa", "Open at the register");
        _interact.Disabled = _progress.AtSupplier ? _progress.HasConfirmedOrder : !_progress.HasConfirmedOrder;
        _travel.Disabled = _progress.DayStarted;
        _talk.Visible = _progress.AtSupplier;
        _talk.Disabled = !_progress.CanTalk;
        _talk.Text = Text("Falar com Nando", "Talk to Nando");
        _coffee.Visible = _progress.CoffeeOfferCents.HasValue;
        if (_progress.CoffeeOfferCents is { } price)
        {
            var culture = CultureInfo.GetCultureInfo(_desk.IsEnglish ? "en-US" : "pt-BR");
            var coffeePrice = $"R$ {(price / 100m).ToString("N2", culture)}";
            _coffee.Text = Text($"Café · {coffeePrice}", $"Coffee · {coffeePrice}");
            _coffee.TooltipText = Text("Compra pessoal", "Personal purchase");
        }
        RefreshCash();
        _dialogue.SetLanguage(_desk.IsEnglish);
        _close.TooltipText = Text("Fechar", "Close");
    }

    private void RefreshCash() => _cashHud.ShowBalance(_desk.CashCents, _progress.WalletCents, _desk.IsEnglish);

    private void BuyCoffee()
    {
        if (_progress.TryBuyCoffee())
        {
            RefreshActions();
        }
    }

    private void Interact()
    {
        if (_progress.AtSupplier)
        {
            if (_progress.HasConfirmedOrder)
            {
                return;
            }
            _desk.OpenOrder();
        }
        else if (_progress.TryStartDay())
        {
            RefreshActions();
            _desk.OpenStore();
        }
        SetWindow(true);
    }

    private void SetWindow(bool visible)
    {
        _window.Visible = visible;
        _camera.Size = visible ? 8.2f : 6.9f;
        var target = visible ? new Vector3(1.9f, 0, -1.15f) : new Vector3(0, 0, 0.45f);
        _camera.Position = target + new Vector3(5, 6, 6);
        _camera.LookAt(target);
    }

    private static Button ActionButton(Container parent, string asset, string title, Action action)
    {
        var button = new Button
        {
            Text = title,
            Icon = GD.Load<Texture2D>($"res://Assets/Grocery/{asset}.png"),
            Alignment = HorizontalAlignment.Left,
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(250, 51)
        };
        button.AddThemeConstantOverride("icon_max_width", 42);
        button.AddThemeFontSizeOverride("font_size", 18);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
