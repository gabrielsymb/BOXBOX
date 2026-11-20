# BOXBOX — mundo aberto, combate e veículos

## Decisões confirmadas

- O protagonista pode andar a pé, pegar e disparar armas.
- O protagonista pode entrar, sair e dirigir veículos.
- O BOXBOX terá mapa aberto e sensação de mundo aberto.
- A aparência continua composta somente por assets curados ou editados no Blender. Código pode selecionar, posicionar, carregar e ocultar módulos, mas não desenha substitutos visuais.

## Primeiro corte jogável

O primeiro corte combina quatro provas no mesmo executável:

1. locomoção a pé com câmera isométrica;
2. equipar e disparar a arma existente;
3. entrar, dirigir e sair de um carro;
4. atravessar células de mapa carregadas deterministicamente ao redor do foco atual.
5. observar tráfego arcade entrando e saindo da área projetada sem criar ou destruir fatos econômicos.

O objetivo ainda não é entregar a cidade final. É validar que personagem, arma, carro, câmera, colisões e streaming convivem sem acoplar a simulação econômica ao frame gráfico.

## Estados e ações

### Protagonista

```text
OnFootUnarmed | OnFootArmed | Driving | Incapacitated
```

| Ação | Pré-condições | Conclusão | Falha |
| --- | --- | --- | --- |
| mover | `OnFoot*`; direção válida | deslocamento físico por frame | bloqueio por colisão |
| equipar/guardar | `OnFoot*`; arma disponível | troca entre armado e desarmado | comando ignorado se dirigindo |
| disparar | `OnFootArmed`; intervalo cumprido | raycast e evento de impacto | nenhum alvo válido, sem mutação de alvo |
| entrar no carro | `OnFoot*`; carro próximo e livre | `Driving`; personagem oculto e associado | longe ou ocupado |
| sair do carro | `Driving`; ponto lateral disponível | `OnFoot*` ao lado do carro | permanece dirigindo se saída bloqueada |

### Veículo

```text
Parked | Occupied | Moving | Disabled
```

O protótipo usa movimento cinemático local. A versão sistêmica futura separará combustível, propriedade, dano, autoridade e persistência do adaptador visual.

### Alvo de combate do protótipo

```text
Active → Hit → Disabled → Respawned
```

O alvo visual é o asset `target.glb`. Ele existe para verificar direção, alcance, cadência e integração de animação; não representa ainda NPC, crime, dano humano ou regra legal.

## Mapa aberto

O espaço é dividido em células quadradas. A coordenada inteira e uma seed estável determinam qual módulo aprovado ocupa cada célula. Ruas seguem uma grade canônica; quadras usam `tile-high` para ganhar volume e acessos usam `road-slant-flat`, ambos do City Kit Roads. Lotes elevados recebem prédios, vegetação ou alvos conforme hash determinístico. Somente um raio em torno do protagonista ou carro permanece projetado em 3D.

Em 2026-11-20, a grade provisória célula a célula foi substituída por geração em duas escalas. Uma regra global cria vias contínuas e cruzamentos espaçados; cada quadra recebe um perfil determinístico comercial, residencial, misto ou verde, e só então distribui fachadas, recuos, vegetação e vazios. Construções são centralizadas e redimensionadas automaticamente pela caixa real da malha para nunca atravessarem o lote elevado. Semáforos, placas e faixas vêm do City Kit Roads; detalhes de asfalto e concreto são texturas CC0 curadas e mescladas no Blender com a paleta cartoon.

O sorteio independente de perfil por quadra foi posteriormente descartado porque ainda produzia uma sopa homogênea. `UrbanRegionPlanner` forma bairros contíguos por regiões determinísticas de baixa frequência; `UrbanBlockTemplateCatalog` escolhe composições curadas compatíveis com a zona, incluindo faixa comercial, pátio misto, conjunto residencial, parque de bolso, praça-âncora e entrada de bairro. Cada região reserva um bloco landmark. Vegetação usa manchas de densidade dentro do template em vez de uma árvore na mesma posição de todo lote.

