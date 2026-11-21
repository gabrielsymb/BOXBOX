# BOXBOX — direção visual, tom e quarta parede

## Status

Decisão confirmada pelo usuário em 19 de novembro de 2026. Este documento especializa o prompt-mestre para câmera, arte, assets, UI, falas e humor.

### Referência visual aprovada

Em 2026-11-19, a composição `BOXBOX/asset-library/reports/boxbox-first-block-curation.png` foi aprovada pelo usuário como a direção correta para o jogo. A cena é deliberadamente crua e funciona como referência de linguagem, não como arte final: diorama isométrico cartoon/low-poly, loja com interior aberto e legível, rua e vizinhança no mesmo enquadramento, kitbash unificado por câmera, escala e iluminação. Evoluções visuais devem refinar essa direção sem substituí-la por realismo, por outra estética modular ou por formas visíveis geradas em código.

A personagem presente nessa composição permanece provisória. A aprovação da direção não decide o protagonista.

A prancha civil do Mini Characters permanece adequada como fonte de NPCs, mas nenhuma aparência está aprovada como protagonista. Em 2026-11-19, o usuário confirmou que o personagem principal precisa pegar e disparar armas e autorizou editar os assets no Blender. A inspeção verificou que todas as variantes civis compartilham o rig compacto e já incluem `holding-both-shoot`, `holding-left-shoot` e `holding-right-shoot`, além de `pick-up`. O rig não possui ossos de mão; a prova `BOXBOX/asset-library/reports/boxbox-weapon-compatibility.blend` adiciona um socket de empunhadura sem alterar os GLBs originais.

O protagonista canônico é homem branco, de cabelo loiro, rosto sem barba e regata vermelho-alaranjada. A versão aprovada pelo usuário em 2026-11-19 está em `BOXBOX/asset-library/reports/boxbox-protagonist-v1.blend`. Ela deriva de `character-male-b`, remove a barba que ocultava a roupa, converte os braços em pele aparente e preserva o armature, os pesos, as animações e o socket de arma. Integrações e refinamentos devem preservar essa identidade, salvo nova decisão explícita do usuário.

## Decisões confirmadas

- O BOXBOX usa diorama isométrico 3D low-poly.
- O interior da loja é a visão operacional principal: câmera superior inclinada e paredes abertas, rebaixadas ou ocultáveis.
- O mapa e a expansão podem usar lotes em blocos ou bases flutuantes, mantendo a linguagem de maquete.
- O tom é leve, engraçado e autoconsciente; o jogo não precisa apresentar seus sistemas com solenidade.
- NPCs e outras vozes podem quebrar a quarta parede.
- Reutilização, mistura e limitações de assets podem virar assunto das piadas.
- A variedade de assets gratuitos é uma restrição convertida em identidade de kitbash deliberado.
- Toda aparência final do jogo vem de assets visuais autorais ou curados. Isso
  inclui protagonista, NPCs, HUB, modelos, materiais, texturas, pisos, prédios,
  mobiliário, placas, setas, cursores, ícones, tipografia, molduras, painéis,
  botões, indicadores e efeitos visuais.
- C#, controles da engine e bibliotecas podem carregar, posicionar, combinar,
  ocultar, animar e trocar o estado desses assets, mas não definem a identidade
  visual nem desenham substitutos finais com primitivas, geometria procedural,
  estilos nativos, fontes padrão ou widgets genéricos.
- Humor e aparência nunca substituem nem alteram a autoridade da simulação.
- A família visual dos personagens é cartoon/toon low-poly, com formas
  simplificadas, silhuetas legíveis e proporções compactas como as de Kenney Mini
  Characters e da linha de personagens gratuitos do Gobkit.
- Quaternius Ultimate Modular Men e Ultimate Modular Women não representam a
  direção desejada e ficam rejeitados como referência de personagem.
- O protagonista terá asset ou variante visual deliberadamente selecionado, não
  será apenas um NPC genérico escolhido por conveniência.

### Requisitos visuais do protagonista

O protagonista é um agente persistente capaz de trabalhar, comprar, operar uma
loja, carregar objetos, conversar, trocar de atividade e circular pelo HUB. O
asset-base precisa oferecer:

