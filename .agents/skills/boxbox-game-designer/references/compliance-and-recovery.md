# Conformidade, fiscalização, dívida e recuperação

## Abstração e distinções

O modo padrão é MEI-inspirado, não aconselhamento ou réplica automática da lei. Valores, prazos, encargos e consequências reais pertencem a `ComplianceRuleset` versionado e exigem nova consulta oficial.

Separe:

- DAS reconhecido e não pago: inadimplência;
- declaração ausente/inconsistente: falha declaratória;
- divergência deliberada entre realidade e declaração: sonegação intencional.

Não rotule atraso como sonegação. Caso irregularidade intencional entre no jogo, mantenha-a abstrata, explicitamente ilegal e sem métodos. A IA nunca a escolhe pelo jogador.

## Camadas e estados

Mantenha `EconomicTruth`, `BusinessRecords`, `DeclaredRecord`, `AuthorityKnowledge` e `PlayerKnowledge` separados.

```text
TaxObligation: Scheduled → Due → Paid
                           └→ Overdue → Notified → Regularizing → Paid | Collection
Declaration: Pending → Submitted | Late | Omitted | Inconsistent | UnderReview | Corrected
ComplianceFinding: None → Suspected → UnderReview → Confirmed → Regularizing → Resolved
```

`Due` precede `Overdue`: pagamento concluído na fase anterior é pontual; depois de eventual tolerância do ruleset, a barreira exige política autorizada ou decisão. Fast-forward não escolhe atraso silenciosamente.

Encargos e consequências são determinísticos no ruleset. Fiscalização pode ser selecionada por elegibilidade, risco e seed, sem ler futuro ou estado privado não descoberto.

## Validade e fiscalização

```text
LotComplianceState = Sellable | NearExpiry | ExpiredAwaitingRemoval |
                     Quarantined | Disposed | ViolationExposed
```

Promoção/saldão só alcançam estoque válido. Vencimento é fato de domínio; lote vencido precisa ser removido, isolado ou descartado. Mantê-lo acessível cria irregularidade. Penalidade e prazo seguem regra versionada após achado.

“Leão” pode ser interface; internamente use `FiscalCompliance`/`RegulatoryInspection`. Aviso oficial é confirmado; fofoca altera somente expectativa.

Irregularidade intencional é sempre bloqueante, explícita e não delegável. Nunca altera `EconomicTruth`, cria caixa ou cancela obrigação reconhecida. Mantenha a ação indisponível até benefício, custo e auditoria estarem definidos.

## Recuperação

Saldão converte estoque válido em caixa sacrificando margem. Venda de equipamento segue `Active → Listed → OfferAccepted → TransferPending → Unavailable`; aceite bloqueia revenda e conclusão confirma caixa + remoção de capacidade atomicamente. Cancelamento/timeout não gera caixa. Antes do aceite, mostre estoques e funções dependentes. Outras rotas: pagar, corrigir, parcelar quando elegível, reduzir operação, negociar fornecedor, buscar crédito, encerrar ou atingir insolvência explícita.

Concorrentes usam as mesmas categorias em estados resumidos como `Healthy`, `CashStrained`, `Delinquent`, `UnderReview`, `Regularizing`, `TemporarilyClosed` e `ExitingMarket`.

## Invariantes

- Atraso não vira sonegação automaticamente.
- Dívida não desaparece sem pagamento, ajuste versionado ou evento oficial.
- Lote vencido não é vendido por ação comum nem promovido.
- Saldão respeita estoque válido.
- Ativo não é vendido duas vezes e sua capacidade é removida.
- Cancelamento/timeout de listagem não gera caixa.
- Autoridade não usa informação futura/privada sem descoberta.
- Fofoca não cria dívida, multa ou infração.
- Subdeclaração não altera verdade, caixa ou obrigação reconhecida.
- Velocidade/idioma não mudam obrigação.
- Estado viável mantém rota; ausência econômica de rota vira insolvência, não deadlock.

Em crise, exiba previsão antes do fast-forward. Dentro de horizonte declarado, a execução chega a pagamento, regularização, operação reduzida, cobrança ou insolvência. Não conte como rota uma ação tardia, inalcançável ou de liquidez insuficiente.

## Sequência recomendada

1. obrigação, pagamento, atraso, aviso e regularização;
2. saldão, venda de ativo e operação reduzida;
3. validade, descarte e fiscalização;
4. declaração, atraso e correção;
5. irregularidade intencional com auditoria madura;
6. pressões equivalentes nos rivais.
