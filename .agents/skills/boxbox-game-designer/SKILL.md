---
name: boxbox-game-designer
description: Conduz o design, a arquitetura e a evolução do BOXBOX, simulador bilíngue de microcomércio MEI para PC. Use para transformar ideias do jogo em GDD, loop, MVP, economia, IA local, localização, distribuição, direção visual, plano técnico ou código; não use para jogos sem esse contexto.
---

# BOXBOX Game Designer

Ajude a transformar o BOXBOX em um jogo de gestão divertido, legível, leve e implementável. Preserve a fantasia de começar operando uma pequena venda e evoluir até gerir pessoas, capital e estratégia.

## Preserve estas decisões

- O jogo vem antes da aula. Conceitos de gestão e estatística devem aparecer como ferramentas úteis dentro das escolhas, nunca como um curso interrompendo a partida.
- Preserve a identidade de **systemic game design**, **emergent gameplay** e **persistent world simulation**: componha regras e agentes reutilizáveis, deixe histórias surgirem de interações auditáveis e nunca faça o mundo depender da presença do protagonista para avançar.
- O nome do jogo é **BOXBOX**.
- Trate BOXBOX como nome confirmado do projeto, mas não como marca comercial já liberada: existe um jogo homônimo na Steam. Preserve o nome nas entregas e sinalize a necessidade de validação comercial ou subtítulo antes de página pública, registro ou investimento pesado em identidade.
- Os três segmentos iniciais são mercearia, cosméticos e adega. Cada um precisa mudar as decisões e os riscos, não apenas os nomes e as artes dos produtos.
- A progressão vai de operador a gestor: no início, o jogador faz quase tudo; depois, contrata, delega e decide em nível estratégico.
- O comércio deve produzir histórias emergentes por meio de demanda, horário, sazonalidade, preço, estoque, filas, reputação, crédito e eventos.
- Clientes escolhem entre BOXBOX, concorrentes admissíveis e a opção de não comprar; concorrentes são inicialmente agregados, nomeados e sujeitos às mesmas categorias causais em resolução menor.
- Promoções ajudam a girar produtos encalhados e disputar clientes, mas sacrificam margem, capacidade e podem canibalizar vendas normais.
- Fofocas e observações circulam para jogador e rivais com atraso, confiança e possível distorção; informação nunca altera o fato original.
- Diferencie DAS atrasado, falha declaratória e sonegação intencional. O modo padrão usa abstração MEI versionada, não aconselhamento jurídico.
- O mundo comercial é persistente e não possui pausa econômica controlada pelo jogador. Tempo ausente avança a mesma linha do tempo simulada pelas mesmas regras; a razão exata não precisa ser `1:1`, e velocidade visual ou processamento resumido nunca muda o resultado econômico.
- A direção visual é um diorama isométrico 3D low-poly, com interior de loja aberto ou parcialmente ocultado e possibilidade de mapa em lotes/blocos dentro da mesma linguagem.
- O tom é cômico, leve e autoconsciente. NPCs podem quebrar a quarta parede e brincar com reutilização ou mistura de assets, sem tornar regras econômicas ambíguas.
- O jogo inclui perdas e segurança: funcionários podem furtar, a loja pode sofrer roubo, sangrias controlam a exposição de caixa e seguros mitigam parte do prejuízo.
- Seguranças evoluem com treinamento, escala, equipamento e redundância. Os iniciais têm cobertura e disciplina limitadas; qualquer nível ainda pode se distrair ou ser surpreendido por causas explicáveis.
- O produto final deve rodar localmente em hardware modesto. Não introduza dependência de nuvem como requisito central.
- O BOXBOX é bilíngue desde a fundação: português do Brasil (`pt-BR`) é o idioma padrão e inglês (`en`) é o segundo idioma.
- O lançamento é planejado para PC. A Steam é a candidata principal conhecida, não uma decisão exclusiva ou irreversível; compare lojas quando isso afetar o plano.
- Godot foi escolhida como engine e C# como linguagem de implementação, usando a edição .NET. Godot 4.7.2 Stable é a versão recomendada para iniciar; reavaliar somente se surgir incompatibilidade concreta.
- O desenvolvimento começa pelos motores de IA/comportamento, como módulos plugáveis no mesmo processo e isolados por contratos versionados; processos separados ficam como opção futura.
- Integrações de loja não podem contaminar o domínio nem impedir a execução offline ou sem um cliente específico.
- Não proponha Python para o código do produto. Não reabra engine, linguagem ou a direção de diorama isométrico low-poly sem motivo concreto; ângulo, zoom, oclusão e composição exatos continuam sujeitos a protótipo.
- Blender e MCP são possibilidades para a futura produção de assets, não uma obrigação antecipada.

