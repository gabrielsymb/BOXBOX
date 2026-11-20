# Perdas, segurança e seguro

## Intenção de design

Crie tensão entre operação, confiança, custo e exposição. O jogador deve conseguir explicar por que perdeu dinheiro, quais sinais ignorou e quais controles poderiam ter reduzido o impacto. Não use crime como evento raro que aparece do nada e apaga uma partida saudável.

Use **perdas** como categoria comum para diferença operacional, desperdício, furto interno, futuro furto por cliente e roubo. Preserve a causa específica nos eventos e relatórios.

## Estado físico e estado registrado

Furto pode permanecer oculto sem violar a auditabilidade técnica:

- o estado real guarda dinheiro e estoque fisicamente existentes;
- livros, sistema e conhecimento do jogador guardam valores esperados ou já observados;
- vendas, entregas e perdas legítimas atualizam os registros correspondentes;
- furto reduz o ativo físico e gera um evento interno oculto;
- conciliação, contagem ou evidência revela a diferença e permite o ajuste contábil.

O log completo da simulação é usado para replay, testes e depuração, mas a projeção da interface filtra fatos que o jogador ainda não descobriu.

## Sangria e locais de caixa

Modele ao menos `Register`, `Safe` e `Bank`. Uma sangria é uma transferência entre locais:

```text
CashDropPolicy
├── limite máximo no caixa
├── reserva mínima para troco
├── horário ou condição
├── executor autorizado
└── destino
```

Pouca sangria deixa mais dinheiro acessível em um roubo. Sangria excessiva consome tempo, pode ampliar filas e causar falta de troco. Permita políticas manuais, agendadas ou por limite, com automação progressiva.

Toda transferência conserva dinheiro, registra responsável e nunca altera receita, custo ou lucro sozinha.

Cofres podem variar em capacidade e proteção. Dinheiro depositado no banco deixa de estar exposto fisicamente na loja.

## Furto interno

Uma política como `EmployeeMisconductPolicy` produz uma intenção; o domínio valida acesso, oportunidade e ativo disponível antes de registrar a perda física.

Considere fatores configuráveis como:

- acesso ao caixa, cofre, estoque e funções sensíveis;
- oportunidade sem conferência ou supervisão;
- valor percebido e facilidade do desvio;
- confiança, satisfação e histórico da relação de trabalho;
- força dos controles e percepção de detecção;
- ruído semeado.

Não use gênero, idade, origem, aparência ou qualquer atributo demográfico. Não marque alguém visivelmente como “ladrão”. O jogador percebe anomalias: diferenças recorrentes por turno, estoque incompatível, cancelamentos incomuns, acessos ou padrões de perda.

Investigação pode gerar níveis de evidência. Restringir acesso, mudar função, auditar ou desligar alguém tem custo. Acusações sem base podem reduzir moral, aumentar rotatividade ou prejudicar reputação. Evidência forte precisa ser distinguível de mera correlação.

## Roubo à loja

Antes de qualquer sorteio, aplique guardas determinísticas: loja aberta, proteção inicial encerrada, intervalo mínimo desde o último incidente cumprido, ausência de incidente incompatível e existência de ativos elegíveis. Se uma guarda falhar, o roubo é inelegível. Somente então uma política como `RobberyRiskPolicy` combina exposição e contexto:

```text
risco-base
× horário e evento local
× perfil do segmento
× dinheiro acessível
× visibilidade e rotina
× histórico recente
× controles de segurança
× ruído semeado
```

O jogador reduz exposição com sangria, cofre, alarme, iluminação, treinamento, layout e seguro. Quando o incidente é elegível, nenhum controle promete eliminar todo o risco residual; guardas como loja fechada, proteção inicial e intervalo mínimo entre incidentes podem bloquear o sorteio por completo.

