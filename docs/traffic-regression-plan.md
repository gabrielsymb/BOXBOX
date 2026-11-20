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

## Teste específico de desvio recíproco (RVO2 / família ORCA)

O solver de desvio local foi ativado **somente nos pedestres**. O grafo de calçadas, a preferência de rota e os controladores de semáforo continuam responsáveis pelos caminhos e pelas permissões de travessia. RVO não constrói um NavMesh e não substitui obstáculos físicos.

- Observe dois NPCs caminhando em sentidos opostos no mesmo trecho de calçada: eles devem antecipar o encontro, reduzir a velocidade ou desviar, sem atravessar paredes ou entrar na rua.
- Observe pedestres parados (atividade, susto, espera na faixa): os demais devem contorná-los quando houver espaço.
- Observe um grupo próximo ao sinal: ninguém deve abandonar o meio-fio para entrar numa faixa com veículos em movimento.
- Observe um pedestre assustado na travessia: a regra do semáforo e a rota marcada têm prioridade sobre a sugestão de desvio local.
- Para comparar, na árvore **Remote** do Godot selecione `PedestrianSpawner` e alterne `EnableReciprocalAvoidance` entre ligado e desligado. Esse controle vale para os pedestres já carregados e para os próximos. Registre vídeo curto com cada configuração.
- Se os NPCs pararem todos no mesmo ponto, colidirem na calçada ou andarem fora da rota, registre o caso: compilação C# não testa o servidor de navegação durante execução real.

## Escala dos assets preservada

**Esta rodada não muda o tamanho de nenhum personagem, veículo, esqueleto ou GLB.** O atributo `Radius` de `NavigationAgent3D` delimita espaço de prevenção de colisões, não redimensiona o modelo visível.

Há vários assets no repositório, incluindo `Assets/World/Props/bench.glb`, duas variantes de `furniture-bench-cushion` e duas cercas (`fence-low.glb`, `construction-fence.glb`).

A dimensão dos bancos **já é normalizada por código** em `UrbanLotDecorator.AddAsset` via `VisualShadowSupport.NormalizeHorizontalLength(instance, 1.25f)`. As cercas ainda usam fatores fixos, por exemplo `0.72f` e `0.8f`, nas chamadas a `AddAsset`. Portanto, é possível corrigir boa parte das proporções de móveis e cercas por C# ou parâmetros de cena **sem abrir o Blender**; Blender só passa a ser necessário se a geometria original, pivô ou transformações importadas estiverem inadequados. Antes de mexer nos fatores, medir a malha carregada e comparar com o espaço real disponível em uma célula de 4 unidades.

## Sinais de regressão que ainda exigem correção

- Veículos controlados pelo jogador não seguem necessariamente as regras da IA: podem entrar num cruzamento sem reserva.
- A reserva atual de cruzamento é **conservadora** (um carro de IA por vez), portanto a capacidade de tráfego pode ser menor do que a de uma cidade real.
- Há desvio recíproco RVO2 para pedestres, mas não um planejador ORCA completo para multidões, carros, obstáculos estáticos ou regiões descarregadas; não há cálculo de filas por interseção nem prioridade de conversão por trajetórias não conflitantes.
- A segurança depende da física e escala real dos assets, que um `dotnet build` não testa.

## Como reportar

Em caso de falha, informe **qual teste**, **o que ocorreu**, **em que momento do semáforo**, e anexe um vídeo de 10–20 segundos se possível. Uma descrição reproduzível é mais útil do que mudar parâmetros de velocidade ao acaso.

Backup pré-correções: `backup/pre-traffic-fixes-2026-11-20`. Discussão e código: [PR #1](https://github.com/gabrielsymb/BOXBOX/pull/1).
