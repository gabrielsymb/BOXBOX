using System.Text;
using BoxBox.Application;
using BoxBox.Domain;
using BoxBox.Infrastructure;
using BoxBox.Presentation;
using BoxBox.Presentation.Vehicles;
using BoxBox.Simulation;
using UnityEngine;

namespace BoxBox.Presentation.Supply
{
    /// <summary>
    /// Connects the physical pickup to the shop rules. Driving onto a site's parking pad is
    /// arriving there; buying puts real crates in the bed; unloading counts the crates that are
    /// still in the bed, so crates lost on the road never become stock.
    /// The rules live in BoxBox.Domain; this class only translates the world into commands.
    /// </summary>
    public sealed class SupplyRunController : MonoBehaviour, IShopEventSink
    {
        private const float MaxTradeSpeedKmh = 5f;
        private const DisplayLanguage Language = DisplayLanguage.Portuguese;
        private static readonly Color[] ProductColors =
        {
            new Color(0.3f, 0.6f, 1f), new Color(0.85f, 0.65f, 0.3f), new Color(0.9f, 0.3f, 0.3f),
            new Color(0.4f, 0.8f, 0.4f), new Color(0.8f, 0.4f, 0.9f)
        };

        private VehiclePhysicsController _vehicle;
        private CargoBed _cargo;
        private FirstSliceSession _session;
        private Bounds _store, _wholesaler;
        private string _message = "";
        private DailyReport _report;

        private ShopState State => _session.State;
        private bool IsStopped => Mathf.Abs(_vehicle.SpeedKmh) < MaxTradeSpeedKmh;

        /// <summary>Where the player should drive now; null while trading or with nothing to do.</summary>
        public Vector3? Destination
        {
            get
            {
                if (_session == null) return null;
                switch (State.Phase)
                {
                    case ShopPhase.AtStore: return HasStock() ? (Vector3?)null : _wholesaler.center;
                    case ShopPhase.DrivingToWholesaler: return _wholesaler.center;
                    case ShopPhase.DrivingToStore: return _store.center;
                    default: return null;
                }
            }
        }

        public void Init(VehiclePhysicsController vehicle, CargoBed cargo, Vector3 storePad, Vector3 wholesalerPad)
        {
            _vehicle = vehicle;
            _cargo = cargo;
            _store = SupplySiteBuilder.Build("Mercearia", storePad, new Vector2(12f, 14f),
                new Vector3(-10f, 0f, 0f), new Vector3(6f, 4f, 12f), new Color(0.3f, 0.75f, 0.35f));
            _wholesaler = SupplySiteBuilder.Build("Atacado", wholesalerPad, new Vector2(14f, 16f),
                new Vector3(13f, 0f, 0f), new Vector3(10f, 8f, 22f), new Color(0.95f, 0.7f, 0.2f));

            ProductCatalog catalog = ProductCatalogLoader.Load();
            ShopState state = ShopRules.CreateNew(catalog.Products, catalog.OpeningCash, catalog.DailyCustomerCount);
            _session = new FirstSliceSession(state, new SeededShopperPolicy(20261124), this);
        }

        private void Update()
        {
            if (_session == null) return;
            TrackArrivals();
            HandleTradeKeys();
        }

        private void TrackArrivals()
        {
            Vector3 p = _vehicle.transform.position;
            bool inStore = _store.Contains(new Vector3(p.x, _store.center.y, p.z));
            bool inWholesaler = _wholesaler.Contains(new Vector3(p.x, _wholesaler.center.y, p.z));

            switch (State.Phase)
            {
                case ShopPhase.AtStore when !inStore: _session.LeaveForWholesaler(); break;
                case ShopPhase.DrivingToWholesaler when inWholesaler: _session.ArriveAtWholesaler(); break;
                case ShopPhase.DrivingToWholesaler when inStore: _session.ArriveAtStore(); break;
                case ShopPhase.AtWholesaler when !inWholesaler: _session.LeaveWholesaler(); break;
                case ShopPhase.DrivingToStore when inWholesaler: _session.ArriveAtWholesaler(); break;
                case ShopPhase.DrivingToStore when inStore: _session.ArriveAtStore(); break;
            }
        }

