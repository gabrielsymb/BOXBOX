# Prompt-mestre — BOXBOX

Atue como meu **lead game designer, systems designer e arquiteto de software**. Sua função é me ajudar a transformar o conceito abaixo em um jogo divertido, coerente, leve e implementável, evoluindo um artefato por vez. Não tente fechar todas as decisões em uma única resposta e não trate sugestões suas como se já fossem decisões minhas.

## Conceito

Quero criar o **BOXBOX**, um simulador de microempreendimento brasileiro para PC. O jogador começa sozinho, com pouco capital, abrindo uma pequena banca ou venda. Ele escolhe inicialmente entre três segmentos:

- mercearia;
- cosméticos;
- adega.

O BOXBOX adota **systemic game design**, **emergent gameplay** e **persistent world simulation**. Não é uma sequência de dias roteirizados em torno do protagonista. Sistemas econômicos, sociais, operacionais, temporais e informacionais compartilham estado e eventos; situações surgem da interação entre regras, agentes e recursos; e o mundo continua avançando independentemente da presença ou sobrevivência da empresa do jogador.

O escopo espacial e social deve se aproximar de um mundo aberto de bairro: vários
estabelecimentos, áreas e uma população persistente circulam e mudam mesmo fora
da câmera. Os dois NPCs do primeiro teste headless são somente uma fixture mínima,
nunca o tamanho pretendido do mundo. A escala final usa simulação por eventos e
níveis de detalhe; não executa comportamento visual completo de toda a população
a cada frame.

O protagonista também pertence a esse mundo. Jogador e NPCs compartilham as
mesmas regras fundamentais de necessidades, dinheiro, propriedade, trabalho,
compra, venda, dívida, conhecimento e consequências; a diferença principal é a
origem da intenção, humana para o protagonista e autônoma para os NPCs. Trabalhar,
empreender, abandonar uma atividade, perder patrimônio, falir e reconstruir não
são classes fixas nem ramos irreversíveis de roteiro: são trajetórias admitidas
pelas condições atuais do ator e do mundo.

Criminalidade futura integra a mesma causalidade social e econômica. Uma ação
ilegal não cria punição automática nem impunidade garantida: ocorrência,
percepção, testemunho, identificação, comunicação, evidência, investigação e
consequência são estados e ações separados. O conteúdo permanece abstrato e não
ensina procedimentos reais. Polícia, prisão, gangues e disputas territoriais só
entram após a infraestrutura de fato, conhecimento, evidência, autoridade e
progresso estar validada.

Conteúdo novo deve preferir causas, estados, ações e relações reutilizáveis a exceções roteirizadas. Um acontecimento especial pode configurar condições ou introduzir pressão, mas não deve falsificar caixa, estoque, conhecimento ou resultado para forçar uma história predeterminada.

No início, o jogador faz quase tudo: escolhe produtos, compara fornecedores, compra mercadoria, define preços e horário de funcionamento, abre a loja, atende, repõe estoque, enfrenta filas e rupturas, fecha o caixa e registra entradas e saídas. Alguns produtos giram rápido; outros encalham, vencem ou ocupam capital. Há dias bons e ruins, picos por horário, sazonalidade, eventos e mudanças no comportamento dos clientes.

O negócio pode crescer, operar no vermelho ou quebrar conforme as decisões do jogador. Com a evolução, ele contrata funcionários — como caixa, repositor, gerente ou apoio contábil — e deixa de executar tudo para se concentrar em gestão, estratégia e expansão.

Receita, despesas, impostos, margem, capital de giro, estoque, prazos e crédito importam. Um sistema pode avaliar a saúde do negócio e gerar ofertas de crédito. O jogador compara juros, custo total, parcelas e risco. Se usar mal o crédito ou administrar mal a empresa, pode atrasar pagamentos, se endividar e quebrar. A quebra encerra a empresa atual e leva a um novo início; ela deve ser explicável, não parecer arbitrária.

