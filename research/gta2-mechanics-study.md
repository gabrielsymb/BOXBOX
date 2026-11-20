# Estudo de mecânicas — GTA2 para o contexto do BOXBOX

Data da pesquisa: 2026-11-20

## Fontes

- `https://gtamp.com/`: índice comunitário com páginas de mapas, scripting, ferramentas e versões executáveis no navegador.
- `https://github.com/CriminalRETeam/gta2_re`: reimplementação/decompilação em C++ do GTA2 10.5.
- `https://github.com/Hezkore/hez-gta-re3`: fork educacional da engenharia reversa de GTA III/Vice City, consultado apenas para decomposição conceitual de mundo, tarefas, streaming e tráfego.
- `https://gtamods.com/wiki/Main_Page`: índice comunitário adicional indicado pelo usuário; permanece somente como referência externa e não como fonte autorizada de código ou assets.
- checkout local de pesquisa: `research/third-party/gta2_re`, commit `d43309d967e0c03d82887bf5a64c778e3e7e0223`.

## Restrição de licença

O repositório `gta2_re` não apresenta arquivo `LICENSE` ou `COPYING` no checkout pesquisado. Portanto, seu código não será copiado, traduzido, vinculado nem incorporado ao BOXBOX. O checkout está ignorado pelo Git e serve somente para observar conceitos e decomposição de sistemas. O BOXBOX terá implementação original em C#/Godot e assets próprios/curados.

O `hez-gta-re3` declara explicitamente que não está em posição de oferecer uma licença e limita o uso indicado a fins educacionais, documentais e de modding. Ele não será clonado nem incorporado ao produto. A consulta serve somente para formar perguntas arquiteturais e será convertida em implementação original.

## Padrões úteis observados

### Crime como fato e relatório separado

O projeto separa a ocorrência de um crime de uma fila de relatos com tipo, identidade e posição. Para o BOXBOX, isso reforça a arquitetura já adotada:

```text
fato físico → percepção → relato → conhecimento policial → resposta
```

Um disparo não deve criar polícia onisciente. Testemunha, canal, atraso e última posição conhecida precisam existir como estados separados.

### Perseguição com alvo e última posição conhecida

A polícia mantém alvos de perseguição, verifica se alguém já está sendo perseguido e atualiza coordenadas vistas recentemente. A adaptação para BOXBOX deve usar `PursuitCase` persistente, evidência/identificação permitida e posição conhecida com timestamp, nunca acesso direto ao estado privado ou futuro do protagonista.

### Coordenação entre múltiplos perseguidores

A IA de carros usa tarefas e estados distintos para seguir, escolher lado, aproximar, ficar ao lado, ultrapassar, parar à frente e lidar com alvo parado. O aprendizado importante não é copiar esses estados literalmente, mas impedir que todas as viaturas escolham a mesma intenção. Uma futura política do BOXBOX receberá papéis admissíveis como `Follow`, `Intercept`, `Contain` e `Search`, com locks de papel e limites de participantes.

### Navegação em duas camadas

O roteamento de veículos trabalha sobre junções e segmentos de rua, enquanto o movimento local cuida de direção, colisão e velocidade. Para o mapa aberto do BOXBOX:

- rota global: grafo de ruas e distritos carregável sem meshes;
- condução local: faixa, obstáculo, distância de frenagem e colisão;
- streaming visual: instancia somente células próximas, sem apagar a rota ou a tarefa.

### Agentes físicos e virtuais

Tráfego e pedestres não precisam manter malha, animação, colisão e sensores completos fora do raio do jogador. O BOXBOX separará o agente persistente da sua projeção física:

```text
agente virtual distante → agente simplificado no anel de LOD → corpo físico próximo
```

O mesmo identificador, rota e tarefa atravessam as três representações. Entrar ou sair do raio visual não cria, mata, teleporta economicamente nem esquece o ator.

### Tarefas interrompíveis e estímulos

Estados locomotores simples permanecem separados da intenção. Uma tarefa como `DriveTo`, `Wander`, `Flee` ou `SearchLastKnownPosition` pode ser interrompida por estímulo admissível e depois retomada, cancelada ou substituída com causa registrada. Visão, audição, relato e conhecimento continuam fatos distintos.

### Serviços de emergência como tarefas limitadas

O serviço de ambulância mantém fila de pacientes, slots de tarefa, equipe, veículo, objetivo, falha e liberação. A adaptação adequada é um despachante com capacidade real: ocorrência pode aguardar, equipe pode estar ocupada, veículo pode falhar e a tarefa sempre conclui, falha ou expira liberando recursos.

### Vida, dano e morte como estados explícitos

O material diferencia estados do pedestre, causa de morte e respostas de serviços. Para o BOXBOX, a proposta inicial é:

```text
Healthy → Injured → Critical → Dead
              ↘ Recovered
```

Dano carrega fonte, tipo, instante e autoria conhecida/real separadas. Morte é persistente e produz agenda, testemunhos, investigação e consequências; animação nunca é a autoridade do estado.

## Ordem recomendada para o BOXBOX

1. vida/dano em alvos abstratos e eventos auditáveis;
2. NPC com percepção e reação a arma/disparo;
3. fato criminal separado de relato;
4. nível de procura derivado de casos conhecidos, não de um contador mágico;
5. policial a pé com busca por última posição conhecida;
6. grafo de ruas e veículo policial;
7. coordenação limitada de perseguição;
8. ambulância e recuperação/morte persistente;
9. somente depois, balanceamento e variedade de crimes.

## O que não importar

- código, símbolos, dados, mapas, sons, sprites ou nomes do GTA2;
- números de balanceamento sem validação no BOXBOX;
- polícia onisciente ou spawn instantâneo sem capacidade e deslocamento;
- morte descartável que remove um NPC persistente sem consequências;
- perseguição ligada ao frame ou à área renderizada.
