# Regras verificáveis e consistência

## Princípio

Não tente antecipar manualmente todas as decisões do jogador. Defina leis, ações válidas, propriedades de progresso e verificadores capazes de explorar combinações automaticamente.

Tabelas-verdade são úteis para poucos booleanos. Sistemas temporais e emergentes exigem também tabelas de decisão, máquinas de estado, invariantes, restrições, análise de alcançabilidade, testes baseados em propriedades e simulações em lote.

Essa camada não torna o modelo local maior ou mais inteligente. Ela torna suas decisões válidas, explicáveis, reproduzíveis e resistentes a contradições.

No vocabulário informal do projeto, “passabilidade” significa admissibilidade lógica: o que é permitido ou bloqueado. Traduza o termo como elegibilidade, pré-condições, guardas, permissões e restrições; não como probabilidade ou pathfinding. A admissibilidade sempre ocorre antes dos pesos de preferência.

## Autoridade

```text
estado
→ gerar candidatas
→ filtrar ações inválidas
→ ponderar somente ações válidas
→ selecionar com PRNG semeado
→ emitir comando tipado
→ domínio valida e calcula estado/eventos propostos
→ verificar invariantes globais e pós-condições
→ confirmar estado/eventos atomicamente, ou descartar tudo
→ registrar decisão e explicação
```

- Invariantes e transições válidas são determinísticas.
- Conteúdo configura valores, limites, curvas e capacidades.
- Políticas/IA escolhem entre ações válidas.
- Regras declarativas nunca mutam o estado diretamente.

## Artefatos conceituais

### Ação

```text
ActionDefinition
├── id e actor_type
├── preconditions e forbidden_conditions
├── required_resources e resource_locks
├── command
├── expected_events e postconditions
├── temporal_contract e duration_or_completion_condition
├── timeout
├── interruption_policy e failure_outcomes
├── tags: progress | recovery | terminal
└── terminal_scope_if_terminal: task | incident | business_day | company_run
```

### Política

```text
PolicyRule
├── id e when
├── applies_to
├── weight_multiplier e weight_bonus
└── explanation_reasons
```

A política recebe um conjunto válido e imutável. `applies_to` apenas identifica ações que recebem modificadores; ações não citadas preservam o peso-base. A política nunca adiciona nem remove ações. Recuperações voltam ao início do pipeline e passam pelas mesmas guardas.

Componha regras coincidentes de forma independente da ordem: `peso_final = max(0, peso_base × produto(multiplicadores) + soma(bônus))`. Não permita substituições implícitas. Ordene candidatos por `action_id` antes do sorteio semeado; isso estabiliza empates e replays. Peso zero altera preferência, não passabilidade.

### Transição

```text
StateTransition
├── entity_type e from_state
├── action e guard
├── to_state
└── additional_assertions
```

Invariantes globais pertencem a um registro central obrigatório e rodam em todo commit. `additional_assertions` apenas acrescenta verificações; transições, regras e modelos não podem desabilitar leis globais.

### Recuperação

```text
RecoveryDefinition
├── id e triggered_by
├── candidate_actions
├── max_attempts_or_duration
├── degraded_terminal
└── degraded_terminal_scope
```

As candidatas de recuperação voltam ao pipeline normal; a definição não concede permissão especial.

### Propriedade de progresso

```text
LivenessProperty
├── starting_condition
├── responsible_actor
├── quantifier: exists_path | all_fair_executions | probability_at_least
├── eventually_condition
├── allowed_terminal_states_and_scopes
├── progress_measure e fairness_assumptions
├── minimum_probability
└── max_steps_or_duration
```

O formato concreto pode ser JSON, TOML, YAML ou recurso da engine. Mantenha expressões tipadas e limitadas; não esconda código arbitrário em arquivos de dados.

Todo terminal pertence a uma máquina e declara escopo. Resolver um incidente ou terminar o dia não encerra a empresa; a transição para o próximo escopo precisa ser explícita. A exploração global não para ao encontrar um terminal local.

## Verificação estática

Ao carregar conteúdo, detecte:

- referências e estados inexistentes;
- tipos e intervalos inválidos;
- condições impossíveis ou contraditórias;
- efeitos incompatíveis;
- regras sobrepostas sem prioridade clara;
- pesos finais negativos ou não finitos;
- conjuntos com peso total zero sem tratamento explícito;
- recuperações recursivas ou sem limite;
- ações ou estados comprovadamente inalcançáveis.

## Verificação de estados e fluxo

Abstraia valores contínuos em faixas e compile um grafo com comandos do jogador, comandos de agentes, passagem do tempo, eventos agendados, conclusões, interrupções e timeouts. Explore-o por horizonte limitado para encontrar:

- estado não terminal sem ação válida;
- ciclo sem progresso;
- conteúdo órfão;
- dependência circular;
- terminal acidental;
- fluxo que não consegue atingir seu estado resolvido.

Espera por um evento futuro é quiescência; ausência de comando, avanço de tempo ou evento capaz de resolver é deadlock. Procure também ciclos de tempo zero. Locks são adquiridos atomicamente em ordem definida e liberados em sucesso, rejeição, interrupção ou timeout.

Distinga falência planejada de deadlock técnico, livelock, soft lock econômico e conteúdo inalcançável.

Uma ação válida não prova progresso. Para controle do jogador, normalmente verifique a existência de um caminho; para agentes, verifique toda execução em que ações válidas não são adiadas para sempre ou uma probabilidade mínima dentro de um horizonte. Avalie os pesos finais e componentes fortemente conectados: uma ação permanentemente com peso zero não satisfaz a propriedade de progresso.

Propriedades mínimas:

- todo estado não terminal oferece comando, evento futuro ou terminal explícito;
- de toda loja aberta existe caminho válido para fechar;
- todo incidente é resolvido, recuperado ou termina explicitamente;
- toda transferência termina ou falha com motivo;
- todo terminal é intencional e explicável.

## Testes gerativos

- Testes baseados em propriedades geram sequências de comandos e verificam invariantes.
- Testes metamórficos confirmam relações: mais fadiga não melhora atenção; mais treinamento não piora detecção; mudar idioma não muda a economia.
- Fuzz procura dados e sequências inválidos.
- Monte Carlo explora seeds, eventos raros, estratégias dominantes e soft locks econômicos.
- Replays dourados confirmam determinismo.

Monte Carlo não prova ausência de contradições. Use-o em conjunto com validação estática e alcançabilidade.

## Recuperação e diagnóstico

Não use “fallback” como categoria única. Separe política heurística quando o modelo estiver indisponível, ação normal de recuperação, rejeição transacional sem mutação e estado degradado/terminal seguro. Toda recuperação é revalidada, limitada, não recursiva e auditada. Em testes, uma ativação inesperada falha com contraexemplo; em produção, preserva o save e sinaliza modo degradado.

Registre `ruleset_version`, versão do conteúdo/modelo, seed, snapshot, candidatas, rejeições e motivos, pesos, sorteio, comando, eventos e primeira invariante violada.

O verificador deve produzir um contraexemplo reproduzível, por exemplo:

```text
contratar segurança
→ negar acesso ao cofre
→ ativar sangria automática por segurança
→ caixa crítico
→ nenhuma pessoa autorizada
→ deadlock
```

Recuperações de produção, como pausar ou fechar a loja, preservam o save. Elas não contam sozinhas como prova de progresso.
