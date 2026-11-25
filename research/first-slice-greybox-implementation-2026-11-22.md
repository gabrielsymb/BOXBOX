# Pesquisa técnica do greybox do primeiro slice

Data: 2026-11-22  
Unity: `6000.3.29f1`  
Renderizador: Built-in Render Pipeline (sem `m_CustomRenderPipeline` configurado)  
Plataforma em validação: Editor desktop Linux

## Hipótese causal

O fluxo atual precisa validar o ciclo de pedido, retirada, viagem, descarga e venda,
não a fidelidade de uma caminhonete ou a arte final da cidade. Uma rota reta de
greybox, uma projeção de veículo controlada por transform e interações de parada
confirmadas pelo domínio devem permitir avaliar a compreensão do abastecimento sem
introduzir tráfego, colisões, suspensão ou física de veículo.

O movimento da caminhonete é somente visual: não usa `Rigidbody`, `Collider` ativo
ou forças. `Time.deltaTime` escala apenas esse movimento visual. Economia e clientes
avançam exclusivamente por comandos aceitos no núcleo e por uma política semeada.
Sem personagem ou pedestres projetados neste corte, não há spawn dentro da câmera.

## Documentação oficial consultada

1. Unity 6.3, `GameObject.CreatePrimitive` (avaliado e não adotado):
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/gameobject/createprimitive
   Confirma criação de primitivo com renderer e collider apropriado. Como o projeto
   não precisa do módulo de física nesta hipótese, o greybox usa uma mesh de cubo
   própria e compartilhada, sem criar nem desativar colliders.
2. Unity 6.3, `Transform.Translate`:
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/transform/translate
   Movimento por eixos do transform/local ou mundo; usado só no adaptador visual.
3. Unity 6.3, `Input.GetAxisRaw`:
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/input/getaxisraw
   API do Input Manager legado, sem suavização e com aviso de que novos projetos
   devem preferir o Input System. Mantida temporariamente para evitar adicionar pacote
   de input ao greybox; deverá ser substituída/avaliada quando a interação do slice
   for prototipada além desta etapa.
4. Unity 6.3, `Camera.orthographic`:
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/camera/orthographic
   Confirma a projeção ortográfica usada pela câmera fixa do protótipo.
5. Unity 6.3, `GUI.Button`:
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/gui/button
   Usado apenas para UI de protótipo separada do domínio; não é proposta de UI final.
6. Unity 6.3, `Time.deltaTime`:
   https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/time/deltatime
   Usado somente para tornar o deslocamento visual independente da taxa de quadros,
   nunca para gerar clientes ou movimentações econômicas.

## Referência de comportamento consultada

O estudo local `research/gta2-mechanics-study.md` foi lido antes da implementação.
Foram consultados `CarAI.cpp` nas seções de planejamento de curva/rota e
`handling.c`/`wheelforces.c` no checkout de pesquisa REDRIVER2. Eles evidenciam que
veículo e navegação real dependem de estados e subsistemas coordenados; isso reforça
o limite deste protótipo, não fornece código ou parâmetros para ele.

Também foi consultado `research/third-party/gta2_re/Source/CarAI.cpp` nas rotinas de
planejamento de curva e deslocamento por junções. Nenhum código, nome, dado, asset ou
valor foi copiado. A implementação independente usa somente uma rota de ida/volta
controlada e não tenta reproduzir navegação de trânsito.

## Critérios de aceitação visual/física

- A caminhonete placeholder tem proporção legível no cenário e cabe na via marcada;
  as medidas são temporárias, não são escala aprovada de asset final.
- WASD permite acelerar, frear/recuar e corrigir direção; o jogador pode parar e
  alcançar cada destino sem teleporte.
- A interação de chegada só é habilitada dentro do marcador de destino. O comando de
  carga/descarga é aceito uma única vez por etapa.
- Caixas-placeholder aparecem apenas depois que o domínio confirma a retirada; somem
  apenas depois que a descarga é confirmada.
- A câmera mostra a rota completa, atacado, loja e veículo sem NPC/objeto dinâmico
  nascer dentro do enquadramento.
- A UI mostra apenas estado confirmado e oferece o fluxo completo de ponta a ponta.

## Limitações e decisão

- A primeira via é reta e sem obstáculos; direção, freio, colisão, suspensão,
  acidentes, trânsito e pedestres não estão representados.
- Os primitivos e cores são greybox descartável. Sem sombras ou iluminação realista;
  o projeto está no renderizador Built-in e o cenário usa materiais unlit.
- A mesh unitária compartilhada é só geometria de placeholder; não representa asset
  aprovado nem medida final de objetos do jogo.
- O critério final requer executar a cena e inspecionar captura real. Compilação ou
  leitura de código, sozinhas, não validam a cena.
- Escolhas provisórias para testar o fluxo: intervenção do expediente é preço;
  o catálogo e valores são fictícios e não afirmam conformidade tributária ou preço
  de mercado brasileiro.
