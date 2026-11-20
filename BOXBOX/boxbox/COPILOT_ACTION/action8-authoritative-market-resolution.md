# ACTION 8 — resolução autoritativa e idempotente do mercado

## Objetivo

Fechar a principal lacuna da Action 7: uma escolha de cliente passa a produzir um único resultado econômico autoritativo para a oportunidade inteira. A IA continua escolhendo somente uma intenção entre alternativas admissíveis; o domínio decide se a intenção pode ser executada e confirma, de forma atômica, a oportunidade, a BOXBOX e o concorrente selecionado.

Este é um slice do núcleo, sem nova tela. Ele prepara uma volta jogável futura, mas ainda não cria relógio, chegada de clientes ou dia de operação no Godot.

## Decisões confirmadas preservadas

- BOXBOX, concorrentes admissíveis e não comprar disputam a mesma oportunidade.
- A IA não baixa estoque, não credita caixa e não encerra oportunidade.
- Produto e quantidade vêm da oportunidade autoritativa, não da proposta do motor.
- Dinheiro permanece em centavos inteiros.
- Uma oportunidade aceita somente um terminal econômico.
- Uma rejeição devolve o mesmo snapshot, sem estado parcial.
- Eventos carregam IDs e valores estruturados, nunca texto traduzido.
- O mesmo snapshot, comandos e seed reproduzem o mesmo resultado.

## Estado e contratos adicionados

### Catálogo mínimo

`ProductCatalog` mantém a relação imutável entre `ProductId` e `CategoryId`. A construção do mercado rejeita produto desconhecido ou produto ligado à categoria errada antes da escolha. Nomes visíveis continuam fora do domínio e deverão usar localização.

### Snapshot compartilhado

`SharedMarketState` reúne, em um snapshot imutável:

- catálogo de produtos;
- estado da BOXBOX;
- estados resumidos dos concorrentes;
- registros de oportunidades.

Cada `MarketOpportunityRecord` possui um dos estados:

- `Pending`: ainda pode receber uma tentativa;
- `Resolved`: guarda exatamente um `MarketOpportunityResolved`.

O commit cria um novo snapshot completo. Uma falha retorna a mesma instância recebida, o que torna verificável a ausência de alteração parcial.

### Comando e terminais

`ResolveMarketPurchaseCommand` contém `OpportunityId`, `DecisionId`, `CustomerId`, o terminal escolhido e, quando necessário, a loja e a oferta. Produto e quantidade são deliberadamente omitidos do comando e recuperados da oportunidade registrada.

Os quatro terminais possíveis são:

1. `PlayerStoreSale`: venda concluída na BOXBOX;
2. `CompetitorSale`: venda resumida concluída no concorrente selecionado;
3. `ChoseNotToPurchase`: havia alternativa de compra, mas o cliente desistiu;
4. `DemandUnserved`: nenhuma loja podia atender e restou somente não comprar.

## Fluxo da aplicação

`SharedMarketPurchaseService` executa a seguinte sequência:

1. rejeita oportunidade desconhecida, já resolvida, sem produto ou com `DecisionId` já confirmado;
2. forma as alternativas rivais por passabilidade usando o snapshot atual;
3. entrega à IA somente as opções admissíveis e observações permitidas;
4. recebe uma intenção canônica, sem mutação econômica;
5. converte a intenção em comando tipado, preservando a oferta real escolhida, inclusive no fallback de produto para categoria;
6. pede ao `SharedMarketState` a validação e o commit atômico;
7. retorna o novo snapshot e um evento causal, ou o snapshot original e o motivo da rejeição.

A checagem de replay e de decisão duplicada ocorre antes de chamar a IA ou consumir aleatoriedade.

## Contrato operacional

### Ator e ação

O ator de aplicação é o caso de uso de mercado. Sua ação é resolver uma oportunidade pendente com a intenção produzida por uma sessão de cliente.

### Pré-condições

