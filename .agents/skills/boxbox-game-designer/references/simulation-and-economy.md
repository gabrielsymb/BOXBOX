# Simulação, economia e IA local

## Princípio

IA gera comportamento; o domínio garante coerência. Use o mecanismo mais simples que produza variedade, controle de design e desempenho aceitável. TinyML é uma opção, não uma meta por si só.

No vocabulário informal do projeto, “passabilidade” significa **o que o sistema admite ou bloqueia**. Traduza isso tecnicamente como elegibilidade, pré-condições, guardas, permissões e restrições. Essa admissibilidade é resolvida antes da política probabilística; pesos de preferência só ordenam ou sorteiam entre ações que passaram por ela.

## Pipeline de demanda

Uma intensidade de demanda pode combinar fatores configuráveis:

```text
demanda-base
× faixa-horária
× dia-da-semana
× sazonalidade
× clima ou evento local
× segmento
× reputação
× preço relativo
× disponibilidade percebida
× capacidade e fila
× concorrência admissível
× promoção conhecida
× ruído semeado
```

Chegadas podem usar uma distribuição probabilística apropriada; a escolha de loja/produto pode usar utilidades ponderadas. O mercado inclui a opção externa de adiar ou desistir. Não exponha a fórmula como verdade exata ao jogador. Mostre sinais, histórico, intervalos e explicações proporcionais às ferramentas que ele conquistou.

## Decisões adequadas para IA leve

- estimar intensidade de clientes por faixa;
- escolher comprar, substituir, esperar ou desistir;
- distribuir preferências entre categorias e SKUs;
- escolher entre lojas admissíveis e não comprar;
- variar sensibilidade a preço, reputação, ruptura e fila;
- escolher ações limitadas de concorrentes agregados;
- propagar e avaliar informações de mercado;
- escolher tarefas de funcionários autônomos;
- classificar quando oferecer crédito ou sugerir contratação;
- estimar intenções de desvio a partir de oportunidade, acesso e controles;
- estimar risco de incidentes externos e gerar ofertas de seguro;
- formar perfis compactos de consumidores.

Opções válidas incluem utility AI, regressão, árvore compacta, modelo linear/logístico ou rede quantizada pequena. Coloque qualquer implementação atrás de contratos como `DemandPolicy`, `CustomerDecisionPolicy`, `EmployeePolicy` e `CreditOfferPolicy`.

Mantenha uma implementação heurística de referência. Ela serve como fallback, oráculo de comparação e meio de executar testes sem runtime de modelo.

Uma política recebe apenas ações candidatas já validadas. Ela pode ordenar ou sortear entre elas; não cria uma transição nova, não ignora permissões e não altera estado diretamente.

## Decisões que não pertencem à IA

- saldo e lançamentos;
- baixa e recebimento de estoque;
- tributos e datas de obrigação;
- cálculo contratual de juros e amortização;
- validade e perdas consumadas;
- conservação de dinheiro em sangrias e depósitos;
- cobertura, franquia, limite e indenização de apólices contratadas;
- pré-condições de comandos;
- insolvência e encerramento da empresa;
- vencimento e estado de obrigação/declaração;
- validade física de lotes e achados confirmados de fiscalização.

Essas regras podem ser configuradas e versionadas, mas sua aplicação é determinística.

## Livro-caixa e indicadores

Registre movimentações em um diário append-only simplificado. Derive saldo, receita, custo, despesa, margem, resultado e capital de giro a partir desse histórico e do estado de estoque. Qualquer painel deve conseguir apontar os lançamentos e eventos que explicam um número.

Introduza indicadores conforme eles criam uma decisão interessante:

- giro e cobertura para decidir reposição;
- margem e contribuição para decidir mix e preço;
- média e mediana para entender dias atípicos;
- Pareto para identificar itens/clientes que concentram resultado;
- probabilidade e intervalo para comunicar previsões imperfeitas.

Evite premiar o jogador por abrir uma tela de aula. Premie o uso da informação para resolver um problema real.

## Estoque e fornecedores

