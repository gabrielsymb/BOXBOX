# BOXBOX — vertical slice de mundo vivo

> A expansão do protagonista como ator do mesmo mundo, incluindo trajetória livre
> e a sequência futura de criminalidade social, está especificada em
> `docs/living-world-protagonist-expansion.md`. Este arquivo continua sendo o
> registro do corte headless já implementado e verificado.

## Propósito e limite

Este documento registra o diagnóstico, as decisões arquiteturais e o primeiro corte executável da simulação sistêmica. O objetivo do corte é provar uma cadeia causal pequena:

```text
necessidade cresce
→ ator recebe salário e compra, ou perde o emprego
→ opções econômicas mudam
→ IA escolhe apenas entre ações admissíveis
→ comando autoritativo transfere ativos
→ consequência altera decisões futuras
→ snapshot preserva e retoma a mesma linha do tempo
```

O corte não tenta implementar a cidade inteira. Criminalidade, polícia, gangues, múltiplos estabelecimentos, investigação prolongada, insolvência pessoal e simulação por coortes continuam planejados. Introduzi-los antes de validar esta cadeia multiplicaria estados, regras e hipóteses sem aumentar o aprendizado principal.

## Estado do repositório antes deste corte

### Já existia

- núcleo C#/.NET 8 executável sem Godot;
- apresentação Godot separada do domínio;
- dinheiro em centavos inteiros;
- compra de fornecedor, preço, estoque e venda autoritativa em `StoreState`;
- mercado compartilhado entre BOXBOX, concorrente e não compra;
- motor substituível de escolha de cliente, com PRNG injetado;
- ações de mercado filtradas por passabilidade antes da preferência;
- snapshots econômicos imutáveis e rejeição sem mutação parcial;
- miniagenda determinística por minuto e ids estáveis;
- equivalência entre avanço incremental e avanço em lote no mini-turno;
- verificações headless de replay, conservação de estoque e reconciliação de caixa.

### Estava ausente

- identidade persistente de NPC fora de uma decisão de compra;
- necessidades que atravessam mais de um evento;
- emprego, salário, perda de emprego, dívida e busca de trabalho no mesmo ciclo;
- uma decisão autônoma geral que não fosse somente escolha de loja;
- estado mundial reunindo atores e instituições econômicas;
- save versionado da agenda, fatos processados, estado dos NPCs e log causal;
- demonstração de que fechar a loja muda as oportunidades dos NPCs sem roteiro alternativo;
- retomada do mundo salvo sem duplicar salário, compra, empréstimo ou ocorrência.

### Capacidades reutilizadas

- `StoreState.CompleteSale` continua sendo a autoridade sobre preço, estoque, caixa da loja e idempotência da venda;
- `SeededChoiceRandomSource` fornece sorteios derivados de seed e id do evento;
- o padrão já usado por `MiniTurnSimulation` orienta relógio monotônico, ordenação e processamento em lote;
- `BOXBOX.Core` continua sem dependência de Godot ou de serialização;
- o executável de verificação continua sendo a barreira headless do projeto.

## Decisões confirmadas preservadas

- a IA produz intenção; o domínio valida e executa;
- a política recebe um conjunto imutável de ações válidas e não pode acrescentar ações;
- toda alteração econômica ocorre por comando transacional;
- salário, compra e empréstimo são transferências entre saldos existentes no protótipo;
- a soma de caixa dos NPCs, loja, empregador e banco é conservada;
- estoque só diminui por uma venda confirmada no `StoreState` existente;
- tempo é inteiro, monotônico e independente de FPS;
- a aleatoriedade deriva de seed, evento e fluxo, não do número de frames ou logins;
- eventos e saves guardam ids e chaves semânticas, nunca frases traduzidas;
- avanço contínuo e recuperação do mesmo intervalo produzem o mesmo estado canônico;
- o save é gravado por adaptador; domínio e aplicação não conhecem JSON nem sistema de arquivos.

## Hipóteses do protótipo

