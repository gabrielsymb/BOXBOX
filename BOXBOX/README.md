# BOXBOX — protótipo técnico

Protótipo em Godot 4.7.2 .NET/C# de um simulador bilíngue de microcomércio brasileiro. A cena inicial apresenta um expediente experimental de mercearia. Escolha fornecedor, quantidade e preço para três produtos e confira o caixa, o resultado e o estoque após a disputa de clientes com um rival. Cada tentativa começa com R$ 200 e a mesma seed, para comparar decisões.

## Pré-requisitos

- .NET SDK 8;
- Godot 4.7.2 Stable .NET.

## Verificação headless

Na raiz do repositório:

```bash
dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj
```

O verificador compila e executa 47 checks contra o núcleo headless, sem depender do assembly do Godot. Além do mini-turno e do Dia 1, os checks do mundo vivo provam trajetória causal de NPCs, BOXBOX/rival/não compra, conservação monetária, intervenção do jogador, equivalência entre avanço incremental e catch-up e retomada de save JSON — incluindo o estado do rival — sem efeitos duplicados.

## Dia 1 headless

O primeiro ciclo diário pode ser executado sem Godot e sem assets:

```bash
dotnet run --project BOXBOX/day-one/BOXBOX.DayOne.csproj
```

Use `--lang=en` para o relatório em inglês e `--seed=123` para repetir outra seed. O cenário de protótipo compra, precifica e vende arroz, refrigerante e leite ao longo de um expediente completo, disputa cada oportunidade com um rival e fecha com um relatório reconciliado de caixa, estoque, margem e resultados do mercado.

Quantidade, preço em reais e canal do fornecedor também podem ser escolhidos sem alterar código:

```bash
dotnet run --project BOXBOX/day-one/BOXBOX.DayOne.csproj -- \
  --rice=6:7.50:regular \
  --soda=8:4.20:regular \
  --milk=5:6.90:emergency
```

O formato é `quantidade:preço:regular|emergency`. O fornecedor emergencial entrega a mesma mercadoria neste experimento, mas cobra mais e reduz a margem; esse custo privado não altera sozinho a preferência dos clientes.

## Abrir no Godot

```bash
BOXBOX/boxbox/Abrir-BOXBOX-Godot.sh
```

O script usa `GODOT_BIN` quando definido; caso contrário, tenta a instalação local padrão registrada nele. O caminho do projeto é descoberto a partir do próprio script.

Para jogar diretamente, execute `BOXBOX/boxbox/Jogar-BOXBOX.sh` a partir da raiz do repositório. A cena inicial é `Presentation/OpenWorld/OpenWorldPrototype.tscn`. Use WASD para andar ou dirigir, E para entrar ou sair da caminhonete e F para interagir com os locais e a carga. Siga o alvo do minimapa até o atacado. Faça o pedido no balcão, estacione perto da retirada e interaja junto à caçamba. Volte à BOXBOX e descarregue antes de abrir o caixa. A cena `Presentation/Grocery/NeighborhoodShops.tscn` continua disponível no editor para testar os menus e conversas isoladamente. [Missão de abastecimento](../docs/grocery-supply-run.md)

A [fatia jogável e a migração de assets](../docs/grocery-slice-and-assets.md) registram o que falta para ligar o balcão a uma mercearia no bairro.

## Fronteiras atuais

- `core/BOXBOX.Core.csproj`: domínio, aplicação, agenda mínima do mini-turno e motores de decisão em .NET puro, incluindo o snapshot econômico autoritativo do mercado;
- `boxbox/`: adaptador Godot com o balcão do Dia 1 e o protótipo de mundo aberto; o núcleo confirma compras, vendas e fechamento;
- `verification/`: provas determinísticas sem framework gráfico;
- `persistence/`: adaptador JSON do snapshot do mundo vivo, separado do domínio;
- `boxbox/COPILOT_ACTION/`: registro incremental dos slices e de seus limites.

O diagnóstico e o contrato do primeiro vertical slice de NPCs persistentes estão em [`docs/living-world-vertical-slice.md`](../docs/living-world-vertical-slice.md).

As fontes de verdade de produto e arquitetura ficam em `../docs/` e no `AGENTS.md` da raiz.
