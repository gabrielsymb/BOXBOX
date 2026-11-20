# Guardrails de arquitetura

## Direção de dependências

```text
Apresentação/Engine ──> Aplicação/Simulação ──> Domínio
Infraestrutura ───────> Portas definidas nas camadas internas
```

O domínio não importa engine, UI, banco, serialização nem runtime de inferência. A simulação pode ser executada em modo headless. Adaptadores externos dependem de contratos internos, nunca o contrário.

## Responsabilidades

### Domínio puro

- dinheiro, produto, SKU e lote de estoque;
- venda, compra, fornecedor, preço, prazo e validade;
- livro-caixa, contas a pagar/receber e fechamento;
- capital de giro, margem e indicadores derivados;
- loja, capacidade e horário;
- funcionário, função, custo e produtividade;
- crédito, parcelas, juros, atraso e inadimplência;
- mercado local, concorrente agregado, alternativa de compra e escolha confirmada;
- campanha promocional, saúde de estoque, sinal e conhecimento de mercado;
- obrigação, declaração, achado fiscal, regularização e ativo liquidável;
- locais de caixa, transferências, sangria e conciliação;
- perdas físicas, incidentes, evidências e controles de segurança;
- apólice, cobertura, prêmio, sinistro, franquia e indenização;
- perfil tributário versionado;
- insolvência e outros estados terminais.

Use centavos inteiros ou decimal exato para dinheiro; nunca `float`. Não mantenha saldo, lucro e indicadores como verdades independentes quando puderem ser derivados dos lançamentos.

### Aplicação

Coordena casos de uso como abrir e fechar a loja, definir horário, comprar, receber, precificar, criar/encerrar promoção, observar concorrente, confirmar escolha de cliente, pagar/regularizar obrigação, descartar lote, vender ativo, realizar sangria, conciliar, contar estoque, investigar diferença, contratar, tomar/pagar crédito, contratar seguro, comunicar sinistro e avançar a simulação. Funcionários autônomos usam os mesmos casos de uso; eles não alteram dinheiro ou estoque por fora.

### Simulação

Mantém relógio soberano, agenda ordenada de eventos, escala de avanço, gerador pseudoaleatório explícito, políticas de demanda/mercado, processamento de comandos, snapshots e publicação de eventos. O tick visual anima projeções; não autoriza transações.

O pipeline de decisão separa validade de preferência:

```text
estado do mundo
→ gerar ações candidatas
→ filtrar por pré-condições e restrições
→ calcular pesos somente das ações válidas
→ selecionar com aleatoriedade semeada
→ enviar comando ao domínio
→ validar e calcular estado/eventos propostos
→ verificar invariantes globais e pós-condições
→ confirmar estado/eventos atomicamente, ou descartar tudo
→ registrar decisão e explicação
```

Regras e conteúdo podem ser declarativos, mas não alteram o estado diretamente. Eles referenciam comandos tipados da aplicação ou do domínio. A política recebe um conjunto válido e imutável e apenas modifica preferências; não acrescenta nem remove ações.

Locks, reservas, lançamentos e eventos fazem parte da mesma transação lógica. Recursos são adquiridos atomicamente em uma ordem definida e liberados em sucesso, rejeição, interrupção ou timeout. Falha de validação preserva integralmente o estado anterior.

### Portas e adaptadores

Defina contratos para repositórios, save, configuração, relógio, PRNG, inferência, telemetria, localização, serviços de plataforma e engine. Motores de IA começam como módulos plugáveis no mesmo processo, recebem observações permitidas e ações candidatas válidas e retornam propostas; não mutam o domínio. Adaptadores implementam banco/arquivo, runtime do modelo, catálogos de idioma, lojas escolhidas e apresentação concreta. Separar um motor em outro processo é uma opção futura atrás do mesmo contrato, não requisito inicial.

### Apresentação

Renderiza projeções ou view models e transforma entradas do jogador em comandos. NPCs visuais representam o estado econômico; animações não criam vendas, clientes ou estoque.

A apresentação também resolve chaves de localização, pluralização e formatação. O domínio fornece códigos semânticos e valores estruturados, nunca frases prontas.

## Fluxo de decisão

```text
estado atual
→ extração de atributos
→ política ou modelo escolhe intenção dentro do conjunto válido
→ aplicação envia comando
→ domínio calcula estado e eventos propostos
→ registro global verifica invariantes
→ commit atômico atualiza estado, agenda e eventos
→ projeções consomem eventos confirmados
→ persistência registra snapshot e log relevante
```

## Invariantes mínimas

- nenhuma venda baixa estoque inexistente;
- toda entrada ou saída de dinheiro gera lançamento;
- dinheiro e mercadoria não aparecem ou somem sem evento explicável;
- contratos, parcelas, tributos e validade seguem regras versionadas;
- a economia independe de FPS e do relógio de parede;
- a mesma versão, configuração, seed e sequência de comandos produz o mesmo resultado;
- modelos probabilísticos sugerem intenções, mas o domínio pode rejeitar a ação;
- idioma e integração com lojas não alteram o resultado econômico da simulação;
- sangria conserva o total de dinheiro e registra origem, destino, valor, horário e responsável;
- furto ou roubo altera o ativo físico por um evento auditável, mesmo quando ainda não foi revelado ao jogador;
- uma indenização nunca excede as regras, limites e perdas cobertas pela apólice vigente;
- pesos finais de seleção são finitos e não negativos, e probabilidades permanecem entre zero e um;
- todo estado não terminal oferece comando válido, evento futuro ou terminal explícito;
- toda transição aceita preserva invariantes globais e possui causa registrada;
- cada oportunidade de compra é confirmada no máximo uma vez;
- rumor altera conhecimento, nunca o fato econômico ou fiscal;
- promoção não alcança lote vencido nem existe sem início/fim;
- ativo vendido não concede capacidade e não é vendido novamente;
- irregularidade declarada não altera verdade econômica, cria caixa ou cancela obrigação;
- trocar escala ou FPS não muda eventos ou resultado econômico.