Estes valores são fixtures técnicas, não balanceamento aprovado:

- dois NPCs (`npc.lia` e `npc.rui`);
- uma necessidade de alimentação limitada a `0..1000`;
- um SKU sintético `meal`, preço de R$ 5,00 e oito unidades;
- salário de R$ 6,00 para Lia e R$ 5,00 para Rui;
- limite de crédito individual simplificado;
- busca de emprego com 55% de sucesso por tentativa válida;
- quatro dias compactados em 4.560 minutos simulados;
- uma ocorrência externa fixa fecha o empregador de Lia no segundo dia.

A ocorrência é fixa para garantir cobertura do fluxo causal. O resultado das decisões posteriores continua emergindo das condições do ator, das guardas, dos pesos e da seed.

## Questões abertas

- cadência final das necessidades e relação entre tempo real e simulado;
- se alimentação será um recurso explícito, uma despesa doméstica agregada ou ambos;
- composição final de renda, moradia, saúde e outras necessidades;
- como vagas, empregadores e salários disputarão o mesmo mercado de trabalho;
- critérios e consequências de inadimplência pessoal;
- quando um NPC distante permanece individual ou entra em coorte;
- quais fatos sobre outro ator podem circular como informação social;
- escopo visual e processual de polícia, investigação e prisão;
- se o jogador poderá controlar um personagem fora da empresa BOXBOX neste produto.

## Arquitetura implementada

```text
LivingWorldPrototypeScenario
        │ conteúdo/fixture
        ▼
LivingWorldSimulation ── agenda, relógio, barreiras e projeção de eventos
        │
        ├── gera ações válidas a partir do estado observado
        ▼
INpcDecisionPolicy ── escolhe uma das candidatas com PRNG semeado
        │ proposta
        ▼
LivingWorldMarketPurchaseService ── cria oportunidade e visão de poder de compra
        │
        ▼
SharedMarketPurchaseService ── escolhe BOXBOX, rival ou não comprar
        │ resolução imutável
        ▼
LivingWorldTransactionService ── revalida e calcula o próximo estado
        │
        ├── StoreState/CompetitorMarketState ── venda autoritativa já existente
        └── LivingWorldInvariantRegistry ── valida antes do commit

ILivingWorldSnapshotStore
        ▲ porta interna
        │
JsonLivingWorldSnapshotStore ── adaptador de arquivo e DTOs de persistência
```

O `LivingWorldState` é um agregado de protótipo para assegurar atomicidade entre os lados de uma transferência. O mercado resolve sobre uma visão imutável que remove ofertas acima do poder de compra do NPC. O mundo só aceita essa resolução quando consegue debitar o comprador e confirmar novamente a venda no estado autoritativo da BOXBOX ou do rival. Se qualquer guarda falhar, o snapshot original é devolvido.

## Modelo de dados

### `SimulatedActorState`

- `ActorId`: identidade estável;
- `CashCents`: caixa disponível;
- `FoodNeed`: pressão atual de alimentação;
- `Employment`: empregado ou desempregado;
- `WageCents`: salário do vínculo simplificado;
- `DebtCents` e `MaximumDebtCents`: dívida e limite;
- `KnownFactIds`: fatos efetivamente conhecidos pelo ator.

### `LivingWorldState`

- coleção de atores;
- `StoreState` autoritativo da BOXBOX;
- concorrentes agregados e suas ofertas/capacidade restantes;
- caixa externo que recebe pagamentos feitos a concorrentes sem fingir uma contabilidade privada completa;
- caixa do empregador;
- caixa do banco;
- ids de comandos já concluídos.

### `LivingWorldSnapshot`

- versão do ruleset;
- seed base;
- minuto simulado atual;
- estado mundial;
- agenda ordenada ainda relevante;
- ids de eventos processados;
- log causal de eventos confirmados.

O snapshot não persiste texto localizado. O adaptador usa DTOs próprios para evitar anotar o domínio com detalhes de JSON e grava primeiro em arquivo temporário antes de substituir o save.