- rig consistente e reutilizável entre variantes civis;
- `idle`, caminhada e corrida;
- pegar, segurar e soltar objetos;
- interação com as duas mãos;
- sentar e poses curtas de confirmação/negação;
- pontos previsíveis para acessórios reais, como caixa, sacola, ferramenta ou
  peça de uniforme;
- silhueta legível na câmera isométrica e materiais compatíveis com recoloração;
- aparência neutra quanto a honestidade, classe, ocupação ou propensão criminal.

O Kenney Mini Characters 2.0 é a família líder para o primeiro protótipo. A
auditoria no Blender 3.6 confirmou o mesmo esqueleto de 7 ossos e 32 ações nos
modelos civis inspecionados, incluindo `idle`, `walk`, `sprint`, `pick-up`,
`holding`, `interact-left`, `interact-right`, `sit`, `drive`, `emote-yes` e
`emote-no`. Isso é aprovação técnica preliminar do rig, não entrada no build.

A identidade principal deve usar uma variante civil escolhida deliberadamente,
com paleta e acessórios aprovados. Roupa, aparência, gênero ou mobilidade não
alteram competência, honestidade nem admissibilidade de ações. Animações de
combate incluídas no pacote não definem o escopo do BOXBOX e ficam fora do
primeiro recorte.

## Fantasia visual

O mundo parece uma coleção de pequenos comércios montados com o que seus donos conseguiram comprar, reaproveitar ou improvisar. A BOXBOX começa simples e ligeiramente remendada; crescer adiciona capacidade e variedade sem apagar totalmente essa personalidade de comércio de bairro.

A cena deve parecer uma maquete jogável, não uma representação arquitetônica realista. Produtos, clientes e ocorrências podem ser um pouco maiores do que seriam em escala real quando isso melhorar leitura e humor.

## Composição do mundo

### Interior operacional

O interior isométrico mostra prateleiras, caixa, estoque acessível, clientes, fila, funcionários e sinais de ocorrência. Paredes podem desaparecer ou rebaixar quando bloquearem informação. Essa convenção pode ser reconhecida pelos personagens em falas meta, mas permanece uma função de apresentação e não altera cobertura, segurança ou espaço lógico.

### Mapa e expansão

Lotes em blocos funcionam como unidades legíveis do bairro. Uma base flutuante é permitida como moldura visual, tela de seleção, evento especial ou exagero cômico. O mapa pode usar ícones, balões e marcadores sobre os dioramas para comunicar demanda, promoções, filas, fofocas e incidentes.

### UI

A UI é uma camada gráfica estável sobre o kitbash. Ela usa hierarquia, tipografia, cores de estado e ícones consistentes, mesmo quando os modelos do mundo vêm de fontes diferentes. Texto funcional nunca depende de estar gravado em textura 3D.

A interface operacional principal usa uma HUD fixa sobre a cena 3D, não um
dashboard de tela cheia. Saldo, caixa, estoque, fila, alertas e relógio ocupam as
bordas; o centro permanece livre para leitura da loja. A HUD usa um kit visual
curado de assets para setas, ícones, cursores, molduras, painéis, botões e estados.
SVG, PNG, texturas, nine-patch e outros formatos importados são aceitáveis quando
preservam nitidez e coerência. Nenhum controle visível conserva a aparência padrão
da engine ou de uma biblioteca.

A tipografia também é asset: cada família e peso usado no jogo precisa ter arquivo
de fonte e licença preservados, cobertura completa de `pt-BR` e `en`, teste de
acentos e legibilidade na escala real da interface. C# resolve chaves localizadas,
valores e estados, mas não desenha glifos, ícones, setas ou decoração própria.

### Política de produção visual por assets

- não gerar a arte final com `draw_*`, malhas primitivas, formas vetoriais criadas
  em runtime, gradientes procedurais ou construção equivalente em C#;
- não usar fonte padrão, ícones Unicode/emoji, setas textuais ou widgets nativos
  como identidade final;
- placeholders técnicos podem existir apenas durante um teste interno isolado,
  precisam estar marcados e não podem ser promovidos para build de apresentação;
