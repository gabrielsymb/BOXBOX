# ACTION 7 — mercado compartilhado seguro e núcleo .NET independente

## Objetivo do ciclo

Este ciclo fortalece duas fronteiras antes de ampliar a simulação:

1. o motor plugável passa a devolver uma proposta não confiável, que a aplicação valida e transforma em um resultado canônico; o mesmo núcleo agora compila como uma biblioteca .NET sem Godot;
2. o concorrente agregado ganha estado resumido, passabilidade explicável e uma separação explícita entre seu estado real e aquilo que o cliente tem permissão para perceber.

O escopo continua sendo um laboratório técnico. Ainda não existe um dia de mercado completo, campanha promocional real, atualização econômica do rival ou persistência.

## Decisões confirmadas preservadas

- Clientes escolhem somente entre alternativas admissíveis formadas antes da ponderação.
- BOXBOX, concorrentes e a opção de não comprar disputam a mesma oportunidade.
- IA e políticas produzem intenção; não podem criar uma alternativa, ignorar uma guarda ou mutar domínio diretamente.
- Concorrentes começam agregados e nomeados, sem uma segunda contabilidade completa.
- Estado privado do rival e do jogador não é entregue ao motor de escolha.
- Fato econômico, observação permitida e preferência do cliente permanecem conceitos distintos.
- Dinheiro usa centavos inteiros no domínio.
- Textos visíveis não são armazenados no domínio; identidades usam chave de localização e bloqueios usam códigos semânticos.
- O núcleo da simulação deve executar e ser verificável sem UI ou engine.

## Entrega 1 — proposta não confiável, resultado canônico e Core puro

### Novo contrato do motor

`ICustomerChoiceEngine` não devolve mais um `CustomerChoiceResult` pronto para ser aceito. Ele implementa `Propose` e retorna `CustomerChoiceProposal`, contendo:

- o ID da decisão;
- o ID da alternativa selecionada;
- avaliações identificadas somente por `OptionId`, com utilidade, probabilidade e contribuições.

O motor não devolve objetos `ChoiceOption` próprios. Isso impede que uma implementação plugável substitua silenciosamente loja, tipo, atributos ou utilidade-base de uma alternativa depois que a aplicação formou o conjunto admissível.

### Validação e canonicalização

`CustomerChoiceRequest` copia e valida o conjunto candidato antes da inferência: ele precisa ser não vazio, não conter opções nulas ou IDs repetidos e conter exatamente uma alternativa de não compra.

Depois, `CustomerChoiceSession.ValidateAndCanonicalize` trata a proposta como entrada não confiável e verifica, antes de registrar a decisão:

- proposta presente e ID da decisão igual ao pedido;
- cada avaliação presente e ligada a um ID admissível;
- ausência de IDs de avaliação desconhecidos ou repetidos;
- cobertura exata do conjunto admissível;
- alternativa selecionada pertencente ao conjunto admissível e com probabilidade maior que zero;
- probabilidades finitas, entre `0` e `1`, cuja soma difere de `1` no máximo pela tolerância `1e-9`;
- lista de contribuições presente.

Após as guardas, a sessão reconstrói `OptionEvaluation` usando os `ChoiceOption` originais do pedido, em ordem ordinal por ID. O `CustomerChoiceResult` possui construção interna e carrega somente essa representação canônica.

Uma proposta rejeitada lança `CustomerChoiceProposalViolationException` com um código estável. A decisão só entra em `_decisions` depois da canonicalização; portanto, uma rejeição não consome o ID nem confirma uma experiência.

Esta fronteira garante admissibilidade estrutural. Ela ainda não prova que um motor sorteou de acordo com a distribuição declarada, nem recalcula utilidades e contribuições para verificar a política interna do plug-in.

### Fronteira de build

Foi criado `BOXBOX/core/BOXBOX.Core.csproj` com `Microsoft.NET.Sdk`, `net8.0` e sem referência a Godot ou pacote NuGet. O projeto compila explicitamente as fontes de:

- `boxbox/AI/**/*.cs`;
- `boxbox/Application/**/*.cs`;
- `boxbox/Domain/**/*.cs`.

`BOXBOX.csproj` remove essas pastas de sua compilação própria e referencia `BOXBOX.Core`. Assim, a camada Godot contém a apresentação/laboratório e consome o Core, em vez de ser a unidade que define domínio, aplicação e IA.

`BOXBOX.Verification.csproj` também referencia diretamente o Core. As fontes ainda ficam fisicamente sob `BOXBOX/boxbox`; esta entrega cria a fronteira de assembly sem fazer uma mudança ampla de diretórios.