        private void HandleTradeKeys()
        {
            if (!IsStopped) return;

            if (State.Phase == ShopPhase.AtWholesaler)
            {
                for (int i = 0; i < State.Products.Count && i < 9; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) BuyOneCrate(i);
            }
            else if (State.Phase == ShopPhase.UnloadPending && Input.GetKeyDown(KeyCode.E))
            {
                Unload();
            }
            else if (State.Phase == ShopPhase.AtStore && Input.GetKeyDown(KeyCode.O))
            {
                RunBusinessDay();
            }
            else if (State.Phase == ShopPhase.Closed && Input.GetKeyDown(KeyCode.N))
            {
                StartNextDay();
            }
        }

        private void StartNextDay()
        {
            if (_session.StartNextDay().IsSuccess) _report = null;
        }

        private void BuyOneCrate(int index)
        {
            if (!_cargo.HasFreeSlot())
            {
                _message = Text("supply.bedFull");
                return;
            }

            var quantities = new int[State.Products.Count];
            quantities[index] = 1;
            OperationResult result = _session.BuyAtWholesaler(quantities);
            if (result.IsSuccess)
                _cargo.TryAddCrate(State.Products[index].Id, ProductColors[index % ProductColors.Length]);
            else
                _message = result.Code.ToString();
        }

        private void Unload()
        {
            var delivered = new int[State.Products.Count];
            for (int i = 0; i < delivered.Length; i++)
                delivered[i] = Mathf.Min(_cargo.CountOnBoard(State.Products[i].Id), State.CargoAt(i));

            if (_session.UnloadCargo(delivered).IsSuccess)
                _cargo.ClearAll();
        }

        private void RunBusinessDay()
        {
            if (!HasStock())
            {
                _message = Text("supply.emptyStore");
                return;
            }
            if (!_session.OpenStore().IsSuccess) return;
            while (State.CustomersRemaining > 0 && _session.ServeNextCustomer().IsSuccess) { }
            if (_session.CloseStore().IsSuccess)
                _report = _session.CreateReport();
        }

        private bool HasStock()
        {
            for (int i = 0; i < State.Products.Count; i++)
                if (State.InventoryAt(i) > 0) return true;
            return false;
        }

        public void Publish(ShopEvent shopEvent)
        {
            switch (shopEvent.Kind)
            {
                case ShopEventKind.CargoPurchased:
                    _message = "-" + Format(shopEvent.Amount);
                    break;
                case ShopEventKind.CargoUnloaded:
                    _message = shopEvent.UnitsLost > 0
                        ? $"{Text("supply.lost")}: {shopEvent.UnitsLost} ({Format(shopEvent.Amount)})"
                        : Text("phase.ready");
                    break;
                case ShopEventKind.PhaseChanged:
                    _message = "";
                    break;
            }
        }

        private void OnGUI()
        {
            if (_session == null) return;
            GUILayout.BeginArea(new Rect(Screen.width - 376f, 16f, 360f, 420f), GUI.skin.box);
            GUILayout.Label($"<b>{string.Format(Text("supply.day"), State.Day)} · {PhaseText(State.Phase)}</b>   " +
                            $"{Text("cash")}: <b>{Format(State.Cash)}</b>");
            GUILayout.Label(StepList());
            if (!IsStopped && (State.Phase == ShopPhase.AtWholesaler || State.Phase == ShopPhase.UnloadPending))
                GUILayout.Label($"<color=orange>{Text("supply.stop")}</color>");
            if (_message.Length > 0) GUILayout.Label($"<b>{_message}</b>");
            GUILayout.Space(4f);
            GUILayout.Label(ProductTable());
            DrawActionButtons();
            if (State.UnitsLost > 0)
                GUILayout.Label($"{Text("supply.lost")}: {State.UnitsLost} ({Format(State.LostGoods)})");
            if (_report != null) GUILayout.Label(ReportText(_report));
            GUILayout.EndArea();
        }

