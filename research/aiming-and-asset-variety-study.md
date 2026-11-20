# BOXBOX — mira isométrica e variedade de assets

Data: 2026-11-20  
Engine: Godot 4.7.2 .NET, renderizador Compatibility

## Mira

Fonte oficial: [Ray-casting — Godot Engine](https://docs.godotengine.org/en/latest/tutorials/physics/ray-casting.html) e [Camera3D 4.7](https://docs.godotengine.org/en/4.7/classes/class_camera3d.html).

A documentação confirma que câmeras ortogonais exigem tanto `ProjectRayOrigin()` quanto `ProjectRayNormal()`: no modo ortogonal a origem varia conforme a posição do mouse. A consulta física deve ocorrer no `_PhysicsProcess`, quando o espaço não está bloqueado.

Decisão implementada:

- projetar o mouse pela câmera ativa;
- raycast da tela para o mundo durante `_PhysicsProcess`;
- orientar o personagem para o ponto atingido;
- usar o mesmo ponto para marcador e direção do disparo;
- reutilizar `Assets/World/Props/target.glb` como marcador visual aprovado;
- impedir disparo sem ponto de mira válido.

Isso elimina o tiro na direção antiga do corpo e mantém mira, animação e raycast coerentes.

## Resposta de pedestres

O evento de disparo agora produz reação determinística por seed:

- 45% fogem imediatamente, com duração variável;
- 31% se abaixam e podem fugir depois;
- 24% se assustam, encaram a origem por curto período e retomam a rotina.

Veículo rápido próximo continua sendo ameaça física e usa fuga imediata. Apenas empunhar a arma não força todos os NPCs ao mesmo estado em todos os frames.

## Auditoria inicial do catálogo

Assets já importados antes desta alteração:

- 38 construções;
- 15 veículos;
- 13 módulos/objetos rodoviários;
- 8 NPCs;
- 7 props;
- 3 assets de natureza.

O acervo curado contém, entre outros, 95 itens do City Kit Roads, 50 do Car Kit, 43 Mini Characters, 41 construções comerciais, 40 suburbanas, 22 itens Mini Forest e 140 itens Furniture Kit. Quantidade bruta não autoriza mistura semântica: reta, curva e cruzamento permanecem categorias distintas; detritos e karts não entram automaticamente no tráfego civil.

Foram adicionadas ao catálogo jogável nove variantes curadas: árvores pequena, grande e alta; pedras; dois conjuntos de rochas; vaso; e dois bancos estofados. O Blender MCP desconectou nesta sessão; o fallback foi o mesmo Blender 3.6 local em modo headless, importando e reexportando GLB sem alteração geométrica.

## Critérios de aceitação

- nenhum NPC aparece dentro do enquadramento;
- banco comporta visualmente ao menos dois personagens compactos;
- `patch-grass` achatado não é usado como detalhe principal;
- árvores e detalhes naturais variam deterministicamente entre todos os stems compatíveis;
- qualquer asset novo passa pela regra de AABB e comparação contextual documentada em `docs/asset-scale-and-variety.md`.
