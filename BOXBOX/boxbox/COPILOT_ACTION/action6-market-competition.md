# ACTION 6 — primeiro mercado compartilhado

## Objetivo

Fazer BOXBOX, um concorrente agregado e a opção de não comprar disputarem a mesma oportunidade de compra, sem permitir que a IA ou o concorrente alterem diretamente o estado da loja do jogador.

## Implementação

- `AdmissibleMarketOffer` representa somente uma oferta que uma etapa anterior de passabilidade já considerou elegível. Ela carrega identificação da alternativa, loja e atributos percebidos pelo cliente; não expõe caixa, estoque ou decisões privadas do rival.
- `CustomerPurchaseService` aceita ofertas alternativas além da BOXBOX e sempre mantém a opção de não comprar.
- Quando o cliente escolhe a BOXBOX, o domínio continua validando e executando a venda.
- Quando escolhe um rival, o resultado é `ChoseAlternativeStore`: nenhum estoque, caixa ou evento de venda da BOXBOX é alterado.
- A sobrecarga antiga do serviço foi preservada para o laboratório existente continuar funcionando.
- Duas ofertas da mesma loja na mesma oportunidade são rejeitadas neste primeiro slice de produto único.

## Verificação fora do Godot

O projeto `BOXBOX/verification` é um executável .NET sem framework gráfico e sem pacotes externos. Ele verifica:

1. BOXBOX, rival e não comprar aparecem na mesma escolha.
2. Escolher o rival preserva integralmente caixa e estoque do jogador.
3. BOXBOX fechada não aparece, mas um rival independentemente admissível continua disponível.
4. Uma promoção conhecida, representada inicialmente por maior atratividade de preço percebida, eleva a preferência relativa pelo rival.
5. A mesma loja não entra duas vezes na oportunidade.

## Limites explícitos

- A origem das ofertas admissíveis ainda não existe; por enquanto a verificação as constrói como fixture. A próxima fatia deve criar o estado resumido do concorrente e o gerador determinístico de alternativas.
- Promoção ainda não possui campanha, orçamento, período, estoque promocional ou alcance. Este teste prova somente que uma mudança percebida influencia a disputa compartilhada.
- A escolha do rival ainda não baixa o estoque dele; isso será responsabilidade do futuro resolvedor resumido de concorrentes.
- Não há fofoca ou atraso de informação nesta etapa.
