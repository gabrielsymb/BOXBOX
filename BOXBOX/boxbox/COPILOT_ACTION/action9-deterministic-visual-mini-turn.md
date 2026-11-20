# ACTION 9 — mini-turno visual determinístico

## Objetivo

Transformar o núcleo de mercado da Action 8 na primeira volta jogável curta: relógio simulado, clientes chegando, escolhendo entre BOXBOX, rival e não comprar, e uma cena isométrica mostrando as consequências em caixa e estoque.

## Slice entregue

- relógio soberano em minutos inteiros, iniciado às 08:00;
- seis planos de cliente com chegada e resolução em instantes estáveis;
- seed econômica derivada do ID estável de cada resolução, nunca do frame;
- avanço monotônico capaz de processar um minuto ou vários minutos com o mesmo resultado;
- eventos sem texto traduzido para chegada e resolução;
- cena isométrica 3D low-poly montada principalmente com os pacotes enviados: piso e arquitetura modular, caixa, concorrente resumido, natureza, operador, clientes, prateleiras e três produtos;
- animação de chegada, escolha, caixa e saída como projeção dos eventos confirmados;
- `CharacterBody3D` com cápsula física para cada cliente, colisores estáticos para cenário e `NavigationRegion3D` assada a partir da geometria física;
- HUD bilíngue `pt-BR`/`en` com relógio, caixa, estoque, consequência mais recente, seed, pausa, velocidade e replay;
- assets CC0 de Quaternius e Kenney, posteriormente removidos em 19 de novembro
  de 2026 por serem apenas testes visuais.

## Contrato temporal

`MiniTurnSimulation` coordena o `SharedMarketPurchaseService` existente. Cada cliente possui `arrivalMinute` e `decisionMinute`; a decisão não pode anteceder a chegada. A agenda ordena por minuto, tipo de etapa e ID estável. `AdvanceTo` rejeita regressão temporal e emite cada etapa uma única vez.

As velocidades `1×`, `3×` e `6×` alteram somente quantos minutos simulados a apresentação solicita por segundo real. Elas não selecionam opções, não consomem aleatoriedade e não mutam ativos. O domínio confirma a venda antes de a cena animá-la.

## Resultados do cliente

O cenário curado usa os mesmos contratos probabilísticos do núcleo, com observações diferentes por cliente para exercitar os três caminhos visuais:

1. venda na BOXBOX: baixa estoque e aumenta caixa atomicamente;
2. venda no rival: o cliente sai em direção ao quiosque e os ativos da BOXBOX permanecem iguais;
3. não comprar: o cliente deixa o diorama sem transação.

Nenhum peso torna uma oferta inválida admissível. O conjunto continua sendo formado pelas guardas de passabilidade antes da escolha.

## Localização e apresentação

Os eventos e o estado carregam somente IDs semânticos, valores e códigos de resultado. Os catálogos `mini-turn.pt-BR.json` e `mini-turn.en.json` resolvem todo texto visível. Trocar idioma não reinicia nem altera a simulação.

A cena usa o kit modular de ficção científica como estrutura improvisada de mercearia, preservando o kitbash autoconsciente definido pela direção visual. Buildings Pack, Nature Pack, Animated Character Pack e Food Kit completam o lote. Os arquivos de origem não são conhecidos pelo domínio ou pela IA.

## Colisão e navegação

A camada visual separa malha renderizada de geometria física. Piso, paredes,
prateleiras, balcão, operador, prédio rival e obstáculos externos possuem
`StaticBody3D` com formas convexas baratas. Clientes possuem `CharacterBody3D`
e cápsula, usam `MoveAndSlide` e pedem caminhos a um `NavigationAgent3D`.

O `NavigationRegion3D` é assado após a montagem dos assets e erode os caminhos
para um raio de agente de 0,5 unidade. A cena rejeita inicialização quando o bake
não produz polígonos ou quando há menos de dez colisores registrados. No layout
atual o bake produz 22 polígonos e dez colisores estáticos.

## Verificações

Três checks foram adicionados, totalizando 30:

- avanço até o fim em lote produz o mesmo log e snapshot que avanço minuto a minuto;
- mesma seed e configuração reproduzem eventos e economia;
- vendas confirmadas explicam exatamente a variação de caixa e estoque.

Validação executada em 19 de novembro de 2026:

- `dotnet build BOXBOX/verification/BOXBOX.Verification.csproj --no-restore`: sucesso, zero avisos e zero erros;
- `dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj --no-build`: 30 checks aprovados;
- `dotnet build BOXBOX/boxbox/BOXBOX.csproj --no-restore`: sucesso, zero avisos e zero erros;
- Godot 4.7.2 .NET headless: 22 polígonos navegáveis e dez colisores validados, sem exceções do jogo;
- captura renderizada em 1152×648: câmera, assets, caminhos físicos, HUD, mudança de caixa/estoque e os três resultados de cliente inspecionados.

## Limites atuais

- chegadas são conteúdo curado, ainda não uma política de demanda por faixa horária;
- a agenda cobre somente chegada e resolução; ainda não implementa fases gerais, barreiras, save ou fechamento diário;
- atendimento é instantâneo no domínio; a duração visual não reserva caixa ou capacidade;
- a navegação ainda não modela ocupação de fila ou reserva de posição; corpos físicos impedem atravessar cenário, mas dois clientes podem disputar o mesmo destino visual;
- o estoque é agregado por produto, sem lote, validade, custo ou reposição;
- o concorrente permanece resumido e sua capacidade não é reposta durante o mini-turno;
- a cena usa uma câmera fixa e paredes permanentemente abertas; ângulo, zoom e oclusão ainda são protótipos.

## Próximo passo de maior aprendizado

Substituir os planos fixos por uma política mínima de intensidade de chegada orientada a dados, preservando os mesmos eventos e a equivalência entre velocidades. O primeiro teste deve comparar duas faixas horárias e permitir ao jogador mudar uma única variável operacional — preço ou reposição — para observar uma consequência legível no mesmo diorama.
