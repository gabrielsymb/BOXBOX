# BOXBOX — contrato da economia da mercearia

Decisão do projeto: o saldo aparece no HUD, a interface persistente sobre o mundo. As regras abaixo são uma proposta de implementação. Faixas, custos e números de balanceamento precisam ser verificados com simulação e sessões de jogo.

## Cinco fragilidades verificadas

| Fragilidade | O que pode falhar | Proposta |
| --- | --- | --- |
| Preço validado em lugares diferentes | O Godot limita a edição a R$ 999,00. O comando de preparação aceita qualquer valor positivo. Outra entrada pode contornar o limite e levar o multiplicador de preço a números que a fórmula não suporta. | O domínio valida preço e total em centavos. Um limite técnico versionado para a simulação protege todos os caminhos. A UI lê as regras de edição do núcleo. |
| O preço pesa na escolha antes de haver um orçamento no Dia 1 | O preço relativo da oferta pode cair para zero enquanto familiaridade e conveniência continuam contribuindo à escolha. O mundo persistente já filtra pelo orçamento no cenário de refeição, mas o Dia 1 segue outro caminho. | Aplicar o mesmo teste de capacidade de pagamento a qualquer loja e quantidade antes da IA. Manter desistência e diferenciar motivo de veto de preferência. |
| O rival pode distorcer o próprio referencial | O Dia 1 usa preços fixos dos rivais e um índice de preço relativo. Um preço extremo do jogador pode saturar a atratividade calculada para o rival, mesmo sem mudança real no preço dele. | Calibrar sensibilidade por produto e por perfil usando uma referência observável. Um rival começa com preço fixo. Ajustes futuros usam custo, estoque e sinais percebidos, com cadência e limites próprios. |
| Confirmar o pedido ainda é uma ação local da tela | A cena guarda uma cópia do pedido. A simulação debita as compras quando o caixa é aberto e já resolve o dia inteiro. Adicionar HUD ou prateleira sobre esse fluxo produziria estados econômicos divergentes. | Confirmar chama um comando atômico do domínio. O depósito recebe as unidades e o caixa é debitado uma vez. Repor transfere unidades do depósito para a prateleira. Abrir inicia a agenda. |
| Saldo pode dizer coisas diferentes em cada janela | A janela hoje calcula caixa após compra a partir do caixa inicial. Se esse número virar HUD, o jogador pode confundir simulação de pedido com dinheiro disponível. | O HUD exibe caixa confirmado da loja. A janela exibe total do pedido e, se necessário, saldo previsto identificado. A cada transação, as duas superfícies leem a mesma projeção confirmada. |

## Preço e concorrência

- O jogador define preço. Um teto técnico bem acima da faixa normal existe para proteger o runtime. O valor será escolhido e versionado com o tamanho máximo da compra e os limites das operações em centavos. Custos e faixa de referência geram avisos, inclusive para vendas abaixo do custo. Isso permite testar liquidação e preço agressivo.
- A oferta precisa atender quantidade, disponibilidade e custo total dentro do orçamento de compra naquele momento. Depois disso, a IA avalia preço, urgência, conveniência, reputação e a opção de desistir. O comprador escolhe com a informação a que tem acesso.
- Se o leite custa R$ 1 milhão e o comprador dispõe de R$ 15, a oferta da BOXBOX é vetada por orçamento antes do sorteio, sujeita também ao teto técnico. Pode comprar do rival se a oferta estiver disponível e fizer sentido. O preço extremo da BOXBOX, por si só, não altera o caixa, o estoque ou o preço anunciado do rival.

### Implementado após a revisão

Na bancada, o jogador consulta preços fixos do Dia 1 e escolhe quantidade e fornecedor. O orçamento filtra o total da oportunidade antes da escolha, inclusive para o rival, e é validado de novo na efetivação. No Dia 1 os limites de R$ 12 e R$ 15 são dados de teste. No mundo vivo, o limite desta compra é o caixa do ator. A atratividade do preço rival no Dia 1 usa a referência de catálogo do cenário. O teto técnico da preparação limita o preço unitário a `long.MaxValue / int.MaxValue` em centavos para que preço × quantidade caiba no tipo monetário.

## Do resultado para o primeiro teste

**Resultado jogável.** O jogador compra, volta, abastece, abre e vê uma venda retirar uma unidade da prateleira e creditar o caixa. Ele consegue explicar por que uma venda aconteceu ou se perdeu.

