# Pesquisa de iluminação — Godot 4.7 / BOXBOX

Data: 2026-11-20  
Renderizador do projeto: `gl_compatibility`

## Fontes oficiais consultadas

- Godot, `DirectionalLight3D`: https://docs.godotengine.org/en/stable/classes/class_directionallight3d.html
- Godot, luzes e sombras 3D: https://docs.godotengine.org/en/latest/tutorials/3d/lights_and_shadows.html
- Godot, visão geral dos renderizadores: https://docs.godotengine.org/en/latest/tutorials/rendering/renderers.html
- Godot, Environment e pós-processamento: https://docs.godotengine.org/en/latest/tutorials/3d/environment_and_post_processing.html
- Godot issue #58332, câmera ortográfica usa `Camera3D.Far` no shadow map: https://github.com/godotengine/godot/issues/58332
- Godot issue #121644, regressão ortográfica confirmada em 4.7.x: https://github.com/godotengine/godot/issues/121644
- Godot, precisão do depth buffer: https://docs.godotengine.org/en/latest/tutorials/3d/3d_rendering_limitations.html
- Godot, antialiasing 3D: https://docs.godotengine.org/en/latest/tutorials/3d/3d_antialiasing.html

## Evidência do defeito observado

Captura analisada: `/tmp/codex-clipboard-a9b5fc3e-4505-40a3-a661-6b2143acbd22.png`.

- exposição configurada em `0.48`, ambiente noturno em `0.16` e fog ativo produziram blackout azul;
- a rotação X percorria 360 graus, deixando a luz direcional abaixo do horizonte em parte do ciclo;
- na câmera ortográfica, a Godot usa o `Far` da câmera para o alcance do shadow map; o valor padrão enorme reduz a precisão e pode produzir faixas móveis;
- a série 4.7.x possui regressão confirmada de culling de sombras ortográficas, então `Near/Far` justos e direção de luz estável são obrigatórios;
- o padrão diagonal fino restante foi identificado como dithering do filtro de sombra suave. A documentação alerta que ele fica visível em cenas com materiais pouco detalhados; Compatibility não oferece TAA/FXAA para mascará-lo;
- o Compatibility compõe luzes com sombra em sRGB por multipass, podendo exigir energia diferente de Forward+;
- contact shadows nativas não existem no Compatibility; o decal do projeto é apenas apoio visual, não substitui shadow map;
- postes e semáforos precisam combinar material emissivo real com poucas luzes posicionais sem sombra. O Compatibility limita Omni/SpotLights por mesh.

## Inspeção das malhas de rua

Blender 4.x, importação dos GLBs originais:

- `road-crossroad.glb`: 116 polígonos, 9 faces degeneradas com normal zero;
- `road-crossing.glb`: 104 polígonos, 9 faces degeneradas com normal zero;
- `road-straight.glb` e `tile-low.glb`: normais cardinais coerentes.

As faces degeneradas são candidatas aos triângulos visuais, mas não serão alteradas antes de comparar uma captura sem fog e com exposição/luz corretas.

## Configuração adotada para o próximo teste

- manter Compatibility por compatibilidade com o Intel HD 4000 do computador de teste;
- ACES sem fog e exposição calibrada separadamente para dia/noite por captura real;
- uma DirectionalLight3D sempre acima do horizonte, yaw móvel, sombras sempre habilitadas;
- câmera isométrica com `Near = 0.25` e `Far = 70`, suficiente para o raio carregado;
- modo Orthogonal, distância 45 m, bias 0.03, normal bias 0.5;
- filtro direcional `Hard`, `ShadowBlur = 0` e `LightAngularDistance = 0`, eliminando dithering temporal; MSAA 4x suaviza bordas geométricas sem borrar o diorama;
- noite legível: ambiente 0.36, luar direcional 0.25 e exposição 0.9;
- postes: emissão quente + OmniLight curta, sem sombras;
- semáforos: emissão da lente real + OmniLight curta da cor ativa, sem geometria falsa.

## Passe visual v1 — ambiente guiado pelo céu

Na branch `visual/lighting-pass-v1`, o primeiro ajuste aplicado foi trocar a fonte
de luz ambiente da cena principal de uma cor azul fixa para `AmbientSource.Sky`.
Isso preserva a leitura de Compatibility e faz o preenchimento acompanhar o céu
procedural durante o ciclo, sem introduzir SDFGI, bloom ou luzes falsas. A energia
e a contribuição do céu ficam mais altas durante o dia e reduzidas à noite, mas a
noite permanece legível sem depender de faróis.

Hipótese: o preenchimento do céu deve recuperar as faces voltadas para longe do
sol sem apagar o contraste das sombras direcionais. Critério: comparar capturas
de dia e noite na mesma câmera; pista, lotes, personagem e veículos precisam
continuar legíveis, com separação clara entre luz direta, ambiente e sombra.

## Critérios de aceitação

1. pista, calçada, gramado, personagem e veículos legíveis à noite sem faróis;
2. personagem, carros, casas, árvores e postes projetam sombras direcionais de dia e à noite;
3. nenhum triângulo/degradê de fog ou face ocupa grandes áreas da pista;
4. postes criam pools de luz quentes e semáforos mostram uma lente ativa com brilho local;
5. resultado validado por captura real, não apenas por build.