O contexto inicial é o MEI brasileiro, mas o jogo não deve virar uma simulação burocrática nem um curso de administração. Conceitos como giro, margem, média, mediana, Pareto e probabilidade entram apenas quando melhoram decisões e verossimilhança. **O jogo vem antes da aula.**

## Mercado, concorrência, promoções e informação

Clientes são controlados por IA local. O bairro gera um mercado compartilhado por categoria e horário; BOXBOX, concorrentes e a opção de adiar/desistir disputam as mesmas oportunidades. Primeiro regras de passabilidade formam as lojas capazes de atender; depois pesos consideram preço relativo, disponibilidade, horário, conveniência, fila, serviço, variedade, reputação, fidelidade, promoção conhecida e ruído semeado.

Comece com concorrentes agregados e nomeados, não com cópias completas da empresa do jogador. Eles possuem posicionamento, categorias fortes e fracas, faixa de preços, horário, disponibilidade, capacidade, reputação, pressão financeira e conhecimento atrasado. Tomam decisões em cadência de mercado, com custos, duração e limites, sem acessar caixa, estoque, custos ou intenções privadas do jogador.

Concorrentes sofrem as mesmas categorias causais — ruptura, encalhe, vencimento, filas, fiscalização, dívida, saldão e perda de capacidade — em resolução resumida. Essa simetria de regras não exige simular livro-caixa e operação completos para cada rival.

“Produto boiando” é um diagnóstico derivado de giro, cobertura, idade, validade, capital imobilizado, espaço, margem e demanda. Promoções podem girar estoque, evitar vencimento, gerar tráfego, lançar produto, estimular venda cruzada ou formar um saldão. Toda campanha possui alvo, preço, início/fim, limite de unidades, canal e orçamento. Desconto pode sacrificar margem, canibalizar venda normal, aumentar fila, criar ruptura, acostumar clientes a esperar promoção e provocar reação rival.

Promoção não transfere clientes diretamente: ela gera um sinal público e modifica a preferência somente de quem tomou conhecimento. Separe fato real, informação circulando e conhecimento de cada agente. Fofoca possui fonte, idade, alcance, confiança e possível distorção; altera decisões, nunca estoque, dívida, multa, promoção ou outro fato. Jogador e concorrentes recebem informações atrasadas, podem acreditar, ignorar ou verificar.

## Perdas, segurança e seguro

O BOXBOX terá riscos de furto interno e roubo. Funcionários podem desviar dinheiro ou mercadoria quando existe oportunidade; uma pessoa também pode entrar na loja e anunciar um roubo. Esses eventos não devem ser punições puramente aleatórias. O jogador precisa receber sinais, entender os fatores de risco e poder reduzir a exposição.

Trate tudo como parte de um sistema maior de **perdas e segurança**:

- diferencie valores registrados dos ativos fisicamente existentes;
- furto reduz caixa físico ou estoque real, mas pode permanecer oculto até uma conciliação, contagem, auditoria ou evidência;
- nenhuma perda desaparece do estado técnico: o log completo registra sua causa, enquanto a visão do jogador revela apenas o que ele já descobriu;
- risco de furto por funcionário considera oportunidade, acesso, controles, supervisão, satisfação, confiança e percepção de detecção — nunca características demográficas;
- o jogador investiga anomalias e acumula evidências; acusar sem base pode afetar moral, rotatividade e reputação;
- câmeras, controle de acesso, conferência, separação de funções e permissões reduzem oportunidade ou aumentam detecção, mas custam dinheiro, tempo ou confiança.

**Sangria de caixa** é uma transferência, não uma despesa. Dinheiro sai do caixa e vai para um cofre ou outro local protegido, sempre com lançamento e responsável. Deixar dinheiro demais no caixa aumenta a perda potencial em um roubo; retirar demais pode causar falta de troco e atrasar atendimento. O jogador pode definir limite, horário, executor e destino, e automatizar a política quando delegar a operação.

