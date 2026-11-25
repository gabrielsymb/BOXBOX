# Ruas e GPS

Código puro em `Assets/BoxBox/Core/Roads` (assembly `BoxBox.Roads`, sem Unity); desenho em
`Assets/BoxBox/Presentation/World`. Testes: `Tests/EditMode/RoadNetworkTests.cs`.

## De onde veio

| Parte | Fonte | O que foi portado |
|---|---|---|
| Faixas (`RoadLanes`) | REDRIVER2 `Game/C/dr2roads.h` (macros `ROAD_*`), `civ_ai.c:29` | contagem por metade, `LaneDirs`, `AILanes`, limite de velocidade, estacionamento nas pontas |
| Peças (`RoadStraight`, `RoadCurve`, `RoadJunction`) | REDRIVER2 `Game/dr2types.h` (`DRIVER2_STRAIGHT/CURVE/JUNCTION`) | centro, comprimento, ângulo, raio interno, ligações |
| Faixa sob um ponto | REDRIVER2 `dr2roads.c` `GetLaneByPositionOnRoad` | `lane = NumLanes − ((across + 512) >> 9)`; na curva `dist/512 − inside·2` |
| Sentido da faixa | REDRIVER2 `civ_ai.c` (cálculo de `cr->dir`) | bit 0 = segue o ângulo da peça, 1 = contrário; curva = tangente ±90° |
| Rota entre cruzamentos (`RouteFinder`) | gta2_re `Source/RouteFinder.cpp` (`NewChildNode`, `InsertIntoOpenList`, `ExpandNode`, `SearchToDest`) | busca com lista aberta ordenada, custo = pai + comprimento + Manhattan até o destino, visitado na inserção, ordem N/S/O/L, ruas fecháveis (`RoadOff`) |
| Cidade em quarteirões (`GridTown`) | ideia de GTA2 / Driver | grade de cruzamentos ligados por retas |

OpenMC2 foi olhado: é só um hook da DLL original, não tem lógica de ruas.

## Escala

512 U por faixa × 0,006 m/U (escala do port do carro) = **3,07 m por faixa**.
Limites `{56, 97, 138}` U/passo → **36 / 63 / 89 km/h**. Ruas comuns: 1 faixa por sentido, 36 km/h.
Avenida: 2 faixas por sentido, 63 km/h. Mão de direção: à direita (faixa 0 = lado direito do ângulo).

## Peculiaridades mantidas de propósito

- O custo do nó no GTA2 acumula a distância Manhattan dos pais (não é A* puro); em grade dá a rota certa.
- `FindSurface` é busca linear (Driver usa BSP por célula); suficiente para a greybox.

## SandboxLab

Cidade 4×4 com avenidas na 3ª linha de cada eixo. A mercearia fica no ponto de nascimento (lote 0,1),
o pátio de testes (rampa + slalom) no lote em frente, o atacado no lote 3,3. Uma linha azul no chão
(GPS) mostra a rota quando há um destino; o HUD mostra faixa, limite e **CONTRAMÃO**.

## Trânsito (Core/Roads/Traffic + Presentation/Traffic)

Carros civis usam **a mesma física** do jogador (`VehiclePhysicsController.SetAiControl`, como
`CivControl` do Driver que escreve `wheel_angle` e `thrust` direto). Portado de REDRIVER2 `civ_ai.c`:

| Parte | Fonte | Comportamento |
|---|---|---|
| `TrafficLights` | `TrafficLightCycle` | ciclo de 512 quadros (~17 s): N/S e L/O alternam, 5 s verde + 1,7 s amarelo |
| `CivPath` | `CivFindStation`, `CivFindPointOnPath` | projeta o carro no trecho atual, descarta trechos passados, anda N metros pelo caminho |
| `CivRouteBuilder` | `CreateNewNode`, `GetNextRoadInfo` | segue a faixa, põe faixa de parada no fim, escolhe saída aleatória (sem retorno), curva com 3 pontos; direita → faixa da direita, esquerda → da esquerda |
| `CivDriver` esterço | `CivSteerAngle` | mira em (velocidade × 20 quadros + meio carro) à frente, máx 45° |
| `CivDriver` aceleração | `CivAccel`, `CivAccelTrafficRules` | `newAccel = 2000`, menos 3 por unidade de esterço; para no vermelho com `velRatio` (velocidade − 120000 Vd espalhada na distância), espera o verde |
| `CivDriver` freio por obstáculo | `SetUpCivCollFlags` | caixa à frente = meio carro + 0,56 m + velocidade × 13 quadros (× 4 virando) → freia proporcional à velocidade, até 2 × newAccel |
| `TrafficManager` | `PingInCivCar`, `CheckPingOut` | 14 carros; quem passa de 230 m do jogador reaparece numa faixa a 45–180 m |

Adaptações (documentadas no código): acima do limite o motorista tira o pé (Driver usava o arrasto
linear que removemos); `velRatio` mínimo 0,6 para o carro não morrer antes da faixa; reduz a velocidade
para o menor limite dos próximos 20 m (as curvas têm metade do limite).

Ainda não tem: preferência em conversão à esquerda (`CIV_AI_CTRL_YIELD_TURN`), troca de faixa
(`CheckChangeLanes`), estacionados, buzina, luz de freio visível, semáforo desenhado.

re3 (GTA III) foi derrubado por DMCA e não foi usado. `gtav-recomp` foi olhado: é só o runtime de
recompilação do Xbox 360, sem a lógica do jogo.

## Próximo

Semáforos visíveis, preferência e troca de faixa; meio-fio com rebaixo
nas entradas; curvas desenhadas (o modelo já suporta, o gerador ainda só usa retas).
