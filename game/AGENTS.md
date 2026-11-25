# Regras obrigatórias para agentes em `game/`

Estas regras valem para todo código, cena, conteúdo e asset criado para o jogo novo.
Leia também `../AGENTS.md` e `README.md` antes de implementar.

## Localização e independência

- Todo código, projeto de engine, cena, conteúdo e asset novo do jogo fica dentro de
  `game/`. Não crie implementação do produto em `research/` ou na raiz.
- Os repositórios de `research/third-party/` podem ser estudados e portados diretamente
  quando a licença permitir (ex.: REDRIVER2, MIT). Ao portar, mantenha o aviso de
  copyright/licença no topo do arquivo, cite a função/arquivo de origem e registre o estudo
  em `research/`. Decisão do criador em 2026-11-24.
- Repositórios sem licença identificada (ex.: `gta2_re`) continuam sendo só referência de
  comportamento: não copie código, dados ou assets deles.
- Consulte e respeite a licença de cada dependência ou asset que venha a ser
  incorporado. Estudar um repositório não autoriza redistribuir seu conteúdo.

## Fronteiras de arquitetura

Todo código executável e todo asset importado pela Unity deve estar sob `Assets/BoxBox/`.
Documentação e configuração do projeto ficam fora dessa árvore nos locais padrão da Unity.

Dependências permitidas:

```text
Presentation -> Core/Application -> Core/Domain
Infrastructure -> contratos internos de Core
Core/AI -> contratos e observações; nunca altera o domínio diretamente
```

- `Core/Domain` contém regras, estados, valores, comandos e invariantes puros. Não
  importa `UnityEngine`, controles de UI, arquivos, JSON ou runtime de inferência.
- `Core/Application` coordena casos de uso e transações usando contratos do domínio.
  Não instancia nós da engine nem apresenta diálogos.
- `Core/Simulation` mantém relógio, agenda e aleatoriedade injetáveis; não depende de
  FPS, UI ou relógio de parede para decidir resultados econômicos.
- `Core/AI` produz propostas/intuições a partir de observações permitidas e ações
  válidas. A aplicação/domínio valida e executa; a IA nunca muta estado diretamente.
- `Infrastructure` implementa portas externas, como save e carregamento de conteúdo.
- `Assets/BoxBox/Presentation` converte entrada em comandos e estado/eventos em apresentação.
  Controladores de cena são adaptadores finos, não donos das regras.
- `Assets/BoxBox/Presentation/UI` contém layout, controles, formatação e interação visual. Não
  calcula vendas, caixa, estoque, elegibilidade, progressão ou resultados de IA.
- Ao criar os primeiros scripts de uma camada, defina seu assembly Unity com `.asmdef`
  para manter o núcleo sem referência à engine. Não crie assemblies vazios. O assembly
  de composição pode referenciar núcleo, adaptadores e apresentação; a lógica nunca
  depende da composição.
- Textos visíveis usam chaves localizadas. O domínio e os eventos guardam códigos
  semânticos e valores, nunca frases traduzidas.
- Dinheiro usa centavos inteiros ou decimal exato; nunca `float`/`double`.
- Comandos calculam e validam o próximo estado antes do commit. Rejeições não podem
  deixar efeitos parciais.

## Código pequeno e legível

- Um arquivo e um tipo devem ter responsabilidade coesa. Extraia componentes quando
  uma classe acumular tarefas independentes.
- O critério para dividir é SOLID, não contagem de linhas: um arquivo grande que tem uma
  única responsabilidade coesa (ex.: o port da física do veículo) não precisa ser dividido.
  Divida quando o tipo acumular responsabilidades independentes. Decisão do criador em
  2026-11-24.
- Prefira métodos curtos com nomes claros. Extraia operações nomeadas em vez de aninhar
  condicionais ou concentrar fluxos independentes num método.
- `Bootstrap`/cena principal é somente raiz de composição: instancia adaptadores e
  conecta dependências. Mire em até 80 linhas; não coloque nela regras, conteúdo,
  ciclo de jogo, persistência ou lógica de interface.
- Não crie `GameManager`, `Main`, controladores ou scripts "utilitários" como depósito
  de responsabilidades alheias. Prefira tipos pequenos com nomes do domínio.
- Não resolva tamanho com regiões, métodos privados gigantes, classes parciais
  artificiais ou comentários que substituam uma decomposição clara.
- Ao tocar código, preserve tipos explícitos, nomes claros e padrões existentes.
  Não silencie erros com `catch` amplo, valores padrão enganosos ou retornos de sucesso
  quando a operação falhou.

## Vertical slice e validação

- Implemente um slice vertical jogável de ponta a ponta, não uma coleção de sistemas
  sem fluxo conectado. Leia `Docs/VERTICAL_SLICE.md` antes de alterar seu escopo.
- Comece pela menor entrega que prove uma escolha do jogador e uma consequência
  econômica observável. Separe estado, comandos, apresentação e persistência.
- Cada regra nova declara pré-condições, efeitos, rejeições, invariantes e como será
  observada/testada antes de ganhar balanceamento ou complexidade.
- Cubra regras econômicas com verificações headless no Core; não dependa de executar
  a interface para validar caixa, estoque ou determinismo.
- Mudanças visuais só são consideradas validadas após executar o jogo e inspecionar
  a cena real, conforme as instruções da raiz.
- Não adicione frameworks, serviços ou sistemas além do necessário para o slice atual.
- Use Unity 6.3 LTS (`6000.3.29f1`) como versão inicial do projeto, salvo incompatibilidade
  concreta ou decisão mais recente do usuário. Não reabra Godot.
