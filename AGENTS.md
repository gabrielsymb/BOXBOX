# BOXBOX

Este repositório contém o projeto do jogo **BOXBOX**, um simulador bilíngue de microcomércio brasileiro.

## Fontes de verdade

- Leia `docs/boxbox-prompt-mestre.md` para a visão, os pilares e as restrições do produto.
- Leia `docs/security-and-losses.md` ao trabalhar em sangria, diferenças de caixa/estoque, furto interno, roubo, segurança ou seguro.
- Leia `docs/rules-and-consistency.md` ao criar regras, políticas ponderadas, estados, ações, eventos ou progressão.
- Leia `docs/market-competition-and-promotions.md` ao trabalhar em clientes, concorrentes, promoções, produtos encalhados, inteligência de mercado ou fofocas.
- Leia `docs/compliance-and-recovery.md` ao trabalhar em DAS, declarações, fiscalização, validade, dívida, saldão, venda de ativos ou insolvência.
- Leia `docs/time-scale.md` ao trabalhar em relógio, velocidade, agenda, pausa, fast-forward, fechamento diário ou progresso offline.
- Leia `docs/visual-direction-and-tone.md` ao trabalhar em câmera, cenários, UI, assets, falas, humor, quarta parede ou direção de arte.
- Use a skill `$boxbox-game-designer` em `.agents/skills/boxbox-game-designer/` para decisões de design, arquitetura, economia, IA, localização e implementação.
- Consulte `docs/distribution-options.md` antes de transformar uma loja de PC em decisão definitiva.

Se houver divergência, use esta precedência: decisão explícita mais recente do usuário; decisões e invariantes deste arquivo; documento especializado para o assunto; prompt-mestre. A skill orienta o trabalho e resume essas fontes, mas nunca as substitui ou contradiz.

## Decisões confirmadas

- Nome: BOXBOX.
- Status do nome comercial: BOXBOX permanece como nome do projeto, mas há um homônimo exato na Steam; validação comercial ou subtítulo precisam ser resolvidos antes da página pública.
- Idioma padrão e fallback: português do Brasil (`pt-BR`).
- Segundo idioma: inglês (`en`).
- Plataforma-alvo inicial: PC.
- Engine escolhida: Godot, com C# pela edição .NET. Godot 4.7.2 Stable é a versão recomendada para iniciar; reavaliar somente se surgir incompatibilidade concreta.
- A implementação começa pelos motores de IA/comportamento. Eles serão módulos plugáveis no mesmo processo, isolados por contratos; processos separados ficam como opção futura se houver necessidade concreta.
- Steam é a candidata principal conhecida, não uma decisão exclusiva.
- O produto não será escrito em Python.
- A identidade de design é **systemic game design**, **emergent gameplay** e **persistent world simulation**: sistemas compartilham estado e causas, resultados surgem de suas interações e o mundo continua independentemente do protagonista.
- O loop principal funciona localmente, offline e em hardware modesto.
- O mundo comercial é persistente e continua avançando sem o jogador. O save registra o último instante observado; ao abrir o jogo, o tempo ausente é convertido em avanço da mesma linha do tempo simulada e processado pela agenda e pelas IAs locais.
- Não existe pausa econômica controlada pelo jogador. Menus, fechamento do aplicativo e ausência não congelam clientes, concorrentes, funcionários, obrigações, validade ou outros sistemas temporais.
- O processamento de retorno pode ser acelerado, agregado ou resumido para desempenho, mas não cria uma segunda simulação nem descarta silenciosamente as consequências do período ausente.
- Fechar o jogo não transforma o protagonista em funcionário automático. Durante a ausência, só continuam tarefas já iniciadas/agendadas e ações de funcionários, contratos, equipamentos, automações ou políticas que realmente existam; sem operação delegada, a loja pode permanecer fechada enquanto o bairro avança.
- O jogo terá um sistema de perdas e segurança: funcionários podem furtar, a loja pode sofrer roubo, sangrias reduzem a exposição de caixa e seguros mitigam parte do prejuízo.
- O protagonista pode pegar, empunhar e disparar armas. As situações em que o disparo é admissível, suas consequências sistêmicas e sua relação com roubos ainda precisam de especificação; a existência da mecânica não revoga por si só o protocolo seguro nem transforma confronto em recompensa.
- O protagonista canônico é o modelo do Manus em `BOXBOX/boxbox/Assets/Characters/NewCast/boxbox-player-fiel.glb`. Preserve sua identidade, o esqueleto, os materiais e as animações nas integrações. A versão derivada de Kenney foi aposentada.
- O mapa jogável cresce como mundo aberto em células determinísticas, mantendo separadas a simulação persistente e sua projeção 3D carregada ao redor do foco. Ruas, lotes elevados, rampas, prédios e vegetação visíveis usam assets aprovados; colisores invisíveis podem ser configurados por código.
- A cidade usa geração híbrida, nunca sorteio independente por célula como resultado final. Bairros são contíguos, templates de quarteirão preservam zoneamento e landmarks reservam identidade; aleatoriedade apenas varia composições admissíveis dentro desse plano.
- O protagonista pode entrar, sair e dirigir veículos. Combate, direção e exploração compartilham o mesmo mundo, mas propriedade, crime, testemunhas, polícia, dano, morte e recuperação exigem fatos e estados explícitos do domínio antes de produzirem consequências sistêmicas.
- Seguranças entram conforme o negócio cresce. Treinamento, escala, equipamentos e redundância melhoram detecção e protocolo, mas distração e surpresa continuam possíveis e explicáveis.
- Clientes escolhem entre alternativas admissíveis por IA local; BOXBOX, concorrentes e a opção de não comprar disputam o mesmo mercado.
- Promoções podem recuperar clientes e girar estoque encalhado, com custo de margem, capacidade e possível canibalização.
- Concorrentes sofrem as mesmas categorias causais da loja do jogador em resolução resumida, e informações circulam para os dois lados com atraso e incerteza.
- O jogo distingue DAS atrasado, falha declaratória e sonegação intencional; regras reais ficam versionadas e a experiência padrão usa abstração declarada.
- A escala temporal não é `1:1` com a vida real; velocidades diferentes processam a mesma linha do tempo simulada.
- A direção visual é um diorama isométrico 3D low-poly. O interior da loja usa visão superior inclinada com paredes abertas ou ocultáveis; mapa e expansão podem usar lotes em blocos ou bases flutuantes dentro da mesma linguagem.
- O tom é leve, cômico e autoconsciente. NPCs e outras vozes do jogo podem quebrar a quarta parede e brincar com reutilização, mistura ou limitações dos assets.
- A variedade de assets gratuitos pode aparecer como kitbash deliberado e parte do humor, mas câmera, escala, paleta, materiais, iluminação, sombras e legibilidade precisam unificar o conjunto.

