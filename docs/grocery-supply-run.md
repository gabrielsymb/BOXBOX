# Primeira viagem de abastecimento

O BOXBOX começa num bairro aberto e percorrível. O jogador recebe a primeira missão automaticamente e usa sua caminhonete para buscar a mercadoria no atacado. A janela do pedido informa o que comprar. O deslocamento, a retirada, o transporte e a descarga acontecem no mundo.

| Etapa | O que o jogador faz | O que o jogo registra |
| --- | --- | --- |
| Encomendar | Escolhe os produtos e confirma o pedido. | O caixa é debitado uma vez e o lote fica reservado no fornecedor. |
| Ir ao atacado | Entra na caminhonete, segue as ruas e para na área de retirada. | O objetivo continua ativo até o jogador chegar com o veículo certo. |
| Carregar | Interage na área de retirada ao lado da caminhonete estacionada. | O lote sai do fornecedor e passa para a carga da caminhonete. |
| Voltar | Dirige de volta ao ponto de descarga da mercearia. | A carga permanece no veículo ao trocar de objetivo, sair do raio visual ou recarregar um save. |
| Descarregar e repor | Descarrega perto do estoque e leva as unidades à prateleira. | O lote entra no depósito. Repor transfere as unidades para o estoque vendável. |

O pedido, a carga e o ponto de destino precisam conservar suas identidades. Confirmar, carregar ou descarregar outra vez não duplica débito, mercadoria nem recompensa. Sair do veículo, errar a rota ou fechar a janela lateral mantém a etapa em andamento. Na primeira fatia, acionar a retirada carrega o lote de uma vez. Decidir quantas caixas carregar por viagem pode ser testado depois.

## Navegação

O protótipo externo tem dois endereços fixos no mesmo trecho de rua, com a caminhonete do protagonista estacionada em frente à BOXBOX. A câmera muda ao entrar e sair. Os endereços estão a 64 m entre si. O marcador de retirada e entrega ainda depende do estado da missão. O GPS exige conectar esses endereços ao grafo navegável do bairro.

O minimapa de canto acompanha o jogador ou a caminhonete. O mapa geral permite ver a cidade e selecionar um destino. Os dois consomem o mesmo destino e a mesma rota de ruas. A rota precisa permanecer conhecida quando as células visuais saem do alcance da câmera. Um desvio recalcula a indicação a partir da posição atual. A direção continua no controle do jogador. [Estudo de navegação do BOXBOX](../research/gta2-mechanics-study.md)

## Conversas na missão

A fala do fornecedor pertence à interação no balcão. Uma fala tem personagem, chave de legenda, próxima fala e áudio opcional. As primeiras falas podem aparecer como legendas curtas com o nome de quem está falando. Com voz gravada, a legenda acompanha o áudio. Uma ligação pode usar a mesma estrutura com apresentação de telefone. Durante a direção, o marcador e o texto curto de objetivo devem bastar para saber o próximo passo.

## Estado da implementação

`NeighborhoodShops.tscn` testa o pedido, a carteira, o HUD e o fechamento. A viagem por botão e o café são exemplos dessa bancada. `OpenWorldPrototype.tscn` testa direção e exploração. A primeira entrega integrada termina quando o jogador dirige da mercearia ao atacado e de volta, conferindo que o débito, a carga e a descarga acontecem uma vez. O primeiro corredor físico já permite dirigir entre os dois pontos, com a janela de compra e o estado da carga ainda pendentes de ligação a esses endereços. GPS completo, mapa geral e voz entram sobre esse percurso.

## Legenda e minimapa em execução

Nando é identificado pelo nome em rosa, com falas em recursos de conteúdo e voz curta sintetizada opcional. A legenda fica sobre o cenário e avança sozinha. A arte do retrato é opcional por fala. O protótipo atual usa o nome como identificação. O rosa provisório `#E776AE` também é usado nos ícones de objetivo ativo. Os ícones são PNGs do Kenney já presentes no acervo.

A cena de exploração usa uma segunda câmera ortográfica para exibir no minimapa as ruas e as construções carregadas. Ela acompanha a posição e direção do personagem a pé ou do veículo quando ele está dirigindo. As cenas internas atuais ficam sem minimapa. Quando os endereços e a missão forem integrados, o marcador rosa deverá usar a coordenada do destino ativo. GPS e mapa geral continuam pendentes.

Na bancada atual, Voltar fecha primeiro a janela lateral e depois retorna ao local registrado na aplicação. Falar com Nando pode abrir uma oferta de café depois da legenda, com preço condicionado à relação comercial. Sair do atacado encerra a oferta. A interface apresenta a oferta e envia comandos à aplicação, que decide disponibilidade, preço e débito na carteira.

## Percurso integrado no protótipo externo

A cena externa é a entrada do jogo. Tem fachadas separadas, E para a caminhonete e F para porta, balcão, retirada, descarga e caixa. O alvo rosa do minimapa muda com a etapa e usa as coordenadas das lojas. Perto da caçamba, a retirada exige o veículo parado no ponto do atacado. A descarga exige a vaga da BOXBOX. A primeira saudação de Nando é disparada ao entrar no atacado.

O pedido inteiro vira uma carga. O número de caixas visuais resume o total em grupos de seis, até quatro caixas por vez. A aplicação preserva a quantidade exata. A caçamba usa movimento local amortecido e com limite medido, sem deixar caixas soltas alterarem estoque por colisão. A descarga libera o expediente neste recorte. Reposição manual na prateleira, persistência da carga entre sessões e outros tipos de item seguem em aberto.
