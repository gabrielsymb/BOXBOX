# Linha do tempo viva e agendamento

## Persistência temporal confirmada

O mundo comercial continua avançando sem o jogador e não possui pausa econômica controlada pelo usuário. O save ancora o último instante de parede observado ao `SimulationClock`; no retorno, uma política temporal versionada converte o intervalo ausente e a mesma agenda recupera o mundo.

O relógio de parede mede quanto recuperar, mas não ordena transações. O `SimulationClock` é a autoridade econômica, usa tempo inteiro/exato e nunca retrocede. A razão real/simulado permanece para balanceamento e não precisa ser `1:1`.

```text
wall elapsed
→ temporal policy
→ simulation target
→ deterministic schedule
→ existing actors and local AI
→ atomic commits
→ knowledge-aware return summary
```

Detalhe visual, processamento em lote e coortes podem reduzir custo, mas execução contínua e recuperação do mesmo intervalo precisam produzir o mesmo hash econômico canônico.

## Mundo independente do protagonista

Concorrentes continuam operando e evoluindo. A empresa do jogador só executa uma tarefa quando existe pessoa, contrato, automação ou política com capacidade, informação, permissão e recursos. Funcionários e gestores podem acertar, errar, ser enganados, furtar ou falhar; ausência não cria um administrador perfeito.

Sem ator capaz, a tarefa não ocorre e o tempo continua: loja pode não abrir, estoque pode acabar, obrigação pode atrasar e empresa pode falir. Falência encerra a empresa do jogador, não o mercado persistente.

O protagonista não vira agente automático quando o aplicativo fecha. Somente tarefas já iniciadas/agendadas e operação realmente delegada continuam; sem equipe ou automação, a loja pode ficar fechada enquanto o bairro avança.

Fato, registro e conhecimento permanecem separados. Perda ou golpe pode existir antes de o jogador descobrir. Auditoria preserva o evento; apresentação revela somente o conhecimento permitido.

## Agenda e autoridade

`ScheduledEvent` carrega id estável, instante, fase, tipo/payload, fonte, versão, chave de aleatoriedade e nível de atenção. Empates usam a ordem canônica. Eventos-filhos não voltam a fase concluída; ids são causais e únicos; aleatoriedade deriva de seed/evento, nunca de frame ou login.

Um agente autorizado ainda passa por passabilidade, contrato temporal, invariantes e commit atômico. Autoridade não garante boa decisão. Contrato desfavorável ou golpe pode ocorrer dentro das ações válidas; conteúdo inválido nunca atravessa o validador determinístico. Ações não delegáveis, como irregularidade intencional, permanecem indisponíveis para IA.

Barreiras simultâneas agrupam decisões para consistência e resumo, sem pausar globalmente a linha do tempo. Incidente offline não exige reflexo do jogador; segurança de pessoas é padrão em roubo, sem impedir prejuízo econômico.

## Invariantes mínimas

- relógio simulado monotônico; relógio local regressivo não desfaz eventos;
- evento não executa cedo nem duas vezes;
- avanço contínuo e recuperação equivalente reconciliam;
- batching/coortes não perdem nem duplicam efeitos;
- ausência não cria ator, conhecimento, autoridade ou recurso;
- agente autorizado não ignora guardas nem invariantes;
- fato oculto continua auditável sem revelação prematura;
- entrar mais vezes não cria vantagem para o mesmo intervalo e condições;
- falência do jogador não interrompe concorrentes nem o mercado persistente;
- save no meio da recuperação retoma idempotentemente ou entra em estado degradado explícito.

Teste sete dias contínuos contra uma recuperação única de sete dias, com mesma seed, estado, atores, políticas, comandos e âncoras. Inclua funcionário falho, furto oculto, golpe válido, obrigação, concorrente e falência offline; após a falência, prove que o rival continua avançando.
