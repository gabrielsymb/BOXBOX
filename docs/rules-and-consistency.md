# BOXBOX — regras e consistência da simulação

## Objetivo

O BOXBOX não precisa antecipar manualmente todas as decisões do jogador. Ele precisa definir:

- quais estados existem;
- quais ações são possíveis;
- quando uma ação é válida;
- quais leis nunca podem ser quebradas;
- quais estados são terminais de propósito;
- como todo estado jogável encontra progresso ou recuperação;
- como reproduzir e explicar uma falha.

A ideia de “tabela-verdade” está correta para regras booleanas pequenas, mas não resolve sozinha tempo, dinheiro, sequências ou centenas de combinações. O projeto usará um **sistema de regras verificáveis**.

## Terminologia do projeto

Quando o usuário disser **passabilidade**, está falando de admissibilidade lógica: o que o sistema deixa passar e o que bloqueia. Na especificação e no código, represente isso por elegibilidade, pré-condições, guardas, permissões e restrições. Não confunda com probabilidade nem com navegação física. Em linguagem simples: primeiro as regras formam a lista do que pode acontecer; depois a IA escolhe dentro dessa lista.

### Glossário rápido

- **seed:** número que permite repetir a mesma sequência de sorteios;
- **commit atômico:** a mudança inteira é confirmada ou nenhuma parte dela é aplicada;
- **lock:** reserva temporária de um recurso para duas ações não usarem a mesma coisa ao mesmo tempo;
- **deadlock:** o jogo chegou a um estado que deveria continuar, mas nada pode fazê-lo avançar;
- **livelock:** o jogo continua executando ações, porém repete um ciclo sem progresso;
- **quiescência:** espera legítima porque existe um evento futuro capaz de continuar o fluxo;
- **estado degradado:** modo seguro e explícito usado quando a recuperação normal se esgotou;
- **quantificador de progresso:** diz se basta existir um caminho, se toda execução normal precisa avançar ou se há uma chance mínima dentro de um prazo;
- **condições de execução justa:** suposições que impedem uma ação válida de ser adiada para sempre pelo agendador.

## Divisão de autoridade

```text
Leis determinísticas  → definem o que é permitido
Conteúdo configurável → define parâmetros, limites e curvas
Políticas/IA local    → escolhem entre ações já válidas
Apresentação          → mostra apenas o que o jogador pode saber
```

“Não hardcoded” significa conteúdo orientado a dados e políticas substituíveis. Não significa um mundo sem leis explícitas. Dinheiro, estoque, acesso, contratos, transições válidas e conservação de ativos continuam determinísticos.

## Pipeline de decisão

```text
Estado atual
→ gerar ações candidatas
→ eliminar ações inválidas
→ calcular pesos das ações válidas
→ selecionar com aleatoriedade semeada
→ enviar comando ao domínio
→ domínio valida e calcula o próximo estado e os eventos propostos
→ verificar invariantes globais e pós-condições
→ confirmar estado e eventos atomicamente, ou descartar tudo
→ registrar decisão e explicação
```

Pesos nunca decidem o que é permitido. Se uma ação exige acesso ao cofre e o funcionário não possui acesso, peso algum pode torná-la válida.

Nenhuma etapa deixa uma mutação parcial. Bloqueios de recursos (`locks`), reservas, lançamentos e eventos pertencem à mesma transação lógica: ou a mudança inteira é confirmada, ou nada muda e o estado anterior permanece intacto.

## Primeiro motor de comportamento: escolha de loja pelo cliente

O primeiro protótipo executável cobre uma decisão delimitada: um cliente escolhe entre ofertas de lojas já consideradas admissíveis ou a opção de não comprar. Ele não decide vendas diretamente nem altera estoque, caixa ou estado de loja.

O contrato recebe ids estáveis da decisão e do cliente, um perfil de preferências e uma coleção imutável de opções admissíveis. Cada opção contém atributos percebidos normalizados e um identificador de ação/loja. O conjunto precisa incluir exatamente uma opção de não comprar; se não houver loja elegível, essa pode ser a única opção. A construção desse conjunto pertence à aplicação/simulação, não ao motor de preferência.