- oportunidade conhecida e pendente;
- `DecisionId` ainda não confirmado no ledger de oportunidades deste snapshot;
- produto concreto conhecido pelo catálogo e pertencente à categoria declarada;
- quantidade positiva definida pela oportunidade;
- conjunto de alternativas rivais formado pelas guardas da Action 7;
- para venda na BOXBOX: loja correta, aberta, produto conhecido e estoque suficiente;
- para venda rival: concorrente e oferta corretos, loja aberta, capacidade positiva, oferta ativa, alvo compatível e disponibilidade suficiente.

### Duração, conclusão e timeout

A resolução é instantânea no tempo simulado e conclui dentro da chamada. Ainda não há duração de atendimento, reserva, fila, interrupção ou timeout porque o relógio da simulação não foi implementado.

### Efeitos

- venda na BOXBOX: baixa a quantidade vendida, credita o total em centavos e resolve a oportunidade no mesmo commit;
- venda rival: baixa a disponibilidade resumida da oferta, consome uma unidade de capacidade de atendimento e resolve a oportunidade no mesmo commit;
- desistência ou demanda não atendida: resolve a oportunidade sem alterar ativos econômicos;
- toda conclusão emite `MarketOpportunityResolved` com oportunidade, decisão, cliente, resultado, produto, quantidade e, quando existe venda, loja, oferta e valores.

### Invariantes obrigatórias

`SharedMarketInvariantRegistry` roda em todo commit bem-sucedido, sem opção para políticas desabilitarem a validação. Ele exige que:

- somente uma oportunidade pendente seja substituída pelo registro resolvido correspondente;
- catálogo, quantidade e produto autoritativos sejam preservados;
- venda na BOXBOX corresponda exatamente ao evento de venda e não altere concorrentes;
- venda rival corresponda exatamente ao evento do concorrente, preserve a BOXBOX e altere somente o rival selecionado;
- resultados sem venda não mudem nenhum ativo;
- nenhum concorrente ou oportunidade seja criado ou removido durante a resolução.

As invariantes locais de `StoreState` e `CompetitorMarketState` continuam responsáveis por caixa, estoque, quantidade de oferta, capacidade, abertura, vínculo e duplicidade.

### Falhas e recuperação limitada

Uma rejeição informa uma razão sem resolver a oportunidade e sem produzir evento econômico. A recuperação permitida neste slice é corrigir a entrada ou atualizar o snapshot e fazer nova tentativa normal enquanto a oportunidade continuar pendente. Não existe fallback que transforme automaticamente uma venda rejeitada em outra venda, nem recursão de tentativas.

Rejeição de comando, fallback de oferta por categoria e terminal sem compra permanecem conceitos distintos.

### Terminal e propriedade de progresso

O terminal possui escopo `task`: encerra somente aquela oportunidade, não o cliente inteiro, o dia ou a empresa.

Propriedade exercitada: para uma oportunidade pendente de produto concreto e uma intenção válida, uma chamada termina em um único `Resolved`; chamadas posteriores para a mesma oportunidade terminam em rejeição sem consumir aleatoriedade nem alterar ativos. A hipótese de justiça é que o chamador forneça uma intenção e um snapshot válidos; geração futura de demanda e agenda ainda estão fora deste slice.

## Concorrente resumido

`CompetitorMarketState.CompleteSale` agora conclui uma venda resumida e idempotente. Uma venda reduz a quantidade da oferta pelo número de unidades compradas e a capacidade em uma unidade por atendimento, independentemente da quantidade de produtos. Essa unidade de capacidade é uma hipótese temporária da janela atual; o relógio futuro deverá definir quando e como ela é restaurada.

O concorrente continua sem caixa, custo, lucro, dívida ou contabilidade completa. O objetivo é preservar causalidade suficiente para disputa de mercado sem simular uma segunda empresa inteira.

## Refatoração do verificador

