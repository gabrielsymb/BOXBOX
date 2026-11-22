# Caminhonete e primeiro trecho do bairro

21/11/2026 · Blender 4.3.2 · Godot 4.7.2 .NET, Jolt e GL Compatibility

A referência de condução serve para ler a rua em perspectiva, com o veículo próximo e área visível à frente. As imagens de Chinatown Wars ajudam a discutir densidade e orientação espacial. A sugestão de mesclar pontos autorais e preenchimento procedural foi adotada como hipótese de trabalho. A porcentagem de geração e as práticas internas atribuídas à Rockstar na captura carecem de confirmação. [Chinatown Wars](https://www.rockstargames.com/games/chinatownwars) · [Vice City](https://www.rockstargames.com/games/vicecity)

A documentação do Godot orienta consultas de colisão, corpo cinemático e a separação entre câmera e veículo. O `VehicleBody3D` tem ressalvas documentadas, então este passe mantém o corpo cinemático e separa aceleração, frenagem, esterço e aderência. O checkout GTA2 foi consultado em CarPhysics e RouteFinder para observar a separação entre controle do veículo e junções de rua. A implementação e os parâmetros são do BOXBOX. [CharacterBody3D](https://docs.godotengine.org/en/4.7/classes/class_characterbody3d.html) · [PhysicsDirectSpaceState3D](https://docs.godotengine.org/en/4.7/classes/class_physicsdirectspacestate3d.html) · [VehicleBody3D](https://docs.godotengine.org/en/4.7/classes/class_vehiclebody3d.html) · [Estudo GTA2](gta2-mechanics-study.md)

## Escolha do asset

| Candidato | Dimensões de referência aferidas no Blender, largura × comprimento × altura |
| --- | --- |
| Polyfork Pickup-truck-70s | 2,24 × 4,88 × 1,81 m |
| City Pack Pickup Truck | 2,31 × 5,18 × 1,85 m |
| Sedã usado pelo protótipo | 1,50 × 2,55 × 1,30 m |

A Pickup-truck-70s tem caçamba aberta, silhueta de veículo de trabalho e proveniência explícita. A cor vem dos vértices do modelo. A proposta começa a 72% da escala de origem para dar aproximadamente 3,51 m de comprimento diante de uma célula de 4 m. Comparar em execução em relação ao protagonista de 0,78 m, à porta de 1 m, ao mobiliário e à largura física da rua. Colisor e raio de roda acompanham a escala. [Pickup-truck-70s, Polyfork, CC BY 4.0](https://polyfork.dev/asset/pickup-truck-70s-8c0080)

## Hipóteses e critérios deste passe

O passeio tem uma face vertical de 0,15 m. O personagem precisa ganhar altura quando encontra um meio-fio baixo e há chão livre do outro lado. Uma parede, um poste ou um vão sem apoio continuam bloqueando. A câmera externa mantém o enquadramento isométrico a pé. Ao entrar na caminhonete, passa para perspectiva traseira, com visão da frente da rua e proteção contra atravessar obstáculos.

A direção deve permitir sair devagar, esterçar em manobra, frear antes da ré e parar com o freio de mão. A velocidade lateral deve se dissipar por aderência em vez de girar junto com o chassi de imediato. Aceitação inicial é dirigir por um trecho, parar e manobrar sem atravessar obstáculos. Suspensão e colisão entre veículos merecem um teste de direção separado.

A mercearia e o atacado ganham endereços estáveis e áreas livres ao redor. O primeiro corredor prova escala e orientação. A revisão da grade inteira deve partir de uma avenida e de ruas locais com larguras e comprimentos distintos, compartilhados por renderização, minimapa e IA de tráfego. Um quarteirão autoral pode combinar uma praça ou comércio distintivo com lotes de preenchimento e grupos de objetos coerentes, como material de obra junto a uma caçamba. Variação por material funciona dentro da identidade do lote. A extensão da cidade será medida em tempo de percurso, variedade de usos e limites de streaming.

## Materiais candidatos

As lojas já têm reboco e blocos fotográficos aprovados. Para a próxima fachada de rua, foram localizados tijolo com reboco e pintura descascada no catálogo CC0 do Poly Haven. Eles são candidatos para comparação em escala de parede, até avaliar visibilidade e adequação ao bairro. [Red Brick Plaster Patch 02](https://polyhaven.com/a/red_brick_plaster_patch_02) · [Peeling Painted Wall](https://polyhaven.com/a/peeling_painted_wall)

A câmera mais baixa revelou listras recorrentes de sombra própria no carro e nas fachadas. O ajuste de bias e de normal bias do sol testa se esse artefato vem da margem da projeção de sombra. Compare as superfícies próximas e se a sombra da caminhonete ainda toca o solo. O quadro em perspectiva também usa o carregamento antecipado de células na direção do veículo. [Godot Light3D](https://docs.godotengine.org/en/4.7/classes/class_light3d.html)


## Recorte implementado

As duas lojas agora reservam lotes no mesmo corredor. Os centros ficam a 64 m um do outro. A escala de 0,72 da pickup foi conferida em cena perto da porta, do protagonista e dos pisos das lojas. O Godot precisou usar explicitamente as cores de vértice do GLB. A pintura e a caçamba então ficaram visíveis no enquadramento a pé e na câmera traseira.

O teste de caminhada cruza a borda do asfalto para a calçada de 0,15 m no lote da mercearia. O teste de direção percorre o corredor até o atacado usando entrada, acelerador, esterço e freio de mão. Também verifica ré e retorno à câmera a pé. A grade procedural ainda mantém cruzamentos regulares. Os prints mostram a borda do streaming no horizonte. Isso limita o que esse primeiro passe demonstra sobre a escala da cidade.

## Fachadas e carga na caçamba

21/11/2026 · Blender 4.3.2 · Godot 4.7.2 .NET

A rua deve mostrar fachadas fechadas. A vista recortada da loja só entra no enquadramento após atravessar o ponto de entrada. Os dois lugares usam modelos comerciais CC0 do Kenney já presentes no projeto, medidos no Blender em aproximadamente 0,88 × 1,24 × 1,29 m na origem. A adaptação de escala conserva a forma original e mantém os edifícios no lote reservado. O critério visual é ver a fechada sem caixa registradora ou mobiliário exposto ao dirigir.

A caçamba da pickup, medida a partir dos vértices no Blender, tem o plano útil perto de 0,50 m na origem, antes da escala de 0,72. Uma caixa física livre pode atravessar o chassi cinemático, espalhar-se ao carregar a cena ou divergir da quantidade em estoque. O primeiro passe deve representar a carga confirmada por caixas do acervo, em posições medidas, com movimento elástico amortecido pelo impulso do veículo. O lote pertence à aplicação. As caixas são sua projeção visual. O critério é que a carga apareça só na retirada, oscile sob aceleração ou frenagem, permaneça dentro das bordas e suma só ao descarregar. [Godot Node3D](https://docs.godotengine.org/en/4.7/classes/class_node3d.html) · [Godot RigidBody3D](https://docs.godotengine.org/en/4.7/classes/class_rigidbody3d.html)

O modelo final da caixa foi `box.glb` do Kenney Car Kit, 0,714 × 0,714 × 0,715 m na origem e com mapa de cores do pacote. A escala de 0,43 resulta em cerca de 0,31 × 0,31 × 0,31 m para os espaços da caçamba. A fachada da BOXBOX usa `corner-shop-market.glb` com identificação própria e a do atacado usa `corner-shop-awning.glb`. As duas ficaram fora do sorteio genérico das quadras. O teste de cena confirmou fachadas exteriores, abertura isolada dos interiores e 0,066 m de deslocamento local de uma caixa durante aceleração.

### Quarteirão da BOXBOX feito à mão

O estudo de mapa do GTA2 e a leitura local de `MapRenderer.cpp` e `RouteFinder.cpp` orientam a separação entre geometria reconhecível e rede de circulação. O quarteirão é uma cena Godot independente, com a malha do piso feita no Blender e materiais UV conforme a documentação de BaseMaterial3D. A malha de ruas e suas conexões continuam seguindo o layout existente.

Hipótese: diferenciar o passeio público, a entrada da loja e os quintais com superfícies reais e dar silhuetas próprias às casas torna a BOXBOX legível como ponto de referência. Para aceitar a mudança, o acesso à fachada e à caminhonete precisa continuar livre, nenhuma célula desse quarteirão deve receber decoração aleatória e as casas devem ter largura superior aos 3,51 m da caminhonete.

Fontes: [documentação do Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Poly Haven](https://polyhaven.com/license)

Medições no Blender em largura × profundidade × altura. Casa térrea g: 1,450 × 1,178 × 0,768 m na origem e 6,09 × 4,95 × 3,23 m na cena. Sobrado k: 0,920 × 1,020 × 1,150 m e 4,23 × 4,49 × 4,72 m. Casa q: 1,240 × 0,886 × 0,918 m e 5,64 × 3,94 × 3,99 m. Árvore `tree`: 0,928 × 0,882 × 1,684 m e 2,41 × 2,29 × 3,96 m. Banco: 0,400 × 0,200 × 0,470 m e 1,24 × 0,50 × 0,82 m. Lixeira: 0,208 × 0,234 × 0,428 m e 0,35 × 0,40 × 0,73 m. Referências da cena: pickup de 3,51 m, protagonista de cerca de 0,78 m, porta de cerca de 1 m, célula de 4 m. As medidas das casas precedem a rotação no lote.

Na revisão visual no Godot 4.7.2 Compatibility, a árvore `tree` do acervo se revelou uma conífera e foi descartada. A árvore do City Pack mede 308,410 × 323,738 × 509,086 unidades na origem e aproximadamente 2,78 × 2,91 × 4,07 m na cena. O varal do mesmo pack mede 1,429 × 1,815 × 2,291 m na origem e 1,21 × 1,54 × 1,95 m no quintal. As calçadas formam um quarteirão contínuo de 24 × 24 m sobre um colisor de 0,15 m de altura.

### Fachadas do quarteirão, 2026-11-21

No Godot 4.7.2 Compatibility, o quarteirão manual de 24 m e a pickup de 3,51 m dão referências fixas de escala. Conforme as notas do GTA2 e o estudo de `MapRenderer.cpp`, a loja deve ser reconhecível pela forma e pelos materiais antes de depender do minimapa. Hipótese: aumentar sobretudo a largura da frente e trocar a paleta lisa por reboco, alvenaria remendada e telha fotografados cria um marco legível sem transformar a venda de bairro em um prédio de luxo. Critério: ler a loja e os materiais nos enquadramentos reais, manter livre o passeio e completar o mesmo percurso de abastecimento.

Fontes: [materiais Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [importação de cenas Godot](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/advanced_import_settings.html) · [texturas Poly Haven CC0](https://polyhaven.com/license).

Medição do exterior no Blender (largura × profundidade × altura): o modelo da mercearia mede 0,884 × 1,267 × 1,293 unidades com toldo reposicionado. A cena usa aproximadamente 6,28 × 5,26 × 4,98 m, com 3,85 m de profundidade no colisor do corpo e o toldo alto sobre o acesso. Antes, a largura era 2,74 m. As outras três malhas mantêm as dimensões do quarteirão anterior. Os materiais fotográficos passaram a usar UVs em metros, albedo, normal e rugosidade em slots próprios, preservando o vidro e a leitura das esquadrias.

### Relevo de superfícies, 2026-11-21

No Godot 4.7.2 Compatibility, usar mapa de normais para responder à luz e mapa de altura com parallax simples para simular juntas profundas. O ensaio usa uma malha UV existente no passeio e a alvenaria remendada do sobrado. A hipótese é que as juntas ficam legíveis na câmera isométrica sem multiplicar a geometria, como alternativa à subdivisão do quarteirão discutida no estudo local do mapa GTA2. Aceitação: os dois materiais são recursos reutilizáveis, o padrão de pedra tem escala coerente com os 2 m do passeio e a calçada continua transponível normalmente. Testar o render no jogo, inclusive em ângulo oblíquo.

Fontes: [BaseMaterial3D do Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Poly Haven](https://polyhaven.com/license).

Para reutilizar, aplique `Paralelepipedo.tres` ou `TijoloRemendado.tres` como material de uma superfície com UVs. Para outra foto, duplique o recurso e troque juntos os mapas de cor, normais OpenGL, rugosidade e altura do mesmo conjunto. Em UVs com repetição a cada metro, use escala UV de 0,5 para uma amostra física de 2 m. No passeio existente, a UV já repete a cada 2,8 m, então o fator é 1,4. Ajuste altura e escala de normais na câmera do jogo. Use geometria onde for preciso mudar a silhueta, fazer degraus ou alterar a colisão. Mantenha o parallax simples nas superfícies com juntas visíveis, recorrendo só ao mapa de normais em superfícies com microtextura ou UV triplanar.

### Relevo em vegetação, 2026-11-21

No Godot 4.7.2 Compatibility, usar os mesmos materiais UV validados na calçada, com relevo menor para casca e grama rala. Conforme a abordagem de superfícies do estudo local de GTA2, os canteiros ficam na malha do quarteirão. A hipótese é que as áreas verdes leiam como grama seca e terra misturada e que o tronco ganhe relevo sem mudar a silhueta low-poly. Conferir no jogo as juntas do piso, a transição com a terra batida e a árvore à altura da câmera. [Materiais Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Poly Haven](https://polyhaven.com/license).

A árvore original e a variante com casca mantêm o AABB em Blender de 308,410 × 323,738 × 509,086 unidades (cerca de 2,78 × 2,91 × 4,07 m na cena). A casa e a caminhonete servem como referência visual no print de detalhe. Os canteiros substituem 30 m² de terra batida em dois pontos dos fundos, no mesmo nível do colisor.

### Separação de materiais das casas, 2026-11-21

No Godot 4.7.2 Compatibility, a leitura em câmera isométrica pede diferenças claras entre planos contíguos, como observado no estudo local do render de mapa de GTA2. A hipótese é que usar telha cinza no sobrado, concreto claro nas faixas estruturais e metal fosco nas esquadrias separe a terra, a alvenaria e o telhado sem depender de saturação. Verificar na câmera do jogo e num enquadramento de detalhe do sobrado. [Materiais Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Poly Haven CC0](https://polyhaven.com/license).

Os quatro GLBs foram medidos novamente no Blender e preservaram as dimensões anteriores. O sobrado usa telha cinza com alvenaria quente, faixas de concreto e esquadrias de metal pintado. Essas fotos compartilham a biblioteca de materiais da Poly Haven, com UV por superfície. O passeio de pedra, o pátio de cimento e os quintais de terra têm recursos próprios.

### Continuidade visual dos quintais, 2026-11-21

Se um lote começa com grama, estender sua presença por toda a área livre junto ao prédio, com variação de cobertura e terra visível em pontos de passagem. Preservar as bordas do lote junto ao passeio e ao concreto. A hipótese para Godot 4.7.2 Compatibility é que misturar os mapas de grama e terra em coordenadas métricas e numa malha por lote elimina a emenda quadrada visível do canteiro. Validar no enquadramento do sobrado. [Shader espacial do Godot](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/spatial_shader.html)

### Frenagem e carga móvel, 2026-11-21

Godot 4.7.2 Compatibility. As consultas a CharacterBody3D e a CarPhysics do checkout de estudo de GTA2 indicam separar velocidade efetiva, frenagem e escorregamento. Hipótese: a ação dedicada de freio deve dominar também quando o acelerador fica pressionado, e a derivada da velocidade efetiva após a colisão deve empurrar a carga no sentido oposto, com retorno amortecido e limites da caçamba. Marcas só devem surgir quando o freio é aplicado em movimento sobre o chão da rua e ficar no mundo. Validar com as duas teclas pressionadas, curva, parada, e o fluxo de abastecimento. [Godot CharacterBody3D](https://docs.godotengine.org/en/4.7/classes/class_characterbody3d.html) · [PhysicsDirectSpaceState3D](https://docs.godotengine.org/en/4.7/classes/class_physicsdirectspacestate3d.html)

O AABB original da caixa Kenney mede 0,714 × 0,714 × 0,715 m. A caixa na caçamba passa de ~0,31 m de lado para aproximadamente 0,49 × 0,46 × 0,52 m, com altura próxima de dois terços do protagonista e dentro da pickup de 3,51 m de comprimento. A marca de pneu usa uma malha plana autoral de 1 × 1 m sem colisão, recortada para 0,18 m de largura por segmento. O mapa de opacidade foi feito sobre a textura fotográfica `asphalt_01` CC0, após busca no acervo e no catálogo de texturas.

### Materiais da caminhonete, 2026-11-21

No Godot 4.7.2 Compatibility, separar as faces do GLB em slots de chapa pintada, aço, borracha, vidro e faróis. As cores de vértice e as posições das faces no Blender identificam as partes do modelo. Hipótese: usar fotos de microtextura, materiais com rugosidades próprias e repintar o teto escuro melhora a identificação da pickup nos enquadramentos de direção e a pé. Conferir a escala de 3,51 m, o giro das quatro rodas, luzes e a leitura da caçamba numa freada. [Materiais PBR do Godot](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html) · [Poly Haven CC0](https://polyhaven.com/license).

### Desgaste da pintura, 2026-11-21

No Godot 4.7.2 Compatibility, a chapa pintada usa o princípio do material de quintal. Fotos de metal e de chão são combinadas com máscaras no espaço do modelo. A hipótese é que variar desbotamento, poeira e rugosidade por painel mostre a história de uso da pickup. O teto e o capô concentram desbotamento, a parte baixa acumula poeira e a caçamba concentra marcas de carga. Conferir o padrão nos dois enquadramentos e durante movimento. [Shaders espaciais do Godot](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/spatial_shader.html)

### Meio-fio e contato das rodas, 2026-11-21

No Godot 4.7.2, os módulos Kenney medidos no Blender têm asfalto na altura de 0,01 e topo do meio-fio em 0,02. Em uso o Y é multiplicado por 4, resultando em 0,04 m de piso e 0,08 m de meio-fio. A borda fica a 3,20 m do centro de uma pista de 8 m. A base global invisível em -0,03 m deixava os pneus cortarem a geometria. A hipótese é que caixas estáticas alinhadas às superfícies, com a caminhonete inicial estacionada já na faixa de asfalto, corrijam contato e preservem o caminho curto de pedestres. Validar piso, bloqueio lateral do veículo, subida a pé e ida ao fornecedor. [Godot CharacterBody3D](https://docs.godotengine.org/en/stable/classes/class_characterbody3d.html) · [Godot BoxShape3D](https://docs.godotengine.org/en/stable/classes/class_boxshape3d.html)

A variante `Roads/TexturedCurb` preserva a geometria dos três módulos e aplica o concreto fotográfico `rough_concrete` nas faces altas. O teste em execução confirmou o eixo do piso em Y = 0,04 m, o topo da guia em Y = 0,08 m, a parada da pickup contra a face da guia e o retorno com carga até a BOXBOX. O degrau a pé aceita a normal inclinada no contato da cápsula, e o snap ocorre após o deslocamento.

### Luzes da pickup e capturas de portfólio

No Godot 4.7.2 Compatibility, as duas luzes dianteiras ficam ativas com energia 0,42 de dia e 3 à noite. O material da lente acompanha os dois estados. As lanternas de freio usam emissão vermelha, com máscara branca no modo multiplicativo para preservar a cor da lente. As capturas em jogo confirmaram os feixes noturnos e o vermelho na traseira. [Emissão em BaseMaterial3D](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html#class-basematerial3d-property-emission-operator)

### Câmera e limites dos interiores

A câmera interna usa `Camera3D.CullMask` e fundo preto no Godot 4.7.2. A hipótese é que um enquadramento fixo, com os blocos internos isolados da rua, melhora a leitura da loja. O estado de ocupação também governa as entradas de combate e veículos, independentemente da câmera. Validar entrada e saída pela porta, movimento interno, impedimento de disparo e entrada em veículo, e a transição para as câmeras de rua e de direção. [Camera3D](https://docs.godotengine.org/en/stable/classes/class_camera3d.html)
