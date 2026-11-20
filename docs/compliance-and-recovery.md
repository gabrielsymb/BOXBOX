# BOXBOX — conformidade, fiscalização, dívida e recuperação

## Escopo

O sistema usa o MEI brasileiro como identidade, mas o balanceamento padrão é uma **abstração de jogo**, não orientação jurídica ou reprodução automática da legislação vigente. Prazos, valores, encargos, programas de negociação e consequências reais ficam em um `ComplianceRuleset` versionado quando forem necessários.

## Distinções obrigatórias

Não trate como sinônimos:

1. **DAS não pago:** obrigação reconhecida que ficou em atraso;
2. **declaração ausente ou inconsistente:** descumprimento declaratório;
3. **sonegação intencional:** divergência deliberada entre realidade econômica e informação declarada.

O jogador pode escolher preservar caixa e deixar uma obrigação vencer. Isso cria inadimplência e dívida, não sonegação automática. Se irregularidade intencional entrar no jogo, será uma ação abstrata e explicitamente ilegal, sem ensinar métodos reais. A IA nunca escolhe sonegar em nome do jogador.

## Camadas de verdade

```text
EconomicTruth       → vendas, caixa e estoque reais
BusinessRecords     → registros internos
DeclaredRecord      → valores declarados
AuthorityKnowledge  → fatos descobertos pela fiscalização
PlayerKnowledge     → avisos e riscos percebidos
```

Erro, atraso, omissão e descoberta alteram camadas diferentes. Nenhuma divergência apaga o fato técnico original.

## Obrigações e declarações

```text
TaxObligation
Scheduled → Due → Paid
              └→ Overdue → Notified → Regularizing → Paid
                                      └→ Collection
```

`Due` significa que o instante de vencimento foi alcançado. A transição para `Overdue` ocorre depois da barreira de decisão e de eventual tolerância declarada pelo ruleset. Pagamento concluído na fase temporal anterior conta como pontual. Sem ator ou política autorizada para pagar, o mundo não pausa: a obrigação segue para atraso pelas regras normais. Isso é consequência da omissão, não uma escolha silenciosa de irregularidade.

```text
Declaration
Pending → Submitted
       ├→ Late → Submitted
       ├→ Omitted → Notified → Submitted
       └→ Inconsistent → Corrected | UnderReview
```

Ações incluem pagar, deixar vencer, corrigir registro, entregar declaração, regularizar, parcelar quando elegível, reduzir operação, buscar crédito, liquidar ativos ou aceitar insolvência.

Encargos, elegibilidade, prazos e restrições são determinísticos dentro da versão do ruleset. A seleção de fiscalização pode usar risco e seed; uma consequência confirmada nunca é inventada pela IA.

## Fiscalização de validade

```text
LotComplianceState
Sellable | NearExpiry | ExpiredAwaitingRemoval | Quarantined | Disposed | ViolationExposed
```

Regras iniciais:

- promoção e saldão alcançam somente estoque válido;
- vencimento é evento de domínio e não pode ser ocultado tecnicamente;
- lote vencido precisa ser retirado, isolado ou descartado;
- manter lote vencido acessível cria irregularidade verificável;
- descarte e perda geram eventos contábeis;
- fiscalização considera elegibilidade, reclamações, histórico e ruído semeado;
- penalidade e prazo de regularização seguem tabela versionada após o achado;
- reincidência pode elevar severidade dentro de limites declarados.

Na primeira versão, uma venda comum de lote vencido é inválida. A possibilidade futura de uma ação ilegal explícita de comercialização permanece aberta e não é necessária para testar fiscalização.

## “Leão contábil”

“Leão” pode ser apelido da interface ou personagem, mas o sistema interno é `FiscalCompliance`/`RegulatoryInspection`. Ele não é um inimigo que trapaceia nem lê o futuro do jogador.

O jogador recebe faixas e causas, como regular, atenção ou elevado. Aviso oficial é conhecimento confirmado; fofoca sobre fiscalização altera somente expectativa.

## Irregularidade intencional

