using System.Globalization;
using Boxbox.Application.BusinessDay;
using Boxbox.Domain.Commerce;
using Boxbox.Domain.Market;
using Boxbox.Presentation.Hud;
using Godot;

namespace Boxbox.Presentation.Grocery;

public partial class GroceryDesk : Control
{
    private const string Art = "res://Assets/Grocery/";
    private static readonly Color Background = new("#142724");
    private static readonly Color Highlight = new("#f3c368");
    private static readonly Color Paper = new("#fbf3de");

    public GroceryOrderSession OrderSession { get; set; } = new();
    private int _selected;
    private bool _english;
    private bool _firstOrder = true;
    private VBoxContainer _page = null!;
    private readonly Dictionary<string, Texture2D> _textures = [];

    public bool Embedded { get; set; }
    public long CashCents { get; private set; } = GroceryDayOneScenario.OpeningCashCents;
    public event Action? CashChanged;

    private void UpdateCash(long cents)
    {
        if (CashCents != cents)
        {
            CashCents = cents;
            CashChanged?.Invoke();
        }
    }
    public event Action? OrderConfirmed;
    public event Action? ReplayRequested;
    public event Action? LanguageChanged;
    public bool IsEnglish => _english;
    public void OpenOrder() => ShowOrder();
    public void OpenStore() => RunDay();

    public override void _Ready()
    {
        _selected = OrderSession.Decisions.Select(item => item.ProductId).ToList().IndexOf(GroceryOrderSession.FirstOrderProduct);
        ShowOrder();
    }

    private string T(string pt, string en) => _english ? en : pt;

    private string Money(long cents)
    {
        var culture = CultureInfo.GetCultureInfo(_english ? "en-US" : "pt-BR");
        return $"R$ {(cents / 100m).ToString("N2", culture)}";
    }

    private string ProductName(string id) => id switch
    {
        "rice" => T("Arroz", "Rice"),
        "soda" => T("Refrigerante", "Soda"),
        _ => T("Leite", "Milk")
    };

    private static string ProductArt(string id) => id switch
    {
        "rice" => "bag",
        "soda" => "soda-can",
        _ => "carton"
    };

    private Texture2D Texture(string asset)
    {
        if (!_textures.TryGetValue(asset, out var texture))
        {
            texture = GD.Load<Texture2D>($"{Art}{asset}.png");
            _textures.Add(asset, texture);
        }
        return texture;
    }

    private void Page(Action switchLanguage)
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        var backdrop = new ColorRect { Color = Background, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var scroll = new ScrollContainer();
        AddChild(scroll);
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.AddChild(center);

        _page = new VBoxContainer();
        _page.AddThemeConstantOverride("separation", 20);
        center.AddChild(_page);

        var bar = Row();
        var brand = LabelFor(Embedded ? "" : "BOXBOX", 28, Highlight);
        brand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bar.AddChild(brand);
        var language = Icon(bar, "gear", T("Mudar para inglês", "Switch to Portuguese"), 34);
        language.Pressed += () =>
        {
            _english = !_english;
            switchLanguage();
            LanguageChanged?.Invoke();
        };
    }

