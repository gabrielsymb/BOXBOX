# Visão do BOXBOX

## Decisões confirmadas de produto

- O nome do jogo é **BOXBOX**.
- BOXBOX é o nome confirmado do projeto. A liberação do título comercial permanece pendente porque já existe um jogo chamado `BoxBox` na Steam; decidir sobre validação de marca ou subtítulo é uma questão de publicação, não uma autorização para renomear o projeto automaticamente.
- Português do Brasil (`pt-BR`) é o idioma padrão; inglês (`en`) é o segundo idioma.
- O lançamento é planejado para PC. A Steam é a candidata principal conhecida, mas a escolha de lojas e a ordem de publicação ainda estão abertas.
- Godot foi escolhida como engine e C# como linguagem de implementação, usando a edição .NET. Godot 4.7.2 Stable é a versão recomendada para iniciar; reavaliar somente se surgir incompatibilidade concreta.
- O desenvolvimento começa pelos motores de IA/comportamento, como módulos plugáveis no mesmo processo e isolados por contratos versionados; processos separados são uma opção futura.
- O jogo deve funcionar localmente e em hardware modesto, sem depender de serviços em nuvem para o loop principal.
- O código do produto não será escrito em Python.
- A identidade estrutural combina **systemic game design**, **emergent gameplay** e **persistent world simulation**. Sistemas compartilham estado e eventos, histórias surgem de interações verificáveis e o mundo não depende da presença do protagonista para avançar.
- O BOXBOX terá perdas gerenciáveis e explicáveis: furto interno por funcionários, roubo à loja, sangria, controles de segurança e seguro.
- Clientes disputam um mercado compartilhado entre BOXBOX, concorrentes e a opção de não comprar; promoções e informação imperfeita fazem parte dessa disputa.
- Concorrentes sofrem categorias equivalentes de ruptura, validade, fiscalização, dívida e recuperação em resolução resumida.
- O jogo diferencia atraso de obrigação, falha declaratória e irregularidade intencional dentro de uma abstração MEI versionada.
- A linha do tempo é simulada e acelerável, sem proporção `1:1` com a vida real.
- A direção visual é um diorama isométrico 3D low-poly. A operação usa interior visto de cima com paredes abertas ou ocultáveis; o mapa pode apresentar lotes em blocos ou bases flutuantes coerentes com essa linguagem.
- O tom é leve, cômico e autoconsciente. NPCs podem quebrar a quarta parede e fazer piadas com assets reutilizados, estilos misturados e limitações visuais.
- O kitbash de assets gratuitos é uma restrição transformada em identidade. A mistura pode ser visível e engraçada, desde que câmera, escala, paleta, materiais, iluminação, sombras e leitura de gameplay unifiquem a cena.

## Promessa central

No BOXBOX, o jogador assume um microcomércio brasileiro com pouco capital, poucos produtos e quase nenhuma ajuda. Ele compra, precifica, abre a loja, atende, repõe, fecha o caixa e decide como sobreviver ao dia seguinte. Com boa gestão, o negócio cresce e o papel do jogador muda: tarefas manuais são delegadas e o desafio passa a ser interpretar sinais, organizar pessoas e alocar capital.

O comércio precisa parecer vivo. Horários de pico, dias fracos, sazonalidade, produtos encalhados, rupturas, variação de fornecedores, filas, eventos e perfis de clientes se combinam para criar situações que não foram roteirizadas uma a uma.

## Fantasia do jogador

"Eu comecei com uma barraquinha, aprendi a ler meu próprio negócio e transformei uma operação frágil em algo que funciona — ou quebrei por causa das minhas escolhas."

## Pilares

1. **Começar pequeno e sentir cada decisão.** Uma compra ruim, uma falta de troco ou um horário mal escolhido importam no início.
2. **Aprender fazendo.** Giro, margem, capital de giro, média, mediana, Pareto e probabilidade surgem quando ajudam a decidir; não viram provas ou aulas obrigatórias.
3. **Negócio vivo e parcialmente imprevisível.** O jogador interpreta sinais e probabilidades, sem receber uma resposta sempre correta.
4. **Crescer muda a função do jogador.** A progressão retira trabalho repetitivo e introduz decisão estratégica.
5. **Consequências explicáveis.** O jogo mostra por que houve lucro, perda, ruptura, fila, furto, roubo, inadimplência ou quebra.
6. **Leve e local.** A simulação e qualquer inferência devem caber em hardware modesto.
7. **Identidade brasileira.** Comércio de bairro e MEI dão contexto e personalidade sem obrigar fidelidade burocrática total.
8. **Bilíngue desde a base.** A experiência funciona integralmente em pt-BR e inglês sem duplicar regras de jogo nem acoplar texto ao domínio.
9. **Improviso com personalidade.** A aparência de negócio montado com recursos encontrados faz parte do humor, sem sacrificar legibilidade ou coerência mecânica.
10. **Mundo sistêmico e persistente.** Conteúdo acrescenta causas e possibilidades reutilizáveis; não força resultados roteirizados, e o mercado continua além da empresa atual do jogador.