Leia [references/vision.md](references/vision.md) ao trabalhar em visão, GDD, pilares, loop, progressão, MVP, câmera, arte ou escopo.

## Localize e distribua sem acoplamento

- Trate localização como requisito de arquitetura desde o primeiro vertical slice, não como tradução aplicada ao final.
- Não grave texto visível ao jogador no domínio, nos saves ou nos eventos. Use identificadores semânticos, códigos de motivo e valores estruturados; a apresentação resolve o texto no idioma ativo.
- Evite concatenação de frases. Catálogos devem suportar variáveis, pluralização, formatos de número, moeda, data e hora.
- O negócio continua brasileiro nos dois idiomas. Preserve valores monetários como BRL no domínio e deixe a apresentação formatá-los conforme o locale.
- Implemente Steamworks ou serviços equivalentes por portas e adaptadores opcionais. O jogo principal deve continuar testável e executável sem SDK ou cliente de loja.
- Não escolha uma loja apenas pela porcentagem de receita. Compare alcance provável, descoberta, comunidade, custo de operação, processo de publicação, recursos, adequação ao estágio do jogo e esforço de manutenção.
- Quando a tarefa depender de preços, regras ou recursos atuais de uma loja, consulte a documentação oficial vigente antes de especificar detalhes.

Leia [references/localization-and-distribution.md](references/localization-and-distribution.md) ao trabalhar em textos, UI, conteúdo, idiomas, formatação, fontes, lojas, conquistas, saves em nuvem, metadados ou empacotamento.

## Respeite as fronteiras técnicas

Lógica de domínio, simulação, persistência, inferência e interface são componentes distintos. A interface nunca é autoridade sobre dinheiro, estoque, tempo ou vendas. O domínio e a simulação não conhecem engine, banco, formato de save ou runtime de IA.

Interprete "nada hardcoded" como resultados emergentes, conteúdo orientado a dados e políticas substituíveis. Não entregue invariantes contábeis a um modelo probabilístico: caixa, baixa de estoque, tributos, juros, validade, contratos e insolvência precisam ser deterministas, auditáveis e testáveis.

Leia [references/architecture.md](references/architecture.md) antes de propor arquitetura, código, estrutura de projeto, persistência, integração com engine/modelo local ou estratégia de testes.

## Modele a simulação com explicabilidade

- Use um relógio próprio da simulação; nunca acople a economia ao FPS ou diretamente ao relógio real.
- Torne a aleatoriedade explícita e semeável. A mesma versão, seed e sequência de comandos deve reproduzir o mesmo resultado.
- Registre eventos e causas suficientes para explicar lucro, prejuízo, ruptura, desperdício, contratação, crédito e quebra.
- Use IA leve para intenções e preferências, não para validar as leis do mundo.
- Motores de IA recebem observações permitidas e ações candidatas válidas, retornam propostas e nunca mutam o domínio diretamente.
- Mantenha uma política heurística de fallback para o jogo continuar executável e testável sem o modelo.
- Modelos podem propor ofertas de crédito ou seguro, mas um validador determinístico de contratos aceita ou rejeita campos, faixas, cronologia e cláusulas antes de criar qualquer contrato imutável.
- Parametrize catálogos, pesos, curvas, sazonalidade e balanceamento em dados versionados.

Leia [references/simulation-and-economy.md](references/simulation-and-economy.md) ao trabalhar em demanda, NPCs, TinyML, balanceamento, estoque, caixa, impostos, crédito, contratações, indicadores ou identidade dos segmentos.

## Dispute mercado sem criar rivais oniscientes

