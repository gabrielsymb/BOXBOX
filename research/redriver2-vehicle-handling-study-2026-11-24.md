# Estudo: física do carro do Driver 2 (REDRIVER2) e porte para Unity

Data: 2026-11-24 · Unity 6000.3.29f1 (PhysX) · Fonte: `research/third-party/redriver2` (MIT, SoapyMan 2020)
Implementação: `game/Assets/BoxBox/Presentation/Vehicles/VehiclePhysicsController.cs`

## Arquivos lidos

| Arquivo | Função | O que faz |
|---|---|---|
| `Game/C/wheelforces.c` | `StepOneCar` | gravidade, 12 pontos de contato da carroceria com o chão, chama as rodas |
| | `GetFrictionScalesDriver1` | divide a aderência entre eixo dianteiro e traseiro conforme acelerar/frear/freio de mão |
| | `AddWheelForcesDriver1` | suspensão, atrito lateral, tração, travamento das rodas |
| | `ConvertTorqueToAngularAcceleration` | torque → aceleração angular (inércia), amortecimento angular |
| `Game/C/handling.c` | `ProcessCarPad` | volante digital, acelerador, freio/ré |
| | `GlobalTimeStep` | integra velocidade/posição/quaternion; colisões carro-carro em 4 subpassos RK2 |
| `Game/C/cars.c:820` | | roda visual = ponto de contato + `wheelSize`, deslocada por `compressão - 14` |
| `Game/C/civ_ai.c:29` | `speedLimits` | {56, 97, 138} unidades/passo — usado para calibrar a escala |
| `Game/dr2math.h` | | `ONE = 4096`, `FIXEDH(a) = (a + 2048) >> 12` |

## Unidades

O jogo é todo em ponto fixo de 12 bits e simula **um passo por quadro a 30 Hz** (PS1 NTSC).

- Posição em unidades de mundo (U). `fposition = pos·16` e `fposition += vel >> 8`, então **posição += vel/4096 por passo**.
- Velocidade (Vd): 1 Vd = U/4096 por passo.
- Forças são **acelerações** somadas direto na velocidade a cada passo. **A massa não entra** nas forças das rodas; ela só é usada em colisões.
- Rotação: `AV = avel >> 13` e o quaternion é escalado por 4096, então ω (rad/passo) = avel / 2²⁴.

**Escala adotada: 1 U = 0,006 m.** Ela foi derivada fazendo `GRAVITY_FORCE = 7456` virar 9,81 m/s²
(7456/4096 U/passo² × 900 = 1638 U/s²). Verificação cruzada: os limites `{56, 97, 138}` U/passo viram
36/63/89 km/h, compatíveis com limites urbanos dos EUA (~25/40/55 mph). **Hipótese, não confirmação.**
Os dados reais dos carros (`CAR_COSMETICS`: massa, `powerRatio`, `twistRate`) vêm dos arquivos de nível,
que não estão no checkout.

## Como o carro funciona (resumo do modelo)

1. **Suspensão:** cada roda tem um ponto fixo na carroceria (`wheelDisp`), que é o ponto de contato do pneu.
   Compressão = (altura do chão − altura do ponto)·normal.y + 14, limitada a [0, 42]. A força é
   `230·nova − 100·antiga`, que equivale a uma mola `130·c` mais um amortecedor `100·Δc`. Em repouso,
   4·130·c = 7456 dá c ≈ 14 — por isso o "+14": o carro parado fica exatamente na altura de projeto.
2. **Pneus são viscosos com saturação:** força lateral = −FS · clamp(ganho(v) · v_lateral, ±12500).
   O ganho cai de ~0,8 parado para ~0,3 acima de ~14 km/h. **Quando a velocidade lateral ultrapassa o
   clamp, a força para de crescer, e isso é a derrapagem do Driver.**
3. **A aderência se divide entre os eixos (FS dianteiro + FS traseiro = 1920):**
   - acelerando: dianteira 820 / traseira 1100 → o carro fica estável e sai de frente;
   - soltando: 937 / 983;
   - freando: 1453 / 467 → a traseira fica leve e escorrega (é o "freou na curva e rodou");
   - segurando o volante: a dianteira ganha +45 por passo, até +585 (o carro entra mais na curva);
     zera quando você está contra-esterçando;
   - freio de mão: traseiras travadas e dianteira +656.
4. **Rodas travadas:** acima de ~11 km/h, frear trava as 4 rodas (no carro do jogador). Pneu travado
   gera atrito contra a direção em que está deslizando. A dianteira travada ainda empurra de lado ao
   longo do eixo esterçado (você consegue esterçar travado).
