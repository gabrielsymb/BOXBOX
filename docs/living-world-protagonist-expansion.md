# BOXBOX — expansão sistêmica do mundo vivo e do protagonista

## Status e objetivo

Especificação técnica registrada em 19 de novembro de 2026 a partir da decisão do
usuário de tornar o protagonista um agente do mesmo mundo econômico e social dos
NPCs. Este documento é o portão anterior à implementação. Ele distingue o que já
existe, o próximo corte executável e sistemas planejados que ainda não possuem
contrato suficiente.

O objetivo não é criar uma cidade genérica completa. É permitir que trajetórias
diferentes surjam de regras compartilhadas:

```text
condição do ator + estado do mundo + conhecimento disponível
→ ações admissíveis
→ decisão humana ou política autônoma
→ comando autoritativo
→ transação e eventos confirmados
→ novas condições, oportunidades e consequências
```

O alvo é um mundo de bairro quase aberto, não uma cena limitada a dois NPCs. Lia
e Rui permanecem fixtures nomeadas dos testes antigos. O primeiro benchmark de
escala usa 128 atores persistentes individuais e serve somente como degrau de
engenharia; não define o teto de população do produto.

## Decisões confirmadas

- O mundo continua independentemente da presença e da empresa do protagonista.
- O protagonista é um ator persistente, não uma exceção externa à simulação.
- Jogador e NPCs usam as mesmas leis de dinheiro, propriedade, estoque, trabalho,
  compra, venda, dívida, conhecimento e consequência.
- A diferença principal é a decisão: comando humano para o protagonista e
  política local para NPCs.
- Trabalho, empreendedorismo, abandono de atividade, dívida, falência e
  reconstrução surgem de condições; não são classes fixas ou menus irreversíveis.
- Criminalidade é parte futura da economia e da sociedade, não uma coleção de
  missões roteirizadas.
- Crime não causa punição automática. Ocorrência, percepção, identificação,
  comunicação, evidência, investigação e consequência são processos separados.
- NPCs não conhecem estado privado ou futuro e não usam aparência ou atributo
  demográfico para inferir culpa, honestidade, competência ou risco.
- Nenhuma mecânica central depende de modelo generativo, API paga ou nuvem.
- O núcleo permanece headless, determinístico por seed e independente do Godot.

## Diagnóstico do repositório

### Implementado e verificado

O repositório já possui um corte funcional em C#/.NET 8:

- `SimulatedActorState` com identidade, caixa, necessidade alimentar, emprego,
  salário, dívida, limite e fatos conhecidos;
- `LivingWorldState` reunindo atores, BOXBOX, rival resumido, empregador, banco e
  comandos concluídos;
- `LivingWorldTransactionService` com commits imutáveis e conservação monetária;
- `WeightedNpcDecisionPolicy`, que escolhe somente entre ações válidas;
- necessidade, salário, perda de emprego, refeição, crédito essencial, busca de
  emprego e espera;
- mercado compartilhado entre BOXBOX, concorrente e não comprar;
- agenda ordenada, ids estáveis, seed derivada por evento e relógio monotônico;
- snapshot versionado e adaptador JSON separado do domínio;
- equivalência entre avanço incremental, catch-up e save/reload;
- intervenção do jogador ao abrir ou fechar a loja;
- 43 verificações headless aprovadas em 19 de novembro de 2026, incluindo replay
  determinístico com 128 atores persistentes.

O registro detalhado do corte atual permanece em
`docs/living-world-vertical-slice.md`.

### Ausente ou ainda simplificado

- o jogador não está representado em `Actors`; `SetStoreOpenCommand` usa o id
  especial `player` sem necessidades, caixa pessoal, emprego ou conhecimento;
- propriedade pessoal e propriedade empresarial ainda não estão separadas;
- não existem moradia, despesas pessoais recorrentes, bens ou patrimônio;
- emprego é um enum e salário direto, sem vaga, empregador persistente, turno,
  contrato, duração, capacidade ou concorrência;
