ACTION 4 — contrato tipado de experiência confirmada

Objetivo

Passar da chamada direta com três valores (`decisionId`, `shopId`, `satisfaction`) para um resultado estruturado enviado explicitamente pela aplicação à sessão do cliente. Esta camada não cria vendas nem confirma resultados sozinha.

Implementação

- `AI/Contracts/ConfirmedCustomerExperience.cs` é um objeto imutável com ID da decisão, ID da loja e satisfação no intervalo inclusivo de -1000 a 1000.
- `CustomerChoiceSession.RecordConfirmedExperience` agora recebe esse contrato, exige uma decisão anterior e que a loja corresponda à opção escolhida, e permite somente uma confirmação por decisão.
- Uma decisão de não comprar não pode receber experiência de compra. IDs vazios, satisfação fora da faixa, decisão desconhecida, loja divergente e repetição são rejeitados.
- A atualização da memória segue após essas validações. Satisfação e taxa de aprendizado continuam limitadas e aplicadas por `CustomerMemory`.
- A política de escolha não cria o resultado nem confirma que a experiência aconteceu. Na arquitetura final, a aplicação deverá construir o contrato somente após receber resultado concluído e validado pela simulação/domínio.

Testes adicionados ao laboratório

O `CustomerAiLab` testa:

1. Construção do contrato com ID vazio ou satisfação fora do limite.
2. Tentativa de registrar uma decisão inexistente.
3. Tentativa de confirmar loja diferente da escolhida.
4. Rejeição de confirmação duplicada.
5. Rejeição de experiência de compra depois da opção de não comprar.
6. Os cenários existentes de experiência positiva, negativa, não confirmada e replay.

Os cenários usam dados e resultados sintéticos. Eles verificam o formato e as guardas do fluxo, mas não provam que uma venda real ocorreu.

Decisões e limites

- O limite de satisfação é `[-1000, 1000]`, compatível com `CustomerMemory` e com os exemplos do laboratório; não é ainda uma escala de satisfação definida pelo design final.
- Uma decisão aceita pelo motor não basta para afirmar que houve atendimento concluído. A confirmação de conclusão é responsabilidade da futura aplicação/domínio.
- O contrato não é um mecanismo de segurança que prove quem o criou: qualquer código com acesso à classe pode instanciá-lo. A fronteira de autoridade é arquitetural e deverá ser respeitada pelo caso de uso quando houver simulação real.
- Não existe persistência da sessão, compra real ou integração com caixa, estoque ou mercado.

Validação

O analisador de problemas do VS Code não apontou erros nos arquivos C# alterados. Esta sessão não conseguiu executar o `dotnet build` porque o sandbox bloqueou a inicialização do processo. Depois, o usuário executou o projeto no Godot e enviou capturas mostrando os oito `PASS`, incluindo “o contrato valida faixa, decisão, loja e confirmação única”. Como as verificações rodam antes da tela ser exibida, a visualização confirma que todas as asserções do laboratório terminaram sem lançar erro. Action 4 aprovada no laboratório.

Esta execução confirma apenas o contrato e o fluxo sintético. Não comprova uma compra real nem uma emissão de experiência pela simulação/domínio; esse ponto de integração ainda não existe.

Próxima etapa

Próxima fatia registrada na Action 5: primeiro fluxo de venda no domínio/aplicação, com estoque, caixa em centavos e evento de venda. A experiência do cliente continua sem pontuação até haver regra de satisfação fundamentada.