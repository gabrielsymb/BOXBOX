# BOXBOX — protótipo técnico

Protótipo em Godot 4.7.2 .NET/C# do simulador bilíngue de microcomércio brasileiro. O estado atual preserva o mundo vivo headless integrado ao mercado compartilhado: necessidades persistentes podem terminar em compra na BOXBOX, compra no rival ou não compra. A apresentação visual anterior e seus assets de teste foram removidos; o projeto Godot abre em uma cena vazia enquanto a nova direção de assets é curada.

## Pré-requisitos

- .NET SDK 8;
- Godot 4.7.2 Stable .NET.

## Verificação headless

Na raiz do repositório:

```bash
dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj
```

O verificador compila e executa 42 checks contra o núcleo headless, sem depender do assembly do Godot. Além do mini-turno e do Dia 1, os checks do mundo vivo provam trajetória causal de NPCs, BOXBOX/rival/não compra, conservação monetária, intervenção do jogador, equivalência entre avanço incremental e catch-up e retomada de save JSON — incluindo o estado do rival — sem efeitos duplicados.

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

A cena inicial é `Presentation/Bootstrap/EmptyBootstrap.tscn`. Ela é intencionalmente vazia e não contém HUD, modelos ou materiais de teste.

## Fronteiras atuais

- `core/BOXBOX.Core.csproj`: domínio, aplicação, agenda mínima do mini-turno e motores de decisão em .NET puro, incluindo o snapshot econômico autoritativo do mercado;
- `boxbox/`: adaptador Godot e ponto de entrada visual atualmente vazio; qualquer apresentação futura deverá consumir eventos confirmados e não autorizar transações;
- `verification/`: provas determinísticas sem framework gráfico;
- `persistence/`: adaptador JSON do snapshot do mundo vivo, separado do domínio;
- `boxbox/COPILOT_ACTION/`: registro incremental dos slices e de seus limites.

O diagnóstico e o contrato do primeiro vertical slice de NPCs persistentes estão em [`docs/living-world-vertical-slice.md`](../docs/living-world-vertical-slice.md).

As fontes de verdade de produto e arquitetura ficam em `../docs/` e no `AGENTS.md` da raiz.