Invariantes globais vivem em um registro central obrigatório e são verificadas em todo commit. Transições e conteúdo podem adicionar asserções locais, mas nunca escolher ou desabilitar leis globais.

## Tempo e retorno do jogador

Modele `SimulationClock` separadamente do tempo real. A escala funciona como zoom sobre uma única linha do tempo: pausa, detalhado, acelerado e salto até decisão processam a mesma agenda. Horários, clientes, promoções, fofocas, entregas, validade, vencimentos e fiscalizações vivem no tempo simulado.

Eventos empatados usam fase e id causais/estáveis. Aleatoriedade deriva de seed/evento, não de frame. Recuperação offline reutiliza a mesma agenda e reúne decisões simultâneas numa barreira conjunta de auditoria, sem congelar o mundo. Somente atores com capacidade, informação, autoridade e recursos podem agir; sem ator capaz, a tarefa falha, expira ou acumula consequência conforme seu contrato temporal. Evento derivado não retorna à fase já processada, e ação não delegável nunca é aplicada por inferência.

## Persistência e replay

Repositórios são interfaces internas. Adaptadores convertem entre objetos de domínio e registros. Um save reproduzível guarda, conforme necessário:

- horário e estado atual;
- estado do PRNG;
- eventos agendados;
- versão e estado do modelo local;
- versão do conjunto de regras e conteúdo;
- estado econômico ou snapshot;
- log de comandos e eventos relevantes desde o snapshot.

Não é obrigatório adotar event sourcing completo. Snapshots periódicos mais um diário append-only das movimentações e eventos importantes oferecem auditoria suficiente para replay, migração e diagnóstico.

Não persista frases traduzidas no save. Guarde identificadores, códigos e valores; textos devem refletir o catálogo da versão em execução. Preferências do usuário, como idioma, ficam separadas do estado econômico quando isso simplificar migração e sincronização.

## Localização e distribuição

Português do Brasil é o locale padrão e inglês é o segundo locale. Strings visíveis ficam em catálogos externos ao domínio. Não monte frases por concatenação nem codifique regras distintas por idioma. Formatação de BRL, números, datas e horários pertence à camada de apresentação.

Trate qualquer loja como um adaptador opcional de plataforma. Conquistas, estatísticas, saves em nuvem, overlay e presença não podem ser chamados diretamente pelo domínio ou pela simulação. Desenvolva contra uma porta de serviços de plataforma e mantenha uma implementação local/nula para testes e execução sem cliente. A Steam pode ser o primeiro adaptador sem se tornar dependência estrutural do BOXBOX.

## Desempenho

- Use eventos discretos ou ticks econômicos bem definidos, separados do frame gráfico.
- Durante picos, simule clientes leves ou coortes e renderize apenas uma amostra coerente.
- Prefira dados compactos e atualizações incrementais a recálculos globais por frame.
- Mantenha a inferência atrás de uma porta e permita uma política heurística de fallback.
- Torne a qualidade visual escalável sem mudar o resultado econômico.

## Estrutura indicativa

```text
domain/
application/
simulation/
decision-models/
market/
compliance/
scheduling/
rules/
  schema/
  validator/
ports/
adapters/
  persistence/
  inference/
  engine/
  localization/
  platform/
presentation/
content/
locales/
tests/
verification/
  reachability/
  liveness/
  property-tests/
  scenario-runner/
```

Adapte os nomes ao ecossistema escolhido. Preserve as responsabilidades e a direção de dependência, não a árvore literal.

## Estratégia de testes

- testes unitários para invariantes do domínio;
- testes de propriedade para conservação de dinheiro e estoque;
- replays com seed e comandos fixos;
- testes de contrato para persistência, engine e inferência;
- simulações em lote para balanceamento;
- testes de migração de save;
- verificação do determinismo entre execuções suportadas;
- testes de cobertura de chaves, paridade de placeholders e pluralização entre pt-BR e inglês;
- testes de contrato de cada adaptador de loja contra uma implementação local/nula;
- testes de conservação de dinheiro em sangrias e depósitos;
- testes de diferença entre saldo registrado e caixa físico;
- replays de incidentes ocultos, descoberta, conciliação e sinistro;
- testes de contrato e limites de apólices e indenizações;
- conservação da demanda entre vendas, desistências e não atendimento;
- contratos de promoção, conhecimento/rumor e concorrente sem estado privado;
- obrigações, validade, liquidação de ativo e recuperação financeira;
- equivalência de logs entre escalas e FPS diferentes;
- ordem canônica, barreiras simultâneas e rejeição de retorno a fase temporal concluída;
- venda de ativo com aceite, timeout, transferência, caixa e remoção de capacidade;
- irregularidade intencional bloqueante sem alteração da verdade econômica;
- validação estática de referências, tipos, intervalos, sobreposição, contradição e recuperação;
- análise limitada de alcançabilidade, quiescência, deadlocks, ciclos de tempo zero, livelocks e conteúdo órfão;
- propriedades de progresso com ator, tipo de garantia, medida, horizonte, condições de execução justa e pesos finais;
- contratos temporais de ações com duração/conclusão, timeout, interrupção e liberação de recursos;
- terminais escopados para tarefa, incidente, dia comercial e empresa;
- testes de aquisição e liberação de locks em sucesso, rejeição, interrupção e timeout;
- testes baseados em propriedades e metamórficos;
- simulações em lote por seed para estratégias dominantes, eventos raros e soft locks econômicos.
