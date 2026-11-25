# Primeiro vertical slice

## Propósito

Provar em uma sessão curta o ciclo de abastecer e operar uma pequena mercearia,
fazendo uma escolha comercial que produza uma consequência econômica visível. O
slice conecta preparação, operação e fechamento; não tenta simular uma cidade inteira.

## Escopo proposto

- Uma mercearia de conveniência e um fornecedor atacadista.
- Três produtos com custos, preços, estoque e comportamentos distinguíveis.
- Um pedido de abastecimento; a carga é reservada, transportada e entregue antes de
  poder ser vendida.
- O transporte acontece numa rota curta controlada pelo jogador em uma caminhonete;
  o controle físico é simples e serve ao abastecimento, sem tráfego autônomo.
- Um expediente completo com clientes e uma alternativa de compra concorrente ou
  desistência, usando regras determinísticas e uma seed explícita.
- Uma intervenção legível do jogador durante a operação (por exemplo, preço ou
  reposição) que possa alterar o resultado sem burlar as regras do núcleo.
- Fechamento que reconcilia caixa, vendas, estoque e oportunidades perdidas.
- PT-BR e inglês para todo texto visível do slice.
- A rota do fornecedor é um greybox curto e controlado; não exige bairro aberto nem
  tráfego autônomo.

## Fora do primeiro corte

Mundo aberto expansível, tráfego e pedestres autônomos, funcionários autônomos,
persistência offline, crime, combate, direção avançada/dano de veículos, crédito,
obrigações fiscais e progressão de campanha. Eles continuam sendo direções do produto,
mas não são pré-requisitos para provar este ciclo mínimo.

## Fronteiras no fluxo

1. `Assets/BoxBox/Presentation` recolhe escolhas e envia comandos tipados.
2. `Application` coordena pedido, entrega, abertura, avanço e fechamento.
3. `Domain` valida dinheiro, estoque, oferta e transições e calcula os efeitos.
4. `Simulation` determina os instantes e decisões de mercado com tempo e seed explícitos.
5. `Infrastructure` persiste apenas quando um contrato de save fizer parte do corte.
6. `Assets/BoxBox/Presentation` mostra estado confirmado, motivos e relatório localizado.

UI não faz cálculos econômicos nem mantém uma segunda cópia autoritativa do estado.
Nenhuma animação ou clique concede estoque, receita ou progresso sem comando aceito.

## Critérios de aceitação

- É possível iniciar, abastecer, abrir, operar e fechar o ciclo sem editar código.
- Cada compra confirmada transfere dinheiro e estoque atomicamente; rejeições não deixam
  efeitos parciais.
- Quantias monetárias são exatas em centavos e nunca usam ponto flutuante.
- Com mesma versão de regras, seed, estado inicial e comandos, o resultado é idêntico.
- O fechamento explica caixa, vendas, estoque remanescente e demanda não atendida.
- As regras críticas rodam em testes headless sem Unity/UI.
- A cena inicial apenas compõe dependências; não contém lógica econômica ou uma UI
  monolítica.
- Um teste de execução confirma que o fluxo visual completo é compreensível e que a UI
  reflete o resultado confirmado pelo núcleo.

## Decisões ainda abertas

- O catálogo, a seed e os valores atuais são um cenário fictício de protótipo, não
  balanceamento nem regra tributária/comercial brasileira.
- A intervenção escolhida para este teste é ajustar preços em incrementos de R$ 0,25.

Reavalie essas escolhas após observar uma sessão; não acrescente sistemas fora do
escopo para contornar um resultado ruim.

## Uso de referências e Blender

O protótipo já exercita o ciclo da mercadoria e a comparação de preços. Para este
corte, use Moonforge somente como referência de casos de falha/transação, REDRIVER2
como contraste de complexidade de direção e Endless Sky como referência complementar
para apresentar catálogo e carga. gta2_re, OpenMC2 e OpenMoHAA não são necessários
para implementar este fluxo.

As conclusões, arquivos consultados, licenças e limitações estão em
`research/first-slice-reference-review-2026-11-22.md`. Não copie código ou assets:
registre observações comportamentais e escreva especificações próprias.

Não produza arte final no Blender nesta rodada. Primeiro execute e inspecione o
greybox dentro do Unity; modele um asset autoral apenas se houver uma necessidade de
leitura comprovada.