## Invariantes de arquitetura

- Não misture domínio, interface, persistência ou inferência.
- O núcleo da simulação roda sem UI e não depende de engine, banco, modelo local ou loja.
- Tempo e aleatoriedade são injetáveis; partidas são reproduzíveis por seed.
- Dinheiro usa centavos inteiros ou decimal exato, nunca ponto flutuante.
- IA produz intenções; o domínio valida e executa ações.
- Motores de IA são substituíveis e recebem observações e ações candidatas válidas; não mutam o estado do domínio diretamente.
- Pesos escolhem somente entre ações válidas. Restrições, transições e invariantes determinísticos definem o que pode acontecer.
- “Passabilidade”, no vocabulário informal do projeto, significa admissibilidade lógica: elegibilidade, pré-condições, guardas, permissões e restrições; não probabilidade nem navegação física.
- Comandos são transacionais: o próximo estado e os eventos são calculados, todas as invariantes globais são verificadas e somente então ocorre commit atômico.
- Políticas recebem um conjunto válido e imutável de ações e não podem acrescentar nem remover ações, ignorar guardas ou desabilitar invariantes; apenas modificam preferências.
- Ter ação válida não basta para provar progresso. Propriedades declaram ator, quantificador, medida de avanço, horizonte e hipóteses de justiça; conteúdo novo passa por validação de contradição e alcançabilidade.
- Recuperação, fallback de inferência, rejeição de comando e terminal degradado são conceitos distintos; nenhum deles pode esconder deadlock ou livelock nos testes.
- Promoções nunca tornam lote vencido vendável, possuem início/fim e não criam estoque ou caixa.
- Fofoca altera conhecimento e pesos, nunca a realidade econômica, tributária ou fiscalizatória.
- Concorrentes não acessam estado privado ou futuro do jogador e não alteram diretamente seus ativos ou obrigações.
- Vender equipamento gera caixa uma vez e remove a capacidade correspondente; saldão respeita somente estoque existente e válido.
- Irregularidade fiscal intencional exige comando explícito, bloqueante e não delegável; nunca altera a verdade econômica nem cria caixa.
- Uma rota de recuperação só é viável se puder concluir antes do prazo e produzir o efeito econômico prometido.
- Mudar escala, FPS ou idioma não altera o resultado econômico com os mesmos comandos e instantes simulados.
- Avançar continuamente ou recuperar o mesmo intervalo após ausência produz o mesmo resultado econômico canônico, dadas a mesma versão, seed, estado inicial, políticas e âncoras temporais.
- Relógio de parede informa quanto tempo precisa ser recuperado, mas não ordena transações nem permite retroceder o `SimulationClock`; alteração regressiva do relógio local nunca desfaz eventos.
- Ausência do jogador não concede competência nem congela o mundo. Funcionários, gestores, contadores, contratos e automações agem somente dentro das autoridades e capacidades que realmente possuem; podem acertar, errar, ser enganados, furtar ou deixar tarefas falharem. Sem ator capaz, a tarefa não acontece e suas consequências continuam. Irregularidade intencional permanece não delegável, e segurança de pessoas continua sendo a resposta padrão a roubo; nenhuma dessas proteções impede prejuízo, insolvência ou falência offline.
- Estado físico, registros e conhecimento permanecem separados durante a ausência. Furto, golpe, perda e dívida podem existir antes de o jogador saber; o retorno revela apenas o que seus canais de informação já poderiam ter detectado.
- Falência offline pode encerrar a empresa do jogador, mas nunca o mercado. Concorrentes e demais atores continuam avançando na linha do tempo persistente independentemente do protagonista.
- Furtos removem ativos físicos por eventos auditáveis, mesmo quando ainda estão ocultos do jogador; conciliações revelam diferenças entre o registrado e o existente.
- Roubo nunca deve premiar confronto físico. Segurança de pessoas é a resposta padrão; o desafio está na prevenção, nas políticas e na recuperação.
- Textos visíveis usam chaves localizadas. Domínio, eventos e saves não armazenam frases traduzidas.
- Piadas sobre assets e quarta parede pertencem à apresentação/narrativa. Domínio e IA econômica não conhecem arquivos, nós da engine ou licenças; recebem no máximo pistas semânticas permitidas, e nenhuma fala altera o resultado econômico.
- Serviços de loja ficam atrás de portas e possuem implementação local/nula.

