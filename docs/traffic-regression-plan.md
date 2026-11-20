# BOXBOX — roteiro de regressão de trânsito (branch de teste)

Revisão: 2026-11-20. Escopo: `fix/traffic-pedestrians-2026-11-20`. **Não executar merge automático em `main`.**

## O que o CI realmente prova

O workflow `.github/workflows/boxbox-csharp.yml` compila C# do adaptador Godot e executa os checks .NET do núcleo econômico e do grafo de faixas. Isso valida **compilação e invariantes determinísticas**, **não** a física do Godot, assets, desempenho, desenhos dos semáforos ou aparições na câmera.

Comando local de verificação:

```bash
dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj
```

## Sequência de testes visuais no Godot (.NET 4.7.2)

Teste na **branch de trabalho**, não na `main`. Para cada item, observe por 1–2 minutos, depois se movimente e repita. Registre vídeo curto do problema e a posição/contexto da cena quando alguma situação falhar.

1. **Spawn:** ao andar pelo bairro, carros e pedestres surgem fora da tela, não na frente da câmera. Pedestres não nascem sobrepostos. Carros aparecem em mais de uma direção, cada um na faixa correspondente e apenas sobre ruas já carregadas.
2. **Fila no vermelho:** pare junto a um cruzamento. Dois carros na mesma faixa devem formar uma fila; o de trás não deve tentar ultrapassar deslocando-se lateralmente. Ao abrir verde, devem retomar progressivamente.
3. **Cruzamento perpendicular:** observe carros chegando por ruas distintas ao mesmo tempo. Apenas um carro de IA ocupa o centro do mesmo cruzamento, os demais aguardam e seguem depois. Semáforos visualmente verdes não devem autorizar colisões.
4. **Viradas:** observe carros virando à direita e à esquerda. Eles devem aproximar-se na faixa correta, obedecer ao semáforo da direção **de entrada** e concluir a manobra antes de o próximo carro receber autorização.
5. **Travessia:** pedestres aguardam juntos, mas separados no meio-fio. Começam a atravessar com sinal seguro, e o semáforo para veículos permanece vermelho no sentido protegido enquanto houver travessia.
6. **Susto no meio da faixa:** aproxime um carro rápido ou acione o evento de perigo previsto no protótipo. Um pedestre que foge **não** deve liberar a faixa antes de sair da área da via; o sinal também não deve ficar permanentemente vermelho depois que sair.
7. **Streaming:** passe repetidamente da área detalhada para outra e retorne. O ciclo do semáforo não deve reiniciar; a lógica de prioridade e a reserva de cruzamento não devem desaparecer só porque os postes foram reconstruídos.
8. **Travamento prolongado:** observe a rua por aproximadamente dois minutos. Se algum carro permanecer imóvel ou ficar girando no mesmo lugar, registre vídeo com visão suficiente para identificar obstáculos, a faixa, as luzes e o estado dos outros NPCs.

## Sinais de regressão que ainda exigem correção

- Veículos controlados pelo jogador não seguem necessariamente as regras da IA: podem entrar num cruzamento sem reserva.
- A reserva atual de cruzamento é **conservadora** (um carro de IA por vez), portanto a capacidade de tráfego pode ser menor do que a de uma cidade real.
- Ainda não há ORCA completo, cálculo de filas por interseção, prioridade de conversão por trajetórias conflitantes ou simulação visual fora da região carregada.
- A segurança depende da física e escala real dos assets, que um `dotnet build` não testa.

## Como reportar

Em caso de falha, informe **qual teste**, **o que ocorreu**, **em que momento do semáforo**, e anexe um vídeo de 10–20 segundos se possível. Uma descrição reproduzível é mais útil do que mudar parâmetros de velocidade ao acaso.

Backup pré-correções: `backup/pre-traffic-fixes-2026-11-20`. Discussão e código: [PR #1](https://github.com/gabrielsymb/BOXBOX/pull/1).