As vias do protótipo ocupam duas células de largura. O eixo nativo dos módulos Kenney é respeitado para que linha central, calçada e faixa de pedestres formem continuidade visual. A próxima expansão topológica permitirá superquadras 2×1 e 2×2 por meio de avenidas principais, ruas secundárias e peças explícitas de cruzamento, T, curva e término; ruas nunca serão simplesmente apagadas por sorteio.

O ciclo visual de dia e noite usa o plugin MIT `Day & Night Cycle` 0.3.0. Ele controla apenas céu, luz e atmosfera. O `SimulationClock` continua soberano para economia, agenda e persistência; duração visual de uma fase nunca altera resultados econômicos.

```text
estado persistente do distrito
        ↓
coordenadas + seed
        ↓
seleção de módulos GLB aprovados
        ↓
células visíveis no Godot
```

Descarregar uma célula remove apenas sua projeção. O desenho futuro persistirá atores, veículos, danos, propriedades e fatos do distrito fora da tela; a economia e a agenda nunca dependem da presença do nó 3D.

O protótipo agora usa um raio circular de carregamento, solicita os GLBs pela API assíncrona do Godot e instancia no máximo poucas células por frame em ordem de proximidade. O anel externo mostra somente ruas e bases de quadra; prédios, colisores e mobiliário urbano aparecem no anel detalhado. Névoa leve mascara o limite. Essa é uma política de apresentação e desempenho: nenhum LOD altera o estado persistente.

O tráfego segue a mesma separação. O primeiro corte projeta até quatro veículos arcade próximos, com variedade de modelos. `RoadNodeGraph` fornece centros de cruzamento, faixas, sentidos e decisões determinísticas de seguir reto, virar à esquerda ou virar à direita. `TrafficVehicle` transforma cada decisão em pontos de uma curva Bézier, reduz velocidade enquanto esterça e usa três raycasts frontais. Frenagem é proporcional à distância; bloqueio prolongado permite um pequeno deslocamento lateral somente quando um sensor diagonal encontra espaço. O veículo controlável nasce no acostamento, não no cruzamento. Agentes distantes futuramente serão pontos matemáticos que preservam identidade, rota e tarefa sem física completa.

## Invariantes

- streaming visual não avança nem pausa o `SimulationClock`;
- FPS, raio de carregamento e distância visual não alteram resultados econômicos;
- um disparo só afeta alvo admissível e nunca cria munição, propriedade ou autoridade implicitamente;
- dirigir não transfere propriedade do veículo;
- entrar em carro exige veículo livre e proximidade;
- uma entidade não ocupa simultaneamente os estados `Driving` e `OnFoot*`;
- a mesma seed e coordenada escolhem o mesmo módulo de mapa;
- formas visíveis vêm de assets; colisores, raycasts e volumes invisíveis podem ser construídos pela engine.

## Questões abertas

- munição, recarga, inventário, aquisição e armazenamento de armas;
- admissibilidade e consequências de disparos contra pessoas ou propriedade;
- polícia, testemunhas, evidência, medo, ferimentos e morte;
- propriedade, combustível, dano, manutenção e furto de veículos;
- topologia final da cidade, distritos, interiores e viagens longas;
- persistência espacial de alterações por célula.

## Critérios de aceitação do corte

- executável inicia na quadra sem UI genérica;
- protagonista alterna `idle`, caminhada e tiro;
- arma aparece somente equipada;
- tiro desativa um alvo após impactos válidos;
- jogador entra no sedã, dirige e sai;
- câmera acompanha protagonista ou carro;
- novas células aparecem ao cruzar limites e células distantes são removidas;
- tráfego aparece somente em vias válidas, mantém densidade limitada, freia diante de obstáculos e é removido fora do raio visual;
- somente GLBs aprovados aparecem na cena;
- projeto C# compila e inicia em modo headless sem erro de cena.