## Entrega 2 — concorrente resumido, passabilidade e percepção

### Estado resumido

O primeiro contrato de concorrente está dividido em objetos pequenos e imutáveis:

- `CompetitorProfile`: identidade, chave de nome localizada, categorias atendidas e atributos públicos de posicionamento;
- `CompetitorMarketState`: abertura, capacidade de atendimento disponível e ofertas atuais;
- `CompetitorOfferState`: alvo de categoria/produto, preço em centavos, quantidade disponível e vigência;
- `MarketPurchaseOpportunity`: oportunidade identificada, categoria, produto opcional e quantidade.

Esse recorte não contém caixa, custo, estratégia privada, dívida ou livro-caixa do concorrente.

### Passabilidade determinística

`AdmissibleMarketOfferGenerator` forma o conjunto do rival antes de qualquer peso. Para cada perfil, ele produz um diagnóstico técnico de `MarketOfferPassability` e somente emite uma oferta quando todas as condições atuais passam:

- o perfil atende a categoria;
- existe um estado atual correspondente;
- a loja está aberta;
- existe capacidade de atendimento;
- existe oferta específica para o produto ou uma oferta ampla da mesma categoria;
- a oferta está ativa;
- a quantidade disponível atende o pedido;
- existe uma observação permitida ligada à mesma oportunidade, concorrente e oferta.

Uma oferta exata válida tem precedência. Se ela estiver inativa, indisponível ou sem observação permitida, uma oferta válida da categoria pode servir como fallback. Peso de preferência não participa dessa escolha de passabilidade.

Falhas usam `MarketOfferPassabilityReason`: `MissingCurrentState`, `CategoryNotServed`, `ShopClosed`, `NoServiceCapacity`, `NoMatchingOffer`, `OfferInactive`, `InsufficientAvailability` ou `MissingPermittedObservation`. Os motivos são deduplicados e ordenados, mas permanecem `internal`: podem revelar disponibilidade ou capacidade reais e servem para replay, testes e diagnóstico técnico, não como conhecimento automático do jogador.

Perfis, estados, observações e vínculos duplicados ou desconhecidos são rejeitados na entrada. Ofertas admissíveis saem em ordem canônica por loja e ID da opção.

### Separação entre realidade e percepção

`CompetitorOfferObservation` representa somente o que o cliente pode perceber para uma oferta nesta oportunidade. Ela fornece o ID estável da opção, utilidade-base e `ChoiceFeatures` percebidos.

O gerador consulta quantidade, capacidade e vigência reais para passabilidade. `UnitPriceCents` permanece no estado resumido, mas este slice ainda não o converte em percepção. Ao criar `AdmissibleMarketOffer`, o gerador não copia esses valores reais: envia ao motor somente a observação permitida. A ausência de observação bloqueia a alternativa em vez de improvisar conhecimento.

`AdmissibleMarketOffer` agora possui construtor `internal`, assim código externo ao Core não fabrica uma oferta rival já marcada como admissível. O resultado público expõe as ofertas permitidas, mas não os diagnósticos privados de passabilidade.

### Fluxo público compartilhado

`SharedMarketPurchaseService` é a fronteira pública do caso de uso. Para uma oportunidade específica de produto, ele:

1. gera as alternativas rivais por passabilidade;
2. encaminha somente o conjunto admissível ao fluxo de escolha e venda;
3. usa `OpportunityId` como ID da decisão;
4. retorna a geração pública e o resultado autoritativo da loja do jogador.

O caminho que aceita ofertas alternativas em `CustomerPurchaseService` ficou `internal`. A única sobrecarga pública sem concorrentes chama-se `ExecutePlayerStoreOnly` e está explicitamente documentada como caminho de laboratório, não de demanda do gameplay.

O resultado distingue `ChoseNotToPurchase`, quando havia ao menos uma loja admissível, de `DemandUnserved`, quando a alternativa externa era a única possível.

### Contrato operacional deste slice

**Ação:** resolver uma oportunidade de compra compartilhada específica de produto por `SharedMarketPurchaseService.Execute`.

**Temporalidade:** ação instantânea no tempo da simulação. Esta implementação não possui duração, locks, reservas, interrupção ou timeout; a chamada termina com um resultado estruturado ou uma rejeição. Isso é uma limitação consciente do slice, não uma regra definitiva para capacidade ou filas.

**Pré-condições:**

