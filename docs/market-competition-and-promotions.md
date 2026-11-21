# BOXBOX — mercado, concorrência, promoções e fofocas

## Decisões confirmadas

- Clientes são controlados por políticas de IA local e escolhem entre alternativas admissíveis.
- O mercado é compartilhado: BOXBOX, concorrentes e a opção de adiar/desistir disputam as mesmas oportunidades de compra.
- Promoções podem atrair clientes da concorrência e ajudar a girar produtos encalhados.
- Concorrentes sofrem as mesmas categorias causais da loja do jogador, mas podem ser simulados em resolução menor.
- Informações sobre promoções, rupturas, fiscalizações e dificuldades circulam como observação ou fofoca para os dois lados.
- Nenhum concorrente conhece instantaneamente caixa, estoque, custos ou decisões privadas do jogador.
- Concorrentes persistem e continuam operando durante a ausência do jogador e depois do encerramento da empresa atual do protagonista. Falência do jogador não é terminal do mercado.

## Mercado compartilhado

Não gere uma demanda separada para cada loja. Por categoria e faixa de horário, o bairro produz intenções de compra que terminam em:

```text
BOXBOX | concorrente | adiar/desistir | demanda não atendida
```

Pipeline:

```text
gerar intenção ou coorte de compra
→ formar alternativas admissíveis
→ calcular preferência entre as alternativas
→ escolher com aleatoriedade semeada
→ validar estoque, capacidade e preço
→ confirmar venda ou resultado sem venda
→ registrar causas e conhecimento disponível
```

A opção externa de adiar ou desistir evita que todo cliente seja obrigado a comprar em alguma loja. O mercado pode crescer somente por causa declarada, como sazonalidade, clima, evento ou campanha com efeito de tráfego limitado.

### Passabilidade das alternativas

Uma loja participa da escolha apenas quando, no instante avaliado:

- está aberta e acessível;
- oferece a categoria ou substituto compatível;
- possui capacidade operacional;
- a oferta está vigente;
- as demais condições determinísticas estão satisfeitas.

No primeiro slice, estoque real disponível também pode integrar a admissibilidade agregada. Uma versão futura pode separar disponibilidade percebida da física para representar alguém que chega e encontra ruptura.

Depois da passabilidade, pesos de preferência consideram preço relativo, disponibilidade, horário, conveniência, fila, serviço, variedade, reputação, fidelidade, promoção conhecida e ruído semeado. A política nunca adiciona nem remove lojas.

## Concorrente agregado e nomeado

O primeiro modelo recomendado é um rival persistente e visível, mas sem contabilidade completa:

```text
CompetitorProfile
├── id e nome localizado
├── posicionamento
├── categorias fortes e fracas
├── faixa de preço por categoria
├── horário e conveniência
├── cobertura de mix
├── confiabilidade de estoque
├── capacidade de atendimento
├── reputação
├── orçamento/pressão financeira abstratos
└── memória de mercado atrasada
```

Posicionamentos possíveis incluem preço, conveniência, variedade, especialização e qualidade. O rival decide em cadência diária ou por faixa estratégica, nunca a cada venda ou frame. Essa cadência pertence à linha do tempo persistente: entrar no jogo não concede uma rodada extra ao rival, e permanecer ausente não o congela. Processamento offline pode resumir decisões, mas preserva causas, limites, conhecimento disponível e resultado agregado.

Ações incluem manter estratégia, alterar preço de uma categoria, iniciar promoção, reforçar disponibilidade, ajustar mix, estender horário ou melhorar atendimento. Cada ação possui custo, duração, intervalo mínimo, informação necessária e limite.

Simetria significa usar as mesmas causas e restrições, não simular outra empresa inteira. Um concorrente pode sofrer ruptura, vencimento, fila, fiscalização, dívida, saldão, perda de capacidade e saída do mercado por estados resumidos equivalentes.

## Produtos que “boiam”

“Boiando” é um diagnóstico derivado, não um atributo permanente:

```text
InventoryHealth = Healthy | SlowMoving | Overstocked | NearExpiry | DeadStock
```

Considere:

- dias sem venda;
- giro e cobertura;
- idade dos lotes e prazo até vencimento;
- capital imobilizado;
- espaço ocupado;
- margem;
- demanda observada e prevista.

A interface explica a classificação com causas. Um item não deve ser chamado de encalhado apenas porque teve um dia fraco.

## Promoções

```text
PromotionCampaign = Draft | Scheduled | Active | Exhausted | Ended | Cancelled | Settled
```