- componentes de UI podem ser montados em cenas e temas da engine, desde que toda
  superfície visível referencie assets importados e aprovados;
- variações de cor, animação, preenchimento, foco, seleção e alerta podem ser
  controladas por código quando aplicadas sobre arte-base aprovada;
- qualquer exceção futura exige decisão explícita do usuário e registro aqui.

## Gramática que unifica assets diferentes

Assets podem discordar em origem e detalhe, mas não podem discordar em tudo ao mesmo tempo. A integração normaliza:

1. projeção e família de câmera;
2. escala, pivô, orientação e ocupação de grade;
3. paleta e saturação;
4. resposta de material e brilho;
5. direção, suavidade e cor das sombras;
6. contorno ou tratamento de borda, se adotado;
7. densidade visual por área;
8. silhueta e cor dos estados interativos;
9. orçamento de polígonos, materiais e animações adequado a hardware modesto.

Cenários, personagens, produtos, mobiliário e objetos do mundo são montados com
geometria 3D real. PNG não substitui modelo 3D, não vira recorte e não serve como
atalho para montar a cena. Imagens raster podem existir como texturas aplicadas a
modelos, elementos de interface ou documentação de curadoria.

A proibição de “arte feita por código” não impede o uso de código para lógica de
apresentação: composição de lotes com peças aprovadas, escolha de variantes,
mudanças de estado, oclusão de paredes, iluminação, interação, localização e
streaming continuam programáveis. Código é o sistema que opera a apresentação;
não é a fonte da aparência.

A mistura precisa parecer escolha cômica, não erro de navegação. Objetos funcionalmente equivalentes mantêm sinais visuais equivalentes. Um caixa, uma saída, uma prateleira bloqueada e um produto vencido precisam ser reconhecíveis independentemente do pacote de origem.

Assets entram no projeto somente após registrar fonte, autor, licença, versão/data de obtenção e alterações realizadas. Um pacote sem licença verificável permanece fora do build distribuível.

## Tom

O humor nasce de improviso, burocracia, repetição, economia apertada e consciência das convenções do próprio jogo. Ele pode coexistir com decisões difíceis; a graça está em como o mundo comenta a situação, não em invalidar a consequência.

Assuntos apropriados incluem:

- modelo ou objeto reutilizado;
- NPC parecido com outro;
- asset de estilo inesperado;
- parede ocultada pela câmera;
- animação limitada ou pose repetida;
- pathfinding, fila e colisão;
- cenário low-poly e orçamento de produção;
- interface, tutorial, missão e estatística;
- o absurdo de usar uma peça sci-fi como estante de mercadinho;
- burocracia fictícia e o “leão” como voz de interface.

O jogo não precisa fingir que todas as costuras são invisíveis. Uma boa piada reconhece a costura e a transforma em personalidade.

## Limites do humor

- A piada não esconde preço, risco, prazo, causa ou ação obrigatória.
- O jogador precisa distinguir claramente humor de erro técnico.
- Falas não mudam estoque, caixa, reputação, multa, decisão ou conhecimento econômico.
- Durante ameaça ativa, risco a pessoas ou decisão bloqueante, a instrução segura e funcional aparece primeiro. Humor pode entrar antes como ambientação ou depois na recuperação, nunca incentivar confronto.
- O jogo não ridiculariza grupos reais nem usa características demográficas como explicação para crime, competência ou honestidade.
- Frequência excessiva destrói surpresa; silêncio também faz parte do ritmo cômico.

## Arquitetura das falas meta

O domínio continua emitindo somente fatos e eventos estruturados. A camada narrativa/apresentação cria pistas não autoritativas a partir do que está sendo mostrado.

Fluxo conceitual:

```text
estado e evento confirmados
→ apresentação escolhe representação visual
→ detector narrativo produz pista semântica permitida
→ regras de fala aplicam personagem, contexto, cooldown e não repetição
→ seleção usa fonte aleatória narrativa própria
→ catálogo resolve a chave no idioma ativo
→ balão, voz ou log exibe a fala
```

Exemplos de pistas:

```text
meta.asset_reused
meta.style_mismatch
meta.wall_hidden
meta.animation_repeated
meta.pathfinding_detour
meta.low_poly
meta.ui_interruption
```

Uma pista não contém caminho de arquivo, nome de nó, pacote comercial ou texto pronto. Quando repetibilidade for útil, a seleção deriva sua seed de IDs estáveis do evento e do personagem. Ela nunca consome a fonte aleatória usada pela economia ou pela decisão do cliente.

Se o sistema narrativo estiver indisponível, não houver chave ou todas as falas estiverem em cooldown, o fallback é silêncio. A ausência da piada nunca bloqueia gameplay.

## Localização

Falas usam chaves completas em `pt-BR` e `en`. Trocadilhos não precisam de tradução literal: cada idioma preserva intenção, personalidade e efeito cômico.

Exemplo conceitual:

```text
bark.customer.meta.asset_reused.01
pt-BR: “Já vi esse sujeito três vezes hoje. Ou economizaram nos modelos.”
en: “I've seen that guy three times today. Somebody saved on character models.”
```

Placeholders são nomeados e validados. Não concatenar fragmentos para montar piadas. Linhas repetíveis possuem variantes suficientes, peso, cooldown, limite por período e histórico de uso.

## Vozes possíveis

- **Cliente:** comenta fila, repetição de personagens, caminho estranho e apresentação dos produtos.
- **Funcionário:** comenta improvisos da loja, tarefa repetida, parede que sumiu e equipamento reaproveitado.
- **Concorrente:** provoca a BOXBOX enquanto revela que também reutiliza assets e enfrenta limitações resumidas.
- **Narrador/UI:** reconhece tutorial, relatório, estatística e convenções de gestão.
- **Leão/fisco de interface:** usa burocracia absurda e humor seco sem alterar regras, prazos ou distinções legais do sistema.
- **Segurança:** pode comentar câmeras, postos e orçamento fora de ameaça ativa; durante incidente segue protocolo claro e seguro.

Essas vozes são conteúdo futuro, não confirmação de que todos esses personagens já estejam implementados.

## Questões ainda abertas

- ângulo e distância exatos da câmera;
- rotação livre, limitada ou câmera fixa;
- grau de zoom entre operação e mapa;
- regra exata para paredes rebaixadas ou ocultadas;
- frequência global das piadas e eventual controle de intensidade;
- uso de contorno, toon shading ou materiais totalmente planos;
- proporção entre blocos sólidos e bases flutuantes no mapa;
- quais assets gratuitos formarão o primeiro kit curado e quais precisarão de adaptação.
- escolha entre protagonista canônico único ou seleção inicial entre variantes
  civis compatíveis com o mesmo rig;
- grau de personalização por paleta e acessórios sem reconstruir corpos modulares.

Nenhuma dessas questões reabre a direção geral de diorama isométrico 3D low-poly e tom cômico autoconsciente.

### Bairro inicial

O primeiro comércio fica num bairro periférico de baixa renda. A leitura vem de escala modesta, materiais reparados e sinais de uso cotidiano. A mercearia usa reboco azul envelhecido e o atacado usa paredes de bloco aparente. Os dois interiores usam os pisos cartoon originais. As paredes têm geometria cartoon low-poly e recebem diretamente cor fotográfica, normais e rugosidade. A projeção em escala de mundo mantém a textura contínua entre módulos.

O texto de interação em pt-BR pode ser direto e coloquial, como “Pra começar, separa cinco leites”. Moradores têm voz própria. O vocabulário varia com a pessoa e a situação, e o bairro não deve ser reduzido a uma única gíria, tom ou comportamento.

### Revisão do protótipo de cenário

As capturas de portfólio desta versão registram trabalho em andamento. A aprovação
do conceito visual final depende da revisão das ruas, da escala dos postes e
semáforos e da leitura distinta entre eles. Os personagens derivados do Kenney
são provisórios e serão substituídos pelo elenco do novo acervo.

As lojas usam um enquadramento interno com fundo preto e a rua volta ao sair pela
porta. A câmera da rua e a câmera de pilotagem têm enquadramentos próprios.
