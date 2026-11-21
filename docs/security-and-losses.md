# BOXBOX — sistema de perdas e segurança

## Decisões confirmadas

- Funcionários podem furtar dinheiro ou mercadorias.
- A loja pode sofrer roubo durante o funcionamento.
- Sangrias corretas reduzem o dinheiro acessível no caixa.
- Seguro em dia pode cobrir parte das perdas conforme a apólice.
- O jogador precisa perceber sinais, definir controles e administrar a recuperação.
- O protagonista pode pegar, empunhar e disparar armas. A elegibilidade do disparo durante incidentes, suas consequências e as respostas dos demais atores ainda são questões abertas. Até essa especificação existir, o protocolo seguro continua sendo a resposta aprovada para roubo e confronto não é tratado como recompensa.

## Direção recomendada

O sistema deve criar escolhas de gestão, não uma loteria. Quando um incidente é elegível, nenhum controle promete risco residual zero, mas perdas graves precisam refletir exposição, horário, valor dos itens, controles, histórico e decisões do jogador. Loja fechada, proteção inicial ou intervalo mínimo entre incidentes podem tornar o evento inelegível por regra determinística. Toda ocorrência é reproduzível por seed e explicável nos relatórios quando descoberta.

O jogo separa três camadas:

1. **Realidade física:** quanto dinheiro e estoque realmente existem.
2. **Registro da empresa:** quanto deveria existir segundo caixa, compras e vendas.
3. **Conhecimento do jogador:** quais diferenças, causas e evidências já foram descobertas.

Isso permite um furto oculto sem fazer dinheiro “sumir” tecnicamente. O evento existe no log da simulação; a conciliação revela a diferença e a investigação pode revelar a causa.

## Sangria de caixa

O dinheiro percorre locais distintos:

```text
Caixa → Cofre → Banco
```

O jogador configura:

- valor máximo no caixa;
- reserva mínima para troco;
- sangria por horário ou por limite;
- funcionário autorizado;
- destino e conferência.

Pouca sangria aumenta o valor perdido num roubo. Sangria demais ocupa o jogador ou funcionário, aumenta fila e pode deixar o caixa sem troco. A sangria é uma transferência: não altera receita ou lucro e sempre registra origem, destino, horário, valor e responsável.

Cofres podem variar em capacidade e proteção. Dinheiro já depositado no banco deixa de estar fisicamente exposto na loja.

## Furto por funcionário

Funcionários não recebem um rótulo visível de “honesto” ou “ladrão”. Uma intenção de furto pode emergir da combinação de acesso, oportunidade, valor, satisfação, confiança, controles, percepção de detecção e ruído semeado. Nenhum atributo demográfico participa do cálculo.

Possíveis sinais, sem ensinar procedimentos criminosos em detalhe:

- diferenças de caixa recorrentes em determinados turnos;
- estoque físico menor que o registrado;
- cancelamentos, descontos ou acessos fora do padrão;
- perdas concentradas quando certos controles estão ausentes.

O jogador pode conferir, auditar, restringir permissões, mudar função ou investigar. Evidência precisa ser separada de suspeita. Agir sem base pode afetar moral, confiança, rotatividade e reputação.

## Roubo à loja

Antes de calcular risco, uma guarda determinística verifica:

- loja aberta e cenário habilitado;
- período de proteção inicial encerrado;
- intervalo mínimo desde o último incidente cumprido;
- ausência de outro incidente crítico incompatível;
- existência de ativos ou exposição elegíveis.

Se qualquer condição falhar, não há sorteio. Somente depois o risco residual considera:

- horário e eventos locais;
- valor e visibilidade dos itens;
- dinheiro acessível no caixa;
- iluminação, alarme, cofre e layout;
- rotina previsível de sangria;
- histórico recente e ruído semeado.

O jogo não premia enfrentar fisicamente o criminoso. O protocolo padrão protege clientes e funcionários; alarmes e outras respostas funcionam conforme os controles instalados. O desafio está em prevenir, limitar a exposição, manter seguro adequado e recuperar a empresa.

Depois que uma ameaça é confirmada, ações rotineiras deixam de ser candidatas. O conjunto válido fica restrito a respostas seguras, como ativar protocolo, orientar pessoas, pedir assistência e preservar evidências quando possível. Uma política ponderada nunca pode preferir patrulha comum ou confronto ao protocolo.

O incidente pertence à loja, não ao segurança:

```text
StoreIncidentState = None | Suspected | Confirmed | SafeResponse | Resolved | Recovery
```

Toda loja aberta possui um `ResponsibleOperator`: o proprietário ou um funcionário presente. Ao chegar em `Confirmed`, esse responsável sempre pode ativar o protocolo seguro; se o jogador estiver ausente, a transição ocorre automaticamente segundo a política configurada. Contratar segurança melhora prevenção, detecção e mitigação e pode assumir `IncidentSupport`, mas nunca é requisito para existir uma saída segura. Assim, uma loja pequena sem guarda não entra em deadlock durante um roubo.

Um incidente pode provocar:

- perda do caixa acessível;
- perda de mercadoria ou dano;
- fechamento temporário;
- queda de moral ou ausência de funcionário;
- franquia, espera pela indenização e pressão no capital de giro.

O início terá proteção determinística contra eventos catastróficos. Incidentes graves terão limites de severidade, sinais e intervalo mínimo para evitar sequências injustas. A perda nunca ultrapassa os ativos fisicamente acessíveis nem os limites da fase atual.

O primeiro roubo pode ensinar o sistema com severidade baixa. Como a simulação avança enquanto o jogador está ausente, nenhuma ocorrência exige reflexo ou presença online: seguranças, funcionários e protocolos existentes respondem conforme capacidade, atenção e autoridade reais. Isso protege pessoas, não garante o patrimônio. O roubo pode remover ativos, permanecer oculto até conciliação e contribuir para insolvência ou falência offline. O retorno mostra somente fatos já detectáveis pelo conhecimento do jogador; o log técnico preserva o evento completo.

## Seguro

Uma apólice informa antes da contratação:

- prêmio e vencimento;
- vigência e carência;
- franquia;
- caixa, estoque, equipamento ou interrupção cobertos;
- limites de indenização;
- requisitos de segurança;
- prazo esperado de pagamento.

O seguro não apaga todo o prejuízo. Uma apólice barata pode ter franquia alta ou limite baixo; uma cobertura ampla consome capital mensal. Seguro vencido ou fora da vigência não cobre o evento. Negativas e pagamentos sempre citam as regras que já existiam no contrato.

Histórico de sinistros pode elevar prêmios ou mudar ofertas futuras, mas somente por regras visíveis, limitadas e previsíveis. Um modelo pode propor uma oferta, mas um **validador determinístico de contratos** aceita ou rejeita tipos, faixas, cronologia, prêmio, limites, coberturas e cláusulas antes de criar o contrato imutável. Propostas inválidas são rejeitadas e substituídas por uma política heurística limitada.

Para a primeira versão, a apólice básica deve cobrir apenas roubo confirmado de caixa e equipamentos. Diferença ainda sem causa e furto interno ficam fora até o sistema de evidências estar maduro. Avise vencimentos com antecedência e permita débito automático ou pequena tolerância configurada, evitando que um clique esquecido invalide silenciosamente toda a cobertura.

## Seguranças e progressão

O primeiro segurança não é “fraco” por força física. Ele possui treinamento básico, percepção situacional limitada, menor disciplina de protocolo, equipamentos simples, pouca familiaridade com a loja e dificuldade para cobrir tarefas simultâneas.

Um segurança combina dimensões separadas e compatíveis entre si:

```text
DutyState      = OffDuty | OnDuty | OnBreak
Assignment     = Unassigned | Entrance | Patrol | Cameras | CashDropEscort | IncidentSupport
Attention      = Focused | Strained | Distracted
Response       = Normal | Alerted | SafeProtocol | Reporting | Recovering
```

“Surpreendido” é o resultado de um incidente não detectado a tempo, não um traço permanente. Isso gera atraso e menor mitigação, mas nunca transforma o sistema em combate.

As dimensões são compostas, porém condicionadas: `OffDuty` e `OnBreak` exigem `Unassigned`; uma atribuição operacional exige `OnDuty`. `Alerted` exige um sinal, suspeita ou alarme ativo; `SafeProtocol` exige ameaça confirmada; `Reporting` e `Recovering` exigem um incidente atual ou recém-encerrado. Assim, combinações contraditórias são rejeitadas antes da política ponderada.

O estado de resposta do segurança acompanha a máquina de incidente da loja, mas não a substitui. Se não houver segurança, proprietário e funcionários continuam usando o protocolo básico; se houver, o profissional adiciona cobertura e mitigação conforme suas capacidades.

