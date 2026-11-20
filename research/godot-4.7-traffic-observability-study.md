# Godot 4.7 — observabilidade de trânsito

Data da pesquisa: 2026-11-20  
Engine: Godot 4.7.2 .NET, GL Compatibility, Jolt Physics

## Fontes oficiais consultadas

- `https://docs.godotengine.org/en/4.7/classes/class_label3d.html`
- `https://docs.godotengine.org/en/4.7/tutorials/inputs/inputevent.html`
- `https://docs.godotengine.org/en/4.7/classes/class_physicsrayqueryparameters3d.html`
- `https://docs.godotengine.org/en/4.7/tutorials/editor/command_line_tutorial.html`

## Conclusão adotada

O diagnóstico por veículo usa `Label3D` em billboard, tamanho fixo e inicialmente oculto; F1 é tratado como evento de teclado não repetido. O raycast mantém a exclusão do RID do próprio veículo e lê o objeto retornado apenas para identificar textualmente o colisor. A cena isolada é executada por caminho explícito, em modo headless e fixed-fps para acelerar 120 segundos simulados sem depender do tempo de parede.

## Hipótese causal e aceitação

Hipótese: quatro veículos forçados a virar à esquerda podem parar mutuamente porque o sensor central zera a velocidade, enquanto o controlador deixa de considerar o sinal após a linha de parada e não representa ocupação/prioridade dentro da junção.

Aceitação desta etapa: o teste imprime ao menos um episódio reproduzível, sem repetição contínua por veículo enquanto a condição persiste, e termina com `deadlocks=<n>` e `carros_atravessaram=<n>/4`. Esta etapa observa; ela não corrige a causa.

## Limitações

`Label3D` é overlay técnico e usa a fonte padrão apenas nesta cena/visualização interna; não é asset final de apresentação. Headless valida lógica, física e logs, mas não substitui inspeção visual do overlay F1 nem valida composição gráfica.

## Resultado observado

A execução de 120 segundos simulados registrou quatro episódios e nenhuma travessia completa:

```text
INTERSECTION_TEST resumo deadlocks=4 carros_atravessaram=0/4 duracao=120.0s
```

Uma execução renderizada separada confirmou que os quatro `Label3D` exibem todos os campos pedidos. A captura técnica foi salva pelo modo de teste em `user://intersection-debug-overlay.png`; ela não é asset do jogo.