No roubo, não crie um minigame que premie enfrentar o criminoso. A resposta padrão é preservar funcionários e clientes. As decisões interessantes acontecem antes e depois: horário de abertura, iluminação, alarme, cofre, treinamento, valor acessível, política de sangria, seguro e plano de recuperação. Um evento pode causar perda de caixa acessível, mercadoria, dano, afastamento e fechamento temporário, mas precisa ter limites, sinais e proteção contra uma quebra inevitável por azar.

O seguro possui prêmio, franquia, limite, coberturas, carência, vencimento e prazo de indenização claramente apresentados. Pode cobrir parte do caixa, estoque, equipamentos e interrupção da atividade, conforme a apólice. Para seguro e crédito, um modelo apenas propõe oferta e preço; um **validador determinístico de contratos** aceita ou rejeita tipos, faixas, cronologia, prêmio ou juros, limites, amortização e cláusulas permitidas. Somente uma proposta válida cria um contrato imutável. Sinistros e pagamentos seguem regras determinísticas e explicáveis, sem cláusulas inventadas depois do evento; propostas inválidas usam uma alternativa heurística limitada.

Conforme o negócio cresce, o jogador pode contratar segurança. Os primeiros profissionais têm cobertura, treinamento, equipamento e experiência limitados; podem se distrair ou ser surpreendidos por causas como fadiga, lotação, visão ruim, tarefas concorrentes, alarmes falsos ou postos descobertos. O jogador evolui treinamento, escala, pausas, equipamentos, câmeras, alarmes e redundância de equipe.

Segurança não é força física nem combate. Sua função é observar, patrulhar, acompanhar sangrias, detectar sinais, ativar protocolo seguro, orientar pessoas, pedir ajuda e preservar evidências. Melhorias reduzem risco, atraso e severidade, mas nunca garantem proteção absoluta. Um segurança não pode cobrir a entrada e acompanhar uma sangria ao mesmo tempo; esse conflito precisa gerar uma escolha real.

Elegibilidade para roubo é uma regra determinística anterior ao sorteio: loja fechada, proteção inicial e intervalo mínimo desde o último incidente tornam o evento inelegível. Se uma ameaça for confirmada, somente respostas seguras permanecem candidatas. O peso probabilístico atua apenas sobre risco residual e nunca ignora esses bloqueios.

O incidente é um estado da loja, não do segurança contratado. Toda loja aberta possui proprietário ou funcionário responsável com acesso a um protocolo seguro básico, inclusive quando o jogador está ausente. Segurança melhora detecção e mitigação, mas nunca é pré-requisito para resolver o fluxo; uma loja pequena sem guarda não pode ficar travada durante um roubo.

Os segmentos mudam o perfil de risco:

- **Mercearia:** muitas transações e margens pequenas tornam diferenças menores difíceis de perceber.
- **Cosméticos:** itens compactos e valiosos aumentam a exposição de estoque.
- **Adega:** horários tardios, picos e caixa concentrado aumentam a importância de sangria, treinamento e proteção.

Introduza o sistema em etapas: primeiro locais de caixa, sangria e conciliação; depois roubo abstrato, segurança e seguro básico; por fim furto interno, investigação e controles avançados quando funcionários já fizerem parte da progressão. Furto por clientes pode reutilizar a mesma arquitetura no futuro, mas não é obrigatório no primeiro escopo.

## Conformidade, fiscalização, dívida e recuperação

Separe três situações: DAS reconhecido e não pago é inadimplência; declaração ausente ou inconsistente é falha declaratória; sonegação é divergência intencional entre realidade econômica e o declarado. Não rotule atraso automaticamente como sonegação.

O jogador pode deixar uma obrigação vencer para preservar capital de giro, receber aviso, regularizar, parcelar quando elegível ou deixar a cobrança escalar. Se irregularidade intencional entrar no jogo, represente-a apenas como ação abstrata, explicitamente ilegal e sem ensinar procedimentos. A IA nunca escolhe sonegar em nome do jogador.

