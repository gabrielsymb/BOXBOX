# Viagem de abastecimento (mercearia ↔ atacado)

Primeiro loop jogável: a caçamba física da caminhonete ligada às regras de estoque e caixa
do núcleo (`Assets/BoxBox/Core`). Decisão de 2026-11-24.

## Regras (Core/Domain/ShopRules.cs)

O dia começa parado na mercearia. Fases: `AtStore → DrivingToWholesaler → AtWholesaler →
DrivingToStore → UnloadPending → AtStore`, quantas viagens quiser, e só então `Open → Closed`.

- **Paga no balcão:** `BuyAtWholesaler` só funciona parado no atacado; o dinheiro sai na hora.
- **Atacado com estoque limitado:** `wholesalerStock` por produto no catálogo JSON;
  `RestockWholesaler` repõe ao máximo (chamado ao criar o dia; repor a cada dia vem com o ciclo de dias).
- **Carga não é estoque:** o que foi comprado fica em `CargoAt(i)` até descarregar.
- **Só entra o que chegou:** `UnloadCargo(delivered)` recebe quantas caixas de cada produto
  ainda estão na caçamba. O resto é prejuízo: `LostGoods` (custo de atacado) e `UnitsLost`,
  que aparecem no relatório (`NetProfit = GrossProfit − LostGoods`).
- Toda operação valida tudo antes de alterar qualquer coisa; uma compra rejeitada não muda
  dinheiro, estoque do atacado nem carga.

## Eventos (Core/Application/ShopEvents.cs)

`FirstSliceSession` publica `ShopEvent` (fase mudou, carga comprada, carga descarregada,
cliente atendido) num `IShopEventSink`, só depois de o comando ter sucesso. A HUD da SandboxLab
é um desses ouvintes.

## Na SandboxLab (Presentation/Supply)

- Mercearia: piso verde no ponto de partida. Atacado: piso amarelo do outro lado das avenidas;
  siga a linha azul (GPS) no chão. Ver [ROADS.md](ROADS.md).
- No atacado, **parado**: teclas **1/2/3** compram 1 caixa do produto (cada caixa = 1 unidade,
  colorida por produto). Capacidade da caçamba: 18 caixas.
- Na mercearia, parado: **E** descarrega (conta as caixas que ainda estão na caçamba);
  **O** abre a loja, atende os clientes do dia e mostra o relatório; **N** começa o próximo dia
  (caixa e estoque continuam, atacado repõe, totais do dia zeram).
- A chegada vale pela posição: dá pra voltar ao atacado com carga e comprar mais.
- O painel mostra os 5 passos do ciclo como checklist, com o passo atual em negrito.

## Origem

- Moonforge RPG Engine (MIT, 3583Bytes), revisão `3864fc36`: ordem de validação de
  `Shops/Commands/BuyFromShopCommandHandler.cs`, estoque máximo + reposição de
  `Shops/Commands/ShopCommandHelpers.cs` (`EnsureRestocked`), e o padrão de event sink de
  `Runtime/Events/IDomainEventSink.cs`. Adaptado, não copiado: o BOXBOX usa um único
  atacado, dinheiro em centavos e fases de viagem.
- Endless Sky é GPL-3: só referência de design, nenhum código.