O antigo `Program.cs` monolítico foi reduzido a sete linhas. Os checks foram separados por responsabilidade em catálogo, fixtures, propostas da IA, passabilidade, mercado compartilhado e resolução autoritativa.

O executável agora possui 27 checks. Os nove novos cenários comprovam:

1. venda da BOXBOX altera caixa, estoque e oportunidade uma única vez;
2. venda rival altera somente o concorrente selecionado;
3. fallback por categoria confirma a oferta exata que passou na admissibilidade;
4. desistência e demanda não atendida resolvem sem alterar ativos;
5. replay de oportunidade resolvida é rejeitado antes da aleatoriedade;
6. venda rival rejeitada preserva integralmente o snapshot e deixa a oportunidade pendente;
7. `DecisionId` duplicado é rejeitado antes da aleatoriedade;
8. catálogo rejeita produto desconhecido ou categoria divergente;
9. mesmo snapshot e seed reproduzem o mesmo resultado econômico.

## Validação executada em 19 de novembro de 2026

- `dotnet build BOXBOX/core/BOXBOX.Core.csproj --no-restore`: sucesso, zero avisos e zero erros;
- `dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj --no-launch-profile`: 27 checks aprovados;
- `dotnet build BOXBOX/verification/BOXBOX.Verification.csproj --no-restore`: sucesso, zero avisos e zero erros;
- `dotnet build BOXBOX/boxbox/BOXBOX.csproj --no-restore`: sucesso, zero avisos e zero erros;
- `dotnet format` em Core e Verification com `--verify-no-changes`: sucesso;
- Godot 4.7.2 .NET em modo headless: projeto aberto e encerrado com código `0`.

## Limites atuais

- A sessão do cliente registra a decisão antes do commit econômico. As pré-validações eliminam as rejeições normais esperadas, mas um estado produzido por um caminho externo ainda pode causar rejeição depois da escolha e consumir aquele `DecisionId` na sessão.
- `ExecutePlayerStoreOnly` continua público para o laboratório Godot e não deve ser usado pelo gameplay futuro para contornar o mercado compartilhado.
- Não existe persistência/save do snapshot ou do ledger de oportunidades.
- Não existem relógio, agenda de demanda, fila, duração de atendimento ou reposição de capacidade.
- Não existe custo da mercadoria, lucro, livro-caixa ou contabilidade do concorrente.
- Promoção, fofoca e conhecimento atrasado continuam fora deste slice.
- A distinção entre desistência e demanda não atendida é derivada das alternativas formadas pela aplicação; ainda não existe um gerador temporal de demanda.
- Os checks são executáveis determinísticos, não uma suíte descoberta por `dotnet test`, e ainda não há CI.

## Próximo slice recomendado

Implementar um relógio mínimo e uma agenda determinística de demanda que criem oportunidades e processem uma volta jogável curta: avançar o tempo, gerar chegada, resolver a compra pelo fluxo desta Action, atualizar um painel provisório e fechar o período. A mesma seed e os mesmos comandos devem produzir o mesmo caixa, estoque e eventos em velocidades diferentes.

Depois desse slice, o projeto terá informação suficiente para definir a lista real de assets de gameplay e iniciar produção visual com menos retrabalho. Direção visual, identidade, paleta, cenário-base e interface provisória já podem ser explorados antes; arte final em volume deve esperar essa volta jogável.

## Hipóteses e questões abertas

Hipóteses deste slice:

- uma unidade de capacidade rival representa um atendimento concluído;
- resolução é instantânea;
- produto concreto é obrigatório no caso de uso compartilhado;
- o concorrente agregado precisa somente de oferta e capacidade para este experimento.

Questões ainda abertas:

- duração real de atendimento e granularidade do relógio;
- política de chegada de clientes e formação das oportunidades;
- momento de reposição da capacidade rival;
- câmera, linguagem visual e lista final de assets;
- persistência e versionamento do ledger de oportunidades.
