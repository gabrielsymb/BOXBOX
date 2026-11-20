# Godot 4.7 — ocupação e admissão de cruzamentos

Data da pesquisa: 2026-11-20  
Engine: Godot 4.7.2 .NET, GL Compatibility, Jolt Physics

## Fontes oficiais

- `https://docs.godotengine.org/en/4.7/classes/class_area3d.html`
- `https://docs.godotengine.org/en/4.7/classes/class_physicsshapequeryparameters3d.html`
- `https://docs.godotengine.org/en/4.7/classes/class_physicsdirectspacestate3d.html`

## Hipótese causal

O veículo deixava de consultar o semáforo depois da linha de parada, mas não havia reserva, ocupação ou verificação da saída. Quatro curvas à esquerda podiam entrar juntas; os sensores centrais então zeravam a velocidade mutuamente.

## Contrato implementado

- **Estados:** aproximação, espera por admissão, interior da junção e saída.
- **Ação:** `TryEnter(veículo, eixo, direção de saída)`.
- **Pré-condições:** sinal não vermelho; caixa de saída livre para um veículo; prioridade resolvida para curva à esquerda oposta; nenhuma reserva ou ocupação autônoma incompatível.
- **Conclusão:** `Area3D.body_exited` libera admissão, ocupação e temporizador de recuperação.
- **Timeout:** reserva não ocupada expira após 6 segundos e informa o veículo.
- **Invariantes:** veículo autônomo só entra após admissão; espera não consome a reserva de outro; sensor de um veículo dentro ignora outros veículos dentro; toda reserva termina por entrada/saída ou expiração.
- **Progresso:** com sinalização justa e saída eventualmente livre, cada uma das quatro aproximações da fixture deixa o cruzamento em até 120 segundos.

Os valores espaciais foram derivados da geometria atual do BOXBOX: linha de parada a 4,2 m, carro com aproximadamente 3,1 m de comprimento e caixa central de 5 m. A primeira tentativa usou caixa de 7,6 m e foi rejeitada pelo teste porque classificava veículos parados na linha como ocupantes.

## Resultado

```text
INTERSECTION_TEST resumo deadlocks=0 carros_atravessaram=4/4 duracao=120.0s
```

O resultado se repetiu duas vezes com as seeds `100, 200, 300, 400`. A execução renderizada do mundo aberto mostrou sucessivas sequências `JUNCTION_ENTER_GRANTED → JUNCTION_INSIDE → JUNCTION_EXIT`, sempre com contagem máxima observada de um veículo e sem novo `DEADLOCK` durante a janela inspecionada.

## Limitações

A admissão serial é deliberadamente conservadora e pode reduzir vazão em tráfego intenso. A etapa atual prova segurança e progresso; admitir trajetórias simultâneas comprovadamente não conflitantes fica para otimização posterior. O ciclo semafórico e a fase exclusiva de pedestres não foram alterados nesta etapa.