- banco e empregador são reservatórios agregados sem livro próprio;
- crédito não possui contrato, parcelas, juros, vencimento ou inadimplência;
- o SKU `meal` e a agenda estão fixos em fixtures C#;
- conhecimento existe apenas como ids conhecidos pelo próprio ator afetado;
- relações, percepção, testemunho, evidência, comunicação e investigação não
  existem;
- não há crime praticado por jogador ou NPC, polícia, prisão ou gangues;
- NPC distante ainda usa a mesma resolução individual do NPC próximo;
- não há apresentação Godot nem input de personagem para este corte.

Os dois atores atuais demonstram causalidade, não capacidade ou escopo final. A
arquitetura não deve codificar quantidade fixa, ids especiais de Lia/Rui nem uma
lista estática como modelo de população.

## Correção arquitetural principal

O próximo corte não deve acrescentar campos de jogador ao `StoreState`. Ele deve
introduzir um ator controlado pelo jogador e manter pessoa, empresa e instituição
como entidades distintas:

```text
PersonActor
├── ActorId
├── Controller: Human | LocalPolicy
├── PersonalCashAccountId
├── NeedState
├── OccupationAndAuthority
├── PropertyIds
├── DebtContractIds
├── RelationshipIds
└── KnowledgeIds

BusinessEntity
├── BusinessId
├── OwnerIds
├── CashAndInventory
├── EmploymentContracts
├── Permissions
└── OperatingState

Institution
├── InstitutionId
├── capabilities
├── offers/contracts
└── knowledge and authority
```

A pessoa pode possuir ou trabalhar numa empresa sem que seus caixas sejam o mesmo
saldo. Uma transferência entre pessoa e empresa sempre possui comando e evento.
Falência da empresa não apaga automaticamente dinheiro, dívida, relações ou
trajetória pessoal; terminais de `company_run` e `actor_life` têm escopos distintos.

Não é necessário criar uma classe base universal para todas as entidades. O
primeiro corte usa contratos pequenos orientados pelos casos de uso reais.

## Responsabilidades e dependências

### Domínio

- identidade e estado econômico pessoal;
- conta pessoal, propriedade e transferências;
- contrato de trabalho, turno, salário e encerramento;
- negócio, propriedade empresarial e autoridade operacional;
- necessidade e despesa confirmada;
- contratos de dívida e estados determinísticos;
- mais tarde: ocorrência, testemunho, evidência e restrições decorrentes.

### Aplicação/simulação

- formar ações candidatas conforme capacidade, conhecimento e autoridade;
- coordenar trabalho, compra, transferência, abertura e operação do negócio;
- agendar duração, conclusão, timeout e despesas recorrentes;
- receber comando humano ou proposta de política local;
- confirmar transações, publicar eventos e atualizar agenda atomicamente;
- produzir projeções detalhadas ou compactas do mesmo resultado canônico.

### IA local

- escolher entre ações válidas de NPCs;
- nunca criar ação, ignorar guarda, definir culpa ou mutar estado;
- usar política heurística como implementação inicial e fallback.

### Persistência

- versionar atores, entidades, contratos, agenda, ids processados, conhecimento e
  log causal;
- migrar snapshots de forma explícita;
- preservar frases e aparência fora do save econômico.

### Apresentação

- transformar input do jogador em comandos;
- projetar eventos confirmados em movimento, animação e UI;
- nunca usar animação, colisão visual ou proximidade renderizada como autoridade
  econômica ou criminal.

## Escala populacional e mundo quase aberto

A população é dividida por relevância de simulação, sem criar economias diferentes:

```text
FocusedDetailed
  protagonista e atores próximos/relevantes com projeção 3D e tarefas visíveis

PersistentIndividual
  atores nomeados ou com emprego, propriedade, dívida, relação, conhecimento,
  ocorrência ou agenda própria; continuam individualmente fora da tela

BackgroundCohort
  fluxo anônimo equivalente usado para demanda e circulação agregada, somente
  quando não existe identidade ou consequência individual a preservar
```

Trocar o nível de detalhe não altera o resultado econômico canônico. Protagonista,
proprietários, funcionários, devedores, credores, testemunhas, suspeitos, vítimas,
investigadores e atores com relações ou contratos ativos permanecem individuais.