Ações possíveis incluem observar, patrulhar, monitorar câmeras, acompanhar sangria, verificar alarme, relatar anomalia, ativar protocolo seguro, orientar pessoas, pedir assistência, preservar evidências, pausar e trocar de posto.

A IA do segurança funciona em duas etapas:

1. o sistema de regras remove ações proibidas ou impossíveis;
2. a política ponderada escolhe entre as ações válidas.

Fatores de peso incluem exposição de caixa, risco do horário, lotação, visão obstruída, tarefa atual, treinamento, familiaridade, fadiga, pausas atrasadas, equipamento, falsos alarmes, áreas descobertas e sangria programada. Distração sempre possui causas registradas, como cansaço, monotonia, sobrecarga, loja cheia, tarefas concorrentes, posto descoberto ou equipamento falhando.

### Progressão sugerida

1. **Cobertura básica:** posto fixo e protocolo simples; não cobre entrada e sangria simultaneamente.
2. **Treinado:** melhor percepção, rondas, pausas, escolta de sangria e protocolo mais consistente.
3. **Equipado:** câmeras, rádio, alarme, iluminação e controle de acesso, todos dependentes de manutenção.
4. **Equipe redundante:** entrada, câmeras e sangria podem receber cobertura simultânea; escala e comunicação passam a importar.
5. **Gestão de segurança:** supervisor ou serviço profissional permite políticas por horário, auditoria e análise de cobertura.

Melhorias reduzem frequência, tempo de detecção e severidade; nunca criam imunidade. Câmeras sem monitoramento podem produzir evidência posterior sem impedir o incidente. Alarmes sensíveis demais provocam falsos positivos e fadiga de alerta. Pausas reduzem fadiga, mas exigem cobertura. Uma equipe maior melhora redundância e aumenta custo fixo.

A existência de armas e disparos no mundo não transforma o segurança em combatente automático nem recompensa resistência. O segurança detecta, alerta, orienta pessoas, cumpre protocolo, acompanha sangria e preserva evidências. Qualquer futura ação armada exige regras explícitas de admissibilidade, autoridade e consequência antes de entrar no conjunto válido.

## Risco na mercearia

Volume alto esconde pequenas diferenças. Sangria compete com atendimento, especialmente em picos de movimento com mais dinheiro no caixa.

## Progressão

### Etapa 1 — controle de caixa

Caixa, cofre, banco, troco, sangria e conciliação. Sem crime ainda; o objetivo é validar a contabilidade física.

### Etapa 2 — risco externo

Roubo abstrato, indicador de exposição, segurança básica, cofre, alarme, protocolo e seguro básico.

### Etapa 3 — risco interno

Funcionários com permissões, diferenças ocultas, auditoria, evidências e decisões de pessoal.

### Etapa 4 — expansão

Segurança treinada ou equipe redundante, controles avançados, coberturas adicionais, histórico de sinistros e possível furto por clientes usando a mesma infraestrutura.

## Menor experimento jogável

Depois de validar vendas e sangrias sem crime, teste um único turno de pico da **mercearia** com caixa, cofre, banco, troco mínimo, um segurança, indicador de exposição, roubo abstrato leve e apólice básica. O segurança alterna entre posto, pausa, escolta de sangria, alerta e protocolo; sua atenção pode estar focada, pressionada ou distraída. Compare em simulações três políticas: nunca abandonar a entrada, sempre acompanhar sangrias e equilibrar as duas exposições. O objetivo é garantir que nenhuma domine sempre e que toda falha seja explicável.

## Indicadores para o jogador

- dinheiro acessível no caixa;
- exposição média e máxima durante o dia;
- diferenças de caixa e estoque;
- perdas conhecidas e não explicadas;
- custo de segurança e seguro;
- valor segurado, franquia e exposição descoberta;
- tempo até pagamento de sinistro;
- ocorrências e sinais por turno, sem acusação automática.

## Questões ainda abertas

- intensidade visual e sonora do roubo;
- presença ou não de polícia como sistema;
- possibilidade futura de furto por clientes;
- regras exatas de investigação e desligamento;
- tipos de seguro disponíveis em cada fase;
- quanto da resolução ocorre em tempo real ou por políticas automáticas;
- contratação direta, empresa terceirizada ou ambas;
- efeitos de controles excessivamente intrusivos sobre conforto e fluxo de clientes.
