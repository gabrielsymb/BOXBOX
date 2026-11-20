# Roadmap de trânsito e pedestres

Data: 2026-11-20

Este plano converte os experimentos propostos em etapas pequenas e verificáveis. Cada etapa só avança quando a cena de teste correspondente produz evidência reproduzível; números de balanceamento continuam hipóteses até medição no BOXBOX.

## Ordem de implementação

1. **Linha de base observável.** Exibir por veículo estado, velocidade, tempo bloqueado, colisor do sensor e fase/eixo do cruzamento; registrar episódios de deadlock; executar quatro curvas à esquerda por 120 segundos. Saída: total de deadlocks e carros que atravessaram. Nenhuma correção comportamental entra nesta etapa.
2. **Máscaras de colisão do sensor — concluída.** As camadas foram nomeadas e o cenário estático deixou de ser interpretado como agente móvel pelo sensor. O teste isolado ignorou `ScenarioPoleProbe` e detectou `DynamicVehicleProbe`; a linha de base permaneceu em quatro deadlocks causados por outros carros, isolando a próxima correção na regra da junção.
3. **Admissão e ocupação da junção — concluída.** O fluxo representa `Approaching → WaitingForAdmission → InsideJunction → Exited`, reserva a passagem, verifica espaço de saída e aplica prioridade estável a curvas à esquerda opostas. A primeira fatia admite conservadoramente um veículo autônomo por vez; se agentes externos colocarem dois dentro e ambos pararem por mais de 2 segundos, o menor `InstanceId` recebe prioridade de recuperação. Resultado confirmado em duas execuções: zero deadlock em 120 segundos e `N=4/4` veículos atravessando.
4. **Ciclo semafórico orientado a dados.** Centralizar durações, adicionar limpeza condicionada à ocupação e uma fase exclusiva de pedestres. Contar ocupação por faixa. Critério: pedestre só inicia na fase autorizada, carros não invadem faixa ocupada e a cena permanece sem deadlock.
5. **Ciclo de vida dos pedestres.** Spawn sempre fora do enquadramento, pontos semanticamente admissíveis, lista mantida pelo spawner e despawn por tempo fora da tela mais distância. Critério: logs reproduzíveis de spawn/despawn e nenhum nascimento visível.
6. **Rota, densidade e streaming de tráfego.** Separar rota global de direção local; controlar densidade; criar apenas em trechos admissíveis fora da câmera; usar estados de despawn. Esta etapa amplia o teste isolado para células contíguas sem misturar o agente persistente com sua projeção 3D.
7. **Flicker de sombra, uma variável por execução.** Rodar A–E separadamente e registrar captura/resultado: congelamento de câmera/sol, quantização solar, interpolação da câmera, distância de sombra e, por último, bias/z-fighting. Nenhuma conclusão será marcada sem inspeção visual do usuário.

## Contrato mínimo da junção (etapas 3 e 4)

- **Ator:** veículo ou pedestre visual próximo.
- **Pré-condições:** rota válida, sinal permissivo, saída disponível e conflito admissível.
- **Conclusão:** o ator deixa a área de ocupação e libera toda reserva.
- **Timeout:** espera fora da junção gera diagnóstico; dentro dela aciona recuperação limitada e auditável.
- **Invariantes:** entrar requer autorização; reserva não cria espaço; pedestre ativo bloqueia a faixa correspondente; toda reserva termina, falha ou expira; seed e sequência iguais reproduzem decisões iguais.
- **Progresso:** sob sinalização justa e saída eventualmente livre, cada veículo admitido deixa a junção dentro do horizonte do teste ou produz um terminal diagnóstico explícito.

## Validação visual pendente

O resultado automatizado de admissão foi confirmado na fixture, mas a etapa 3 ainda requer vídeo ou sequência temporal do mundo aberto. Captura isolada não comprova fluidez, ausência de colisões ou deadlocks. Sombras exigem vídeo para avaliar flicker. Não tratar os resultados locais como aprovação visual global.

## Evidência exigida por etapa

- `dotnet build` sem erros;
- cena isolada executada pela Godot 4.7.2 com seed registrada;
- resumo textual no console;
- inspeção visual real para mudanças de câmera, sombra, escala, colisão e composição;
- registro separado de confirmado, hipótese e questão aberta.

## Linha de base confirmada — etapa 1

Com Godot 4.7.2, fixed-fps de 60 e seeds `100, 200, 300, 400`, a cena isolada terminou em 120 segundos simulados com `deadlocks=4` e `carros_atravessaram=0/4`. Cada seed produziu um único log de episódio, e os quatro bloqueadores reportados eram outros veículos. O teste confirma a falha sem ainda atribuir a correção a uma causa única.