Metas recomendadas para protótipo, ainda sujeitas a benchmark visual:

- 16–32 personagens projetados simultaneamente no entorno detalhado;
- 128 atores persistentes individuais no primeiro teste headless de escala;
- crescimento posterior para centenas de indivíduos agendados por eventos;
- coortes apenas para população anônima sem estado exclusivo.

Esses números não são limites de design. Hardware modesto, densidade visual,
pathfinding e custo de persistência definirão faixas finais por benchmark, nunca
uma constante arbitrária de dois NPCs.

## Primeiro corte implementável — trajetória econômica do protagonista

O menor corte funcional contém um protagonista, atores persistentes configuráveis,
um empregador, a BOXBOX, um concorrente e o banco agregado já existentes. Lia e
Rui continuam como casos dourados; uma fixture separada com 128 atores prova que
o motor não depende deles nem de uma população fixa.

### Estados mínimos novos

- protagonista em `LivingWorldState.Actors` com `ControllerKind.Human`;
- conta pessoal distinta do caixa da BOXBOX;
- vínculo `Unemployed | Applied | Employed | Ended`;
- autoridade `None | Worker | OwnerOperator` sobre uma entidade;
- propriedade da BOXBOX separada de presença e operação;
- despesa pessoal recorrente abstrata e conhecida;
- capital mínimo configurado para iniciar o negócio.

### Ações do protagonista

| Ação | Pré-condições | Duração/conclusão | Efeito confirmado | Falha/timeout |
| --- | --- | --- | --- | --- |
| candidatar-se a trabalho | desempregado; vaga conhecida e disponível | processo agendado | candidatura registrada | vaga encerrada ou timeout |
| cumprir turno | vínculo ativo; presente/capaz; horário válido | intervalo de turno | trabalho concluído e salário devido | ausência, interrupção ou empregador incapaz |
| receber salário | trabalho confirmado; empregador possui fundos/obrigação | evento de pagamento | transferência empregador → pessoa | dívida salarial explícita, nunca dinheiro criado |
| comprar necessidade | necessidade; oferta admissível; recursos | transação instantânea | pessoa debita, vendedor credita, necessidade reduz | rejeição sem mutação |
| transferir capital ao negócio | proprietário; saldo pessoal; destino válido | instantânea | pessoa debita, empresa credita | fundos ou autoridade insuficientes |
| iniciar BOXBOX | capital, local/kit e condições mínimas configuradas | ação temporal | entidade passa a `Preparing` | cancelamento devolve reservas segundo contrato |
| operar BOXBOX | autoridade, capacidade e janela operacional | turno/tarefa | tarefas comerciais usam casos de uso existentes | tarefa expira ou falha com motivo |
| procurar recuperação | sem renda ou empresa encerrada | decisão normal | vaga, venda válida, crédito elegível ou redução | insolvência pessoal explícita se não houver rota |

O corte não inclui crime. Primeiro ele precisa provar que trabalhar, economizar,
abrir a loja, perder a empresa e voltar ao mercado de trabalho são transições do
mesmo ator, sem branch roteirizado nem caixa compartilhado indevidamente.

## Pipeline de decisão compartilhado

```text
observação permitida do ator
→ gerador de ações candidatas
→ passabilidade por estado, recursos, autoridade e conhecimento
→ comando humano ou seleção da política entre as candidatas válidas
→ revalidação autoritativa
→ próximo estado, eventos, agenda e locks propostos
→ invariantes globais e pós-condições
→ commit atômico ou rejeição integral
```

O input humano não contorna regras. A interface recebe o mesmo conjunto válido e
expõe ações bloqueadas apenas quando houver uma razão localizada útil.

## Criminalidade incremental futura

A criminalidade começa somente após o corte econômico e a separação entre fato,
registro e conhecimento.

### Sequência segura de implementação

