# Pesquisa de referências para o primeiro slice do BOXBOX

Data: 2026-11-22  
Engine alvo: Unity 6.3 LTS `6000.3.29f1`  
Linguagem: C#  
Ferramenta de modelagem disponível: Blender 3.6.23  
Escopo pesquisado: abastecimento físico, carga, estoque, venda e direção mínima.

## Pergunta e hipótese

Quais referências ajudam a validar o primeiro ciclo jogável sem transformar o
primeiro slice em estudo simultâneo de cidade aberta, trânsito, combate e economia
completa?

Hipótese: o slice precisa comprovar o ciclo da mercadoria — pedido confirmado,
retirada física no atacado, transporte, descarga, venda e fechamento — com uma rota
controlada. A economia e o estado da carga devem ser determinísticos e independentes
da cena; a direção é uma projeção física simples para tornar o abastecimento uma
ação jogável. Tráfego e população autônomos não são necessários para provar esse
ciclo.

## Referências prioritárias

### Moonforge — contratos de comando e transação

- Checkout local: `research/third-party/moonforge`, revisão
  `3864fc363033111b93c68965ed36fcab87a31aa7`.
- Origem: https://github.com/3583Bytes/moonforge-rpg-engine
- Licença identificada: MIT (`research/third-party/moonforge/LICENSE`).
- Arquivos estudados:
  - `unity-packages/com.moonforge.core/Runtime/Shops/Commands/BuyFromShopCommandHandler.cs`
  - `unity-packages/com.moonforge.core/Runtime/Economy/Commands/EconomyTransactionCommandHandler.cs`
  - `unity-packages/com.moonforge.core/Runtime/Runtime/Commands/CommandDispatcher.cs`

Observação de design: comandos de compra compõem validação de oferta, saldo, estoque
e eventos; o dispatcher guarda snapshot, bufferiza eventos e restaura o estado se o
handler ou um reactor falha. Isso é útil para elaborar casos de teste do BOXBOX:
compra de fornecedor ou venda rejeitada não pode deixar dinheiro, carga, estoque ou
eventos parcialmente alterados.

Uso neste slice: referência principal para decompor comandos/casos de uso e montar
testes headless; não importar código, nomes ou estrutura. O dispatcher faz parte do
projeto de referência e não prova, por si só, que qualquer domínio encaixa nos mesmos
tipos ou limites. O domínio do BOXBOX deve definir suas próprias transações e testar
as próprias invariantes.

### REDRIVER2 — comportamento de veículo para a viagem

- Checkout local: `research/third-party/redriver2`, revisão
  `b2d8857426f815007486def9bd23f9a6007a2c64`.
- Origem: https://github.com/OpenDriver2/REDRIVER2
- Licença identificada no repositório: MIT.
- Arquivos localizados/estudados:
  - `src_rebuild/Game/C/handling.c`
  - `src_rebuild/Game/C/wheelforces.c`

Observação de design: a direção depende de subsistemas coordenados de rodas, forças,
aderência, estado do veículo e câmera; não é apenas mover uma caixa na direção do
input. O código é legado e ligado à representação e aos dados de Driver 2. Sua
complexidade não é requisito para um trajeto curto de abastecimento.

Uso neste slice: referência comportamental secundária para formular critérios de
sensação e observar a resposta ao acelerar, virar, frear e recuperar o veículo numa
rota pequena. O primeiro slice não precisa reproduzir suspensão completa, dano,
trânsito civil, perseguição ou física fiel. Nenhum código é portado.

### Endless Sky — comércio e carga legíveis

- Checkout local: `research/third-party/endless-sky`, revisão
  `b71cbc6791ec2c8035ffa810f510c3b8b501c96c`.
- Origem: https://github.com/endless-sky/endless-sky
- Licença declarada pelo projeto: GPL-3.0-or-later.
- Arquivos estudados:
  - `source/Trade.cpp`
  - `source/CargoHold.cpp`
  - `source/TradingPanel.cpp`

Observação de design: mercadorias são conteúdo orientado a dados; a carga tem
manifesto e capacidade; a tela comercial dá contexto de preço, espaço disponível e
resultado da negociação. Essas ideias ajudam a avaliar se pedido, unidades na
caminhonete e saldo são compreensíveis para o jogador.

