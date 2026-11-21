using Boxbox.Application.BusinessDay;
using Boxbox.Presentation.Grocery;
using Boxbox.Presentation.Dialogue;
using Boxbox.Presentation.Hud;
using Godot;

namespace Boxbox.Presentation.OpenWorld;

internal sealed partial class StreetSupplyInteraction : Node
{
    public PlayerController Player { get; init; } = null!;
    public DriveableCar Pickup { get; init; } = null!;
    public StreetMinimap Minimap { get; init; } = null!;
    public StreetStore Grocery { get; init; } = null!;
    public StreetStore Supplier { get; init; } = null!;

    private readonly GrocerySupplyRun _run = new();
    private StreetStore? _indoors;
    private CashHud _cash = null!;
    private GroceryDesk _desk = null!;
    private Control _window = null!;
    private Label _objective = null!;
    private Label _prompt = null!;
    private SubtitleOverlay _dialogue = null!;
    private bool _supplierIntroduced;

    public override void _Ready()
    {
        Grocery.SetInterior(false);
        Supplier.SetInterior(false);
        _cash = GD.Load<PackedScene>("res://Presentation/Grocery/CashHud.tscn").Instantiate<CashHud>();
        AddChild(_cash);
        _dialogue = GD.Load<PackedScene>("res://Presentation/Dialogue/SubtitleOverlay.tscn").Instantiate<SubtitleOverlay>();
        AddChild(_dialogue);

        var interfaceLayer = new CanvasLayer { Layer = 3 };
        AddChild(interfaceLayer);
        var ui = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        interfaceLayer.AddChild(ui);
        ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var objectiveRow = new HBoxContainer { Position = new Vector2(28, 34) };
        objectiveRow.AddThemeConstantOverride("separation", 12);
        ui.AddChild(objectiveRow);
        objectiveRow.AddChild(new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://Assets/Grocery/target.png"),
            CustomMinimumSize = new Vector2(28, 28),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SelfModulate = HudColors.MissionPink
        });
        _objective = new Label();
        _objective.AddThemeFontSizeOverride("font_size", 22);
        objectiveRow.AddChild(_objective);

        _prompt = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _prompt.AddThemeFontSizeOverride("font_size", 24);
        _prompt.AddThemeConstantOverride("outline_size", 7);
        ui.AddChild(_prompt);
        _prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _prompt.OffsetTop = -100;
        _prompt.OffsetBottom = -45;