1. perda física abstrata de um ativo por ação tipada e auditável;
2. percepção por ator presente com capacidade sensorial simplificada;
3. testemunho como conhecimento, não como verdade universal;
4. comunicação da ocorrência como ação voluntária ou política;
5. evidência com origem, integridade, relevância e acesso;
6. investigação temporal com hipóteses e resultados limitados;
7. consequência autorizada somente após identificação/evidência suficiente;
8. polícia e prisão com capacidade, agenda, jurisdição e terminais próprios;
9. grupos e território apenas quando relações e instituições estiverem maduros.

Nenhuma aparência visual marca culpado. Nenhuma pessoa é punida porque um evento
global “soube” do crime. Furto, roubo e atividades clandestinas permanecem
abstratos e não descrevem técnicas reais.

## Simulação fora da área visível

O resultado econômico canônico continua baseado em eventos discretos:

- `LiveDetailed`: indivíduo é simulado e projetado visualmente;
- `LiveCompact`: indivíduo usa os mesmos comandos sem projeção visual;
- `CatchingUp`: agenda é processada até o alvo sem depender de frames;
- coortes: somente após benchmark e contrato de equivalência.

Uma coorte não pode conter protagonista, ator com relação importante, contrato
individual, fato exclusivo, evidência, propriedade relevante ou decisão pendente.
Promover ou despromover detalhe não altera dinheiro, dívida, emprego, propriedade,
conhecimento, agenda ou ids já processados.

O carregamento espacial do HUB não pausa bairros fora da câmera. Cada distrito
mantém agenda e estado compacto; a apresentação instancia somente o recorte
visível. Viajar entre áreas muda projeção e custo de detalhe, não avança uma linha
do tempo paralela nem concede turnos extras.

## Invariantes novas

- conta pessoal e caixa empresarial não são o mesmo saldo;
- toda transferência identifica origem, destino, valor, instante e causa;
- ator humano passa pelas mesmas guardas e invariantes que um NPC;
- salário depende de trabalho/obrigação confirmada e não cria dinheiro;
- possuir negócio não concede presença, capacidade ou trabalho automático;
- encerrar empresa não apaga trajetória, patrimônio ou dívida pessoal sem regra;
- mudar ocupação não duplica salário, autoridade ou propriedade;
- uma ação criminal remove ou altera ativo por evento causal mesmo se oculta;
- conhecimento de testemunha não altera o fato e não se propaga sem ação/canal;
- suspeita não equivale a identificação; identificação não equivale a condenação;
- prisão futura restringe ações por estado temporal, não remove o ator do mundo;
- detalhe visual, FPS, idioma e distância não mudam o resultado canônico;
- mesma versão, seed, estado, comandos e instantes reproduzem a trajetória.

## Propriedades de progresso

- um ator desempregado com vaga admissível consegue concluir candidatura ou obter
  falha/timeout explícito dentro do horizonte;
- todo turno iniciado conclui, é interrompido ou falha liberando seus recursos;
- toda despesa chega a pagamento, atraso, renegociação ou insolvência explícita;
- toda empresa em `Preparing` abre, cancela ou falha com causa;
- perder a BOXBOX não encerra o mercado nem remove caminhos pessoais válidos;
- toda ocorrência criminal futura termina em não percebida, registrada,
  comunicada, investigada, arquivada ou consequência explícita; nunca fica presa
  aguardando uma autoridade inexistente.

## Persistência e migração

O próximo snapshot adicionará versão de schema separada da versão do ruleset e
persistirá contas, vínculos, propriedades, autoridades e eventos temporais. A
migração do protótipo atual deve:

1. criar o protagonista com id estável;
2. preservar NPCs, loja, rival, agenda, log e ids processados;
3. criar conta pessoal sem mover silenciosamente o caixa empresarial;
4. declarar a propriedade inicial da BOXBOX apenas na fixture que realmente a
   pressupõe;
5. rejeitar versão desconhecida sem sobrescrever o save original.

## Cenários de teste e aceitação

### Paridade humano/NPC

O mesmo comando de compra, com o mesmo estado, produz o mesmo efeito econômico
para protagonista ou NPC. Somente a origem da intenção difere.

### Trajetória sem classe fixa

