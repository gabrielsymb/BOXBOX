using System.Collections.Generic;
using System.Globalization;
using BoxBox.Domain;
using UnityEngine;

namespace BoxBox.Presentation
{
    public enum DisplayLanguage
    {
        Portuguese,
        English
    }

    public static class LocalizationCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> Portuguese =
            new Dictionary<string, string>
            {
                ["title"] = "BOXBOX | Primeiro dia",
                ["language"] = "Idioma",
                ["cash"] = "Caixa",
                ["phase.toWholesaler"] = "A caminho do atacado",
                ["phase.pickup"] = "Retirada no atacado",
                ["phase.toStore"] = "Voltando para a mercearia",
                ["phase.unload"] = "Descarga",
                ["phase.ready"] = "Na mercearia",
                ["phase.open"] = "Loja aberta",
                ["phase.closed"] = "Expediente encerrado",
                ["supply.store"] = "Mercearia",
                ["supply.wholesaler"] = "Atacado",
                ["supply.stop"] = "Pare o veículo para negociar",
                ["supply.buy"] = "Teclas 1–{0}: comprar 1 caixa",
                ["supply.bedFull"] = "Caçamba cheia",
                ["supply.unload"] = "E: descarregar",
                ["supply.open"] = "O: abrir a loja e atender os clientes",
                ["supply.cargo"] = "Na caçamba",
                ["supply.stock"] = "Na loja",
                ["supply.atWholesaler"] = "estoque do atacado",
                ["supply.lost"] = "Perdido na estrada",
                ["supply.netProfit"] = "Lucro líquido",
                ["supply.sales"] = "Vendas",
                ["supply.goTo"] = "Dirija até: {0}",
                ["supply.emptyStore"] = "Loja vazia: compre no atacado primeiro",
                ["supply.goal"] = "Objetivo: buscar mercadoria no atacado (piso amarelo) e trazer na caçamba",
                ["order.heading"] = "Monte o pedido",
                ["order.cost"] = "Custo por unidade",
                ["order.confirm"] = "Confirmar e sair",
                ["drive.controls"] = "W/S acelera ou recua; A/D vira. Freie perto do destino.",
                ["drive.distance"] = "Distância até o destino",
                ["drive.arrive"] = "Confirmar chegada",
                ["pickup.action"] = "Carregar a caminhonete",
                ["unload.action"] = "Descarregar na loja",
                ["store.open"] = "Abrir a mercearia",
                ["store.customers"] = "Clientes restantes",
                ["store.price"] = "Preço",
                ["store.competitor"] = "Concorrente",
                ["store.serve"] = "Atender próximo cliente",
                ["store.close"] = "Encerrar expediente",
                ["report.heading"] = "Resumo do expediente",
                ["report.openingCash"] = "Caixa inicial",
                ["report.purchases"] = "Compra do fornecedor",
                ["report.revenue"] = "Vendas",
                ["report.cogs"] = "Custo das unidades vendidas",
                ["report.grossProfit"] = "Margem bruta",
                ["report.closingCash"] = "Caixa final",
                ["report.unitsSold"] = "Unidades vendidas",
                ["report.competitor"] = "Escolheram o concorrente",
                ["report.passed"] = "Não compraram",
                ["report.stock"] = "Estoque restante",
                ["product.water"] = "Água",
                ["product.bread"] = "Pão",
                ["product.snack"] = "Salgadinho",
                ["message.welcome"] = "Escolha o primeiro pedido da mercearia.",
                ["message.orderPlaced"] = "Pedido pago. Dirija até o atacado.",
                ["message.atWholesaler"] = "Você chegou. A carga ainda não está no veículo.",
                ["message.loaded"] = "Carga conferida e colocada na caminhonete.",
                ["message.atStore"] = "Você voltou. A mercadoria ainda está no veículo.",
                ["message.unloaded"] = "Estoque atualizado após a descarga.",
                ["message.opened"] = "Portas abertas. Preço e estoque influenciam as escolhas.",
                ["message.bought"] = "O cliente comprou na BOXBOX.",
                ["message.competitor"] = "O cliente escolheu o concorrente.",
                ["message.passed"] = "O cliente decidiu não comprar.",
                ["message.closed"] = "Dia encerrado; confira a reconciliação.",
                ["error.wrongPhase"] = "Essa ação não está disponível nesta etapa.",
                ["error.invalidOrder"] = "Revise as quantidades do pedido.",
                ["error.insufficientFunds"] = "O caixa não cobre esse pedido.",
                ["error.unknownProduct"] = "Produto não encontrado no catálogo.",
                ["error.invalidPrice"] = "O preço precisa ser maior que zero.",
                ["error.noCustomers"] = "Não há mais clientes neste expediente.",
                ["error.invalidChoice"] = "Essa compra não é admissível para este cliente.",
                ["error.overflow"] = "A operação excede os limites numéricos do protótipo."
            };

        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>
            {
                ["title"] = "BOXBOX | First day",
                ["language"] = "Language",
                ["cash"] = "Cash",
                ["phase.toWholesaler"] = "Driving to wholesaler",
                ["phase.pickup"] = "Wholesaler pickup",
                ["phase.toStore"] = "Returning to the store",
                ["phase.unload"] = "Unloading",
                ["phase.ready"] = "At the store",
                ["phase.open"] = "Store open",
                ["phase.closed"] = "Business day closed",
                ["supply.store"] = "Grocery",
                ["supply.wholesaler"] = "Wholesaler",
                ["supply.stop"] = "Stop the vehicle to trade",
                ["supply.buy"] = "Keys 1–{0}: buy 1 crate",
                ["supply.bedFull"] = "Bed is full",
                ["supply.unload"] = "E: unload",
                ["supply.open"] = "O: open the store and serve customers",
                ["supply.cargo"] = "In the bed",
                ["supply.stock"] = "In the store",
                ["supply.atWholesaler"] = "wholesaler stock",
                ["supply.lost"] = "Lost on the road",
                ["supply.netProfit"] = "Net profit",
                ["supply.sales"] = "Sales",
                ["supply.goTo"] = "Drive to: {0}",
                ["supply.emptyStore"] = "Store is empty: buy at the wholesaler first",
                ["supply.goal"] = "Goal: pick up goods at the wholesaler (yellow pad) and bring them in the bed",
                ["order.heading"] = "Prepare the order",
                ["order.cost"] = "Unit cost",
                ["order.confirm"] = "Confirm and drive",
                ["drive.controls"] = "W/S accelerate or reverse; A/D steer. Brake near the destination.",
                ["drive.distance"] = "Distance to destination",
                ["drive.arrive"] = "Confirm arrival",
                ["pickup.action"] = "Load the van",
                ["unload.action"] = "Unload at the store",
                ["store.open"] = "Open the store",
                ["store.customers"] = "Customers remaining",
                ["store.price"] = "Price",
                ["store.competitor"] = "Competitor",
                ["store.serve"] = "Serve next customer",
                ["store.close"] = "Close business day",
                ["report.heading"] = "Business day report",
                ["report.openingCash"] = "Opening cash",
                ["report.purchases"] = "Supplier purchase",
                ["report.revenue"] = "Sales",
                ["report.cogs"] = "Cost of units sold",
                ["report.grossProfit"] = "Gross profit",
                ["report.closingCash"] = "Closing cash",
                ["report.unitsSold"] = "Units sold",
                ["report.competitor"] = "Chose the competitor",
                ["report.passed"] = "Did not buy",
                ["report.stock"] = "Remaining stock",
                ["product.water"] = "Water",
                ["product.bread"] = "Bread",
                ["product.snack"] = "Snack",
                ["message.welcome"] = "Choose the store's first order.",
                ["message.orderPlaced"] = "Order paid. Drive to the wholesaler.",
                ["message.atWholesaler"] = "You arrived. The cargo is not in the vehicle yet.",
                ["message.loaded"] = "Cargo checked and loaded into the van.",
                ["message.atStore"] = "You're back. The goods are still in the vehicle.",
                ["message.unloaded"] = "Inventory updated after unloading.",
                ["message.opened"] = "Doors are open. Price and stock affect customer choices.",
                ["message.bought"] = "The customer bought from BOXBOX.",
                ["message.competitor"] = "The customer chose the competitor.",
                ["message.passed"] = "The customer decided not to buy.",
                ["message.closed"] = "Day closed; review the reconciliation.",
                ["error.wrongPhase"] = "That action is not available at this stage.",
                ["error.invalidOrder"] = "Review the order quantities.",
                ["error.insufficientFunds"] = "The cash balance cannot cover this order.",
                ["error.unknownProduct"] = "Product not found in the catalog.",
                ["error.invalidPrice"] = "Price must be greater than zero.",
                ["error.noCustomers"] = "There are no customers left today.",
                ["error.invalidChoice"] = "This purchase is not admissible for this customer.",
                ["error.overflow"] = "The operation exceeds this prototype's numeric limits."
            };

        public static string Text(string key, DisplayLanguage language)
        {
            IReadOnlyDictionary<string, string> catalog =
                language == DisplayLanguage.Portuguese ? Portuguese : English;
            if (catalog.TryGetValue(key, out string text))
                return text;

            Debug.LogError($"Missing localization key '{key}' for {language}.");
            return key;
        }

        public static string ProductName(ProductDefinition product, DisplayLanguage language)
        {
            return Text(product.NameKey, language);
        }

        public static string FormatMoney(Money money, DisplayLanguage language)
        {
            decimal amount = money.Cents / 100m;
            if (language == DisplayLanguage.Portuguese)
                return amount.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

            return "R$ " + amount.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
        }

        public static string FormatNumber(int value, DisplayLanguage language)
        {
            string locale = language == DisplayLanguage.Portuguese ? "pt-BR" : "en-US";
            return value.ToString(CultureInfo.GetCultureInfo(locale));
        }
    }
}