## Loop diário

1. Consultar caixa, estoque, compromissos e sinais de demanda.
2. Comparar fornecedores, sinais da concorrência e decidir quantidades, preços, mix e promoções.
3. Definir horário e preparar a operação.
4. Abrir e acompanhar clientes, filas, vendas, rupturas e ocorrências.
5. Intervir quando necessário: atender, repor, comprar com urgência ou aceitar uma perda.
6. Fazer sangrias e responder a ocorrências conforme as políticas definidas.
7. Fechar e conciliar caixa, cofre e estoque; observar margem, campanhas, oportunidades perdidas, diferenças e capital de giro.
8. Pagar ou regularizar compromissos, reinvestir, liquidar estoque/ativos, contratar, tomar crédito ou ajustar a estratégia do próximo dia.

## Arco de progressão

- **Sobrevivência:** banca pequena, operação manual, caixa apertado e pouca informação.
- **Estabilidade:** mais variedade, fornecedores, sazonalidade, concorrência visível, promoções, tributos, capital de giro, sangria e seguro básico.
- **Delegação:** caixa, repositor, gerente ou apoio contábil assumem tarefas mediante custo, confiança, acesso e qualidade variável.
- **Expansão:** capacidade maior, novos riscos, segurança, auditoria, crédito e decisões de portfólio.
- **Gestão:** o jogador define estratégia, acompanha indicadores e corrige a operação em vez de executar tudo.

## Mercearia de conveniência

O jogador começa com uma mercearia e expande o sortimento de alimentos, bebidas e itens de conveniência. Categorias compartilham o mesmo núcleo de simulação. Giro, margem, validade e picos de demanda variam por produto.

## Falha e continuidade

Insolvência encerra a empresa atual e leva a um novo início. A falha precisa ser justa e diagnosticável: o jogador deve conseguir reconstruir a cadeia de decisões que levou à quebra. Qualquer persistência entre partidas deve acelerar o entendimento ou a variedade, não remover o risco central.

## Decisões deliberadamente abertas

- ângulo, zoom, rotação permitida e regras exatas de oclusão da câmera isométrica;
- proporção entre interior operacional, mapa em blocos e eventuais bases flutuantes;
- intensidade e frequência das falas de quarta parede;
- razão entre tempo real decorrido e tempo simulado, nível de agregação do retorno e apresentação do resumo offline;
- grau de interação manual durante o expediente;
- tecnologia exata do modelo local;
- nível de fidelidade às regras reais do MEI;
- existência e formato de metaprogressão após a quebra;
- estratégia de distribuição: Steam primeiro, lançamento simultâneo em mais lojas ou expansão posterior.

Avalie essas decisões a partir do que o jogador precisa ver e fazer, do custo de conteúdo e do desempenho. Não as feche apenas por preferência estética.

## Primeira fatia recomendada

Um vertical slice deve provar o ciclo econômico e a sensação de loja viva com apenas:

- uma mercearia de conveniência pequena;
- poucos SKUs com comportamentos diferentes;
- um fornecedor normal e uma compra emergencial cara;
- um dia completo com abertura e fechamento;
- fluxo de clientes por horário;
- preço, estoque, venda, perda e livro-caixa;
- um resumo explicável do resultado;
- seed fixa e execução sem interface para testes;
- um conjunto pequeno, mas representativo, de telas e mensagens funcionando em pt-BR e inglês.

Contratação, crédito complexo, impostos detalhados e produção extensa de assets podem entrar depois que essa fatia for divertida e confiável.

Sangria e conciliação podem ser a primeira extensão do núcleo econômico. Roubo, seguro e segurança entram depois que locais de caixa funcionarem. Furto interno e investigação entram quando a contratação já estiver jogável.

Concorrência, promoção e fofoca entram depois que demanda, preço, estoque e relatório causal funcionarem sem rivais. A agenda temporal determinística deve existir desde o núcleo. Conformidade começa por obrigação abstrata e recuperação; declaração complexa e irregularidade intencional ficam para depois.