O motor experimental calcula utilidades por fatores configuráveis e usa uma escolha softmax com fonte aleatória injetada. Os perfis, a temperatura, a utilidade externa e a taxa de aprendizado ficam em configuração JSON versionada; mudar esses dados não deve exigir reescrever regras por cliente. A sessão do cliente mantém memória entre decisões. A aplicação só deve encaminhar `ConfirmedCustomerExperience` depois que a conclusão do atendimento e o resultado forem validados pela simulação/domínio; esse contrato carrega ids da decisão e da loja e uma satisfação limitada a `[-1000, 1000]`. A sessão rejeita decisão desconhecida, loja diferente da escolhida, não compra e confirmação duplicada. A atualização modifica apenas familiaridade futura, nunca a realidade econômica.

O retorno inclui a opção escolhida, probabilidades e contribuições dos fatores para permitir replay e diagnóstico. A tela de laboratório verifica repetibilidade com seed, escolha limitada às opções recebidas, monotonicidade de preço para o perfil sensível a preço, efeitos positivos e negativos de uma experiência confirmada, ausência de alteração sem confirmação, replay do fluxo e guardas do contrato. Essas verificações usam experiências sintéticas do laboratório; ainda não há fluxo real de compra nem evento do domínio conectado. Os testes validam o mecanismo, não balanceamento final nem comportamento realista.

Este protótipo não usa rede neural nem aprendizado de máquina. O algoritmo é um motor substituível; objetivos e preferências são dados. A próxima integração precisa formar opções a partir do estado observado do mercado, sem expor ao cliente estoque privado, custos ou qualquer informação que ele não poderia perceber.

### Primeira venda de cliente no laboratório

O fluxo experimental de compra fica separado em aplicação e domínio:

```text
estado da loja
→ aplicação forma oferta BOXBOX somente se loja aberta, produto conhecido e quantidade disponível
→ sessão escolhe entre oferta admissível e não comprar
→ domínio valida comando de venda
→ novo estado imutável + evento CompletedSale, ou rejeição sem mutação
```

`StoreState.CompleteSale` calcula a baixa de estoque e o recebimento em centavos inteiros, rejeita loja errada/fechada, produto desconhecido, estoque insuficiente e decisão já processada, e retorna o mesmo estado quando rejeita. A aplicação só retorna o novo estado e evento se a transação for aceita. O chamador deve substituir o snapshot pelo resultado aceito; a tela não é autoridade sobre dinheiro ou estoque.

O laboratório usa um SKU, preço e estoque sintéticos apenas como fixture técnica; não há ainda catálogo nem preços reais no projeto. O experimento não calcula custo da mercadoria, lucro, fila, pagamento alternativo ou satisfação. Uma venda concluída ainda não gera `ConfirmedCustomerExperience`: falta uma regra de experiência fundamentada em resultados de atendimento, e não devemos inventar observações reais nem atualizar memória sem ela. Não há persistência; os snapshots vivem em memória durante o processo.

## Ferramentas lógicas

### Tabela-verdade

Serve para poucos valores booleanos. Exemplo: apólice ativa, risco coberto, evento na vigência e requisitos cumpridos resultam ou não em sinistro elegível.

### Tabela de decisão

Combina condições booleanas, categorias e faixas. Permite detectar:

- combinação sem resposta;
- duas respostas incompatíveis;
- regra redundante;
- prioridade ambígua;
- ausência de tratamento explícito para uma combinação.

É adequada para seguro, permissões, resposta de segurança e políticas de sangria.

### Máquina de estados

Representa ciclos de vida e transições. Prefira estados exclusivos a grupos de booleanos que podem se contradizer. Uma loja não deve ser simultaneamente `Open` e `Closed`; um segurança não deve estar `OffDuty` e cobrindo a entrada.