## Forma de trabalhar

- Antes de integrar qualquer asset 3D, meça seu AABB real no Blender, registre a dimensão de origem e compare-o em execução com protagonista, veículo, porta, mobiliário e célula de 4 m. Nunca aceite a escala bruta do pacote nem aplique um multiplicador sem essa comparação contextual; colisores e pontos de luz devem acompanhar a dimensão visual aprovada.
- Geradores visuais devem consumir todas as variantes semanticamente compatíveis dos packs curados, misturando famílias quando câmera, escala, material e função permanecerem legíveis. Não reduza uma categoria a poucos caminhos fixos; variantes geometricamente incompatíveis, como reta/curva/cruzamento, permanecem em subcategorias próprias.
- Um NPC ou objeto dinâmico nunca pode aparecer dentro do enquadramento. O spawn visual ocorre fora da câmera e entra organicamente na cena; se não houver posição externa válida, adie o spawn.
- Não implemente nem ajuste de memória sistemas visuais, iluminação, sombras, física de veículos, tráfego, pedestres ou geração urbana. Antes de alterar código, pesquise documentação oficial da versão usada e referências existentes no repositório; registre fontes, hipótese causal e critério visual/físico de aceitação.
- Para cidade, tráfego, veículos e pedestres, leia primeiro `research/gta2-mechanics-study.md` e consulte o checkout de pesquisa `research/third-party/gta2_re` nos pontos indicados pelo estudo. O checkout não possui licença identificada: use somente para compreender conceitos e comportamento; nunca copie, traduza, vincule ou incorpore código, assets, dados, mapas, sons, sprites ou nomes.
- Mudanças visuais só estão concluídas após executar o jogo, capturar/inspecionar o resultado real e comparar com o problema relatado. Compilação limpa, isoladamente, não valida iluminação, sombras, escala, colisão nem composição.
- Registre pesquisas técnicas relevantes em `research/` com data, versão da engine/renderizador, links consultados, conclusão adotada e limitações. Se a evidência não estiver disponível, pare e pesquise em vez de inventar valores.
- Separe decisões confirmadas, hipóteses e questões abertas.
- Prefira o menor vertical slice capaz de testar a dúvida atual.
- Não reabra engine nem a direção de diorama isométrico low-poly sem motivo concreto. Ângulo, zoom, oclusão de paredes e composição exata ainda precisam de protótipo; a escolha de loja de PC continua aberta.
- Ao usar regras atuais de MEI ou requisitos comerciais de lojas, consulte fontes oficiais e registre a data da pesquisa.
- Para cada sistema novo, defina estados, ações, pré-condições, duração/conclusão/timeout, efeitos, invariantes, falhas, recuperação limitada, escopo dos terminais e propriedades de progresso antes de balancear pesos.
