# Mercado, concorrência, promoções e fofocas

## Mercado compartilhado

Gere intenções de compra por mercado, categoria e faixa; não uma demanda separada para cada loja. Cada intenção termina em BOXBOX, concorrente, adiamento/desistência ou demanda não atendida.

Forme alternativas por passabilidade determinística e só depois pondere preço relativo, disponibilidade, horário, conveniência, fila, serviço, variedade, reputação, fidelidade, promoção conhecida e ruído semeado. Mantenha opção externa sempre válida.

Use inicialmente um concorrente agregado e nomeado com posicionamento, categorias fortes/fracas, preço por faixa, horário, mix, confiabilidade de estoque, capacidade, reputação, pressão financeira e conhecimento atrasado. Ele toma ações em cadência de mercado, com custo, duração e intervalo mínimo; nunca lê estado privado ou futuro do jogador.

Simetria é causal, não de resolução: concorrentes sofrem ruptura, vencimento, fiscalização, dívida, saldão e perda de capacidade em estados resumidos, sem uma empresa completa duplicada.

## Produtos lentos e promoções

Derive `InventoryHealth = Healthy | SlowMoving | Overstocked | NearExpiry | DeadStock` de giro, cobertura, idade, validade, capital imobilizado, espaço e demanda. Explique a classificação.

```text
PromotionCampaign = Draft | Scheduled | Active | Exhausted | Ended | Cancelled | Settled
```

Campanha declara objetivo, alvo, preço, início/fim, limite de unidades, canal, orçamento e público. Só ativa para produto/lote existente, válido, com estoque, período coerente, dinheiro exato e sem conflito de campanha.

Permita venda abaixo do custo para liquidação consciente, mostrando margem negativa. Promoção altera preferência somente de clientes informados; não transfere clientes diretamente nem garante venda.

Meça margem sacrificada, capital liberado, vencimento evitado, canibalização, fila, ruptura e confiança das estimativas. Retorno decrescente e reação rival impedem promoção dominante.

## Conhecimento imperfeito

Separe fato real, informação circulando e conhecimento do agente:

```text
MarketRumor: subject, target, source, created_at, expires_at,
             reach, confidence, accuracy, related_fact_id?
MarketKnowledge = Unknown | Rumored | Observed | Verified | Stale | Disproved
```

Rumor pode tratar de promoção, preço, ruptura, fila, horário, fiscalização, validade ou dificuldade financeira. Ele altera conhecimento e pesos, nunca estoque, dívida, multa ou fato. Jogador e concorrentes podem verificar; informação envelhece e pode ser corrigida.

## Invariantes

- Cada oportunidade é resolvida no máximo uma vez e reconcilia com vendas/não vendas.
- Loja inválida não recebe venda; estoque baixa uma vez.
- Promoção possui início, fim, orçamento e unidades limitadas.
- Lote vencido não recebe promoção.
- Unidade recebe um único preço confirmado.
- Rival não lê estado privado/futuro nem altera diretamente o jogador.
- Rumor não altera a verdade.
- Mesma versão, seed, comandos e instantes reproduzem mercado e conhecimento.
- Toda campanha termina ou falha com motivo.

## Primeiro experimento

Mercearia, três produtos, dois perfis de cliente, um rival e três dias: referência, ação do rival e resposta. Inclua item normal, encalhado e próximo do vencimento; compare nenhuma promoção, desconto agressivo e saldão direcionado.