Máquinas distintas podem compor o mesmo ator, evitando uma explosão de estados:

```text
DutyState      = OffDuty | OnDuty | OnBreak
Assignment     = Unassigned | Entrance | Patrol | Cameras | CashDropEscort
Attention      = Focused | Strained | Distracted
Response       = Normal | Alerted | SafeProtocol | Reporting | Recovering
```

Essas dimensões são compostas, mas não totalmente independentes. Uma matriz de compatibilidade impede combinações como `OffDuty + Entrance`, `OnBreak + CashDropEscort` ou `OffDuty + SafeProtocol`. `OffDuty` e `OnBreak` exigem `Unassigned`; atribuições operacionais exigem `OnDuty`. `Alerted` exige sinal, suspeita ou alarme ativo; `SafeProtocol` exige ameaça confirmada; `Reporting` e `Recovering` exigem incidente atual ou recém-encerrado.

### Invariantes

Precisam ser verdadeiras no estado proposto antes do commit e continuar verdadeiras depois de toda ação aceita:

- dinheiro físico nunca é negativo;
- sangria e depósito conservam o total entre origem e destino autorizados;
- uma pessoa não executa duas tarefas exclusivas ao mesmo tempo;
- funcionário sem permissão não acessa o cofre;
- equipamento desligado não concede proteção;
- loja fechada não inicia atendimento;
- indenização respeita perda elegível, franquia, percentual e limite;
- todo ativo removido possui evento causal;
- probabilidades permanecem entre `0` e `1`.

### Restrições

Impedem conteúdo inválido antes da partida:

- referências e estados precisam existir;
- tipos e intervalos precisam ser compatíveis;
- uma regra não exige condições mutuamente impossíveis;
- efeitos não atribuem valores contraditórios;
- pesos finais de seleção não podem ser negativos nem não finitos; um conjunto com todos os pesos zerados exige tratamento explícito;
- conteúdo desbloqueado precisa ter ao menos uma forma de uso.

### Alcançabilidade e verificação de modelo

Uma versão reduzida do mundo vira um grafo:

```text
estado ──ação──> próximo estado
```

Valores grandes ou contínuos são agrupados em faixas, como `cash = empty | low | adequate | high`. O grafo inclui comandos do jogador, comandos de agentes, passagem do tempo, eventos agendados, conclusão de tarefas, interrupções e timeouts. O verificador percorre esse grafo por um horizonte limitado e procura estados inalcançáveis, ações impossíveis, estados sem saída, ciclos de tempo zero, ciclos sem progresso e dependências circulares.

Um estado que aguarda uma entrega futura é **quiescente**, não travado, desde que o relógio possa avançar e o evento esteja agendado. Um estado sem comando, avanço de tempo ou evento capaz de resolvê-lo é um deadlock.

Bloqueios e reservas são adquiridos em conjunto, seguem uma ordem total definida, possuem proprietário e são liberados em sucesso, rejeição, interrupção ou timeout. Nenhuma ação pode aguardar indefinidamente um recurso sem limite e diagnóstico.

O mundo completo terá combinações demais para prova exaustiva. A análise abstrata verifica estrutura; testes gerativos e simulações cobrem o restante.

## Formato conceitual

```text
ActionDefinition
├── id
├── actor_type
├── preconditions
├── forbidden_conditions
├── required_resources
├── resource_locks
├── command
├── expected_events
├── postconditions
├── temporal_contract: instant | timed
├── duration_or_completion_condition
├── timeout
├── interruption_policy
├── failure_outcomes
├── tags: progress | recovery | terminal
└── terminal_scope_if_terminal: task | incident | business_day | company_run
```

```text
PolicyRule
├── id
├── when
├── applies_to
├── weight_multiplier
├── weight_bonus
└── explanation_reasons
```

A política recebe um conjunto validado e imutável. `applies_to` identifica quais ações recebem o modificador; ações não citadas mantêm o peso-base. A política nunca adiciona nem remove ações. Qualquer ação de recuperação volta ao início do pipeline e passa pelos mesmos filtros e validações.