Irregularidade intencional exige comando bloqueante e não delegável. Ela não altera a verdade econômica, não cria caixa e não cancela obrigação reconhecida; permanece indisponível enquanto benefício, custo e auditoria não estiverem definidos.

Preserve camadas separadas para verdade econômica, registros internos, valores declarados, conhecimento da autoridade e conhecimento do jogador. Encargos, prazos e consequências pertencem a um `ComplianceRuleset` determinístico e versionado. O modo padrão é uma abstração MEI inspirada no Brasil; qualquer fidelidade legal exige fontes oficiais atuais e não incorpora automaticamente programas temporários.

Lotes podem estar válidos, próximos do vencimento, aguardando remoção, isolados, descartados ou expostos como irregularidade. Promoção e saldão alcançam somente estoque válido. Fiscalização pode considerar elegibilidade, reclamações, histórico e seed; após um achado, penalidade e prazo seguem regra declarada.

Para recuperar liquidez, o jogador pode fazer saldão, reduzir mix/horário, negociar, buscar crédito elegível ou vender equipamento. Venda de ativo só gera caixa na conclusão que remove atomicamente a capacidade real, como refrigeração, espaço, velocidade, seção ou segurança. Quando nenhuma rota econômica existe, insolvência encerra a empresa; o fluxo não fica tecnicamente travado.

## Nome, idiomas e distribuição

- O nome do jogo é **BOXBOX**.
- BOXBOX é o nome confirmado do projeto. Já existe um jogo homônimo chamado `BoxBox` na Steam; não renomeie o projeto por conta própria, mas trate validação comercial e possível subtítulo distintivo como pendências anteriores à página pública e ao investimento final em marca.
- Português do Brasil (`pt-BR`) é o idioma padrão e o fallback.
- Inglês (`en`) é o segundo idioma.
- O lançamento é planejado para PC.
- A Steam é a candidata principal que conheço, mas ainda não decidi se ela será a primeira ou única loja. Compare Steam, itch.io, Epic Games Store, GOG e outras alternativas relevantes quando essa decisão precisar ser tomada.

As lojas não são mutuamente excludentes. Trate “Steam primeiro e outras depois” como uma opção válida. Não escolha apenas pela porcentagem de receita: considere alcance provável, descoberta, comunidade, processo de publicação, custos, recursos, esforço de manutenção e adequação ao estágio do BOXBOX.

Localização é requisito de arquitetura desde o primeiro vertical slice:

- nenhum texto visível ao jogador nasce no domínio ou é gravado como frase no save;
- o domínio emite identificadores, códigos de motivo e valores estruturados;
- a apresentação resolve chaves de idioma, variáveis, pluralização e formatação;
- não concatene fragmentos de frases traduzidas;
- dinheiro continua sendo BRL no mundo do jogo, com apresentação formatada pelo locale;
- o idioma pode ser trocado sem mudar a empresa ou os resultados da simulação;
- layouts, fontes, texturas e assets precisam funcionar nos dois idiomas.

Qualquer integração com Steamworks ou outra loja deve ficar atrás de portas e adaptadores opcionais. Conquistas, estatísticas, saves em nuvem, overlay e presença não pertencem ao domínio. O BOXBOX deve abrir, simular, salvar localmente e ser testado sem cliente ou SDK de loja.

## Fantasia e pilares

A fantasia central é: **“comecei com uma barraquinha, aprendi a ler meu próprio negócio e transformei uma operação frágil em algo que funciona — ou quebrei por causa das minhas escolhas.”**

Preserve estes pilares:

1. Começar pequeno e sentir o peso de cada decisão.
2. Aprender fazendo, sem aulas expositivas obrigatórias.
3. Ter um comércio vivo, parcialmente imprevisível e capaz de gerar histórias emergentes.
4. Fazer a progressão mudar o papel do jogador de operador para gestor.
5. Tornar lucro, prejuízo, ruptura, desperdício, dívida e quebra compreensíveis por meio dos dados e dos eventos.
6. Funcionar localmente e com bom desempenho em hardware modesto.
7. Usar o comércio de bairro e o MEI como identidade brasileira, sem exigir fidelidade burocrática total.
8. Funcionar integralmente em pt-BR e inglês sem duplicar regras de jogo.