    private void ShowOrder()
    {
        Page(ShowOrder);
        Action refresh = null!;
        TextureRect? objectiveIcon = null;
        Label? objective = null;
        if (_firstOrder)
        {
            var objectiveRow = Row();
            objectiveIcon = Picture(objectiveRow, "target", 34);
            objectiveIcon.SelfModulate = HudColors.MissionPink;
            objective = LabelFor("", 20, Paper);
            objectiveRow.AddChild(objective);
        }

        var products = Row();
        var names = new List<Label>();
        var selection = new List<TextureButton>();
        var quantities = new List<Label>();
        TextureRect? milkMarker = null;
        for (var index = 0; index < OrderSession.Decisions.Count; index++)
        {
            var slot = index;
            var item = OrderSession.Decisions[index];
            var product = Stack(products);
            var image = Icon(product, ProductArt(item.ProductId), ProductName(item.ProductId), 118);
            image.Pressed += () =>
            {
                _selected = slot;
                refresh();
            };
            selection.Add(image);
            var name = LabelFor(ProductName(item.ProductId), 20, Paper);
            product.AddChild(name);
            names.Add(name);
            if (_firstOrder)
            {
                var marker = Picture(product, "target", 20);
                if (item.ProductId == GroceryOrderSession.FirstOrderProduct)
                {
                    milkMarker = marker;
                }
                else
                {
                    // Reserve the same space in each slot so the shelf stays aligned.
                    marker.Modulate = Colors.Transparent;
                }
            }
            var quantity = LabelFor("", 23, Paper);
            product.AddChild(quantity);
            quantities.Add(quantity);
        }

        var selectedProduct = LabelFor("", 25, Highlight);
        _page.AddChild(selectedProduct);
        var controls = Row();
        var supplierGroup = Stack(controls);
        var supplierName = LabelFor("", 17, Paper);
        supplierGroup.AddChild(supplierName);
        var supplier = Icon(supplierGroup, "cart", "", 44);
        var unitCost = LabelFor("", 20, Paper);
        supplierGroup.AddChild(unitCost);
        var amount = Stepper(controls, "basket", T("Quantidade", "Quantity"),
            () => ChangeQuantity(-1), () => ChangeQuantity(1));
        var priceColumn = Stack(controls);
        priceColumn.AddChild(LabelFor(T("Preço na prateleira", "Shelf price"), 17, Paper));
        Picture(priceColumn, "cash-register", 40);
        var price = LabelFor("", 24, Paper);
        priceColumn.AddChild(price);
        supplier.Pressed += () =>
        {
            OrderSession.CycleSupplier(_selected);
            refresh();
        };

        var checkout = Row();
        var cashGroup = Stack(checkout);
        cashGroup.AddChild(LabelFor(T("Total do pedido", "Order total"), 17, Paper));
        var cashValue = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cashGroup.AddChild(cashValue);
        Picture(cashValue, "cash-register", 36);
        var balance = LabelFor("", 24, Paper);
        cashValue.AddChild(balance);
        var confirmation = Stack(checkout);
        var open = Icon(confirmation, "wall-door-rotate", T("Abrir a mercearia", "Open the store"), 43);
        confirmation.AddChild(LabelFor(
            Embedded ? T("Confirmar pedido", "Confirm order") : T("Abrir a mercearia", "Open the store"),
            17, Paper));
        if (Embedded)
        {
            open.TextureNormal = Texture("checkmark");
        }
        open.Pressed += () =>
        {
            if (Embedded)
            {
                if (OrderSession.TryConfirm() is not { } confirmedCash)
                {
                    return;
                }

                UpdateCash(confirmedCash);
                OrderConfirmed?.Invoke();
            }
            else
            {
                RunDay();
            }
        };

        refresh = () =>
        {
            for (var index = 0; index < OrderSession.Decisions.Count; index++)
            {
                var active = index == _selected;
                selection[index].SelfModulate = active ? Colors.White : new Color("#91a29a");
                names[index].AddThemeColorOverride("font_color", active ? Highlight : Paper);
                quantities[index].Text = $"×{OrderSession.Decisions[index].OrderQuantity}";
                quantities[index].AddThemeColorOverride("font_color", active ? Highlight : Paper);
            }

            var item = OrderSession.Decisions[_selected];
            selectedProduct.Text = ProductName(item.ProductId);
            var offer = OrderSession.OfferAt(_selected);
            var emergency = offer.Channel == SupplierChannel.Emergency;
            supplier.TextureNormal = Texture(emergency ? "fastForward" : "cart");
            supplier.TooltipText = emergency
                ? T($"Urgente · {Money(offer.UnitCostCents)} / un.", $"Emergency · {Money(offer.UnitCostCents)} / unit")
                : T($"Atacado · {Money(offer.UnitCostCents)} / un.", $"Wholesale · {Money(offer.UnitCostCents)} / unit");
            supplierName.Text = emergency ? T("Urgente", "Emergency") : T("Atacado", "Wholesale");
            unitCost.Text = Money(offer.UnitCostCents);
            amount.Text = $"×{item.OrderQuantity}";
            price.Text = Money(item.SalePriceCents);
            var affordable = OrderSession.DraftFitsBudget;
            balance.Text = Money(OrderSession.DraftCostCents);
            balance.AddThemeColorOverride("font_color", !affordable ? new Color("#ff9d88") : Paper);
            open.Disabled = !affordable;
            if (objective is not null && objectiveIcon is not null && milkMarker is not null)
            {
                var reachedTarget = OrderSession.ReachedFirstTarget;
                var completed = reachedTarget && affordable;
                objectiveIcon.Texture = Texture(completed ? "checkmark" : "target");
                objectiveIcon.SelfModulate = completed ? Paper : HudColors.MissionPink;
                objective.Text = completed
                    ? (Embedded ? T("Pedido pronto para confirmar.", "Order ready to confirm.") : T("Pedido pronto. Abra pela porta.", "Order ready. Click the door to open."))
                    : reachedTarget
                        ? T("Ajuste o pedido para caber no caixa.", "Bring the order within budget.")
                        : T($"Pra começar, separa {GroceryOrderSession.FirstOrderTarget} leites.", $"Start with {GroceryOrderSession.FirstOrderTarget} cartons of milk.");
                milkMarker.Modulate = reachedTarget ? Colors.Transparent : HudColors.MissionPink;
            }
            open.TooltipText = !affordable
                ? T("O pedido passa do caixa disponível", "The order exceeds your cash")
                : Embedded ? T("Confirmar pedido", "Confirm order") : T("Abrir a mercearia", "Open the store");
        };
        refresh();

        void ChangeQuantity(int step)
        {
            OrderSession.ChangeQuantity(_selected, step);
            refresh();
        }

    }