5. **Tração traseira:** o empuxo é aplicado nas rodas de trás, na direção do carro.
   Acelerar = 4915·powerRatio; frear/ré = −5000, diminuindo até zerar numa ré muito rápida.
6. **Arrasto:** −vel·12/4096 por roda no chão. Velocidade máxima ≈ 133 km/h na escala adotada;
   0–100 km/h em ~3,5 s.
7. **Volante digital:** +32 unidades/passo (≈ 84°/s), máximo 352 (≈ 31°), e volta ao centro a 64/passo.
8. **Forças dos pneus são sempre horizontais**, calculadas a partir da guinada do carro. A suspensão age
   ao longo da normal do chão. Tudo é aplicado no ponto de contato, o que gera rolagem e mergulho
   naturalmente.
9. **Chão inclinado:** o atrito cai quando o carro tomba mais de 60° ou quando a normal do chão tem y < 0,8.

## O que foi portado e como

Cada fórmula é calculada em unidades do Driver e convertida para SI só na hora de aplicar no `Rigidbody`
(`força · AccUnitToMs2 · massa`, em `AddForceAtPosition`). Assim o código fica lado a lado com o C.

| Original | Porte |
|---|---|
| integrador próprio a 30 Hz | `Rigidbody` da Unity no `FixedUpdate` (50 Hz); termos por passo escalados por `dt/passo` |
| termo `−100·antiga` (depende da taxa) | reescrito como `130·c + 100·Δc/passos`, para manter o mesmo amortecimento em qualquer tick |
| inércia via `twistRateY/Z` | tensor de inércia da Unity calculado pelo colisor da carroceria |
| amortecimento angular 128/4096 por passo | `angularDamping = 0,95` (mesmo decaimento por segundo) |
| gravidade −7456 | `useGravity = false` + aceleração equivalente (9,8 m/s²) |

## Ajustes após o primeiro teste (2026-11-24)

- **Capotamento fácil a ~80 km/h.** Os pneus seguram até ~2 g de força lateral (4 rodas × ~4,9 m/s² no
  teto do clamp). Com centro de massa a 0,55 m e meia-bitola de 0,85 m, o limite de tombamento é
  0,85/0,55 ≈ 1,5 g. O próprio Driver encurta a alavanca vertical das viaturas
  (`wheelforces.c:497`: `wheelPos[1] * 19/32` ou `* 12/32`). Adotado como `VehicleSpec.tyreForceHeight`
  (padrão 19/32): a força é aplicada na altura `CM + (contato − CM) × fator`.
- **Carro preso de cabeça pra baixo.** Testei desvirar automático; o criador rejeitou (carro capotado
  fica capotado). Removido.
- **Massa passa a importar (desvio intencional do Driver).** No Driver, o empuxo é uma aceleração
  igual para todos os carros, e a massa só entra nas colisões. Decisão do criador: veículo pesado é mais
  lento mesmo com motor mais forte. Agora o empuxo vira força = `thrust × 1350 kg` (`ReferenceMassKg`),
  e a aceleração é `força ÷ massa`. A velocidade máxima cai na mesma proporção, porque o arrasto continua
  sendo aceleração. Pneus (curva/freio travado) e suspensão continuam independentes da massa: carga
  normal maior compensa massa maior, como num pneu real.
- **Características por veículo.** Os valores de `CAR_COSMETICS`/`HANDLING_TYPE` saíram de campos fixos
  e foram para o asset `VehicleSpec`. Guia de uso: `game/Docs/VEHICLES.md`.

## Não portado (ainda)

- 12 pontos de contato da carroceria com impulso e atrito (`StepOneCar`, "lifting"): no porte, o colisor PhysX faz esse papel.
- Colisões carro-carro em RK2 e dano/amassado (`GlobalTimeStep`, `DamageCar3D`).
- Burnout (`wheelspin`), grama/água/areia, chuva (`wetness`), elástico de perseguição (rubber band), estabilidade extra das viaturas.

## Critério de aceitação (a verificar jogando)

- Parado, as rodas encostam no chão e a carroceria fica a ~0,4 m dele.
- Frear em curva faz a traseira escorregar; acelerar estabiliza.
- Freio de mão em velocidade gira o carro; soltando, ele recupera.
- A rampa da cena de teste faz o carro pular e a suspensão absorver sem quicar indefinidamente.

## Limitações conhecidas

- A escala de 0,006 m/U é inferida. Se o carro parecer rápido ou lento demais, ajuste `MetersPerUnit`
  (o que muda todas as forças de forma consistente) em vez de mexer em constantes isoladas.
- Validado jogando pelo criador em 2026-11-24 (primeiro teste). Os ajustes de capotamento ainda precisam de um novo teste.