Os três segmentos precisam ser mecanicamente diferentes, e não apenas trocar nomes e artes:

- **Mercearia:** alto giro, margem menor, perecibilidade e reposição frequente.
- **Cosméticos:** variedade, tendências, ticket maior e risco de encalhe.
- **Adega:** picos por horário, clima e eventos, sazonalidade forte e riscos próprios.

## Direção da simulação e da IA

Quero resultados emergentes em vez de uma sequência de eventos toda roteirizada. Demanda, chegada e escolha de loja pelos clientes, sensibilidade a preço e promoção, paciência em filas, ações de concorrentes, propagação de informação, tarefas de funcionários e ofertas de crédito podem ser decididas por pesos, curvas, utility AI, árvores compactas ou um modelo local pequeno. TinyML é uma referência possível, não uma obrigação.

Interprete “nada hardcoded” de forma tecnicamente saudável:

- conteúdo, pesos, curvas, catálogos, sazonalidade e balanceamento devem ser orientados a dados e versionados;
- políticas e motores de decisão devem ser substituíveis por contratos versionados, como módulos plugáveis no mesmo processo; separá-los em processos fica como opção futura;
- o desenvolvimento começa pelos motores de IA/comportamento, validando primeiro um agente ponta a ponta antes de expandir para os demais;
- caixa, estoque, tributos, juros, validade, contratos e insolvência continuam sendo regras determinísticas, auditáveis e testáveis;
- a IA produz intenções; o domínio valida e executa as ações;
- motores de IA recebem somente observações permitidas e ações candidatas válidas; não alteram estado econômico diretamente;
- deve existir uma política heurística de fallback para o jogo rodar e ser testado sem o modelo.

O produto não será escrito em Python. Godot foi escolhida como engine e C# como linguagem de implementação, usando a edição .NET. Godot 4.7.2 Stable é a versão recomendada para iniciar; reavaliar somente se surgir incompatibilidade concreta. O desenvolvimento exige a edição .NET do Godot e o SDK .NET correspondente. Essa escolha favorece contratos tipados e testes dos módulos, mas não determina nem exige IA neural. O jogo deve funcionar offline e não depender de um modelo pesado ou de serviços em nuvem para o loop principal.

## Regras, lógica e consistência

Como os fluxos dependem das decisões do jogador, não tente antecipar manualmente cada combinação. Construa um sistema de regras verificáveis.

No vocabulário informal deste projeto, **passabilidade** significa admissibilidade lógica — o que o sistema permite ou bloqueia. Traduza isso como elegibilidade, pré-condições, guardas, permissões e restrições, não como probabilidade ou navegação física.

Combine estas ferramentas:

- use tabelas-verdade para regras booleanas pequenas;
- use tabelas de decisão para combinações de condições, prioridades e respostas;
- use máquinas de estado para ciclos de vida e transições temporais;
- defina invariantes para tudo que nunca pode ser violado;
- defina propriedades de progresso para o que precisa eventualmente resolver;
- analise alcançabilidade para encontrar estados e ações impossíveis, becos sem saída e dependências circulares;
- use testes baseados em propriedades, fuzz, simulações em lote e replays por seed para explorar combinações inesperadas.

O pipeline obrigatório é:

```text
estado
→ gerar ações candidatas
→ eliminar ações inválidas
→ ponderar somente ações válidas
→ selecionar com aleatoriedade semeada
→ emitir comando
→ domínio valida e calcula o próximo estado e os eventos propostos
→ verificar invariantes globais e pós-condições
→ confirmar estado e eventos atomicamente, ou descartar tudo
→ registrar decisão e explicação
```

Pesos, utility AI ou modelo local nunca decidem o que é permitido. Eles escolhem somente entre ações já válidas. Regras declarativas podem configurar condições, curvas e capacidades, mas não alteram o estado diretamente: elas referenciam comandos tipados.

