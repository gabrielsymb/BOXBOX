# Compatibilidade de armas — Mini Characters

Data: 2026-11-19

## Decisão confirmada

O protagonista poderá pegar e disparar armas. Assets existentes podem ser editados no Blender desde que a direção cartoon/toon aprovada seja preservada.

## Evidência técnica

- O rig civil possui sete ossos e é compartilhado pelas variantes.
- O pacote contém `pick-up`, `holding-both-shoot`, `holding-left-shoot` e `holding-right-shoot`.
- O pacote contém `weapon-shooter.glb` na mesma linguagem e escala.
- O rig não possui ossos separados de mão.
- Foi criado um socket de empunhadura derivado do osso do braço, sem modificar os GLBs-fonte.
- A prova visual confirmou leitura frontal da arma nas poses de uma e duas mãos.

## Resultado

As variantes civis continuam elegíveis como base técnica do protagonista. A aparência pode ser editada ou combinada no Blender sem perder as animações existentes, desde que o armature e os pesos compatíveis sejam preservados.

## Questões abertas

- aparência definitiva do protagonista;
- necessidade de recarga, troca de arma, coldre e locomoção armada;
- regras sistêmicas que tornam um disparo válido;
- consequências legais, sociais, físicas e econômicas;
- comportamento de NPCs diante de arma visível ou disparo.
