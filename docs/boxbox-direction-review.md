# BOXBOX — estudo de direção

21/11/2026 · Avaliação do checkout atual

**Hipótese a testar.** Você toca uma mercearia de conveniência na periferia brasileira e vive no mesmo bairro que sustenta o seu negócio. Dirigir, conversar e operar o comércio criam escolhas que aparecem juntas. Hoje o projeto já demonstra partes desse jogo em execuções separadas.

## O que existe hoje

| Frente | Evidência | Lacuna que afeta o jogador |
| --- | --- | --- |
| Rua | A cena externa permite andar, entrar num veículo e dirigir. O minimapa mostra a cena 3D carregada e segue o foco do jogador. | A mercearia e o atacado ainda não são destinos conectados a uma mesma missão de rua. |
| Comércio | A cena inicial é a bancada dos interiores. Ela tem pedido, débito na confirmação, atendimento por janela, HUD de caixa e carteira e fechamento. Nando tem uma oferta condicionada a conversa e relação. | A bancada troca de local por botão. A mercadoria confirmada entra na preparação do dia sem percorrer a caminhonete, o depósito e a prateleira. |
| Expediente | O Dia 1 calcula o dia inteiro ao abrir e revela os resultados em sequência. | O jogador não consegue reagir ao faltar mercadoria, ajustar um preço durante o dia ou observar a próxima decisão do cliente responder à ação. |
| Mundo vivo | O núcleo headless tem atores, mercado, agenda, save JSON e replay. | Os compradores visuais e o comércio que abrem no Godot ainda não compartilham esse estado persistente. |
| Arte e voz | As paredes fotográficas, os pisos cartoon, os interiores recortados e a legenda contextual já formam uma linguagem. | Falta essa linguagem aparecer num trecho contínuo de rua. A leitura da loja precisa incluir os produtos que o jogador realmente vende. |
| Entrega | O recorte headless é verificável e existe histórico de teste no Godot. | A primeira sessão integrada e o empacotamento para distribuição seguem em aberto. O nome comercial também precisa de validação. |

[Entrada atual do Godot](../BOXBOX/boxbox/project.godot) · [Dia 1](../BOXBOX/boxbox/Application/BusinessDay/GroceryDayOneScenario.cs) · [Mundo vivo](living-world-vertical-slice.md) · [Distribuição para PC](distribution-options.md)

## Verificação executada

As 47 verificações headless passaram no checkout analisado. Em um teste adicional pela entrada de linha de comando, o Dia 1 com seed 1 e leite a R$ 1 milhão vendeu duas unidades desse leite e terminou com R$ 2.000.178,00 de caixa. O editor de preço do Godot tem um limite próprio. O caso reproduzível confirma uma lacuna de validação no núcleo e na admissibilidade da oferta.

```bash
dotnet run --project BOXBOX/day-one/BOXBOX.DayOne.csproj --configuration Release -- \
  --seed=1 --milk=4:1000000:regular
```

## O conflito de direção

O projeto promete duas coisas que podem se reforçar. Na rua, o jogador tem liberdade para ir aonde quiser. No comércio, o tempo, o estoque e as relações dão consequências a essa liberdade. Se dirigir servir só para levar caixas entre marcadores, a segunda viagem vira repetição. Se todas as decisões acontecerem no menu, a rua vira cenário.

O primeiro teste deve pôr uma escolha no percurso. Por exemplo, buscar pouco deixa caixa livre para outra compra, mas pode faltar leite no meio do expediente. Buscar mais toma espaço e empata dinheiro. Depois de abrir, uma ruptura precisa ter uma resposta ao alcance do jogador, como repor a partir do que ficou no depósito. A oferta de café do Nando já é um ensaio de relação contextual. Para descobrir o valor dessa relação no jogo, testar mais tarde um efeito diretamente ligado à viagem, como separar mercadoria para retirada em certo horário.

As referências trazem princípios distintos. *Big Ambitions* ajuda a avaliar quando a logística dá significado à progressão e quando pode ser delegada. *Supermarket Simulator* ajuda a estudar o retorno visual de abastecer e ver vender. GTA foi a referência do usuário para objetivo, orientação e presença na rua. O teste do BOXBOX é se esses princípios sustentam uma história original de comércio e vizinhança. [Estudo de referências](../research/grocery-interiors-and-shopping-ui.md)