    private void RunDay()
    {
        if (OrderSession.TryStart(Embedded) is not { } day)
        {
            return;
        }

        _firstOrder = false;
        ShowDay(day);
    }

    private void ShowDay(GroceryTradingDay day)
    {
        Page(() => ShowDay(day));
        var clock = Row();
        Picture(clock, "wall-door-rotate", 80);
        var clockText = LabelFor("", 37, Highlight);
        clock.AddChild(clockText);
        var market = Row();
        var product = Picture(market, "shelf-boxes", 150);
        var outcome = Picture(market, "basket", 65);
        var productName = LabelFor("", 21, Paper);
        _page.AddChild(productName);
        var cash = Row();
        var cashText = LabelFor("", 29, Paper);
        if (!Embedded)
        {
            Picture(cash, "cash-register", 64);
        }
        cashText.Visible = !Embedded;
        cash.AddChild(cashText);
        Icon(cash, "fastForward", T("Ver fechamento", "See closing results"), 52).Pressed += () => ShowReport(day.Close().Report);

        var timer = new Godot.Timer { WaitTime = 0.75 };
        _page.AddChild(timer);
        timer.Timeout += () =>
        {
            if (!day.HasPendingCustomers)
            {
                ShowReport(day.Close().Report);
                return;
            }
            day.AdvanceToNextCustomer();
            Refresh();
        };
        Refresh();
        timer.Start();

        void Refresh()
        {
            var latest = day.LatestResolution;
            var minute = day.Minute;
            clockText.Text = $"{minute / 60:00}:{minute % 60:00}";
            var cashCents = day.CashCents;
            cashText.Text = Money(cashCents);
            UpdateCash(cashCents);
            if (latest is null)
            {
                return;
            }

            product.Texture = Texture(ProductArt(latest.ProductId));
            productName.Text = ProductName(latest.ProductId);
            product.TooltipText = productName.Text;
            var (icon, description) = latest.Outcome switch
            {
                MarketPurchaseOutcomeKind.PlayerStoreSale => ("checkmark", T("Vendeu na BOXBOX", "Sold at BOXBOX")),
                MarketPurchaseOutcomeKind.CompetitorSale => ("home", T("Comprou no rival", "Bought from the rival")),
                MarketPurchaseOutcomeKind.ChoseNotToPurchase => ("cross", T("Desistiu", "Did not buy")),
                _ => ("warning", T("Sem estoque no bairro", "Out of stock in the neighborhood"))
            };
            outcome.Texture = Texture(icon);
            outcome.TooltipText = description;
        }
    }

