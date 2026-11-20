# BOXBOX — escala do mundo e pedestres

Data: 2026-11-20

## Diagnóstico medido

As malhas importadas não compartilham uma unidade visual consistente. Medições dos AABBs dos GLBs mostraram, antes de qualquer escala da Godot:

- protagonista: aproximadamente 0,66 unidade de altura;
- NPCs: 0,133–0,151 unidade de altura, medidos individualmente em lote no Blender;
- sedã: aproximadamente 2,55 unidades de comprimento;
- poste `light-square`: aproximadamente 0,60 unidade de altura;
- semáforo: aproximadamente 0,52 unidade de altura.

Por isso, um multiplicador fixo diferente em cada script não consegue produzir uma cidade coerente. O personagem não era isoladamente “gigante”; personagens, veículos e mobiliário estavam em escalas de origem diferentes.

## Regra implementada

Os visuais são normalizados pelo AABB completo da instância, preservando a proporção da malha:

- protagonista: 0,78 unidade visual;
- NPC adulto: 0,74–0,82 unidade visual, variação determinística;
- criança: 0,62 unidade visual;
- veículos: escala nativa do pacote, evitando que ocupem várias faixas;
- poste: aproximadamente 1,55 unidade visual;
- semáforo: aproximadamente 1,65 unidade visual.

Após o teste em câmera isométrica, a escala humana realista de 1,7 m foi rejeitada pelo usuário: ela tornava os Mini Characters gigantes diante dos lotes estilizados. A escala humana do diorama voltou ao valor visual próximo dos 0,66 originais, com pequeno aumento para legibilidade. Veículos e mobiliário permanecem maiores para preservar a leitura de maquete.

Exceção documentada: os NPCs animados contêm uma transformação de conversão `0.01` no armature. O AABB retornado pela Godot fica cem vezes menor e não pode dimensionar corretamente a raiz do esqueleto. Para eles, o catálogo usa a altura real medida de cada GLB no Blender e calcula `altura_alvo / altura_fonte`.

Colisores continuam sendo formas invisíveis configuradas por código, conforme permitido pelas decisões do projeto, e são dimensionados em metros do mundo, não pela unidade arbitrária de cada pacote.

## Navegação de pedestres

A implementação segue a arquitetura em duas camadas registrada no estudo de GTA 2 do projeto:

1. grafo global determinístico de calçadas e faixas, compatível com streaming;
2. direção local com separação suave e reação a perigo.

O grafo não depende de geometria improvisada nem de um `NavigationMesh` regenerado a cada célula. Cada travessia consulta o mesmo `IntersectionTrafficController` usado pelos carros. Ao começar a travessia, o pedestre registra ocupação; os carros mantêm o vermelho efetivo até a faixa ficar livre.

## Estados e limites

- `Walking`: percorre a margem interna da calçada;
- `WaitingCrosswalk`: aguarda vermelho com janela suficiente;
- `Crossing`: reserva a faixa e acelera se a fase mudar;
- `DoingActivity`: pausa determinística de 4–10 s em pontos de atividade;
- `Fleeing`: reage a arma, disparo ou veículo rápido próximo.

Há no máximo 14 pedestres materializados, e instâncias a mais de 32 m do foco são descartadas. O estado econômico persistente continua separado desta projeção visual local.
