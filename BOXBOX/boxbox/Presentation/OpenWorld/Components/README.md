# Componentes do bairro

Arraste `CasaTerrea.tscn`, `Sobrado.tscn`, `CasaDosFundos.tscn` ou
`ArvoreCopaLarga.tscn` para uma cena. A raiz fica no chão, leva a colisão
junto e aceita posição e rotação próprias. As correções de escala dos modelos
já estão nas cenas. Use a caminhonete de 3,51 m e a célula de 4 m para
conferir as proporções de novas variantes.

[ShopBlock.tscn](../ShopBlock.tscn) é o exemplo montado de 24 × 24 m.
Ele contém as posições no mapa inicial. Os modelos, materiais e malhas
ficam em [StartingBlock](../../../Assets/World/StartingBlock).
Os OBJ de piso foram feitos para esse layout. Em outro lote, aplique os
mesmos materiais às malhas do novo traçado.

| Superfície | Recurso |
| --- | --- |
| Passeio de pedra | `Paralelepipedo.tres` |
| Pátio de cimento | `ConcretoDePatio.tres` |
| Quintal de terra | `TerraBatida.tres` |
| Quintal com grama irregular | `SoloDeQuintal.tres` |
| Paredes, telhados, vigas, esquadrias e casca | Recursos por material, vinculados aos slots de cada GLB |

Para grama com falhas, use uma superfície UV contínua no lote e ajuste
`metres_per_uv` para quantos metros correspondem a uma repetição de UV.
No quarteirão inicial são 5 m. O material mistura as fotos de terra e
grama em manchas. Calçada e pátio são superfícies próprias.

## Fontes e portabilidade

- Modelos GLB e malhas OBJ conservam a geometria e as UVs. As fotos de cor,
  normais, rugosidade e altura são arquivos separados em
  [Materials/PolyHaven](../../../Assets/World/Materials/PolyHaven).
- As cenas, os colisores, os vínculos de slots dos GLB nos arquivos
  `.import`, os materiais `.tres` e o shader de grama são específicos do
  Godot. Em outra engine, recrie esses vínculos e o comportamento do shader
  usando os mesmos mapas. Os GLB das casas carregam os nomes dos slots e
  usam os materiais fotográficos externos no Godot.
- Os scripts em `tools/blender` recriam os pisos, as UVs das fachadas e
  as UVs do tronco. No interior e no exterior da mercearia, os assets de
  arte são separados da entrada, do balcão e do fluxo de abastecimento.
- [Créditos dos modelos](../../../Assets/World/StartingBlock/CREDITS.md) e
  [licenças das fotos](../../../Assets/World/Materials/PolyHaven/LICENSE.md).
