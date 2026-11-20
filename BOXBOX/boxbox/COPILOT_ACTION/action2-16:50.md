ETAPA 2 — fluxo de experiência do cliente

Objetivo

Sair do teste isolado de memória e verificar um fluxo pequeno, sem interface, em que uma sessão mantém a memória do mesmo cliente entre decisões. Uma experiência só altera essa memória depois que a aplicação chama explicitamente o registro de uma experiência confirmada. O motor de escolha continua retornando uma proposta e não altera domínio nem memória diretamente.

Implementação

- `AI/Customers/CustomerChoiceSession.cs` representa a sessão de um cliente, contendo perfil, motor, taxa de aprendizado, memória e decisões já realizadas.
- `Decide` personaliza o fator de familiaridade de cada loja com a memória atual, recebe opções admissíveis e uma fonte aleatória injetada e registra qual loja foi escolhida.
- `RecordConfirmedExperience` exige uma decisão anterior e que a loja informada corresponda à loja selecionada. Também impede registrar duas vezes a experiência da mesma decisão. Só então atualiza a memória.
- IDs de decisão repetidos são rejeitados. Se a pessoa não compra, não há loja para registrar como experiência.
- Não há persistência entre sessões nem conexão com estoque, venda, caixa ou eventos reais do jogo.

Verificações automatizadas do laboratório

Ao abrir a cena, `CustomerAiLab.cs` executa estas provas:

1. Experiência positiva confirmada na BOXBOX aumenta familiaridade e probabilidade da loja na próxima decisão.
2. Experiência negativa confirmada na BOXBOX reduz familiaridade e probabilidade na próxima decisão.
3. Sem chamar o registro de confirmação, a familiaridade e a probabilidade permanecem iguais.
4. Quatro decisões com experiências confirmadas e fontes aleatórias derivadas da mesma seed produzem a mesma sequência ao repetir o fluxo.
5. Os testes positivo/negativo usam uma fonte aleatória controlada para selecionar BOXBOX nas duas avaliações, mantendo as ofertas iguais e isolando o efeito da memória.

Os testes interrompem o laboratório com erro se uma propriedade não for atendida e mostram `PASS` no relatório quando passam. Eles exercitam a sessão em memória; não são ainda testes unitários num projeto de testes separado.

Critério para avançar

O fluxo é considerado aprovado quando o projeto compilar e o Godot mostrar os novos `PASS`, com a experiência positiva aumentando e a negativa diminuindo a preferência, nenhuma alteração sem confirmação e replay repetível. Depois disso, a próxima fatia pode ligar um resultado estruturado emitido pela aplicação/simulação ao método de confirmação. A origem do resultado deve ser autoritativa e confirmada pelo domínio, nunca inferida pelo motor de IA.

Validação nesta etapa

O analisador de problemas do VS Code não apontou erros nos dois arquivos C# alterados. A tentativa de executar `dotnet build` nesta sessão foi bloqueada pelo sandbox antes de iniciar o processo. Depois, o usuário executou o projeto no Godot e enviou capturas do novo relatório. Os valores observados foram:

- Positiva (+900): familiaridade 500 → 581; probabilidade 38,5% → 46,7%.
- Negativa (-900): familiaridade 500 → 419; probabilidade 38,5% → 30,7%.
- Sem confirmação: familiaridade 500 → 500; probabilidade 38,5% → 38,5%.

A cena exibiu o relatório completo; como `RunChecks()` roda antes de renderizar, isso também confirma que as asserções de replay e dos demais cenários não lançaram erro. A etapa 2 está aprovada no laboratório. Ainda não há compras reais nem eventos da simulação conectados à sessão.

Fatia técnica sugerida — contrato de experiência confirmada

Definir um resultado estruturado de experiência produzido pela aplicação após um atendimento concluído e validado pelo domínio. A aplicação poderá encaminhar esse resultado à sessão do cliente; não deverá aceitar satisfação escolhida pelo motor de IA nem permitir que a própria política confirme a experiência. Antes de integrar com economia, criar e testar esse contrato independente de Godot, incluindo loja correspondente, decisão concluída, satisfação limitada e prevenção de duplicidade.

Esta fatia foi implementada na Action 4. A validação em execução dessa nova alteração ainda está pendente; o contrato recebe eventos sintéticos e não está conectado a resultados reais do domínio.