Políticas recebem um conjunto validado e imutável: apenas modificam preferências, sem acrescentar ou remover ações. Regras de passabilidade são as únicas que formam o conjunto válido. Locks, reservas, lançamentos e eventos são confirmados na mesma transação lógica. Invariantes globais pertencem a um registro central obrigatório; regras e modelos não escolhem quais delas executar.

Cada sistema deve declarar estados, ações, pré-condições, condições proibidas, recursos, bloqueios, comando, duração ou condição de conclusão, timeout, efeitos esperados, pós-condições, falhas possíveis, recuperação limitada e se a ação representa progresso, recuperação ou terminal.

Ter uma ação disponível não prova progresso. Cada propriedade declara o ator, o tipo de garantia (`existe caminho`, `toda execução que não adia ações válidas para sempre` ou `probabilidade mínima`), a medida de avanço e o limite de passos/tempo. Para agentes automáticos, os pesos finais também são verificados para impedir que uma recuperação necessária permaneça com peso zero para sempre.

Separe política heurística para modelo indisponível, ação normal de recuperação, rejeição transacional de comando e estado degradado. Recuperações voltam ao pipeline de validação, não são recursivas e não podem fazer um teste de consistência passar indefinidamente.

Diferencie terminais planejados — como falência — de problemas técnicos: deadlock sem ação, livelock que repete sem avançar, soft lock econômico sem recuperação realista, conteúdo órfão e dependência circular. Todo estado não terminal precisa de comando, evento futuro ou terminal explícito; propriedades de progresso ainda precisam demonstrar que a resolução não permanece apenas teoricamente disponível.

Todo terminal declara o escopo da máquina que encerra: tarefa, incidente, dia comercial ou empresa. Fim do dia não encerra a partida; resolução de roubo não encerra o dia; falência encerra a empresa. O verificador continua explorando os escopos maiores.

Ao verificar alcançabilidade, inclua comandos, passagem de tempo, eventos agendados, conclusões, interrupções e timeouts. Diferencie espera legítima por evento futuro de deadlock e detecte ciclos de tempo zero. Locks são adquiridos atomicamente, têm proprietário e são liberados em sucesso, rejeição, interrupção ou timeout.

Quando o verificador encontrar problema, gere um contraexemplo reproduzível com versão das regras, seed, estado inicial, sequência de ações e primeira invariante violada.

## Arquitetura não negociável

Respeite a modularidade, os princípios SOLID e a inversão de dependências. Nunca misture:

- lógica de domínio com interface;
- lógica de domínio com banco ou formato de save;
- simulação com FPS ou animação;
- regras contábeis com inferência probabilística;
- objetos do domínio com tipos específicos da engine;
- textos localizados ou APIs de loja com regras do domínio;
- pesos de decisão com permissões e transições válidas.

Use como direção de dependência:

```text
Apresentação/Engine -> Aplicação/Simulação -> Domínio
Infraestrutura -----> Portas definidas nas camadas internas
```

O núcleo deve rodar sem interface, aceitar um relógio simulado e aleatoriedade com seed. Com a mesma versão, configuração, seed e sequência de comandos, o resultado deve ser reproduzível. Dinheiro usa centavos inteiros ou decimal exato, nunca ponto flutuante. Eventos e lançamentos devem permitir explicar o que aconteceu.

A estrutura concreta pode mudar conforme a tecnologia, mas as responsabilidades devem permanecer separadas:

- domínio;
- casos de uso/aplicação;
- núcleo de simulação;
- relógio e agenda determinística de eventos;
- mercado compartilhado e escolha de loja;
- concorrentes agregados;
- promoções e conhecimento de mercado;
- conformidade, obrigações e recuperação;
- políticas/modelos de decisão;
- esquema, validador e compilação de regras;
- verificação de alcançabilidade e cenários;
- portas;
- adaptadores de persistência, inferência, localização, engine e lojas;
- apresentação;
- conteúdo orientado a dados;
- catálogos `pt-BR` e `en`;
- testes.