O protagonista trabalha, recebe, compra, transfere capital, inicia a BOXBOX,
opera e depois pode encerrar ou perder a empresa e voltar a buscar trabalho.
Nenhuma transição troca para uma implementação econômica paralela.

### Separação patrimonial

Receita da BOXBOX não aparece automaticamente na conta pessoal. Retirada ou
aporte usa transferência explícita e conserva o total.

### Tempo e save

Execução contínua, catch-up e save/reload produzem o mesmo fingerprint de contas,
emprego, propriedade, negócio, agenda, conhecimento e log.

### Falha explicável

Empregador sem caixa gera obrigação salarial ou falha definida; negócio sem
capital não abre; ator sem rota chega a insolvência pessoal explícita, nunca a um
deadlock silencioso.

### Criminalidade futura

A mesma ocorrência pode permanecer oculta, ser percebida e não comunicada, ou
ser comunicada e investigada conforme atores, conhecimento e seed. Nenhum caminho
aplica punição automática no momento da ação.

## Estratégia incremental

1. **Paridade do protagonista:** ator humano, conta pessoal e compra compartilhada.
2. **Escala individual:** população configurável, benchmark de 128 atores e
   partição de agenda sem mudar resultados.
3. **Trabalho temporal:** vaga, candidatura, turno, salário e encerramento.
4. **Propriedade e empreendimento:** aporte, preparação, operação e encerramento.
5. **Despesas e dívida pessoal:** contratos, parcelas, atraso e insolvência.
6. **Conhecimento e relações:** observação, conversa, confiança e memória.
7. **Perda e testemunho:** fato oculto, percepção, comunicação e evidência.
8. **Investigação e autoridade:** processo temporal, polícia e restrições.
9. **Grupos e território:** somente após as relações e instituições anteriores.
10. **Coortes:** otimização apenas após equivalência mensurada.

Cada etapa precisa manter todos os checks anteriores e adicionar replay, rejeição
atômica, save/reload, progresso e contraexemplo reproduzível.

## Requisitos derivados para o protagonista visual

O primeiro corte requer `idle`, caminhada, corrida, pegar, segurar, interagir com
as duas mãos, sentar e confirmação/negação. Trabalho, caixa, reposição e transporte
usam acessórios separados e pontos de ancoragem; a animação não confirma a
transação.

A auditoria preliminar do Kenney Mini Characters 2.0 encontrou 32 ações e um rig
comum de 7 ossos nos modelos civis inspecionados. A família é tecnicamente
promissora para o protótipo e visualmente aprovada, sujeita ao teste de importação,
escala, pivô, root motion, materiais e sockets no Godot. Animações de combate do
pacote não são requisito do primeiro corte.

## Riscos e limites

- a proposta ampla pode diluir a fantasia de microcomércio; os sistemas pessoais
  devem alimentar a progressão para operar e gerir, não virar um sandbox sem foco;
- simular todo NPC com contabilidade completa é inviável; individualização exige
  relevância persistente e coortes exigem equivalência comprovada;
- crime, polícia e gangues multiplicam estados de conhecimento, autoridade e
  progresso; antecipá-los produziria punição mágica ou sistemas opacos;
- um agregado mundial único crescerá demais; novos subdomínios devem coordenar por
  comandos e transações, não por acesso irrestrito a um objeto monolítico;
- conteúdo fixo em C# precisa migrar para dados versionados sem permitir scripts
  arbitrários ou regras que mutem estado diretamente;
- o asset Kenney cobre ações gerais, mas sua expressividade facial e interação
  fina são limitadas; isso deve virar linguagem toon deliberada, não promessa de
  atuação cinematográfica.

## Próximo portão

O benchmark inicial de população deve provar 128 atores persistentes, conservação
de dinheiro e igualdade entre avanço incremental e catch-up. Depois, implementar
a paridade do protagonista: inseri-lo como ator humano persistente, separar conta
pessoal do caixa da BOXBOX e reutilizar a compra autoritativa já existente. O
corte passa quando protagonista e NPC obedecem às mesmas regras, save/reload e
catch-up continuam equivalentes e os checks anteriores permanecem verdes.