Durante um roubo, a resposta padrão preserva clientes e funcionários. Não recompense luta ou heroísmo físico. Quando a ameaça for confirmada, retire ações rotineiras do conjunto candidato e mantenha somente respostas seguras. Alarmes e protocolos podem agir quando instalados e seguros. Consequências possíveis incluem perda de caixa acessível, mercadoria, dano, interrupção e impacto temporário na equipe. A severidade respeita os ativos acessíveis e os limites da fase atual.

Modele `StoreIncidentState = None | Suspected | Confirmed | SafeResponse | Resolved | Recovery` no nível da loja. Toda loja aberta possui um `ResponsibleOperator`, proprietário ou funcionário presente, com acesso não bloqueável ao protocolo seguro quando a ameaça é confirmada. Na ausência do jogador, a política configurada executa a transição. Segurança é opcional: melhora prevenção, detecção e mitigação, mas não habilita a única saída do incidente.

Use proteção inicial determinística, limites de severidade, intervalos entre incidentes graves e sinais de risco. Uma única rolagem azarada não deve destruir uma empresa saudável sem contrajogo anterior.

O primeiro incidente pode funcionar como tutorial de baixa severidade, mas precisa continuar coerente com a simulação. Eventos ocorridos durante avanço offline nunca exigem uma reação imediata: protocolos e políticas configurados previamente resolvem a situação, e o retorno mostra um resumo causal.

## Seguro

Modele apólices como contratos determinísticos e versionados:

- prêmio e vencimento;
- período de vigência e eventual carência;
- riscos e ativos cobertos;
- franquia;
- limite por categoria e limite agregado;
- requisitos de segurança declarados antecipadamente;
- prazo de análise e indenização;
- estado de adimplência.

IA ou regras de análise de risco podem propor quais ofertas aparecem e a que preço. Um validador determinístico de contratos aceita ou rejeita tipos, faixas, cronologia, prêmio, limites, coberturas e cláusulas permitidas antes de criar o contrato imutável. Propostas inválidas são rejeitadas e substituídas por uma política heurística limitada. Uma vez contratada, a apólice não muda retroativamente. Pagamento ou negativa de sinistro precisa citar cláusula, perda reconhecida, franquia e limite.

Seguro reduz volatilidade; não restaura automaticamente tudo nem substitui boa gestão. Uma indenização tardia ainda pode pressionar o capital de giro.

Sinistros anteriores podem afetar ofertas e prêmios futuros, desde que a consequência seja limitada, antecipada e explicável.

Na primeira versão, prefira uma apólice simples para roubo confirmado de caixa e equipamentos. Não cubra automaticamente diferenças sem causa ou furto interno antes de o sistema de evidências existir. Vencimentos devem ter alerta claro; débito automático ou uma tolerância explícita evitam perda silenciosa de cobertura por falha de interface.

## Segurança humana

Modele segurança por capacidades, não por força física ou um nível único:

- percepção situacional;
- disciplina de protocolo;
- resistência à fadiga;
- familiaridade com o local;
- monitoramento de câmeras;
- cobertura de patrulha;
- escolta de sangria;
- consistência de resposta.

Componha dimensões de estado para evitar centenas de combinações monolíticas:

```text
DutyState      = OffDuty | OnDuty | OnBreak
Assignment     = Unassigned | Entrance | Patrol | Cameras | CashDropEscort | IncidentSupport
Attention      = Focused | Strained | Distracted
Response       = Normal | Alerted | SafeProtocol | Reporting | Recovering
```

“Surprised” é um resultado de detecção tardia, não uma personalidade. Ele atrasa protocolo e reduz mitigação sem criar combate.

Essas dimensões são condicionadas: `OffDuty` e `OnBreak` exigem `Unassigned`; atribuições operacionais exigem `OnDuty`. `Alerted` exige sinal, suspeita ou alarme ativo; `SafeProtocol` exige ameaça confirmada; `Reporting` e `Recovering` exigem incidente atual ou recém-encerrado. Valide a matriz de compatibilidade antes da política ponderada.

O estado do segurança acompanha a máquina de incidente da loja sem substituí-la. Sem segurança contratado, o operador responsável mantém o protocolo básico e o fluxo continua resolvível.