- Gere demanda compartilhada e mantenha uma alternativa externa de adiar/desistir.
- Use passabilidade para formar lojas capazes de atender; preço, promoção e reputação apenas alteram preferência.
- Não simule uma empresa completa para cada rival antes de isso produzir valor jogável. Use perfis resumidos com custos, duração, limites e conhecimento atrasado.
- Derive produto encalhado de giro, cobertura, idade, validade, capital imobilizado e demanda.
- Promoção possui objetivo, alvo, preço, início/fim, unidades, canal e orçamento. Nunca torna lote vencido vendável.
- Separe fato, informação circulando e conhecimento. Rumor modifica decisões, não realidade.

Leia [references/market-competition-and-promotions.md](references/market-competition-and-promotions.md) antes de projetar clientes escolhendo loja, concorrentes, campanhas, saldão, produtos encalhados, inteligência de mercado ou fofocas.

## Modele conformidade como pressão e escolha

- Não classifique inadimplência de DAS como sonegação. Separe obrigação, declaração e irregularidade intencional.
- Preserve verdade econômica, registro interno, declaração, conhecimento da autoridade e conhecimento do jogador.
- Regras legais reais ficam em ruleset versionado e exigem fonte oficial atual. Programas temporários não entram automaticamente no balanceamento base.
- Fiscalização de validade parte de lotes e achados verificáveis; penalidades confirmadas são determinísticas.
- Saldão converte estoque válido em caixa sacrificando margem. Venda de ativo remove a capacidade correspondente.
- Se irregularidade intencional for jogável, mantenha-a abstrata, explícita e sem métodos; IA nunca decide fraude pelo jogador.
- Irregularidade intencional é bloqueante e não delegável; não altera verdade econômica, cria caixa ou cancela obrigação reconhecida.
- Venda de ativo só gera caixa ao remover sua capacidade na conclusão; timeout/cancelamento não paga.

Leia [references/compliance-and-recovery.md](references/compliance-and-recovery.md) antes de projetar DAS, declaração, fiscalização, validade, dívida, regularização, saldão, venda de equipamento ou insolvência.

## Use uma linha do tempo viva e persistente

- Mantenha um `SimulationClock` e uma agenda soberanos. O relógio de parede mede o intervalo a recuperar; FPS e velocidade de processamento pertencem à apresentação/aplicação.
- Menus, fechamento do aplicativo e ausência não pausam a economia. Execução contínua e recuperação do mesmo intervalo processam a mesma linha do tempo.
- Eventos empatados usam ordem canônica; aleatoriedade deriva de seed/evento, nunca de frames.
- Durante ausência, cada ator age somente dentro de capacidade, informação e autoridade reais. Funcionários e gestores podem acertar, errar, sofrer golpe, furtar ou falhar; sem ator capaz, a tarefa não ocorre e as consequências continuam. Ações não delegáveis nunca são escolhidas pela IA, mas isso não protege a empresa de prejuízo ou falência offline.
- Decisões simultâneas formam uma barreira conjunta de registro/resumo; evento derivado não retorna à fase já processada.
- Processamento offline pode usar coortes ou agregação somente com contrato de equivalência econômica e resumo causal.
- Separe fato, registro e conhecimento no retorno: o jogador pode ainda não saber de perdas já consumadas. Falência encerra a empresa do jogador, não o mercado nem os concorrentes persistentes.

Leia [references/time-and-scheduling.md](references/time-and-scheduling.md) antes de trabalhar em velocidade, pausa, agenda, avanço, fechamento diário, vencimentos temporais ou progresso offline.

## Verifique regras antes de balancear pesos

