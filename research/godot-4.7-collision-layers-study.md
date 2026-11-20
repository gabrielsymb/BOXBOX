# Godot 4.7 — camadas do sensor de tráfego

Data da pesquisa: 2026-11-20  
Engine: Godot 4.7.2 .NET, GL Compatibility, Jolt Physics

## Fontes oficiais

- `https://docs.godotengine.org/en/4.7/tutorials/physics/physics_introduction.html#collision-layers-and-masks`
- `https://docs.godotengine.org/en/4.7/classes/class_collisionobject3d.html`
- `https://docs.godotengine.org/en/4.7/classes/class_physicsrayqueryparameters3d.html`

## Hipótese e decisão

O raycast de tráfego usava a máscara padrão de todas as camadas, portanto qualquer `StaticBody3D` próximo podia virar obstáculo semântico. A correção mantém colisão física e percepção como consultas diferentes: o corpo do carro colide com `Cenario` e atores, enquanto o raio percebe somente camadas de atores móveis. Um teste isolado coloca um corpo de cenário mais perto e um veículo mais longe no mesmo raio; o aceite exige que o segundo seja identificado.

## Limitações

A documentação oficial alerta que a física da Godot não é deterministicamente idêntica em todas as condições e plataformas. As seeds estabilizam escolhas e fixtures do BOXBOX, mas os testes físicos validam a versão, backend e ambiente declarados; não transformam Jolt em parte determinística da simulação econômica canônica.

## Resultado

```text
TRAFFIC_SENSOR_MASK_TEST resultado=PASS detectado=DynamicVehicleProbe cenario_ignorado=true
```

Depois da mudança, o `IntersectionTest` manteve o contraexemplo original em `deadlocks=4` e `carros_atravessaram=0/4`, e todos os campos `bloqueado_por` continuaram apontando para veículos. Isso demonstra que a máscara remove o ruído de cenário sem esconder o deadlock real que será tratado pela ocupação da junção.