        _window = new Control { Visible = false };
        ui.AddChild(_window);
        _window.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
        _window.OffsetLeft = -615;
        _window.OffsetRight = -24;
        _window.OffsetTop = -350;
        _window.OffsetBottom = 350;
        _desk = GD.Load<PackedScene>("res://Presentation/Grocery/GroceryDesk.tscn").Instantiate<GroceryDesk>();
        _desk.Embedded = true;
        _desk.OrderSession = _run.Order;
        _window.AddChild(_desk);
        _desk.CashChanged += RefreshCash;
        _desk.LanguageChanged += () =>
        {
            RefreshCash();
            _dialogue.SetLanguage(_desk.IsEnglish);
        };
        _desk.OrderConfirmed += () =>
        {
            if (_run.RecordReservation())
            {
                SetWindow(false);
            }
        };
        _desk.ReplayRequested += () =>
        {
            _run.Replay();
            _supplierIntroduced = false;
            SetWindow(false);
        };
        RefreshCash();
    }

    public override void _Process(double delta)
    {
        _objective.Text = _run.Stage switch
        {
            GrocerySupplyStage.NeedOrder => Text("Vá ao atacado e faça o pedido", "Visit the wholesaler and place the order"),
            GrocerySupplyStage.Reserved => Text("Estacione no atacado e carregue a caçamba", "Park at the wholesaler and load the pickup"),
            GrocerySupplyStage.Loaded => Text($"Leve a carga à BOXBOX · {_run.UnitsOnPickup} unidades", $"Bring the cargo to BOXBOX · {_run.UnitsOnPickup} units"),
            GrocerySupplyStage.Delivered => Text("Abra o expediente no caixa da BOXBOX", "Open the shop at the BOXBOX register"),
            _ => ""
        };
        Minimap.MissionPosition = _run.Stage switch
        {
            GrocerySupplyStage.NeedOrder => Supplier.Door,
            GrocerySupplyStage.Reserved => Supplier.Parking,
            GrocerySupplyStage.Loaded => Grocery.Parking,
            GrocerySupplyStage.Delivered => Grocery.Door,
            _ => null
        };

        if (_window.Visible)
        {
            _prompt.Text = Text("Esc  Fechar janela", "Esc  Close panel");
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                SetWindow(false);
            }
            return;
        }

        var (label, action) = NearbyAction();
        _prompt.Text = label is not null
            ? $"F  {label}"
            : Player.IsDriving
                ? Text("E  Sair da caminhonete", "E  Exit the pickup")
                : _indoors is null && Near(Player.GlobalPosition, Pickup.GlobalPosition, 2.55f)
                    ? Text("E  Entrar na caminhonete", "E  Enter the pickup")
                    : "";
        if (action is not null && Input.IsActionJustPressed("shop_interact"))
        {
            action();
        }
    }

    private (string? Label, Action? Action) NearbyAction()
    {
        if (Player.IsDriving)
        {
            return (null, null);
        }

        if (_indoors is { } store)
        {
            if (store == Supplier && _run.Stage == GrocerySupplyStage.NeedOrder && Near(Player.GlobalPosition, store.Counter, 1.85f))
            {
                return (Text("Fazer pedido", "Place an order"), () =>
                {
                    _desk.OpenOrder();
                    SetWindow(true);
                });
            }
            if (store == Grocery && _run.Stage is GrocerySupplyStage.Delivered or GrocerySupplyStage.Trading &&
                Near(Player.GlobalPosition, store.Counter, 1.85f))
            {
                return (Text("Usar o caixa", "Use the register"), OpenRegister);
            }
            if (Near(Player.GlobalPosition, store.JustInside, 1.2f))
            {
                return (Text("Sair", "Leave"), () => Exit(store));
            }
            return (null, null);
        }

        var atTailgate = Near(Player.GlobalPosition, Pickup.ToGlobal(new Vector3(0, 0, 1.8f)), 2.25f);
        if (atTailgate && _run.Stage == GrocerySupplyStage.Reserved && ParkedAt(Supplier))
        {
            return (Text("Carregar caixas", "Load cartons"), () =>
            {
                if (_run.TryLoad(ParkedAt(Supplier)))
                {
                    Pickup.ShowCargo(_run.UnitsOnPickup);
                }
            });
        }
        if (atTailgate && _run.Stage == GrocerySupplyStage.Loaded && ParkedAt(Grocery))
        {
            return (Text("Descarregar na BOXBOX", "Unload at BOXBOX"), () =>
            {
                if (_run.TryUnload(ParkedAt(Grocery)))
                {
                    Pickup.ShowCargo(_run.UnitsOnPickup);
                }
            });
        }
        if (Near(Player.GlobalPosition, Supplier.Door, 1.85f))
        {
            return (Text("Entrar no atacado", "Enter the wholesaler"), () => Enter(Supplier));
        }
        if (Near(Player.GlobalPosition, Grocery.Door, 1.85f))
        {
            return (Text("Entrar na BOXBOX", "Enter BOXBOX"), () => Enter(Grocery));
        }
        return (null, null);
    }

    private void OpenRegister()
    {
        if (_run.TryStartTrading())
        {
            _desk.OpenStore();
        }
        SetWindow(true);
    }

    private bool ParkedAt(StreetStore store) =>
        Pickup.IsParked && Near(Pickup.GlobalPosition, store.Parking, 3.6f);

    private void Enter(StreetStore store)
    {
        _indoors = store;
        store.SetInterior(true);
        Player.SetInterior(store);
        Player.GlobalPosition = store.JustInside + Vector3.Up * 0.04f;
        Minimap.Hide();
        if (store == Supplier && !_supplierIntroduced)
        {
            _supplierIntroduced = true;
            _dialogue.Play(GD.Load<DialogueLine>("res://Content/Dialogue/wholesaler-welcome.tres"));
        }
    }

    private void Exit(StreetStore store)
    {
        Player.GlobalPosition = store.Door;
        Player.SetInterior(null);
        store.SetInterior(false);
        _indoors = null;
        _dialogue.Stop();
        Minimap.Show();
    }

    private void SetWindow(bool visible)
    {
        _window.Visible = visible;
        Player.ShopMenuOpen = visible;
    }

    private void RefreshCash() => _cash.ShowBalance(
        _desk.CashCents, GroceryNeighborhoodProgress.OpeningWalletCents, _desk.IsEnglish);

    private string Text(string pt, string en) => _desk.IsEnglish ? en : pt;

    private static bool Near(Vector3 first, Vector3 second, float radius) =>
        new Vector2(first.X - second.X, first.Z - second.Z).LengthSquared() <= radius * radius;
}
