# BOXBOX — linha do tempo viva e recuperação offline

## Decisão confirmada

O mundo comercial do BOXBOX é persistente e continua avançando independentemente da presença do jogador. Não existe pausa econômica controlada pelo jogador: menus, fechamento do aplicativo e dias sem entrar não congelam clientes, concorrentes, funcionários, estoque, validade, obrigações, incidentes ou o mercado.

O save registra o último instante de parede observado e o instante atual da simulação. Ao retornar, o jogo calcula o intervalo ausente, converte esse intervalo pela política temporal versionada e processa a mesma agenda e as mesmas IAs locais usadas durante a execução visível.

O tempo simulado não precisa ser `1:1` com o tempo real. A razão exata permanece para protótipo e balanceamento, mas o intervalo ausente não pode ser simplesmente descartado. Otimizações podem reduzir o detalhe computacional, nunca apagar silenciosamente suas consequências.

## Filosofia do mundo independente

O protagonista não é o relógio do mundo nem seu administrador invisível. Cada ator continua conforme capacidade, função, informação, autoridade, recursos e política próprios:

- concorrentes operam, reagem, crescem, perdem capacidade, endividam-se ou saem do mercado sem depender do jogador;
- funcionários, gerentes e contadores da empresa do jogador executam somente tarefas e decisões que suas funções e permissões permitem;
- agentes podem acertar, errar, ser incompetentes, sofrer golpe, furtar, omitir sinais ou deixar uma tarefa falhar quando isso for admissível e explicável;
- sem pessoa, contrato, automação ou política capaz, a tarefa não acontece: a loja pode ficar fechada, não repor, perder prazo ou deteriorar;
- o protagonista não trabalha silenciosamente quando o aplicativo está fechado; somente comandos já iniciados/agendados e operação realmente delegada continuam;
- a empresa do jogador pode entrar em insolvência e falir durante a ausência;
- falência encerra a empresa do jogador, não o bairro: concorrentes e demais atores continuam na linha do tempo persistente.

O sistema não cria um “gerente perfeito” para proteger o jogador offline. Proteções obrigatórias têm escopo estreito: irregularidade intencional continua não delegável; durante roubo, preservar pessoas é a resposta padrão. Essas garantias não impedem decisões ruins delegadas, furto, golpe, perda, dívida ou falência.

Fato, registro e conhecimento permanecem separados. Um ativo pode já ter sido furtado, um golpe pode ter ocorrido ou uma dívida pode ter crescido sem que o jogador saiba. O retorno revela somente o que funcionários, conciliações, câmeras, relatórios e outros canais realmente detectaram.

## Autoridade temporal

```text
Relógio de parede       → mede o intervalo desde a última âncora salva
Política temporal       → converte intervalo real em intervalo simulado
SimulationClock         → autoridade econômica monotônica
Agenda de eventos       → ordena mudanças do mundo
Atores e IAs locais     → escolhem somente ações válidas dentro de sua autoridade
Apresentação            → mostra conhecimento disponível e resume o período
```

O relógio de parede não ordena vendas, pagamentos ou incidentes. Ele informa somente quanto da linha do tempo precisa ser recuperado. O `SimulationClock` usa unidade inteira ou representação exata e nunca retrocede.

Uma âncora mínima de save contém:

```text
TimeAnchor
├── observed_wall_time_utc
├── simulation_time
├── temporal_policy_version
├── ruleset_version
├── schedule_snapshot
└── randomness_state_or_derivation_version
```

No retorno:

1. ler a última âncora válida;
2. obter o instante de parede atual por uma porta injetável;
3. calcular `elapsed = max(0, current - observed)`;
4. converter `elapsed` em alvo simulado pela política temporal versionada;
5. processar a agenda até o alvo com os atores existentes;
6. verificar invariantes e confirmar cada transação atomicamente;
7. produzir snapshot, nova âncora e resumo causal conforme o conhecimento disponível.

Se o relógio local estiver anterior à última âncora, a simulação não retrocede e registra a anomalia. A política para saltos muito grandes do relógio, migração de fuso e manipulação deliberada ainda precisa de protótipo; nenhuma dessas situações autoriza desfazer eventos.

## Estados de processamento e apresentação

Não são velocidades econômicas escolhidas pelo jogador. São maneiras de observar ou calcular a mesma linha do tempo:

```text
LiveDetailed | LiveCompact | CatchingUp | CatchUpSummary | Degraded
```

- **LiveDetailed:** processa o presente e projeta clientes e tarefas individualmente.
- **LiveCompact:** preserva a economia e reduz detalhe visual.
- **CatchingUp:** recupera rapidamente o intervalo ausente.
- **CatchUpSummary:** apresenta fatos conhecidos, sinais e resultados relevantes.
- **Degraded:** preserva o save e pede intervenção quando a recuperação técnica limitada falha; não é uma pausa de gameplay.

FPS, animação e quantidade de NPCs renderizados não alteram a economia. Coortes e agregação só podem substituir indivíduos quando cumprem um contrato de equivalência econômica para os agregados canônicos definidos.

## Agenda e decisões durante ausência

