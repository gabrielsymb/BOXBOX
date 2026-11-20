# Curadoria da primeira quadra

Data: 2026-11-19

## Objetivo

Comparar, fora do build do Godot, os pacotes aprovados em uma única composição isométrica. A cena não é arte final nem implementação do jogo; é um artefato visual de decisão.

## Assets usados

- Kenney Mini Characters 2.0: candidata provisória e dois exemplos de NPCs persistentes.
- Kenney Mini Shop 2.1: piso, paredes, caixa, prateleiras, expositor e freezer.
- Kenney Food Kit 2.0: alimentos soltos para verificar escala e leitura.
- Kenney Furniture Kit 2.0: mesa, cadeira e caixas de estoque.
- Kenney City Kit Roads 2.2: rua e faixa de pedestres.
- Kenney City Kit Commercial 2.1 e City Kit Suburban 2.0: vizinhança.
- Kenney Car Kit 3.1: veículo de entrega.
- Kenney Mini Forest 1.0: árvores e plantas.
- Fredoka: rótulos da prancha de candidatos, usados somente na avaliação.

## Resultado preliminar

- A linguagem cartoon/low-poly é compatível entre os pacotes quando câmera, luz e escala são unificadas.
- Personagens, Mini Shop e Furniture combinam quase diretamente.
- Roads, Commercial e Suburban exigem adaptadores de escala por pacote.
- Car Kit chega significativamente maior e exige escala própria.
- A loja aberta é legível em câmera ortográfica e preserva a sensação de diorama.
- A personagem `character-female-a` aparece apenas como candidata provisória; ainda não foi aprovada como protagonista.

## Decisão do usuário

Em 2026-11-19, a direção da primeira quadra foi aprovada pelo usuário como a referência visual correta para o BOXBOX: ainda crua, mas exatamente no estilo desejado. A aprovação cobre a linguagem cartoon/low-poly, a composição isométrica, a loja aberta e a convivência dos kits após normalização. Ela não confirma a personagem provisória como protagonista. A prancha civil atual fica aproveitável para NPCs, mas provavelmente não contém o protagonista porque os requisitos futuros incluem interação com armas.

## Regra preservada

Nenhuma forma visível destinada ao jogo foi criada por código. A composição usa somente assets baixados. Câmera, luz, organização e transformações existem apenas para a curadoria no Blender. Os rótulos da prancha são material de avaliação, não UI do jogo, e usam a fonte Fredoka aprovada.

## Próxima decisão visual

1. Especificar se a interação do protagonista com armas significa apenas pegar, carregar e guardar ou também mirar e disparar.
2. Curar um protagonista cujo rig e conjunto de animações atendam ao requisito confirmado.
3. Preparar o primeiro recorte para importação no Godot sem desviar da referência aprovada.