        // Same actions as the keys, as buttons, so the next step is always visible.
        private void DrawActionButtons()
        {
            GUI.enabled = IsStopped;
            if (State.Phase == ShopPhase.AtWholesaler)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < State.Products.Count; i++)
                    if (GUILayout.Button($"{i + 1}: +1 {LocalizationCatalog.ProductName(State.Products[i], Language)}"))
                        BuyOneCrate(i);
                GUILayout.EndHorizontal();
            }
            else if (State.Phase == ShopPhase.UnloadPending && GUILayout.Button(Text("supply.unload")))
            {
                Unload();
            }
            else if (State.Phase == ShopPhase.AtStore && HasStock() && GUILayout.Button(Text("supply.open")))
            {
                RunBusinessDay();
            }
            else if (State.Phase == ShopPhase.Closed && GUILayout.Button(Text("supply.nextDay")))
            {
                StartNextDay();
            }
            GUI.enabled = true;
        }

        // The whole loop as a checklist: done steps greyed, the current one bold with the distance.
        private string StepList()
        {
            int current = CurrentStep();
            var sb = new StringBuilder();
            for (int step = 1; step <= 5; step++)
            {
                string text = string.Format(Text("supply.step" + step), State.Products.Count);
                if (step == current)
                {
                    sb.Append("▶ <b>").Append(text).Append("</b>");
                    if (step == 1) sb.Append($" ({Distance(_wholesaler):0} m)");
                    if (step == 3) sb.Append($" ({Distance(_store):0} m)");
                }
                else sb.Append(step < current ? "<color=#888>✓ " : "<color=#aaa>   ").Append(text).Append("</color>");
                if (step < 5) sb.Append('\n');
            }
            return sb.ToString();
        }

        private int CurrentStep()
        {
            switch (State.Phase)
            {
                case ShopPhase.AtStore: return HasStock() ? 5 : 1;
                case ShopPhase.DrivingToWholesaler: return 1;
                case ShopPhase.AtWholesaler: return 2;
                case ShopPhase.DrivingToStore: return 3;
                case ShopPhase.UnloadPending: return 4;
                default: return 6;   // open or closed: every step done
            }
        }

        private float Distance(Bounds site) => Vector3.Distance(_vehicle.transform.position, site.center);

        private string ProductTable()
        {
            var sb = new StringBuilder();
            sb.Append($"{Text("supply.cargo")} / {Text("supply.stock")} / {Text("supply.atWholesaler")}\n");
            for (int i = 0; i < State.Products.Count; i++)
            {
                ProductDefinition product = State.Products[i];
                sb.Append($"{i + 1}. {LocalizationCatalog.ProductName(product, Language)} ({Format(product.WholesaleCost)}): " +
                          $"<b>{_cargo.CountOnBoard(product.Id)}/{State.CargoAt(i)}</b> · {State.InventoryAt(i)} · {State.WholesalerStockAt(i)}\n");
            }
            return sb.ToString();
        }

        private string ReportText(DailyReport r)
        {
            return $"<b>{Text("phase.closed")}</b>\n{Text("supply.sales")}: {Format(r.SalesRevenue)} ({r.UnitsSold})\n" +
                   $"{Text("supply.lost")}: {Format(r.LostGoods)}\n{Text("supply.netProfit")}: <b>{Format(r.NetProfit)}</b>";
        }

        private static string PhaseText(ShopPhase phase)
        {
            switch (phase)
            {
                case ShopPhase.AtStore: return Text("phase.ready");
                case ShopPhase.DrivingToWholesaler: return Text("phase.toWholesaler");
                case ShopPhase.AtWholesaler: return Text("phase.pickup");
                case ShopPhase.DrivingToStore: return Text("phase.toStore");
                case ShopPhase.UnloadPending: return Text("phase.unload");
                case ShopPhase.Open: return Text("phase.open");
                default: return Text("phase.closed");
            }
        }

        private static string Text(string key) => LocalizationCatalog.Text(key, Language);
        private static string Format(Money money) => LocalizationCatalog.FormatMoney(money, Language);
    }
}