Produtos devem diferir em demanda, margem, lote, prazo, validade, substituição e espaço. Fornecedores variam em preço, prazo, confiabilidade, pedido mínimo e condições. Uma compra emergencial resolve ruptura, mas cobra um custo reconhecível em margem, tempo ou reputação.

Ruptura, excesso e encalhe precisam ter causas legíveis. O relatório de fechamento deve relacionar previsão, pedido, demanda observada e impacto financeiro sem entregar uma única decisão correta.

Derive saúde de estoque de giro, cobertura, idade, validade, capital imobilizado e espaço. Promoção possui janela, alvo, unidades e orçamento; pode liberar caixa sacrificando margem, canibalizar venda normal e gerar fila. Saldão só usa estoque válido.

## Funcionários e delegação

Contratar deve trocar tempo e controle por capacidade. Funcionários possuem custo, disponibilidade, produtividade e qualidade. Eles executam os mesmos casos de uso do jogador conforme uma política; não recebem atalhos para alterar o estado.

A progressão saudável automatiza tarefas já compreendidas e abre novas decisões. Não preserve trabalho repetitivo apenas para ocupar o jogador.

## Crédito

Ofertas consideram sinais explicáveis de saúde, risco e histórico. O jogador compara valor, custo total, juros, prazo, parcela e efeito sobre capital de giro. Crédito não é um botão neutro de dinheiro: pode financiar crescimento, cobrir um descasamento temporário ou acelerar a quebra.

O modelo pode emitir uma `CreditOfferProposal`; um validador determinístico de contratos aceita ou rejeita tipos, faixas, cronologia, valor, juros, custo total, amortização, prazo e cláusulas permitidas antes de criar o contrato imutável. Propostas inválidas são rejeitadas e substituídas por uma política heurística limitada. Contrato e cálculos da dívida permanecem deterministas. Atraso e inadimplência precisam de consequências graduais e legíveis.

## Identidade mecânica dos segmentos

Modele os segmentos como composição de dados, não como subclasses independentes:

```text
SegmentDefinition
├── catálogo e categorias
├── fornecedores
├── curvas de demanda
├── sazonalidade
├── perfil de margem
├── validade e perdas
├── substituições
└── eventos e riscos próprios
```

- **Mercearia:** frequência alta, margem apertada, perecibilidade e reposição constante.
- **Cosméticos:** variedade e tendência, ticket maior, recomendação e risco de encalhe.
- **Adega:** demanda concentrada em horários, clima e eventos, com picos e riscos operacionais específicos.

## Balanceamento e observabilidade

Rode simulações em lote com políticas de jogador simples: conservadora, agressiva, aleatória e heurística competente. Observe distribuições, não apenas médias: tempo até a insolvência, capital mínimo, taxa de ruptura, desperdício, concentração de vendas, uso de crédito e momento de contratação.

A cada alteração de balanceamento, registre a versão, a seed e a configuração. Preserve logs suficientes para investigar partidas impossíveis, estratégias dominantes e resultados surpreendentes.

Para perdas e segurança, acompanhe frequência, severidade, tempo até detecção, falsos positivos, custo de controle, exposição média de caixa, prêmio pago, indenização e falências provocadas por um único incidente. Nenhuma política de risco deve tornar segurança o sistema dominante do BOXBOX.

Para mercado e promoções, acompanhe demanda potencial, captura por alternativa, opção de não comprar, margem incremental, capital liberado, canibalização, fila, duração de campanha e tempo de reação dos rivais. Para conformidade, acompanhe atraso, avisos, regularização, ativos liquidados e terminais econômicos; não transforme fiscalização no sistema dominante.

Use testes metamórficos para relações que precisam preservar direção: mantendo as outras entradas constantes, mais treinamento não piora detecção; mais fadiga não melhora atenção; melhor cobertura não aumenta o sucesso de roubo; equipamento quebrado não concede benefício integral; trocar idioma não muda a economia.

Inclua também: remover um rival não reduz a participação esperada do jogador; menor preço não reduz a preferência imediata do perfil sensível a preço; promoção encerrada deixa de modificar pesos; rumor não altera o fato; vender equipamento não aumenta capacidade; mudar velocidade não altera o log econômico.