Se adicionada depois que livro-caixa, declaração e auditoria estiverem maduros, uma ação abstrata como `UnderreportRevenue` pode produzir alívio aparente, passivo oculto e risco de descoberta. Não descreva procedimentos, documentos falsos ou formas de evitar fiscalização.

Essa decisão exige comando explícito do jogador, é sempre `Blocking` e não pode ser delegada a política ou fast-forward. Funcionário ou contador pode cometer erro, mas não escolhe fraude intencional pela empresa de forma autônoma.

Subdeclaração nunca altera `EconomicTruth`, não cria caixa e não cancela obrigação já reconhecida. Enquanto benefício, custo e auditoria não estiverem definidos e testados, a ação permanece indisponível.

## Recuperação financeira

### Saldão

Reutiliza o sistema de promoção para converter estoque válido em liquidez. Reduz margem, pode canibalizar venda normal, aumentar fila, criar ruptura e acostumar parte da clientela a descontos. Não alcança lote vencido.

### Venda de equipamento

```text
EquipmentAsset = Active | Listed | OfferAccepted | TransferPending | Unavailable
```

Em `Listed`, o ativo pode continuar em uso se a regra da listagem permitir. `OfferAccepted` o bloqueia contra nova venda; `TransferPending` aguarda retirada. A conclusão da transferência confirma atomicamente o recebimento e `Unavailable`, removendo capacidade. Cancelamento ou timeout antes da conclusão retorna ao estado permitido sem gerar caixa.

Vender equipamento gera caixa uma vez e remove capacidade real. Consequências possíveis:

- menos refrigeração ou espaço;
- atendimento mais lento;
- seção desativada;
- menor segurança ou controle;
- operação reduzida ou fechamento temporário.

Equipamento essencial não pode ser vendido e continuar concedendo a mesma capacidade. A venda possui duração, preço de mercado, confirmação e evento contábil.

Antes de aceitar a oferta, mostre dependências: estoque refrigerado, seção afetada, tarefas, controles e prazo até retirada. O jogador precisa escolher saldão, transferência, descarte, substituição ou operação reduzida para itens dependentes.

### Regularização e outras rotas

- pagamento à vista;
- parcelamento quando o ruleset permitir;
- correção de declaração;
- redução de mix, horário ou despesas;
- negociação com fornecedor;
- crédito quando elegível;
- encerramento voluntário;
- insolvência explícita quando não existe rota econômica.

Nenhuma recuperação é dinheiro gratuito. Cada escolha troca tempo, capacidade, margem, reputação ou custo futuro por liquidez imediata.

## Simetria com concorrentes

Concorrentes usam as mesmas categorias causais em estado resumido:

```text
CompetitorComplianceState
Healthy | CashStrained | Delinquent | UnderReview |
Regularizing | TemporarilyClosed | ExitingMarket
```

Eles também podem atrasar obrigações, sofrer vencimento e fiscalização, fazer saldão, vender capacidade, perder reputação, reduzir horário ou sair do mercado. Essas mudanças chegam ao jogador como observação ou rumor; o rival recebe sinais equivalentes sobre o BOXBOX.

## Proteções contra injustiça e soft lock

- Uma obrigação atrasada não destrói a empresa sem aviso e progressão de estados.
- Toda notificação oferece prazo, resposta ou terminal explícito.
- Recuperação offline executa somente atores e políticas realmente autorizados; sem capacidade de regularização, prazo e consequências continuam.
- Fiscalizações graves possuem limites e não encadeiam infinitamente.
- Dificuldade não aumenta risco apenas porque o jogador tem caixa.
- O sistema não pressupõe futuro perdão de dívida.
- Empresa viável mantém rota entre pagar, regularizar, liquidar, reduzir operação ou buscar crédito elegível.
- Quando nenhuma rota econômica existe, insolvência encerra a empresa sem deadlock técnico.
- Antes de delegar políticas de crise, uma previsão conjunta mostra autoridades concedidas, eventos esperados, limites e consequências prováveis; o retorno mostra o que de fato ocorreu e o que ainda é conhecido.