    private void ShowReport(BusinessDayReport report)
    {
        UpdateCash(report.ClosingCashCents);
        Page(() => ShowReport(report));
        var totals = Row();
        if (!Embedded)
        {
            Value(totals, "cash-register", Money(report.ClosingCashCents), T("Caixa final", "Closing cash"), 95);
        }
        Value(totals, "trophy", Money(report.GrossResultCents), T("Resultado bruto", "Gross profit"), 68);

        var products = Row();
        foreach (var item in report.Products)
        {
            Value(products, ProductArt(item.ProductId), $"{item.SoldUnits}/{item.PurchasedUnits}",
                T($"{ProductName(item.ProductId)} · vendidas/compradas",
                    $"{ProductName(item.ProductId)} · sold/ordered"), 116, ProductName(item.ProductId));
        }

        var outcomes = Row();
        Value(outcomes, "checkmark", report.PlayerSales.ToString(), T("Vendas BOXBOX", "BOXBOX sales"), 40);
        Value(outcomes, "home", report.CompetitorSales.ToString(), T("Vendas do rival", "Rival sales"), 40);
        Value(outcomes, "cross", report.NoPurchases.ToString(), T("Desistências", "Did not buy"), 40);
        Value(outcomes, "warning", report.UnservedDemand.ToString(), T("Demanda sem oferta", "Unserved demand"), 40);
        Icon(Row(), "return", T("Repetir Dia 1", "Replay Day 1"), 58).Pressed += () =>
        {
            if (Embedded)
            {
                OrderSession.ResetConfirmation();
                UpdateCash(GroceryDayOneScenario.OpeningCashCents);
                ReplayRequested?.Invoke();
            }
            else
            {
                ShowOrder();
            }
        };
    }

    private HBoxContainer Row()
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 26);
        _page.AddChild(row);
        return row;
    }

    private static VBoxContainer Stack(Container parent)
    {
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 4);
        parent.AddChild(column);
        return column;
    }

    private TextureRect Picture(Container parent, string asset, int size)
    {
        var picture = new TextureRect
        {
            Texture = Texture(asset),
            CustomMinimumSize = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Pass
        };
        parent.AddChild(picture);
        return picture;
    }

    private TextureButton Icon(Container parent, string asset, string tooltip, int size)
    {
        var button = new TextureButton
        {
            TextureNormal = Texture(asset),
            TextureDisabled = Texture("warning"),
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(size, size),
            TooltipText = tooltip,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            FocusMode = FocusModeEnum.All
        };
        button.MouseEntered += () => button.Modulate = Highlight;
        button.MouseExited += () => button.Modulate = Colors.White;
        button.FocusEntered += () => button.Modulate = Highlight;
        button.FocusExited += () => button.Modulate = Colors.White;
        parent.AddChild(button);
        return button;
    }

    private Label Stepper(Container parent, string asset, string tooltip, Action decrease, Action increase)
    {
        var group = Stack(parent);
        group.AddChild(LabelFor(tooltip, 17, Paper));
        Picture(group, asset, 40).TooltipText = tooltip;
        var row = new HBoxContainer();
        group.AddChild(row);
        Icon(row, "minus", $"{tooltip} −", 34).Pressed += decrease;
        var value = LabelFor("", 24, Paper);
        value.CustomMinimumSize = new Vector2(100, 0);
        row.AddChild(value);
        Icon(row, "plus", $"{tooltip} +", 34).Pressed += increase;
        return value;
    }

    private void Value(Container parent, string asset, string value, string tooltip, int size, string? name = null)
    {
        var column = Stack(parent);
        Picture(column, asset, size).TooltipText = tooltip;
        if (name is not null)
        {
            column.AddChild(LabelFor(name, 20, Paper));
        }
        column.AddChild(LabelFor(value, 25, Paper));
    }

    private static Label LabelFor(string value, int size, Color color)
    {
        var label = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