Uso neste slice: referência de apresentação e modelagem conceitual de catálogo/carga,
não de código econômico, escala ou regras de comércio espacial. A linguagem e a
licença tornam inadequado transferir código para este projeto independente; o uso
fica na observação e em especificações próprias.

## Referências fora do primeiro corte

### gta2_re

- Checkout local: `research/third-party/gta2_re`, revisão
  `d43309d967e0c03d82887bf5a64c778e3e7e0223`.
- Origem: https://github.com/CriminalRETeam/gta2_re
- Não foi localizada licença no checkout auditado; a própria auditoria do projeto
  restringe o uso a observar comportamento e convertê-lo em especificações próprias.
- `Source/CarAI.cpp` e `Source/Ped.cpp` indicam material útil para uma futura rodada
  de tráfego, pedestres e sistemas urbanos, não para validar pedido, transporte e
  venda numa quadra controlada.

Conclusão: não é dependência nem leitura necessária para iniciar o primeiro fluxo.
Retomar quando o slice precisar de agentes urbanos; sem copiar estrutura, nomes,
dados ou assets.

### OpenMC2

- Checkout local: `research/third-party/openmc2`, revisão
  `794b6e7a52dc3e19ea842a9c0f8a4ec5d2193bbe`.
- Origem: https://github.com/OpenMC2/OpenMC2
- Licença identificada: GPL-3.0.
- O README declara o projeto abandonado e explica que usa o binário original onde a
  reimplementação ainda não existe.

Conclusão: não serve como implementação independente da direção/física do primeiro
slice. Não é necessário para esse fluxo.

### OpenMoHAA

- Checkout local: `research/third-party/openmohaa`, revisão
  `a2f340195975f4f042e28a60b62561dd9a0b2700`.
- Origem: https://github.com/openmoh/openmohaa
- Licença identificada: GPL-2.0.

Conclusão: percepção de combate, armas, patrulha e objetivos não participam do ciclo
de abastecimento e venda. Deixar para uma investigação futura, se esse sistema entrar
no escopo priorizado.

## Decisão para o primeiro slice

Incluir:

1. Pedido comercial validado por saldo, preço e quantidade.
2. Estados distintos para pedido confirmado, carga retirada, carga transportada,
   descarga concluída e estoque disponível para venda.
3. Uma rota curta e controlada entre mercearia e atacado, conduzida pelo jogador em
   caminhonete.
4. Operação de loja, clientes, vendas e fechamento explicável.
5. Testes headless para dinheiro, estoque/carga, transações, rejeições e replay
   determinístico.
6. Greybox simples e explicitamente temporário para medir percurso, interação e
   leitura; UI funcional separada da simulação.

Adiar:

- tráfego autônomo, pedestres persistentes, perseguições e mundo urbano expansível;
- tuning avançado de veículo, suspensão realista e damage model;
- combate e inteligência de combate;
- arte final de bairro, loja, veículo ou personagem.

Critério para a direção: alcançar o atacado, retirar a carga, voltar e descarregar
deve ser claro e funcional; colisão, falha de interação ou cancelamento não pode
duplicar mercadoria, criar estoque nem cobrar duas vezes. A direção serve ao
abastecimento e não deve competir com a decisão econômica pelo foco do slice.

## Blender

Não modelar assets finais antes de executar e avaliar esse greybox. O Blender não é
pré-requisito para testar estados da carga, economia ou duração legível do trajeto.
Se a cena revelar uma lacuna visual que afete compreensão, a primeira peça autoral
de baixo custo a avaliar é uma caixa de transporte reutilizável e facilmente
contável; ela comunica a carga sem antecipar um kit completo de cidade ou veículo.

Antes de integrar qualquer modelo, registrar AABB real no Blender, pivô, orientação,
dimensão-alvo e licença/origem, depois comparar no Unity com protagonista, veículo,
porta, mobiliário e módulo de rua aprovado. Validar o objeto no jogo, não só na
janela do Blender. A versão local disponível nesta pesquisa é Blender 3.6.23.

## Fontes e limitações

- A auditoria local de licenças e revisões está em
  `research/reboot-reference-audit-2026-11-22.md`.
- Os arquivos listados foram consultados nos checkouts locais em seus commits
  registrados. O estudo compara padrões observáveis, não mede a qualidade relativa
  das engines nem estabelece parâmetros numéricos de física.
- Nenhum executável de jogo foi instalado ou jogado durante esta revisão.
- Esta nota é uma decisão de escopo/produto, não aconselhamento jurídico.
