using System.Globalization;
using Godot;

namespace Boxbox.Presentation.Grocery;

public partial class CashHud : CanvasLayer
{
    public void ShowBalance(long cashCents, long walletCents, bool english)
    {
        WriteBalance("Frame", english ? "Store cash" : "Caixa da loja", cashCents);
        WriteBalance("WalletFrame", english ? "Personal wallet" : "Carteira pessoal", walletCents);

        void WriteBalance(string frame, string title, long amount)
        {
            var culture = CultureInfo.GetCultureInfo(english ? "en-US" : "pt-BR");
            GetNode<Label>($"{frame}/Content/Readout/Title").Text = title;
            GetNode<Label>($"{frame}/Content/Readout/Balance").Text = $"R$ {(amount / 100m).ToString("N2", culture)}";
        }
    }
}