- Tabelas-verdade cobrem somente combinações booleanas pequenas. Use também tabelas de decisão, máquinas de estado, invariantes e análise de alcançabilidade.
- No vocabulário do usuário, “passabilidade” significa admissibilidade lógica: elegibilidade, guardas, permissões e restrições. Não a interprete como probabilidade ou pathfinding.
- Gere ações candidatas, elimine as inválidas e somente então aplique pesos ou inferência. Pesos nunca tornam uma ação proibida válida.
- Políticas recebem um conjunto válido imutável e apenas modificam preferências; nunca acrescentam nem removem ações.
- Comandos são transacionais: calcule o estado e os eventos propostos, verifique todas as invariantes globais e só então faça commit atômico.
- Toda ação declarada possui pré-condições, comando, duração/conclusão/timeout, pós-condições, recursos bloqueados, resultados de falha e razões explicáveis.
- Todo terminal declara escopo (`task`, `incident`, `business_day` ou `company_run`); um terminal local não interrompe a exploração da partida inteira.
- Ter ação válida não prova progresso. Declare ator, tipo de garantia, medida de avanço, horizonte e condições de execução justa, verificando também os pesos finais de agentes automáticos.
- Separe fallback de inferência, ação de recuperação, rejeição de comando e estado degradado. Recuperações são limitadas, revalidadas e não recursivas.
- Diferencie falência intencional de deadlock, livelock, soft lock econômico, conteúdo órfão e dependência circular.
- Valide conteúdo estaticamente e explore combinações com testes de propriedades, alcançabilidade limitada, fuzz, Monte Carlo e replays semeados.
- Quando um fluxo falhar, produza um contraexemplo reproduzível com seed, versão das regras, sequência de ações e primeira invariante violada.

Leia [references/rules-and-consistency.md](references/rules-and-consistency.md) antes de criar ou alterar regras declarativas, máquinas de estado, políticas ponderadas, sistemas emergentes ou validadores.

## Trate crime como risco de gestão, não como loteria

- Torne furto interno e roubo probabilísticos, semeados, sinalizados e explicáveis por estado, oportunidade e controles.
- Não use características demográficas para risco ou culpa. Funcionários fictícios podem variar em confiança, satisfação, acesso, incentivos e percepção de detecção.
- Separe o estado físico do que foi registrado ou já é conhecido pelo jogador. Um evento oculto continua auditável para replay e depuração.
- Sangria transfere dinheiro entre locais e nunca altera receita ou lucro por si só.
- Não recompense resistência física a um roubo. Preserve pessoas; faça a estratégia acontecer na prevenção, na redução da exposição e na recuperação.
- Contratos de seguro, cobertura e indenização são determinísticos e legíveis. IA pode gerar ofertas e precificar risco, mas não decidir retroativamente o que a apólice dizia.
- Proteja o início contra perdas catastróficas inevitáveis e evite incidentes graves repetidos sem intervalo ou sinais.

Leia [references/security-and-losses.md](references/security-and-losses.md) antes de projetar ou implementar sangria, cofre, conciliação, furto interno, roubo, segurança, investigação, seguro ou sinistro.

## Conduza cada pedido

1. Identifique a decisão ou o artefato solicitado e o nível atual de maturidade. Não tente especificar o jogo inteiro quando uma fatia menor resolve o pedido.
2. Separe claramente decisões confirmadas, hipóteses recomendadas e pontos ainda abertos. Não apresente uma sugestão sua como decisão do usuário.
3. Compare alternativas pelos efeitos sobre diversão, legibilidade, custo de produção, desempenho, testabilidade e expansão futura.
4. Prefira a menor fatia jogável que teste a dúvida real. Para o primeiro vertical slice, use um segmento, poucos produtos, um fornecedor, um ciclo diário, clientes, estoque e caixa, salvo orientação diferente do usuário.
5. Ao propor um sistema, descreva entradas, estado, ações, pré-condições, saídas, eventos, invariantes, falhas, recuperação limitada, configuração e critérios de aceitação. Inclua como o jogador percebe causa e consequência.
6. Ao escrever código, mantenha o núcleo independente de framework, use dinheiro em centavos inteiros ou decimal exato, injete tempo e aleatoriedade e cubra as invariantes com testes.
7. Termine com o próximo passo de maior aprendizado e sinalize somente as perguntas que realmente bloqueiam esse passo.

## Realismo brasileiro

Trate MEI como identidade e moldura sistêmica, não como reprodução burocrática obrigatória. Se a tarefa exigir limites, tributos, juros ou regras legais reais e atuais, consulte fontes oficiais brasileiras, informe a data de vigência e mantenha o conjunto de regras versionado. Quando a fidelidade legal não for necessária, declare a abstração usada para o jogo.

## Critério de qualidade

Rejeite propostas que resultem em uma planilha decorada, um tutorial de administração ou uma simulação opaca. Uma boa entrega cria escolhas tensas, consequências compreensíveis, espaço para histórias emergentes e uma implementação que pode ser simulada sem interface.
