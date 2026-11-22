# Mercearia, fornecedor e janela de compra

20/11/2026 · Godot 4.7.2 .NET · GL Compatibility

- **Big Ambitions.** A apresentação oficial descreve começar com um pequeno negócio e administrar o abastecimento e a logística. Para o BOXBOX, a viagem inicial ao fornecedor funciona como um objetivo concreto e dá um lugar à compra no bairro. [Página oficial na Steam](https://store.steampowered.com/app/1331550/Big_Ambitions/)
- **Supermarket Simulator.** A descrição oficial liga compra de estoque, recepção e reposição nas prateleiras. Para o BOXBOX, confirmar o pedido e abrir a mercearia são ações separadas. [Página oficial na Steam](https://store.steampowered.com/app/2670630/Supermarket_Simulator/)
- **Referência do usuário.** A inspiração em GTA orienta a sequência de objetivo, local e ação. O protótipo usa a direção isométrica já aprovada para o BOXBOX e deixa o cenário visível ao interagir. [Direção visual do BOXBOX](../docs/visual-direction-and-tone.md) · [GTA VI na Rockstar Games](https://www.rockstargames.com/VI)
- **Godot.** A câmera ortogonal preserva a leitura do interior aberto. O `CanvasLayer` mantém a janela separada da cena 3D. Ao abrir a compra, a câmera recua e reenquadra o fornecedor para o lado livre da tela. [Camera3D](https://docs.godotengine.org/en/4.7/classes/class_camera3d.html) · [CanvasLayer](https://docs.godotengine.org/en/4.7/classes/class_canvaslayer.html)

## Hipótese e critério de aceitação

O jogador entende melhor a compra se o lugar e a próxima ação permanecerem visíveis. Na mercearia, a entrada e o caixa devem ter espaço ao redor. No fornecedor, prateleiras e carrinho de carga distinguem o depósito. Ao abrir a janela, o cenário permanece no mesmo quadro e o jogador pode fechar pelo X ou por Escape. Confirmar o pedido libera o caixa da mercearia. A janela ainda usa os controles de compra do protótipo. O acabamento da hierarquia e os rótulos de todas as ações ficam para o próximo passe.

## Assets e escala

Os interiores usam Mini Shop 2.1, com o arquivo `Textures/colormap.png` original ao lado dos GLBs. O material depende desse caminho relativo. O ZIP da nova coleção permanece em revisão. A arquitetura do diorama segue a composição já aprovada em [first-block-curation.md](../BOXBOX/asset-library/reports/first-block-curation.md).

Medidas de origem aferidas no Blender 4.3.2, largura × profundidade × altura:

| Asset | Tamanho |
| --- | --- |
| Piso de madeira e piso | 1,00 × 1,00 × 0,00 |
| Parede, janela e porta | 1,00 × 0,60 × 1,00 |
| Caixa | 0,90 × 0,85 × 0,59 |
| Prateleiras de caixas e sacos | 0,80 × 0,70 × 0,85 / 0,89 |
| Expositor de frutas | 0,60 × 0,60 × 0,52 |
| Freezer | 0,80 × 0,60 × 0,34 |
| Carrinho de carga | 0,37 × 0,37 × 0,55 |

Os pisos de 1 m formam a profundidade de 4 m dos dois interiores. A altura da porta e do mobiliário foi comparada às dimensões do protagonista já normalizado para 0,78 m. A comparação em movimento com o protagonista e com o veículo na rua fica pendente até conectar os interiores ao bairro.

## Material do atacado

21/11/2026 · Godot 4.7.2 .NET · GL Compatibility

A geometria e o mapa de cores continuam low-poly. O atacado recebe uma paleta de ardósia e terracota, com grão do concreto real aplicado em espaço de mundo nas paredes e na entrada. Isso mantém o grão na mesma escala entre módulos. O critério visual é distinguir o atacado da mercearia e ler o desgaste sem perder o contorno dos objetos. [Poly Haven Concrete Floor Worn 001](https://polyhaven.com/a/concrete_floor_worn_001) · [Godot Spatial Shader Reference](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/spatial_shader.html)

A revisão de 21/11/2026 substitui o material de grão sobre a paleta por materiais fotográficos diretos nas paredes e pisos. A cor da foto fornece o albedo sem tingimento, com normais OpenGL e rugosidade correspondentes. A projeção triplanar em espaço de mundo reaproveita as malhas do Mini Shop sem esticar a paleta UV original. Os vidros preservam a transparência original. [Godot BaseMaterial3D](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Créditos dos materiais](../BOXBOX/boxbox/Assets/Grocery/CREDITS.md)

Depois da comparação visual, a direção aprovada voltou aos pisos originais do Mini Shop. Os materiais fotográficos diretos permanecem nas paredes. Essa combinação preserva a leitura do interior e o contraste entre as duas lojas.

## Mobiliário da mercearia

21/11/2026 · Blender 4.3.2 · Godot 4.7.2 .NET

A hipótese é que exposição por categoria ajuda a ler a loja de cima. O expositor de bebidas, o refrigerador vertical, o cesto de pães e o cesto de compras vêm do Mini Shop. O Blender confirmou malhas com polígonos e materiais ligados ao mapa de cores do pacote. As medidas de origem em metros, em largura × profundidade × altura, são 0,80 × 0,80 × 0,90 para bebidas, 1,00 × 0,48 × 0,90 para o refrigerador, 0,70 × 0,60 × 0,50 para pães e 0,35 × 0,35 × 0,25 para o cesto. A porta mede 1,00 × 0,60 × 1,00. O protagonista tem 0,78 m de altura no protótipo externo e o interior tem 4 m de profundidade. Os modelos preservam a escala de origem. A passagem física com o protagonista será verificável quando o cenário interno for conectado à exploração.

### Espaço e exposição do atacado

No Godot 4.7.2, as cenas internas usam coordenadas separadas da rua. A hipótese é que um piso de 9 × 7 m na mercearia e de 13 × 10 m no atacado permita corredores, atendimento e exposição por setor, sem colisões invisíveis com a vizinhança. As portas e vagas continuam nos endereços da missão. Conferir visuais, colisões e as ações do fornecedor e do caixa. [Camera3D](https://docs.godotengine.org/en/stable/classes/class_camera3d.html) · [escala de assets](../docs/asset-scale-and-variety.md).

As malhas exportadas no Blender medem 9 × 7 m e 13 × 10 m. As paredes principais têm 1,85 m e 2,25 m. As prateleiras de 0,80 × 0,70 × 0,85–0,89 m na origem chegam a 1,56 × 1,37 × 1,66–1,74 m no atacado. O refrigerador vertical de 1 × 0,48 × 0,90 m chega a 1,95 × 0,94 × 1,76 m. O expositor de ponta, a laranja e a garrafa são do acervo Kenney Mini Shop/Food Kit. As fotos em aço já fazem parte da biblioteca Poly Haven. Duas figuras de teste dão escala nos prints.

### Revisão de mobiliário e elenco

Godot 4.7.2 Compatibility e Blender 4.3.2. O objetivo é trocar os expositores genéricos por gôndolas com estrutura, prateleiras e embalagens, refrigeradores com portas e produtos visíveis e bancas com caixas de madeira. O elenco passa a usar modelos CC BY do lote Polyfork, com escala pela silhueta visível e animações do rig importado. Verificar no jogo a leitura dos setores, a circulação até os caixas e o movimento do novo protagonista e dos transeuntes. [Importação de cenas 3D do Godot](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/index.html) · [Licença Polyfork](https://polyfork.dev/licensing)

O novo conjunto autoral mede, em metros antes das escalas locais, 0,79 × 0,63 × 0,88 na gôndola, 1 × 0,53 × 0,90 na geladeira e 0,65 × 0,63 × 0,52 na banca. À escala do atacado, a gôndola tem cerca de 1,72 m de altura e a geladeira 1,76 m. Comparamos esses móveis com os personagens, o piso de 13 × 10 m e a porta no Godot. O protagonista é o arquivo do criador via Manus, cuja malha caiu de 1.904.641 para 36.000 triângulos e manteve o rig e as animações de locomoção. O material de pele/roupa tem mapas próprios. Os pedestres são da seleção Polyfork licenciada.

### Proporção do elenco em relação ao cenário

O AABB visível do protagonista no Blender, a caminhonete de 3,51 m e o piso de células de 4 m servem de referência. A altura em jogo de 1,12 m produzia a leitura de miniatura. A hipótese é que 1,68 m para ele e alturas próximas para os outros adultos melhoram a comparação com a porta e os refrigeradores, sem precisar mudar a densidade de NPCs. No Godot 4.7.2, validar em execução com a cápsula e as alturas visuais lado a lado. [CharacterBody3D do Godot](https://docs.godotengine.org/en/stable/classes/class_characterbody3d.html)