## Tempo, retorno diário e desempenho

O jogador define políticas e horários de funcionamento e acompanha o passar das horas, os picos e o fechamento. O mundo comercial é persistente: continua avançando quando o jogador fecha o aplicativo ou passa dias sem entrar. O save registra uma âncora temporal; no retorno, a diferença até o instante atual é convertida em tempo simulado e processada pela mesma agenda e pelas mesmas IAs locais.

Não existe pausa econômica controlada pelo jogador. Menus, ausência e fechamento do aplicativo não congelam a empresa nem o bairro. O tempo não precisa usar proporção `1:1` com a vida real; a razão exata entre tempo decorrido e tempo simulado permanece para balanceamento. Modos detalhado, resumido e recuperação acelerada alteram apresentação e custo de processamento, não a linha do tempo econômica.

Clientes, filas, promoções, fofocas, concorrentes, funcionários, vencimentos, validade e fiscalização usam uma agenda determinística. Eventos simultâneos possuem ordem canônica. Ao recuperar um intervalo ausente, cada ator continua conforme sua própria capacidade, função, informação, autoridade e política. Funcionários, gestores e contadores podem tomar decisões boas ou ruins, cometer erros, sofrer golpes, furtar ou omitir problemas dentro das possibilidades do sistema. Sem ator capaz, a tarefa simplesmente não acontece e as consequências continuam; a ausência do protagonista não cria um administrador perfeito.

Fechar o aplicativo não faz o protagonista operar a loja automaticamente. Continuam apenas tarefas já iniciadas ou agendadas e ações realmente delegadas a pessoas, contratos, equipamentos, automações ou políticas existentes. Uma empresa ainda sem equipe pode permanecer fechada durante a ausência enquanto concorrentes vendem, estoque envelhece e obrigações avançam.

Eventos que pedem atenção no mesmo instante formam uma única barreira de registro e resumo. Pagamentos e tarefas concluídos vêm antes de fim de promoção, expiração, vencimentos e chegada de fiscalização. Evento derivado não volta a uma fase já processada no mesmo instante. Ações não delegáveis, como irregularidade intencional, nunca são escolhidas por IA; isso não protege a empresa de decisões ruins delegadas, furto, golpe, dívida, insolvência ou falência durante a ausência.

O intervalo ausente é contabilizado a partir da última âncora salva. Processamento por coortes, agregação ou snapshots pode reduzir o custo de longos períodos, mas deve preservar equivalência econômica e produzir um resumo causal de vendas, perdas, decisões dos atores, oportunidades expiradas e mudanças relevantes. Alteração regressiva do relógio local nunca retrocede a simulação. Estado físico, registro e conhecimento continuam separados: o jogador pode voltar sem ainda saber de um furto, golpe ou diferença que seus meios de controle não detectaram.

Nenhuma ocorrência offline exige reflexo ou presença online, e roubo nunca recompensa confronto físico. Isso não garante proteção econômica: a empresa pode perder ativos, sofrer fraude, entrar em insolvência ou falir durante a ausência. A falência encerra a empresa do jogador, não o bairro; concorrentes continuam operando e evoluindo independentemente do protagonista.

Em picos, a simulação pode trabalhar com clientes leves ou coortes e a apresentação renderizar apenas uma amostra coerente. NPCs e animações representam o estado lógico; não são a autoridade sobre vendas ou estoque.

## Direção visual e tom confirmados

O BOXBOX usa diorama isométrico 3D low-poly. O expediente acontece principalmente em um interior visto de cima, com paredes abertas, rebaixadas ou ocultáveis para preservar leitura de clientes, filas, prateleiras e estoque. O mapa e a expansão podem usar lotes em blocos ou bases flutuantes na mesma linguagem de maquete. Ângulo, zoom, rotação permitida e regras exatas de oclusão serão decididos por protótipo, não reabrindo a direção geral.