## Ações e contratos

| Ação | Pré-condições principais | Efeito confirmado | Falha/rejeição | Duração/terminal |
|---|---|---|---|---|
| aumentar necessidade | ator existe; incremento positivo | necessidade cresce até 1000 | valor inválido ou ator ausente | instantânea; não terminal |
| pagar salário | ator empregado; empregador possui caixa | empregador debita e ator credita | desempregado ou caixa insuficiente | instantânea; não terminal |
| perder emprego | ator empregado | estado vira desempregado e fato entra no conhecimento | ator já desempregado | ocorrência instantânea; não terminal |
| procurar refeição | necessidade presente | mercado forma BOXBOX/rivais admissíveis e não compra; uma venda confirmada debita o NPC, atualiza o vendedor e reduz a necessidade | resolução inválida é rejeitada sem commit | instantânea; não terminal |
| tomar crédito essencial | necessidade alta; falta caixa; limite e liquidez bancária | banco debita, NPC credita e dívida cresce | valor inválido, limite ou liquidez | instantânea; não terminal |
| buscar emprego | ator desempregado | vínculo é criado ou tentativa falha conforme seed | ator já empregado | instantânea nesta fixture; não terminal |
| esperar | sempre disponível na barreira | nenhum ativo muda | não rejeita | termina somente a decisão atual |
| abrir/fechar loja | comando explícito do jogador | muda a passabilidade de compras futuras | id duplicado | instantânea; não terminal |

O próximo incremento deverá transformar busca de emprego e crédito em contratos temporais mais ricos, com vagas/ofertas, duração, timeout e efeitos de falha. O corte atual mantém essas ações instantâneas de propósito.

## Estratégia de decisão dos NPCs

1. A aplicação observa apenas o estado permitido do ator e das instituições.
2. Guardas produzem candidatas:
   - procurar refeição quando existe necessidade;
   - tomar crédito somente se a necessidade for severa e limite/liquidez permitirem;
   - buscar emprego somente quando desempregado;
   - esperar permanece como quiescência limitada à barreira atual.
3. Pesos inteiros expressam preferência, não permissão.
4. Candidatas são ordenadas por id estável.
5. `WeightedNpcDecisionPolicy` escolhe usando PRNG derivado da seed e do id da barreira.
6. Para alimentação, a proposta cria uma `MarketPurchaseOpportunity`; ofertas acima do caixa do ator são inadmissíveis.
7. O motor de cliente escolhe entre BOXBOX, rival admissível e não comprar.
8. A resolução vira comando tipado e o domínio revalida comprador e vendedor antes do commit conjunto.

Falha de busca de emprego é um resultado normal. Rejeição de comando é diferente: indica que o comando deixou de ser admissível e preserva o estado anterior. Indisponibilidade futura de um motor sofisticado poderá usar a política heurística atual como fallback, sem conceder permissões extras.

## Comunicação entre sistemas

`LivingWorldEvent` é a saída confirmada para apresentação, relatórios e persistência. Ele carrega:

- id estável e minuto;
- tipo de evento agendado que originou a mudança;
- ator;
- chave semântica do motivo;
- ação selecionada, quando existe;
- deltas estruturados de caixa, necessidade e estoque;
- resultado do mercado e loja escolhida, quando aplicável;
- marcador de conhecimento do ator.

Neste corte, todos os fatos emitidos são conhecidos pelo próprio ator afetado. A estrutura já impede que a apresentação precise inferir mudanças a partir de animações, mas ainda não implementa fatos ocultos sobre terceiros. Furto e investigação deverão acrescentar projeções de conhecimento sem retirar o evento completo do log técnico.

## Tempo, persistência e simulação fora da área visível

O primeiro corte usa eventos discretos, não ticks de frame. Um evento é ordenado por:

1. minuto;
2. fase (`ExternalOccurrence`, `Obligations`, `Needs`, `DecisionBarrier`);
3. id estável.