Ações seguras incluem `ObserveZone`, `PatrolRoute`, `MonitorCameras`, `EscortCashDrop`, `CheckAlarm`, `ReportAnomaly`, `ActivateSafeProtocol`, `GuidePeople`, `CallForAssistance`, `PreserveEvidence`, `TakeBreak` e `HandoffPost`.

Uma `SecurityDecisionPolicy` recebe somente ações válidas e pondera risco, exposição, confiança do sinal, cobertura, treinamento, fadiga, tarefa atual, equipamento, lotação, visão, alarmes falsos, postos descobertos e sangrias agendadas. Distração precisa resultar de causas registradas, como cansaço, monotonia, sobrecarga, tarefas concorrentes ou falha de equipamento.

Progressão:

1. cobertura básica e posto fixo;
2. treinamento, rondas, pausas e escolta;
3. câmeras, rádio, alarme e controle de acesso;
4. equipe redundante com troca de posto e comunicação;
5. supervisão e políticas automatizadas por horário.

Um segurança não cobre dois postos simultaneamente. Acompanhar sangria pode deixar a entrada exposta; pausar exige rendição; câmera sem operador pode fornecer evidência sem detecção imediata; equipamento sem manutenção não concede benefício integral. Melhorias reduzem risco e severidade, nunca criam imunidade.

Não inclua ações de confronto nem conquistas por resistência.

## Contratos técnicos sugeridos

Domínio:

- `CashLocation`, `CashTransfer`, `CashDropPolicy`;
- `RecordedBalance`, `PhysicalBalance`, `Reconciliation`;
- `LossIncident`, `AssetLoss`, `Evidence`, `SecurityControl`;
- `StoreIncidentState`, `ResponsibleOperator`, `SafeResponseProtocol`;
- `InsurancePolicy`, `Coverage`, `Claim`, `Indemnity`;
- `EmployeeAccess` e permissões.

Políticas substituíveis:

- `EmployeeMisconductPolicy`;
- `RobberyRiskPolicy`;
- `DetectionPolicy`;
- `InsuranceOfferPolicy`;
- `SecurityDecisionPolicy`.

Casos de uso:

- configurar e realizar sangria;
- conciliar caixa e contar estoque;
- instalar controle e atribuir acesso;
- contratar, treinar, escalar e atribuir segurança;
- investigar diferença;
- contratar/pagar seguro;
- comunicar incidente e abrir sinistro.

Eventos relevantes:

- `CashDropPerformed`;
- `CashVarianceDetected` e `InventoryVarianceDetected`;
- `InternalTheftOccurred`;
- `RobberyOccurred`;
- `EvidenceDiscovered`;
- `ClaimFiled`, `ClaimApproved`, `ClaimDenied`, `IndemnityPaid`;
- `GuardAssigned`, `GuardDistracted`, `ThreatDetected`, `SafeProtocolActivated`.

## Progressão recomendada

1. Locais de caixa, sangria, troco e conciliação.
2. Roubo abstrato, segurança básica, cofre, alarme e seguro básico.
3. Funcionários com permissões, diferenças ocultas e investigação.
4. Segurança treinada ou redundante, controles avançados, sinistros mais variados e futura perda por clientes.

Para testar o sistema isoladamente, uma adega torna o trade-off mais visível por concentrar caixa e risco noturno. Compare políticas de nunca abandonar a entrada, sempre acompanhar sangrias e equilibrar exposição. Observe detecção, surpresa, cobertura, perda, fila, falta de troco e custo de segurança.

## Localização

Use chaves semânticas e mantenha termos consistentes:

- `sangria de caixa` → `cash drop`;
- `furto interno` → `internal theft` ou `employee theft`;
- `roubo à loja` → `store robbery`;
- `diferença de caixa` → `cash variance`;
- `perdas` no sentido de estoque/caixa → `shrinkage` quando o contexto exigir.

Evite texto gráfico ou sensacionalista por padrão. A intensidade visual e a classificação indicativa permanecem decisões abertas.