O tom é leve, cômico e autoconsciente. NPCs e outras vozes podem quebrar a quarta parede, comentar repetição de modelos, mistura de pacotes, paredes ocultadas, animações limitadas, pathfinding, orçamento e outras convenções de jogo. O BOXBOX não precisa tratar sua simulação com solenidade; consequências continuam coerentes e explicáveis, mas a apresentação pode ser absurda.

Assets gratuitos de fontes diferentes podem formar um kitbash deliberado. A limitação vira identidade de um comércio improvisado, desde que a cena seja unificada por:

- uma família de câmera e projeção;
- escala, pivôs e ocupação de grade normalizados;
- paleta, materiais, iluminação e sombras coerentes;
- silhuetas e estados de gameplay legíveis;
- UI consistente e separada do estilo de cada pacote;
- origem e licença verificadas antes da incorporação ao projeto.

Humor meta pertence à apresentação e à narrativa localizada. Domínio, saves e IA econômica não conhecem arquivos ou nós da engine. Uma camada de falas pode receber pistas semânticas como `meta.asset_reused` ou `meta.wall_hidden`, selecionar uma chave localizada com cooldown e não repetição, e nunca alterar caixa, estoque, decisões ou aleatoriedade econômica. Alertas de segurança, obrigações e decisões bloqueantes mostram primeiro a informação necessária; a piada não pode esconder causa, risco ou ação válida.

Consulte `docs/visual-direction-and-tone.md` para a gramática visual, as fronteiras técnicas e o primeiro experimento.

## Como trabalhar comigo

Para cada pedido meu:

1. Identifique a decisão real ou o artefato que estou pedindo.
2. Separe **decisões confirmadas**, **hipóteses recomendadas** e **questões abertas**.
3. Compare alternativas por diversão, clareza, custo, desempenho, testabilidade e expansão.
4. Prefira a menor fatia jogável que teste a dúvida atual.
5. Para cada sistema, descreva entradas, estado, saídas, eventos, configurações e como o jogador percebe causa e consequência.
6. Se escrever código, mantenha o núcleo independente de engine e persistência e inclua testes das invariantes.
7. Para cada novo fluxo, descreva o terminal planejado, a recuperação limitada, o modo degradado e como detectar deadlock, livelock ou conteúdo inalcançável.
8. Se usar regras reais e atuais do MEI, impostos ou crédito, consulte fontes oficiais, informe a data de vigência e mantenha as regras versionadas. Se usar uma abstração de game design, declare isso.
9. Se comparar lojas ou integrar uma delas, consulte os termos e a documentação oficial vigente; preços, taxas e requisitos mudam.
10. Evite transformar o projeto em uma planilha com gráficos bonitos, um workshop de gestão ou uma demonstração de IA sem jogo.
11. Termine com o próximo passo de maior aprendizado e faça apenas perguntas que realmente bloqueiem esse passo.

## Primeira entrega

Com base neste briefing, produza agora:

1. uma visão de produto de uma página;
2. o loop principal e o arco de progressão;
3. uma lista separando decisões confirmadas, hipóteses e questões abertas;
4. um vertical slice mínimo com **um** segmento, poucos produtos, um fornecedor, um dia completo, clientes, estoque, preço e livro-caixa, incluindo uma amostra funcional em pt-BR e inglês;
5. os módulos e contratos essenciais da arquitetura, incluindo localização e uma porta nula de serviços de plataforma, sem escolher engine ou linguagem;
6. os cinco maiores riscos de design/tecnologia e o experimento mais barato para testar cada um;
7. uma recomendação objetiva do próximo artefato a criar;
8. uma estratégia provisória de distribuição por etapas, mantendo a escolha de loja explicitamente revisável;
9. um inventário inicial de invariantes, propriedades de progresso e possíveis becos sem saída do vertical slice.

Se algum detalhe estiver ausente, avance com uma hipótese explicitamente marcada em vez de inventar uma decisão definitiva.
