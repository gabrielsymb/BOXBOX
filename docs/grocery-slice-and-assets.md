# BOXBOX — Mercearia de bairro

O jogo começa na mercearia BOXBOX. O fornecedor tem uma cena própria e o pedido abre em uma janela lateral. O primeiro expediente usa a simulação existente e reconcilia caixa, vendas, estoque e resultados do mercado. A seed fixa deixa o jogador repetir o Dia 1 com outras decisões nas mesmas condições. Abrir a loja inicia a agenda. O expediente avança um cliente por vez e o caixa acompanha as vendas confirmadas. Nesta bancada, o preço de prateleira vem do cenário e o jogador decide quantidade e fornecedor.

## Interação com a loja

O jogador seleciona a mercadoria pelo sprite, ajusta a quantidade com ícones e consulta o preço fixo de prateleira, troca de fornecedor pelo carrinho e abre o comércio pela porta. O expediente e o fechamento usam os mesmos produtos e símbolos, com valores numéricos e dicas localizadas ao passar o mouse. As imagens vêm dos packs Kenney documentados em `res://Assets/Grocery/CREDITS.md`.

Na abertura da primeira partida, um marcador destaca o leite e sugere separar cinco unidades. Ao atingir a quantidade com um pedido dentro do orçamento, o objetivo aponta para a porta. A sugestão é opcional e desaparece depois do primeiro expediente na sessão. Neste recorte a orientação é roteirizada. Recomendações baseadas nas vendas observadas dependem do histórico entre dias.

Arquivos do ZIP com apenas textura, primitivas auxiliares ou materiais incompletos ficam em revisão. Antes de aprovar um GLB, confira que a malha visível corresponde ao objeto anunciado, que texturas e materiais estão presentes ou resolvem seus caminhos, e que o modelo aparece corretamente no Godot. Um arquivo com extensão GLB ou um preview bonito por si só não comprova que a geometria e o acabamento vieram juntos.

## Evolução da fatia jogável

1. Ligar o deslocamento entre os dois interiores ao bairro. Associar compra e abertura aos pontos de interação do balcão e do caixa, com ações acessíveis por teclado e mouse. Nesta versão, os interiores são conectados por botões de viagem.
2. Deixar o jogador intervir entre oportunidades de compra, com atendimento e reposição que alterem resultados confirmados pelo núcleo. O jogador deve identificar qual produto acabou e conseguir reagir durante o expediente.
3. Preservar a loja e um pequeno grupo de moradores entre dias, usando IDs persistentes e retomada da simulação. Demonstrar que o caixa fecha após recarregar e que uma decisão de ontem produz uma consequência compreensível hoje.
4. Montar uma demo de 10 a 15 minutos para teste sem orientação. Observar se cada pessoa consegue explicar uma decisão de compra, uma venda perdida e uma próxima ação. Usar o resultado para decidir se o recorte está pronto para testes externos.

## Triagem do ZIP

O arquivo `BOXBOX/asset-library/NEW_ASSETS.zip` chegou na main em `d99faf6`. A árvore extraída de revisão contém 199 GLBs, incluindo kits e variantes. O lote é adequado para avaliar cidade, vias, veículos e papéis de moradores. A triagem pelo nome dos modelos ainda não encontrou substitutos para prateleiras, caixa registradora ou os três produtos da mercearia.

Dimensões de origem medidas no Blender 4.3.2 em 20/11/2026. Os valores seguem a ordem X, Y, Z do Blender e incluem toda a geometria importada.

| GLB | Dimensões | Aplicação a testar |
| --- | --- | --- |
| `road-tile.glb` do kit retrô | 4,000 × 4,000 × 0,050 | Reta em célula de 4 m. Conferir bordas, rampas e cruzamentos separadamente. |
| `boxy-sedan-70s.glb` | 2,068 × 4,880 × 1,424 | Carro civil. Conferir rodas, chão, faixa e dimensões do colisor. |
| `Pizza Corner.glb` | 1,328 × 1,340 × 3,465 | Referência para fachada comercial. Conferir portas e ocupação de lote. |
| `ATM.glb` | 0,222 × 0,229 × 0,547 | Mobiliário de rua. Comparar altura de uso com o protagonista. |
| `Bench.glb` | 2,511 × 1,225 × 1,621 | Mobiliário de rua. A geometria medida chega a Z = −0,869. Conferir origem e assento. |
| `shopkeeper-be5547.glb` | 1,902 × 2,000 × 2,642 | Candidato a comerciante. Conferir rig, geometria auxiliar e acabamento. |
| `commuter-381e7f.glb` | 1,902 × 2,000 × 2,694 | Candidato a morador. Conferir os mesmos itens e comparar silhueta com o protagonista. |

Para cada família, selecionar modelos com uso equivalente, conferir licença por arquivo, importar no Godot 4.7.2 .NET e comparar em execução com o protagonista, o veículo, a porta, o mobiliário e a célula de 4 m. Registrar a escala, o pivô, os créditos e o resultado visual antes de trocar referências. Para personagens, medir também apenas a malha visível, pois os dois AABBs acima incluem geometria abaixo do piso nominal. O catálogo registra que seis personagens Polyfork de uma seleção anterior foram rejeitados pelo acabamento com materiais planos.

Os GLBs Polyfork com licença individual ou do kit permitem usar CC BY 4.0 com crédito e indicação de modificações, ou a licença Polyfork conforme os termos do pacote. Os módulos fonte `.mjs` têm termos próprios. O crédito do City Pack identifica J-Toastie via Poly Pizza sob CC BY. Os GLBs avulsos sem origem e licença confirmadas ficam fora do build comercial até a identificação de seus direitos. O ZIP permanece como acervo de revisão. [Licença Polyfork](https://polyfork.dev/licensing) · [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)

## Preparação para distribuição

A demo precisa de build Windows testado em máquina limpa, fluxo de início e retomada, opções básicas de apresentação e som, controles legíveis e créditos por asset efetivamente distribuído. O nome comercial também precisa de validação antes da página pública. [Opções de distribuição do BOXBOX](distribution-options.md)