## Invariantes e testes

- DAS atrasado não é classificado automaticamente como sonegação.
- Dívida não desaparece sem pagamento, ajuste versionado ou evento oficial explícito.
- Lote vencido não é vendido por ação comum nem promovido.
- Saldão não vende mais estoque do que existe e está válido.
- Um equipamento não é vendido duas vezes.
- Transferência concluída gera caixa e remove a capacidade na mesma transação.
- Listagem cancelada ou expirada não gera caixa.
- Autoridade não usa informação futura ou privada sem descoberta.
- Fofoca não cria dívida, multa ou infração.
- Subdeclaração não altera verdade econômica, não cria caixa e não cancela obrigação reconhecida.
- Concorrente não altera diretamente obrigações do jogador.
- Velocidade visual e idioma não mudam obrigações.
- Mesma versão, seed, comandos e instantes reproduzem fiscalização e resultados.

Propriedade de progresso da crise: dentro do horizonte declarado, toda execução justa alcança pagamento, regularização, operação reduzida, cobrança ou insolvência explícita. Uma ação não conta como rota viável se termina após o prazo crítico, depende de recurso inalcançável ou gera liquidez líquida insuficiente para a transição prometida.

## Ordem de implementação

1. obrigação abstrata, pagamento, atraso, aviso e regularização;
2. saldão, venda de ativo e operação reduzida;
3. validade, descarte e fiscalização;
4. registro, declaração, atraso e correção;
5. irregularidade intencional, somente com auditoria madura;
6. aplicação das mesmas pressões ao estado resumido dos rivais.

O primeiro experimento usa uma obrigação abstrata, estoque encalhado válido e um ativo não essencial. Compare pagar em dia, atrasar para comprar estoque e atrasar seguido de recuperação. A tentação precisa ser real, mas não dominante.

## Pesquisa oficial registrada em 19 de novembro de 2026

- A Receita Federal distingue sonegação de inadimplência e a legislação trata omissão ou declaração falsa como condutas próprias: [Receita Federal — cidadania fiscal](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/materiais-didaticos/cidadania-fiscal/curriculo-escolar/quem-ensina/planos-de-aula/modelos-aulas/modelos-aulas-03) e [Lei nº 8.137/1990](https://www.planalto.gov.br/ccivil_03/leis/l8137.htm).
- O DAS-MEI é uma obrigação periódica e o atraso recebe acréscimos segundo as regras vigentes: [Manual do PGMEI](https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2025/setembro/cartao-de-credito-e-uma-nova-opcao-de-pagamento-do-das-para-o-mei/manual_pgmei_2018.pdf/%40%40download/file/MANUAL_PGMEI_2018.pdf).
- Débitos declarados podem possuir caminho oficial de parcelamento enquanto elegíveis: [Serviço de parcelamento do MEI](https://www.gov.br/pt-br/servicos/parcelar-imposto-mei).
- Omissão persistente de declaração possui consequências próprias: [Receita Federal — omissão na entrega de declarações](https://www.gov.br/receitafederal/pt-br/assuntos/orientacao-tributaria/cobrancas-e-intimacoes/controle-de-entrega-de-declaracoes/orientacoes-sobre-o-termo-de-intimacao-por-omissao-na-entrega-de-declaracoes).
- O Código de Defesa do Consumidor classifica produtos vencidos como impróprios ao uso e consumo: [Lei nº 8.078/1990](https://www.planalto.gov.br/ccivil_03/leis/l8078compilado.htm).

Revalide essas fontes antes de criar um ruleset fiel à legislação. Não incorpore automaticamente programas temporários, perdões, valores ou prazos ao balanceamento base.

## Questões abertas

- O “leão” será apenas linguagem de interface ou personagem recorrente?
- Qual nível de fidelidade legal o modo padrão terá?
- A irregularidade intencional entra no escopo inicial ou somente depois?
- Fiscalização aparece presencialmente ou por eventos e documentos?
- Venda ilegal de produto vencido permanecerá bloqueada ou será uma ação explícita posterior?