Modificadores coincidentes se compõem por uma fórmula única e independente da ordem:

```text
peso_final = max(0, peso_base × produto(dos multiplicadores) + soma(dos bônus))
```

Não existem substituições implícitas. Valores são finitos e limitados; candidatos são ordenados por `action_id` antes do sorteio semeado. Assim, empates e replays permanecem determinísticos. Um peso zero muda preferência, não passabilidade; propriedades de progresso impedem que uma ação automática necessária permaneça zerada para sempre.

```text
StateTransition
├── entity_type
├── from_state
├── action
├── guard
├── to_state
└── additional_assertions
```

Invariantes globais vivem em um registro central obrigatório e são verificadas em todo commit. `additional_assertions` só acrescenta verificações específicas; uma transição, regra ou modelo nunca pode desabilitar leis globais.

```text
RecoveryDefinition
├── id
├── triggered_by
├── candidate_actions
├── max_attempts_or_duration
├── degraded_terminal
└── degraded_terminal_scope
```

Não use um único “fallback” para tudo. Separe:

- política heurística quando o modelo de inferência estiver indisponível;
- ações normais de recuperação para estados válidos, sempre revalidadas;
- rejeição transacional de um comando, sem mutação parcial;
- estado degradado ou terminal seguro quando a recuperação limitada se esgotar.

Recuperações não são recursivas, possuem limite e geram diagnóstico. Em testes, uma ativação inesperada falha com contraexemplo; em produção, preserva o save e torna o modo degradado explícito.

Regras declarativas nunca mutam o estado diretamente. Elas referenciam comandos tipados. O formato físico pode ser JSON, TOML, YAML ou recurso da engine; essa escolha permanece aberta. Expressões precisam ser limitadas e tipadas, sem código arbitrário escondido nos dados.

Todo terminal pertence a uma máquina e possui escopo. Encerrar `business_day` não encerra `company_run`: a máquina diária precisa declarar a transição autorizada para preparação do próximo dia. Falência encerra a empresa do jogador, mas não o mercado persistente, o relógio do bairro nem os concorrentes; resolução encerra somente o incidente. O verificador não interrompe a exploração de escopos maiores ao encontrar um terminal local.

## Propriedades de progresso

Ter uma ação válida não prova progresso: `Wait` pode existir para sempre, ou uma ação de recuperação pode receber peso zero em todas as decisões. Cada propriedade declara o ator responsável, o tipo de garantia, o horizonte, as condições de execução justa e uma medida observável de avanço:

```text
LivenessProperty
├── starting_condition
├── responsible_actor
├── quantifier: exists_path | all_fair_executions | probability_at_least
├── eventually_condition
├── allowed_terminal_states_and_scopes
├── progress_measure
├── fairness_assumptions
├── minimum_probability
└── max_steps_or_duration
```

Para decisões totalmente do jogador, geralmente se prova que **existe um caminho** de recuperação. Para agentes automáticos, verifica-se que toda execução em que ações válidas não são adiadas para sempre progride, ou que existe uma probabilidade mínima de resolução dentro do horizonte. Os pesos já modificados entram na análise: uma ação estruturalmente alcançável, mas permanentemente com peso zero, não satisfaz a propriedade.

Além de provar o que nunca acontece, precisamos verificar o que **eventualmente** acontece sob essas condições:

- todo estado não terminal possui transição legal, agendamento futuro ou terminal explícito;
- de toda loja aberta existe um caminho válido para fechar;
- todo incidente termina em resolução, recuperação ou terminal explícito;
- toda sangria termina, é interrompida com segurança ou falha com motivo;
- toda diferença pode ser conciliada, investigada ou registrada como não explicada;
- toda dívida chega a pagamento, renegociação ou insolvência explícita;
- todo terminal é intencional e apresenta causa compreensível.