| Ordem de implementação | Entrega | Prova necessária |
| --- | --- | --- |
| 1 | Contrato de preço, orçamento e commit no núcleo | Casos com preço negativo, extremo, limítrofe, abaixo do custo e importe total acima do orçamento. Rejeições preservam caixa, estoque, evento e estado do rival. O mesmo estado e seed produzem o mesmo resultado. |
| 2 | Pedido, depósito, prateleira e venda incremental | Pedido acima do caixa ou repetido não dobra o débito. Cada reposição conserva unidades. Cada venda casa baixa de estoque e crédito. Recarregar em cada etapa preserva o resultado. |
| 3 | HUD de caixa e ligação às duas cenas | Fechar o menu ou trocar de cena não muda o saldo. Pedido em edição não muda o HUD. Confirmação e venda mudam o HUD exatamente uma vez. |
| 4 | Primeiro expediente com jogador novo | O jogador segue a dica inicial, confirma, acha a prateleira e explica por que o caixa mudou. Só depois medir se faz sentido adicionar resposta de preço do rival. |

## HUD e direção de arte

O primeiro HUD precisa de ícone de caixa e valor em BRL, no mesmo lugar na mercearia e no fornecedor. É uma área compacta da tela, com contraste sob o cenário e que mantém livre a janela lateral. O saldo pessoal ganha identidade própria quando entrar na simulação. O HUD não reúne valores pessoais e da loja.

Candidatos para revisão depois do contrato econômico: combinar as molduras do Kenney UI Pack com o sprite do caixa Mini Shop que já aparece no jogo. Verificar moldura de nove regiões no Godot, ícone legível, contraste e encaixe em resoluções diferentes. Os packs abaixo são candidatos de arte, sem adoção confirmada. [Kenney UI Pack](https://kenney.nl/assets/ui-pack) · [Kenney Mini Shop](https://kenney.nl/assets/mini-shop) · [Catálogo do BOXBOX](../BOXBOX/asset-library/CATALOG.md)

## Primeiro HUD implementado

O Dia 1 confirma um pedido por tentativa e prepara a compra no núcleo antes de liberar o caixa da loja. O HUD muda na confirmação e acompanha as vendas à medida que a agenda é processada. A janela exibe o total do pedido. Abrir o expediente reaproveita a preparação confirmada. A transição entre depósito e prateleira e a persistência continuam pendentes.

## Dez oportunidades de jogo para testar

São hipóteses de apelo para uma futura demo. O público indicado é quem poderia valorizar a mecânica. A disposição de pagar depende de observar a demo em uso.

| Oportunidade | Por que existe e para quem | Teste barato |
| --- | --- | --- |
| Missão de reposição | Jogadores novos aprendem melhor com uma tarefa concreta. | Marcar o leite e medir se abastecem sem ajuda. |
| Compra emergencial | Fãs de gestão ganham uma saída cara quando erram estoque. | Comparar o custo da entrega urgente com uma venda perdida. |
| Compra por horário | Fãs de rotina podem antecipar a demanda do bairro. | Testar um pico de bebidas num horário reconhecível. |
| Liquidar perecíveis | Fãs de otimização têm um motivo para vender abaixo do custo. | Colocar um lote perto do vencimento em uma única sessão. |
| Preço de atração | Fãs de estratégia podem usar um item barato para atrair uma cesta maior. | Verificar o efeito da segunda compra e o caixa do conjunto. |
| Concorrente legível | Fãs de simulação entendem a disputa ao observar sinais da rua. | Mostrar um rival com preço estável e uma ruptura percebida. |
| Pedido e prateleira visíveis | Jogadores de loja querem ver a mercadoria que compraram ocupar espaço. | Fazer a entrega e a primeira reposição de um só SKU. |
| Fechamento com causa | Jogadores curiosos querem aprender com o dia anterior. | Mostrar a principal venda perdida e pedir que expliquem a causa. |
| Dica que perde força com domínio | Jogadores iniciantes ganham direção e veteranos preservam autonomia. | Exibir a dica automática no início e medir se a pessoa passa a decidir sozinha. |
| Cliente recorrente | Fãs de histórias emergentes podem ligar uma decisão a uma pessoa. | Fazer um morador voltar outro dia com a mesma necessidade. |

## Carteira e relação com o fornecedor no Dia 1

O HUD mostra caixa da mercearia e carteira do personagem em molduras identificadas. O dinheiro pessoal começa em R$ 20,00 nesta tentativa. Comprar um café usa apenas a carteira. Comprar estoque usa apenas o caixa. A primeira compra comercial confirmada faz o fornecedor reconhecer o personagem, reduzindo o café de R$ 6,00 para R$ 4,00. A relação e a carteira são locais à tentativa do Dia 1 e reiniciam ao repetir o cenário. A integração com o mundo persistente, aportes e retiradas ainda depende do fluxo de continuidade entre dias.