- oportunidade, sessão, snapshot da BOXBOX, perfis, estados, observações e fonte aleatória presentes;
- oportunidade com quantidade positiva e `ProductId` definido;
- atributos percebidos da BOXBOX dentro de `0..1000`;
- IDs de perfis, estados, ofertas e observações únicos e vínculos conhecidos;
- alternativas rivais aprovadas pelas guardas de categoria, abertura, capacidade, vigência, disponibilidade e observação permitida;
- no máximo uma alternativa por loja e nenhum ID rival colidindo com os IDs reservados da BOXBOX ou de não compra;
- pedido ao motor com IDs candidatos únicos e exatamente uma alternativa externa.

**Conclusão, efeitos e saídas:**

- `SaleCompleted`: produz novo snapshot da BOXBOX e `CompletedSale`, com baixa de estoque e crédito em centavos confirmados atomicamente;
- `ChoseAlternativeStore`: preserva o snapshot da BOXBOX; o rival ainda não recebe baixa neste slice;
- `ChoseNotToPurchase`: preserva o snapshot quando havia loja admissível;
- `DemandUnserved`: preserva o snapshot quando nenhuma loja podia atender;
- `SaleRejected`: preserva a loja e retorna o código de rejeição do domínio;
- o resultado canônico da escolha contém apenas opções originalmente admissíveis; diagnósticos privados de passabilidade permanecem internos.

**Invariantes:** uma proposta inválida não altera estoque ou caixa; uma escolha rival nunca altera ativos da BOXBOX; somente venda concluída na BOXBOX produz `CompletedSale`; dinheiro permanece inteiro em centavos; a política nunca acrescenta alternativas; informação percebida não substitui o estado real usado pela passabilidade.

**Falhas e recuperação limitada:** entradas ou vínculos inválidos são rejeitados antes da escolha. Proposta malformada é rejeitada antes de registrar a decisão. Nesses casos, o protocolo deste slice é corrigir a entrada ou trocar/corrigir o motor e fazer um único retry pelo fluxo normal com o mesmo ID; os checks confirmam que essa correção seguida de retry não encontra caixa ou estoque parcialmente mutados. O limite de uma tentativa é operacional neste momento, pois ainda não existe contador persistido de recuperação. Não existe fallback recursivo. O fallback de oferta exata para categoria é uma regra normal de passabilidade, não recuperação de erro. Se uma decisão válida já foi registrada, o mesmo ID não pode ser reutilizado na mesma sessão.

**Terminal:** cada status acima encerra a tarefa local de escolha/compra com escopo `task`. Ele não encerra o dia nem a empresa. Também ainda não constitui um terminal autoritativo e persistido da `MarketPurchaseOpportunity`: não existe ledger que impeça outra sessão de tentar resolver novamente a mesma oportunidade, especialmente quando o resultado foi rival, desistência ou demanda não atendida.

## Hipóteses específicas deste slice

Estas escolhas tornam o experimento implementável, mas ainda não são decisões finais de balanceamento:

- atributos normalizados de conveniência, reputação e percepção usam a faixa `0..1000`;
- o contrato de oportunidade aceita produto opcional, mas o caso de uso compartilhado deste slice exige produto específico e rejeita resolução apenas por categoria antes de consumir decisão ou aleatoriedade;
- uma oferta exata válida vence a oferta da categoria; uma oferta ampla da categoria funciona como fallback quando a exata não passa;
- a relação entre `CategoryId` e `ProductId` é hipótese do chamador porque ainda não existe catálogo autoritativo que prove que o produto pertence à categoria;
- cada concorrente possui no máximo uma oferta atual por par categoria/produto;
- capacidade é um inteiro resumido e precisa ser maior que zero, sem fila ou reserva temporal;
- uma observação permitida já chega pronta à aplicação; origem, idade, confiança, alcance e distorção ainda não foram modelados;
- preço real não é convertido automaticamente em atratividade percebida;
- `CompetitorPublicAttributes` descreve sinais públicos, mas ainda não compõe automaticamente as `ChoiceFeatures` da observação.

## Invariantes e checks deste ciclo

### Propriedades exercitadas pelo verificador

O executável `BOXBOX.Verification` executou dezoito checks com sucesso:

1. mercado compartilhado contém BOXBOX, rival e não comprar;
2. escolher rival não altera caixa ou estoque do jogador;
3. BOXBOX fechada fica fora da escolha enquanto um rival admissível permanece;
4. promoção conhecida, ainda representada por percepção de preço, aumenta a preferência relativa pelo rival;
5. mudar preço, quantidade e capacidade reais sem refletir a mudança na observação permitida não altera a preferência do cliente;
6. IDs reservados para a opção da BOXBOX ou para não comprar são rejeitados antes da escolha;
7. proposta com ID de decisão divergente é rejeitada;
8. avaliações desconhecidas, duplicadas ou incompletas são rejeitadas;
9. alternativa selecionada desconhecida é rejeitada;
10. uma alternativa com probabilidade zero não pode ser selecionada;
11. probabilidades não finitas ou fora de `0..1` são rejeitadas;
12. probabilidades que não somam `1` são rejeitadas;
13. cada uma das oito guardas de passabilidade filtra a oferta rival inválida e registra o motivo técnico esperado;
14. uma oferta válida de categoria substitui uma oferta exata inválida;
15. uma oferta exata válida tem precedência sobre a oferta da categoria;
16. permutar perfis, estados e observações não altera a ordem canônica das alternativas;
17. oportunidade apenas de categoria falha antes de consumir decisão ou aleatoriedade, e uma tentativa válida posterior reutiliza o ID;
18. demanda não atendida é distinguida de desistência voluntária.

Nos checks de proposta inválida, a fixture também confirma que caixa e estoque permanecem inalterados e que um novo resultado válido pode reutilizar o mesmo ID de decisão e concluir exatamente uma venda.

Existem guardas implementadas para proposta nula, avaliação nula e contribuições ausentes, mas ainda não há checks específicos para esses três códigos.

### Verificações executadas em 19 de novembro de 2026

- `dotnet build BOXBOX/core/BOXBOX.Core.csproj`: sucesso, sem avisos ou erros;
- `dotnet build BOXBOX/boxbox/BOXBOX.csproj`: sucesso, compilando Core e projeto Godot, sem avisos ou erros;
- `dotnet run --project BOXBOX/verification/BOXBOX.Verification.csproj --no-launch-profile`: dezoito checks aprovados;
- Godot 4.7.2 .NET em modo headless, abrindo o projeto e a cena principal: encerramento com código `0`.

Os checks de mercado exercitam o `SharedMarketPurchaseService`, o gerador real e o fluxo de compra. `InternalsVisibleTo("BOXBOX.Verification")` permite inspecionar somente no verificador os motivos técnicos que não são públicos para a apresentação.

## Limites atuais

- Escolher o rival ainda não baixa disponibilidade ou capacidade resumida dele e não produz um resultado econômico do concorrente.
- Não existe um ledger autoritativo de oportunidades resolvidas; outra sessão ainda pode tentar resolver o mesmo `OpportunityId`, e a escolha do rival não possui idempotência de domínio.
- Não existe catálogo que valide a associação categoria/produto antes da passabilidade.
- Não existem horário agendado, fila, duração, reserva, timeout ou relógio de simulação nesta fatia.
- Promoção permanece uma alteração sintética da percepção; campanha, orçamento, período, unidades e canibalização não existem.
- Observações não possuem idade, fonte, confiança ou estado de conhecimento; fofoca e informação atrasada continuam fora do código.
- Uma observação precisa apontar para uma oferta ainda presente no snapshot atual; conhecimento atrasado sobre oferta já removida não é representável neste slice.
- Preço real, atributos públicos e posicionamento ainda não geram automaticamente as características percebidas.
- Não há geração de demanda, atualização de preço percebido ou reconciliação contábil completa do mercado.
- `ExecutePlayerStoreOnly` continua público para o laboratório; a separação de intenção está documentada, mas o tipo ainda não impede uso indevido por gameplay futuro.
- Os checks são um executável de verificação, não uma suíte descoberta por `dotnet test`, e ainda não há CI.

## Próximo passo de maior aprendizado

Adicionar a resolução autoritativa e idempotente da oportunidade, sem expandir ainda para promoção ou fofoca:

1. introduzir um registro imutável de oportunidade pendente/resolvida e um comando de resolução;
2. confirmar atomicamente um único resultado: venda na BOXBOX, venda resumida no rival, desistência ou demanda não atendida;
3. quando o rival for escolhido, baixar exatamente uma vez sua disponibilidade resumida e a capacidade aplicável;
4. rejeitar repetição sem mutação parcial e manter um evento causal reproduzível;
5. ligar produto e categoria a um catálogo autoritativo antes de formar as alternativas.

Esse slice fecha a principal lacuna restante do mercado compartilhado: hoje a escolha é segura para a BOXBOX, mas o rival e a própria oportunidade ainda não recebem um commit econômico autoritativo. A resolução deve continuar sem tocar diretamente os ativos da BOXBOX quando o resultado pertence ao concorrente.