`ScheduledEvent` carrega `stable_id`, `simulation_time`, fase, tipo/payload, fonte, versão de regras, chave de aleatoriedade e nível de atenção. Empates seguem a ordem canônica: `CompletionsAndReleases`; `EffectEnd`; `LotExpiry`; `ObligationDue`; `ExternalIncidentArrival`; `DecisionBarrier`; `StartsAndSchedules`; `AutomaticDecisions`; `DemandAndArrivals`; `AlertsAndProjections`.

Eventos-filhos não voltam a fase concluída. IDs são causais e únicos. Aleatoriedade deriva de seed, evento e fluxo, nunca de frame ou frequência de login.

Uma decisão offline só pode ser executada por ator que possua simultaneamente passabilidade, informação suficiente, função, permissão e recursos. Autoridade não garante competência nem resultado favorável. Contratos desfavoráveis, golpes e erros podem ser aceitos por agentes que realmente tenham essa autoridade, mas contratos ainda passam pelo validador determinístico e não podem quebrar leis do domínio.

Sem ator capaz, a decisão não congela o mundo: a tarefa fica sem execução, expira, acumula atraso ou permanece pendente conforme seu contrato temporal, com consequências normais. Ações explicitamente não delegáveis não ganham autorização durante a ausência.

Barreiras simultâneas agrupam decisões do mesmo timestamp para consistência, auditoria e resumo, sem congelar toda a linha do tempo. O conjunto termina em ação de um ator autorizado, ausência de ação, timeout, recuperação limitada ou terminal explícito.

## Fechamento e continuidade entre dias

```text
Open
→ Closing
→ SettlingQueuesAndTasks
→ Reconciliation
→ DayClosed
→ PreparingNextDay
→ Open
```

`DayClosed` encerra somente o escopo diário. A empresa, o bairro e a linha do tempo continuam. Horários, pessoas, contratos, automações e políticas determinam se e como a empresa opera no dia seguinte; o jogador não constrói cada dia manualmente.

Clientes, rivais, promoções, fofocas, validade, obrigações, fiscalização, tarefas e incidentes usam timestamps simulados. Concorrentes e funcionários tomam decisões em suas cadências próprias, não por frame nem por frequência de login.

## Resumo de retorno e conhecimento

O retorno diferencia três camadas:

1. fatos confirmados que o jogador já pode conhecer;
2. sinais e diferenças percebidas que ainda exigem investigação;
3. fatos ocultos que permanecem fora da apresentação até algum mecanismo revelá-los.

O resumo conhecido pode incluir:

- tempo real ausente e intervalo simulado processado;
- dias e expedientes transcorridos;
- compras, vendas, receita, margem, caixa e estoque registrados;
- rupturas, vencimentos, perdas e incidentes detectados;
- ações e omissões de funcionários, gestores, contadores e automações;
- decisões expiradas ou ainda pendentes;
- mudanças conhecidas de concorrentes e sinais de mercado;
- causa conhecida de insolvência, falência ou outro terminal.

O resumo não inventa uma narrativa separada: ele projeta eventos confirmados e conhecimento permitido. Auditoria técnica preserva o log completo para replay, mas a interface não revela automaticamente fatos ocultos ao jogador.

## Invariantes

- `SimulationClock` é monotônico e nunca segue o relógio local para trás.
- Evento nunca executa antes da hora nem duas vezes.
- Mesma versão, seed, estado, atores, políticas, comandos e âncoras reproduzem o mesmo resultado.
- Execução contínua e recuperação do mesmo intervalo produzem o mesmo hash econômico canônico.
- Processamento detalhado, em lote ou por coortes não perde nem duplica efeitos.
- Ausência não cria funcionário, competência, informação, permissão ou recurso inexistente.
- Ação não delegável nunca é escolhida por IA.
- Agente autorizado ainda passa por guardas, invariantes e commit atômico.
- Entrar com maior frequência não cria vendas, sorteios ou ações extras para o mesmo intervalo e mesmas condições.
- Fato oculto continua auditável sem ser revelado prematuramente ao jogador.
- Falência do jogador encerra `company_run` da empresa, não a simulação do mercado persistente.
- Alterar FPS, idioma ou detalhe visual não muda o resultado econômico.

## Teste mínimo

Execute o mesmo intervalo de sete dias de duas formas:

1. processamento contínuo em pequenos avanços;
2. save no início e uma única recuperação offline no final.

Use a mesma seed, regras, atores, políticas e âncoras. Compare hash econômico, agenda, eventos confirmados, conhecimento de cada agente e estado dos concorrentes. Inclua fechamento e reabertura, funcionário competente, funcionário falho, furto oculto, golpe contratual válido, estoque insuficiente, produto vencendo, obrigação, rival e falência offline.

O segundo fluxo pode calcular mais rápido, mas não pode produzir outra história econômica. Após a falência da empresa do jogador, verifique que o rival continua avançando. Teste também relógio regressivo, salto grande para frente, save no meio da recuperação, retomada idempotente e falha técnica que leva a `Degraded` sem corromper o save.

## Questões abertas

- Razão entre tempo real decorrido e tempo simulado.
- Grau de detalhe individual preservado em ausências longas.
- Política para saltos muito grandes ou manipulação do relógio local.
- Autoridades iniciais de cada cargo e quais exigem delegação explícita.
- Como o jogador acompanha o mercado depois da falência de sua empresa.
- Formato e nível de interação do resumo de retorno.
- Se alguma parte noturna será visualizada ou apenas resumida.