O verificador também inspeciona componentes fortemente conectados do grafo para encontrar ciclos sem saída ou sem redução da medida de progresso.

## Categorias de problema

1. **Terminal planejado e escopado:** resolução de tarefa/incidente, fim do dia ou falência da empresa.
2. **Deadlock:** nenhum comando válido em estado não terminal.
3. **Livelock:** ações se repetem sem produzir progresso.
4. **Soft lock econômico:** há ações, mas nenhum caminho realista de recuperação.
5. **Conteúdo órfão:** item, melhoria, estado ou evento nunca é alcançado.
6. **Dependência circular:** um desbloqueio exige outro que depende do primeiro.

Uma má gestão pode levar legitimamente à falência. Isso é diferente de um fluxo quebrado que deixa a empresa parada sem resolução.

## Validação em camadas

### Ao carregar conteúdo

Rejeite referências inexistentes, tipos incompatíveis, intervalos inválidos, condições impossíveis, efeitos contraditórios, tabelas de decisão com resultados exclusivos sobrepostos sem precedência, composição de peso fora da fórmula permitida, transições inexistentes, terminais sem escopo, ciclos de recuperação e conjuntos ponderados sem tratamento para peso total zero.

### Em testes

- verifique o estado proposto antes do commit e confirme as invariantes depois de cada comando aceito;
- explore grafos abstratos por horizonte limitado;
- avalie pesos finais e condições de execução justa nas propriedades de progresso;
- procure componentes fortemente conectados sem saída ou avanço;
- gere sequências aleatórias de comandos;
- execute milhares de seeds e políticas de jogador;
- procure ações nunca escolhidas, estratégias dominantes e falências causadas por uma rolagem;
- teste relações monotônicas, não somente resultados exatos.

Exemplos de relações monotônicas:

- mais treinamento não piora detecção mantendo o restante igual;
- mais fadiga não melhora atenção;
- melhor cobertura não aumenta o sucesso do roubo;
- equipamento quebrado não concede benefício integral;
- mudar idioma não altera a economia;
- contratar alguém, sozinho, não altera o caixa.

Monte Carlo encontra anomalias estatísticas, mas não prova ausência de contradição. Ele complementa tabelas, invariantes, alcançabilidade e testes baseados em propriedades.

## Diagnóstico reproduzível

Uma falha precisa produzir um contraexemplo:

```text
ruleset: 0.3.0
seed: 84192
dia 4 → contratar segurança
→ negar acesso ao cofre
→ ativar sangria automática pelo segurança
→ caixa atinge limite crítico
→ nenhuma pessoa autorizada está disponível
→ deadlock: nenhuma ação de recuperação
```

Registre versão do conteúdo, versão do modelo, seed, snapshot, ações candidatas, rejeições e motivos, pesos, sorteio, comando e primeira invariante violada.

Em produção pode existir recuperação limitada, como pausar a operação, fechar a loja ou solicitar intervenção. Ela não conta como prova de progresso por si só. Em testes e CI, uma ativação inesperada deve falhar com contraexemplo para não esconder uma regra incompleta.

## Primeiro experimento

Modele apenas:

- loja aberta/fechada;
- um segurança;
- posto, pausa, escolta de sangria, distração, alerta, protocolo e recuperação;
- caixa abaixo/adequado/acima do limite;
- fadiga baixa/alta;
- um incidente externo;
- sangria manual e automática.

Critérios:

- nenhum estado não terminal fica sem comando, evento futuro ou terminal explícito;
- segurança nunca ocupa tarefas exclusivas simultaneamente;
- toda sangria termina ou falha com motivo;
- todo incidente termina em resolução, recuperação ou perda explícita;
- nenhuma política automática mantém uma ação obrigatória com peso zero para sempre;
- locks e reservas são liberados em sucesso, rejeição, interrupção e timeout;
- mesma seed e comandos reproduzem decisões e resultados;
- simulações em lote não encontram ciclos infinitos, probabilidades inválidas ou falência catastrófica injusta.