## Próximo recorte recomendado

1. **Uma viagem de ida e volta em uma rua.** Colocar mercearia e atacado em endereços estáveis no mesmo trecho e ligar o pedido confirmado à caminhonete. Um marcador acompanha o objetivo ativo. GPS completo e mapa geral podem esperar até existir um trajeto para orientar.
2. **Uma venda que o jogador consegue influenciar.** Ao voltar, descarregar e repor um produto. Abrir o expediente inicia o avanço de agenda. Testar uma reposição durante o movimento de clientes e exibir a baixa física e o crédito no caixa.
3. **Um retorno que preserve escolhas.** Salvar com a carga na caminhonete e retomar sem cobrança em dobro. Depois de fechar, voltar outro dia e encontrar pelo menos uma consequência observável do expediente anterior.

Para o primeiro teste, usar um fornecedor, um produto que possa acabar, um destino por vez e um rival simples. Manter a bancada como teste das interfaces durante a ligação das cenas. Retratar o bairro com materiais, comércio, gente e hábitos reconhecíveis em uma quadra antes de expandir os distritos. Roda radial, eventos complexos de polícia, dublagem extensa e catálogo grande podem ser avaliados depois da primeira sessão integrada.

## Decisões de arquitetura para validar nessa fatia

- A simulação é dona da sequência pedido reservado → carga na caminhonete → depósito → prateleira → venda. Caixa, carga e estoque precisam conservar seus totais. Repetir um comando ou recarregar no meio da viagem não cria outra cobrança nem outra entrega.
- A aplicação valida preço e capacidade de pagamento antes de a preferência do cliente escolher uma oferta. O caixa e a carteira permanecem separados. O HUD e as animações consomem resultados confirmados.
- Cliente, fornecedor, pedido, veículo e endereço têm identidades estáveis. A câmera, o minimapa e a célula carregada podem mudar sem modificar os fatos do mundo.
- O Dia 1 e o mundo vivo já possuem partes reutilizáveis. Antes de os ligar, definir qual agenda e qual estado serão a fonte da primeira sessão jogável. Evitar que a bancada, a rua e o save mantenham três cópias da mesma compra.

[Contrato econômico](grocery-economy-contract.md) · [Missão de abastecimento](grocery-supply-run.md)

## O que observar num primeiro playtest

- A pessoa encontra o atacado sem explicação verbal e consegue voltar com o pedido.
- Ao ver o produto na prateleira e o saldo mudar, consegue apontar a causa.
- Em outra tentativa, muda por iniciativa própria uma decisão de compra, preço ou reposição.
- Quando perguntada sobre o trecho que repetiria, aponta uma escolha ou situação do bairro. Se quiser pular a viagem inteira, investigar o trajeto e a decisão que ele deveria sustentar.

O objetivo do estudo é decidir se vale aprofundar essa combinação de rua e comércio. Uma sessão curta e contínua dá evidência para essa decisão.

## Premissa narrativa esclarecida

O protagonista vem da periferia, passou pela prisão e, ao sair, abre uma pequena mercearia. Ele tenta subir na vida enquanto mantém vínculos e toma decisões moralmente questionáveis. Agiotagem, vínculos com uma organização criminosa e conflitos com concorrentes fazem parte do horizonte narrativo. A mercearia é o ponto de partida da trajetória. A história de origem será ficcionalizada, com personagens e detalhes identificáveis próprios do BOXBOX.

A hipótese de narrativa é que o jogador possa sentir o peso de uma escolha em duas partes da vida. Um favor pode resolver uma dificuldade imediata e criar uma obrigação que aparece mais tarde. Relações, dívidas e consequências precisam persistir para que a história seja sentida no bairro e no comércio. As regras de caixa continuam explícitas, e negociação ou reputação podem alterar opções disponíveis conforme o contexto. A primeira viagem de abastecimento continua sendo o teste da base jogável. O grau de agência sobre o envolvimento clandestino e a ordem de introdução desses conflitos permanecem abertos.
