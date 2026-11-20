# BOXBOX — escala, variedade e integração de assets

## Regra obrigatória de escala

Todo GLB ou TSCN precisa ser medido no Blender antes de entrar em geração procedural. O registro mínimo contém caminho, pacote de origem, AABB, eixo vertical, pivô e dimensão-alvo no diorama.

O teste contextual compara o asset com:

- protagonista visual de 0,78 unidade;
- NPC adulto de 0,74–0,82 unidade;
- criança de 0,62 unidade;
- célula urbana de 4 unidades;
- carro na escala nativa aprovada;
- porta, banco, poste e meio-fio próximos.

Compilar não valida escala. A integração só termina após captura dentro do jogo, inspeção de colisão e comparação na câmera isométrica.

## Regra obrigatória de variedade

- O catálogo descobre recursivamente todos os `.glb` e `.tscn` importados.
- O gerador sorteia entre todas as variantes semanticamente compatíveis, com seed determinística.
- Packs curados podem ser misturados; câmera, escala, material, sombra e função unificam o kitbash.
- Compatibilidade vem antes da quantidade: uma curva não substitui uma reta, um kart não vira tráfego civil e um móvel interno não aparece na calçada sem curadoria.
- Cada categoria registra quantos assets existem no acervo, quantos foram importados e por que os restantes foram excluídos.
- Nenhum caminho individual novo entra numa lista procedural fixa quando a categoria pode ser descoberta pelo catálogo.

## Vegetação e terreno

O `patch-grass` achatado não deve ser repetido como decoração principal. Gramado é superfície de terreno delimitada; volume vem de plantas, árvores, pedras, vasos e grupos curados dos packs. Tufos baixos aparecem somente como detalhe esparso e nunca substituem um lote verde coerente.

## Spawn visual

Pedestres, veículos e objetos temporários materializam fora do enquadramento ativo. Eles entram andando ou dirigindo. Falta de posição externa válida adia o spawn; nunca autoriza aparecer diante da câmera.