Uma campanha declara objetivo, SKU/categoria, preço promocional, início, fim, limite de unidades, canal, orçamento e público mais sensível. Objetivos possíveis:

- girar estoque lento;
- evitar perda de lote próximo do vencimento;
- gerar tráfego;
- lançar produto;
- estimular venda cruzada;
- realizar saldão para obter liquidez.

Passabilidade mínima:

- produto e lote existem;
- estoque elegível é positivo;
- lote ainda está dentro da validade;
- início e fim são válidos;
- não existe campanha incompatível;
- preço usa dinheiro exato;
- orçamento e canal necessários estão disponíveis.

Vender abaixo do custo pode ser permitido como decisão consciente de liquidação, desde que margem negativa e capital liberado sejam mostrados claramente. Promoção nunca torna lote vencido vendável.

Benefícios possíveis:

- mais preferência entre clientes que tomaram conhecimento;
- capital e espaço liberados;
- menor perda por vencimento;
- venda cruzada;
- recuperação de parte da clientela.

Custos e riscos:

- margem sacrificada;
- vendas normais canibalizadas;
- fila e ruptura;
- retorno decrescente;
- hábito de esperar desconto;
- desgaste de posicionamento;
- reação da concorrência.

Promoção não “rouba” clientes diretamente. Ela publica um sinal e altera os pesos somente de quem ficou sabendo. Divulgação não garante venda.

## Fofocas e conhecimento imperfeito

Separe:

1. fato real;
2. informação circulando;
3. conhecimento de cada agente.

```text
MarketRumor
├── subject e target
├── source
├── created_at e expires_at
├── reach
├── confidence
├── accuracy
└── related_fact_id opcional
```

```text
MarketKnowledge = Unknown | Rumored | Observed | Verified | Stale | Disproved
```

Assuntos incluem promoção, preço, ruptura, fila, horário, fiscalização, produto vencido, dificuldade financeira e chegada de produto procurado.

Rumor altera conhecimento e pesos de decisão; nunca cria promoção, dívida, estoque, multa ou infração. Jogador e concorrentes podem ignorar, acreditar ou gastar recurso para verificar. Informação envelhece e pode ser corrigida.

## Escolha na mercearia

Preço, proximidade, tempo de fila, frescor e disponibilidade dos básicos influenciam a escolha. Horário, clima e eventos podem aumentar a procura por bebidas geladas, gelo e petiscos.

## Invariantes e propriedades

- Cada oportunidade de compra é resolvida no máximo uma vez.
- Vendas, desistências e demanda não atendida reconciliam com o mercado gerado.
- Loja fechada ou alternativa inválida não recebe venda.
- Estoque é baixado uma única vez.
- Promoção possui início, fim, estoque e orçamento limitados.
- Lote vencido não participa de promoção.
- Cada unidade recebe um único preço confirmado.
- Rumor altera conhecimento, nunca a realidade econômica.
- Concorrente não lê estado privado ou futuro do jogador.
- Nenhuma ação rival altera diretamente caixa, estoque ou reputação do jogador.
- Mesma versão, seed, comandos e instantes reproduzem escolhas e informações.
- Toda campanha termina, é cancelada ou falha com motivo.
- Um negócio ainda viável mantém rota de reposicionamento; insolvência legítima é terminal, não deadlock.
- Terminal `company_run` da empresa do jogador não encerra relógio, agenda ou empresas concorrentes.

## Menor experimento

Use uma mercearia, três produtos, dois perfis de cliente e um concorrente agregado e nomeado durante três dias:

1. referência normal;
2. promoção ou ruptura do rival;
3. reação do jogador.

Inclua um produto de giro normal, um encalhado e um perecível próximo do vencimento. Compare nenhuma promoção, desconto agressivo e saldão direcionado.

O experimento passa se preço baixo não dominar sempre, saldão liberar caixa sacrificando margem, apenas clientes informados reagirem à promoção, o concorrente não agir antes de receber informação e os relatórios reconciliarem estoque, caixa e causas.

## Questões abertas

- Quantos concorrentes aparecem por bairro?
- Eles serão visíveis fisicamente ou apenas por sinais e relatórios?
- Pesquisa de mercado custa dinheiro, tempo ou ambos?
- O jogador pode visitar concorrentes ou delega essa observação?
- Rivais podem fechar, mudar o sortimento ou cooperar futuramente?
- Rumores deliberadamente falsos e sabotagem permanecerão fora do jogo ou entrarão em fase posterior?
