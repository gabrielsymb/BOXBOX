# Veículos — como funcionam e como criar um novo

Estado: protótipo jogável na `SandboxLab` (2026-11-24).
Estudo técnico completo (fórmulas, unidades, o que foi e o que não foi portado):
[`research/redriver2-vehicle-handling-study-2026-11-24.md`](../../research/redriver2-vehicle-handling-study-2026-11-24.md)

## Regra do projeto

**Cada veículo tem a própria velocidade, peso e características.** Nada de valor de handling fixo
no código: tudo que diferencia um carro de outro fica num asset `VehicleSpec`.

## Arquivos

| Arquivo | Papel |
|---|---|
| `Assets/BoxBox/Presentation/Vehicles/VehicleSpec.cs` | ficha do veículo (o `CAR_COSMETICS` do Driver) |
| `Assets/BoxBox/Presentation/Vehicles/VehiclePhysicsController.cs` | física portada do Driver 2 (suspensão, pneus, tração, freio, volante) |
| `Assets/BoxBox/Presentation/Vehicles/VehicleInputAdapter.cs` | teclado → comandos do carro |
| `Assets/BoxBox/Presentation/Vehicles/VehicleChaseCamera.cs` | câmera de perseguição |
| `Assets/BoxBox/Presentation/Vehicles/SandboxVehicleBuilder.cs` | monta carro, câmera, rampa e slalom na cena de teste |

## Criar um veículo novo

1. Na Unity: `Assets > Create > BOXBOX > Vehicle Spec`. Salve em `Assets/BoxBox/Content/` (ex.: `Caminhonete.asset`).
2. Preencha os valores (tabela abaixo).
3. Na `SandboxLab`, selecione o objeto com `SandboxVehicleBuilder` e arraste o asset para **Vehicle Spec**.
4. Aperte Play e ajuste.

## O que cada valor faz

| Campo | Efeito | Referência |
|---|---|---|
| `massKg` | mais pesado = acelera menos, tem velocidade máxima menor e empurra mais nas batidas. A aderência nas curvas e no freio não muda com o peso, como num pneu real | 1350 = carro médio |
| `centerOfMass` | mais alto = rola e capota mais fácil | 0.55 m |
| `powerRatio` | força do motor. Aceleração = força ÷ peso | 1.0 com 1350 kg ≈ 133 km/h, 0–100 em ~3,5 s |
| `traction` | aderência nos dois eixos | 1.0 |
| `frictionScaleRatio` | aderência geral do tipo de handling | jogador 1.0; perseguidores do Driver 1.4–2.1 |
| `tyreForceHeight` | altura em que a força do pneu age; menor = capota menos | 0.59 (viatura do Driver); 1.0 = físico puro |
| `maxSteerDegrees` / `highSpeedSteerDegrees` | ângulo máximo parado / a partir de `highSteerSpeedKmh` | 31° / 10° a 110 km/h |
| `steerRateDegPerSec` | velocidade do volante segurando A/D | 40°/s |
| `fastSteerDegrees` / `fastSteerRateDegPerSec` | Shift+A/D | 45° a 170°/s |
| `bodySize`, `groundClearance` | caixa de colisão da carroceria e vão livre | 1.85×1.0×4.4, 0.4 m |
| `wheelRadius`, `track`, `wheelbase` | roda, bitola e entre-eixos | 0.35, 1.7, 2.7 m |

Ideias de perfil (ainda não testadas):
- **Caminhonete carregada:** `massKg` 2200, `powerRatio` 1.3 (motor mais forte), `centerOfMass.y` 0.75, `traction` 0.9 → acelera ~80% do carro médio.
- **Caminhão:** `massKg` 3000, `powerRatio` 1.6 → motor 60% mais forte, mas acelera ~72% do carro médio (~96 km/h de máxima).
- **Carro popular:** `massKg` 1000, `powerRatio` 0.85, `bodySize` 1.7×1.0×3.9.
- **Esportivo:** `powerRatio` 1.3, `traction` 1.15, `centerOfMass.y` 0.45, `groundClearance` 0.3.

## Controles (teclado)

W acelera · S freia/ré · A/D esterça (toque = curva suave, segurar = mais fechada; vira menos em alta velocidade) · Shift+A/D curva fechada rápida · Espaço freio de mão · C troca câmera · R reseta (só na SandboxLab, ferramenta de teste)

## Histórico de testes

**2026-11-24 — primeiro teste jogando (criador):**
- Deixou de ser "uma lata com roda": suspensão perceptível, carro voa na rampa, desliza.
- Capotava fácil: a ~80 km/h, esterçando com W+A/D, ele virava. Causa: o pneu segura até ~2 g, mas
  com centro de massa a 0,55 m e bitola de 1,7 m o carro tomba a partir de ~1,5 g.
  Correção: `tyreForceHeight` = 19/32, o mesmo truque do Driver para as viaturas (`wheelforces.c:497`).
- Ficava de cabeça pra baixo ("tartaruga"). Tentei desvirar sozinho; **rejeitado pelo criador: carro capotado fica capotado**, como na vida real. Removido. Só existe o R da SandboxLab, para teste.
- **Peso tem que pesar:** "um caminhão tem que ser mais lento por ser pesado, apesar de ser mais forte". Mudança: o empuxo do Driver virou força de motor calibrada para 1350 kg (`ReferenceMassKg`), então aceleração = força ÷ massa.
- Toda curva virava "cinematográfica" e repetitiva: era a câmera demorando pra girar. Agora ela fica colada
  atrás do carro e só balança quando ele derrapa (ângulo de deslizamento > 8°, total em 25°).
- Segurar A/D levava o volante ao máximo em 0,4 s, e as curvas saíam fechadas demais. Agora o volante gira
  a 40°/s, vira menos em alta velocidade, e Shift+A/D faz curva fechada.
- **A verificar no próximo teste:** se ainda capota fácil (baixar `tyreForceHeight` para 0.38) ou se
  ficou impossível capotar (subir).
