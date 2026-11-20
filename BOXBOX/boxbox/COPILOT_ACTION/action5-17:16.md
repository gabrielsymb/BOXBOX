ACTION 5 — primeiro fluxo de compra e venda confirmada

Objetivo

Conectar uma decisão de cliente a uma transação econômica mínima, sem misturar UI, domínio e inferência. A compra deve ocorrer apenas se a loja for uma opção admissível e escolhida; estoque e caixa mudam juntos ou não mudam.

Implementação

- `Domain/Commerce/StoreState.cs` mantém estado imutável da loja: aberta/fechada, estoque por produto, preço unitário em centavos, caixa e decisões já processadas.
- `Domain/Commerce/SaleCommand.cs` descreve decisão, cliente, loja, produto e quantidade.
- `StoreState.CompleteSale` valida loja, abertura, produto, estoque e idempotência. Em sucesso, retorna um novo snapshot e `CompletedSale`; em rejeição, devolve o snapshot original sem mutação.
- `Application/Customers/CustomerPurchaseService.cs` forma a opção da BOXBOX somente se loja e estoque permitirem atender; sempre inclui não comprar; chama a sessão de cliente e envia venda ao domínio somente quando a BOXBOX é selecionada.
- `CompletedSale` registra IDs causais, quantidade e valores monetários em centavos inteiros. O fluxo não calcula custo de mercadoria nem lucro.
- O estado só se torna efetivo quando o chamador adota `ResultingStoreState` após `SaleCompleted`; a UI não aplica mutações.

Verificações no laboratório

O `CustomerAiLab` agora verifica:

1. Uma opção de compra escolhida produz evento de venda, baixa uma unidade e adiciona o total em centavos ao caixa.
2. Desistência não altera o estado da loja.
3. Produto sem estoque não é oferecido ao motor; tentativa direta ao domínio é rejeitada sem alteração.
4. Loja fechada é rejeitada sem alteração.
5. Repetir a decisão da venda é rejeitado sem nova baixa ou recebimento.

Dados utilizados

O repositório não contém catálogo ou preços reais para esta prova. O laboratório usa a fixture sintética `lab-product`, estoque 2 e preço de 500 centavos, explicitamente marcada como dado de teste. Os perfis e parâmetros de preferência vêm do arquivo real do projeto `Content/AI/customer-choice-v1.json`. Nenhum valor da fixture deve ser interpretado como dado de mercado ou comportamento real de cliente.

Limites e regra de memória

- A venda confirmada é um fato do domínio; não é ainda avaliação de satisfação.
- Não atribuímos pontuação ao sucesso da venda, porque o projeto ainda não define como medir qualidade, espera, substituição ou satisfação, e não há dataset real de clientes.
- Portanto, esse fluxo não gera `ConfirmedCustomerExperience` nem atualiza a memória. Esse ponto será implementado quando houver uma regra determinística de experiência ligada a resultados observáveis do atendimento.
- O snapshot é volátil; não há save nem banco de dados.
- A fatia não simula rivais, atendimento de fila, custo/CMV, margem ou fechamento de caixa.

Validação

O analisador de problemas do VS Code foi consultado para todos os arquivos C# alterados e não apontou erros. A tentativa de executar ferramentas CLI nesta sessão é bloqueada pelo ambiente sandbox. O usuário precisa abrir o projeto no Godot e apertar F5; só então os novos checks podem ser marcados como executados.

Próxima etapa

Validar no Godot e, depois, decidir com base em regras de atendimento e dados disponíveis quais fatos observáveis podem compor uma avaliação de experiência. Não usar uma pontuação de cliente inventada como dado real. O próximo vertical slice econômico pode adicionar catálogo/estoque parametrizados e custo de mercadoria antes de expandir o mercado.