Cada id processado é persistido. Ao recuperar um snapshot, a simulação ignora eventos já concluídos e continua a mesma agenda. O teste salva no minuto 1.620, recarrega de JSON e avança até 4.560; o fingerprint final precisa ser igual ao de uma execução contínua.

NPC visível e NPC distante usam, por enquanto, exatamente a mesma resolução individual. Isso é deliberado: primeiro se estabelece o resultado canônico. A otimização futura seguirá duas camadas:

- `LiveDetailed`: projeta o mesmo evento econômico em personagem, movimento e animação;
- `LiveCompact/CatchingUp`: executa a agenda sem projeção visual.

Coortes só entram quando houver um contrato explícito de equivalência. O benchmark mínimo será comparar dinheiro, estoque, dívida, emprego, conhecimento, ids processados e log causal com a resolução individual.

## Invariantes verificadas

- nenhum saldo físico fica negativo;
- dívida permanece entre zero e o limite do ator;
- necessidade permanece entre zero e 1000;
- salário conserva dinheiro entre empregador e ator;
- compra conserva dinheiro entre ator e loja;
- compra rival conserva dinheiro entre ator e caixa externo e reduz somente a oferta/capacidade rival;
- empréstimo conserva dinheiro entre banco e ator;
- estoque só cai pela venda confirmada já existente;
- comando duplicado não produz segundo efeito;
- rejeição devolve a mesma instância de estado;
- relógio não retrocede;
- evento agendado não executa duas vezes;
- política não recebe compra de loja fechada;
- mesma seed, agenda e estado reproduzem a mesma trajetória;
- avanço incremental e catch-up produzem o mesmo fingerprint;
- save/reload não duplica efeitos;
- fechar a loja pelo jogador muda oportunidades e consequências econômicas dos NPCs.

## Cenários de teste e critérios de aceitação

### Cenário causal

- Lia e Rui recebem renda e desenvolvem necessidade;
- compras confirmadas alteram caixa e estoque;
- o empregador de Lia fecha por ocorrência identificada;
- Lia passa a avaliar crédito e busca de emprego conforme suas condições;
- pelo menos uma decisão posterior reflete a perda de renda;
- o total monetário do mundo permanece constante.

### Passabilidade

- com a BOXBOX fechada, ela não entra nas opções admissíveis do mercado;
- o rival continua elegível quando sua oferta, capacidade e preço permitem;
- nenhuma venda rival altera caixa ou estoque da BOXBOX;
- a opção de não comprar permanece alcançável.

### Atomicidade

- tentar comprar com a loja fechada retorna `StoreClosed`;
- caixa, estoque, necessidade e log não mudam;
- o resultado aponta para o snapshot original.

### Tempo e save

- execução em quatro avanços e execução em um catch-up terminam iguais;
- salvar e recarregar no meio produz o mesmo fingerprint final;
- ids processados impedem salário, ocorrência, compra e empréstimo duplicados.

### Intervenção do jogador

- mesma seed e mundo inicial, variando apenas o comando de fechar a loja;
- loja aberta vende e altera caixa/estoque dos atores;
- loja fechada retira a oportunidade e gera outra trajetória sem branch roteirizado.

## Plano incremental recomendado

### Etapa 1 — concluída neste corte

- atores persistentes;
- necessidade, emprego, renda e dívida mínima;
- ocorrência causal de perda de emprego;
- política local plugável;
- compra pelo `StoreState` existente;
- agenda, replay, intervenção e save JSON.

### Etapa 2 — integração ao mercado compartilhado concluída

- transformar necessidade em `MarketPurchaseOpportunity`;
- permitir BOXBOX, concorrente admissível e não compra;
- manter o débito do comprador e o commit da loja numa transação mundial;
- persistir oferta, capacidade, decisões consumidas e recebimentos agregados do rival;
- provar os três resultados com seeds douradas.

Permanecem para o próximo corte de conteúdo a memória/experiência real do cliente e a substituição do SKU sintético pelo catálogo versionado do segmento.

### Próximo corte — projeção jogável na UI

- expor caixa, estoque, necessidade e estado de emprego em view models;
- apresentar feed causal com loja escolhida e não compra;
- ligar abrir/fechar loja, relógio, save e carregamento à cena Godot;
- projetar NPCs visíveis a partir dos eventos confirmados, sem usar animação como autoridade;
- manter pt-BR e inglês por chaves localizadas.

### Etapa 3 — trabalho, despesas e dívida temporal

- vagas e empregadores persistentes;
- busca de trabalho com duração, capacidade e concorrência;
- despesas recorrentes agendadas;
- contrato de crédito, parcelas, atraso e recuperação limitada;
- insolvência pessoal explícita sem confundi-la com deadlock.

### Etapa 4 — conhecimento e relações

- fato, registro e conhecimento separados por ator;
- observação, conversa e rumor com fonte, confiança e atraso;
- relações que alteram preferências sem alterar fatos;
- resumos de retorno respeitando o que o jogador pode saber.

### Etapa 5 — perdas e criminalidade mínima

- reutilizar realidade física, registro e conhecimento do sistema de perdas;
- começar por furto abstrato de ativo com oportunidade, testemunha e evidência;
- comunicação de ocorrência como ação de vítima/testemunha, não gatilho automático;
- investigação como máquina temporal separada;
- nenhuma punição automática e nenhuma onisciência policial;
- segurança de pessoas como resposta padrão a roubo.

Polícia, prisão e gangues só entram depois de definir estados, autoridades, informações, ações, prazos, terminais e propriedades de progresso. A presença de polícia ainda é uma questão aberta nas fontes de verdade de perdas e segurança.

### Etapa 6 — níveis de detalhe e escala

- benchmark da resolução individual;
- `LiveCompact` sem apresentação;
- coortes somente para populações equivalentes;
- testes metamórficos entre detalhado, compacto e catch-up;
- orçamento de CPU/memória em hardware modesto.

## Riscos e limitações atuais

- a agenda do cenário é conteúdo C# fixo; ainda não existe carregador de conteúdo versionado;
- o mundo contém uma loja, um banco agregado, um empregador agregado e dois NPCs;
- o caixa do empregador e do banco é um reservatório simplificado, sem livro-caixa próprio;
- juros, parcelas, vencimentos e inadimplência ainda não existem;
- a busca de emprego não disputa vagas reais;
- a necessidade de alimentação não produz dano ou terminal e pode ficar em 1000;
- `Wait` pode ser escolhido repetidamente; o horizonte finito da fixture evita livelock, mas uma propriedade formal de progresso ainda é necessária antes de agendas abertas;
- o save tem versão, mas ainda não possui migração, checksum, backup ou recuperação degradada;
- a gravação é atômica no nível de substituição de arquivo suportado pelo sistema, mas ainda não possui journal;
- conhecimento está modelado apenas no ator afetado; não existe percepção social;
- a memória de familiaridade do cliente ainda é recriada a cada decisão;
- o catálogo de alimentação continua sendo a fixture sintética `meal/essentials`;
- o caixa rival é agregado em um reservatório externo; ainda não existe livro-caixa privado do concorrente;
- não há UI Godot para observar esta fatia; a prova atual é headless;
- valores econômicos são hipóteses técnicas e não representam escala final ou regras reais brasileiras.

## Por que estas decisões

O corte reutiliza a venda existente porque duplicar estoque e caixa criaria duas autoridades econômicas. O agregado mundial envolve essa venda porque a transação agora possui um segundo lado persistente: o dinheiro do comprador. A política ponderada permanece simples para que falhas sejam atribuíveis a estados, guardas e pesos, não a um modelo opaco. O adaptador JSON fica em projeto separado para que formato de save não contamine o domínio. A ocorrência de perda de emprego foi escolhida antes de crime porque demonstra causalidade, autonomia, pressão econômica e recuperação com menos estados de segurança, evidência e autoridade.

Essa implementação valida a espinha dorsal; ela não afirma que o design amplo já está concluído.
