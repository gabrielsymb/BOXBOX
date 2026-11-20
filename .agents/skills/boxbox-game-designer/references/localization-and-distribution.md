# Localização e distribuição do BOXBOX

## Decisões confirmadas

- Nome do produto: **BOXBOX**.
- Idioma padrão: português do Brasil (`pt-BR`).
- Segundo idioma: inglês (`en`).
- Plataforma-alvo: PC.
- Steam: candidata principal conhecida, ainda não escolhida como loja exclusiva.

O cenário e a economia continuam brasileiros nos dois idiomas. Traduzir a interface não muda moeda, regras, produtos ou identidade cultural, salvo quando uma adaptação específica melhorar a compreensão sem descaracterizar o jogo.

## Modelo de localização

Use chaves semânticas estáveis e catálogos separados por locale:

```text
locales/
  pt-BR/
  en/
```

O formato concreto depende da engine escolhida. Preserve estes contratos independentemente da tecnologia:

- o domínio emite `reason codes`, identificadores e valores estruturados;
- a apresentação transforma esses dados em mensagens no idioma ativo;
- catálogos suportam variáveis nomeadas e pluralização;
- nenhuma regra econômica depende do texto traduzido;
- saves guardam identificadores, nunca frases renderizadas;
- ausência de uma chave é detectável em desenvolvimento e usa fallback controlado, não texto silenciosamente vazio.

Exemplo conceitual:

```text
evento: SaleLost
reason: out_of_stock
product_id: rice_1kg
quantity: 2

chave de UI: report.sale_lost.out_of_stock
variáveis: product_name, quantity
```

Evite concatenação como `quantidade + " produtos vendidos"`. Use uma mensagem completa com regras de plural. Preserve a paridade de placeholders entre os catálogos.

## Humor e quarta parede

Falas autoconscientes e piadas sobre assets são conteúdo localizado da apresentação. O domínio, os eventos econômicos e os saves não armazenam a frase nem conhecem nomes de arquivos, pacotes, nós da engine ou licenças.

Use pistas semânticas, por exemplo:

```text
cue: meta.asset_reused
speaker_role: customer
context: checkout_queue

chave selecionada: bark.customer.meta.asset_reused.01
```

O catálogo em cada idioma pode adaptar a piada em vez de traduzi-la literalmente. Preserve intenção, personalidade e informação funcional; trocadilhos podem receber equivalentes culturais diferentes. Falas possuem cooldown, controle de repetição e fallback silencioso quando nenhuma linha válida existir. A seleção narrativa usa aleatoriedade própria, derivada de IDs estáveis quando a repetibilidade for útil, e nunca consome a fonte aleatória econômica.

## Formatação e conteúdo

- O domínio representa dinheiro em unidades mínimas e identifica a moeda como BRL.
- A apresentação formata moeda, números, percentuais, datas e horários conforme o locale ativo sem alterar os valores.
- Prefira fontes com cobertura completa de português e inglês.
- Evite texto gravado em texturas e modelos 3D. Quando inevitável, trate a arte como asset localizado.
- Use layout flexível: botões, cartões, tutoriais e relatórios devem absorver variações de comprimento sem truncar.
- Mantenha nomes próprios e termos culturais quando fizerem parte da identidade brasileira; ofereça contexto curto em inglês quando necessário.
- Inclua localização em pseudolocale ou expansão artificial nos testes visuais quando a tecnologia escolhida permitir.

## Escopo do primeiro vertical slice

Inclua desde o início:

- catálogo mínimo completo em pt-BR e inglês;
- seletor de idioma acessível sem reiniciar a empresa;
- uma tela de compra, um trecho do expediente e o fechamento nos dois idiomas;
- mensagens causais de ruptura, preço, fila, perda e resultado;
- testes de chaves ausentes, placeholders e pluralização;
- uma porta de serviços de plataforma com implementação local/nula.

Não é necessário integrar o SDK de nenhuma loja para validar a economia e o loop do primeiro slice.

## Fronteira de plataforma

Mantenha serviços específicos atrás de contratos, por exemplo:

```text
PlatformServices
├── achievements
├── statistics
├── cloud_saves
├── presence
└── overlay
```

Cada adaptador de loja implementa somente os recursos escolhidos. Uma implementação nula permite desenvolvimento, testes e execução sem cliente. Falha ou indisponibilidade da plataforma não pode corromper o save nem bloquear o loop principal.

Mantenha identificadores de aplicativo, ambientes, caminhos de build e credenciais fora do domínio. Nunca grave segredos no repositório. Saves precisam de versão e estratégia de conflito antes de ativar sincronização em nuvem.

## Estratégia de lojas

As lojas não são mutuamente excludentes. Avalie a ordem de publicação separadamente da arquitetura do jogo:

- **Steam:** candidata forte para o lançamento comercial principal no PC, com wishlists, comunidade e recursos de plataforma; exige processo de onboarding e preparação de página/build.
- **itch.io:** opção simples para protótipos, builds de teste, demos e venda direta; não exige que o jogo adote APIs proprietárias.
- **Epic Games Store:** opção de expansão comercial com termos próprios e publicação self-service; avalie quando houver build e operação de lançamento mais maduros.
- **GOG:** opção DRM-free e curada, normalmente mais adequada quando o jogo estiver próximo do estado final ou já lançado.

Não fixe percentuais, taxas ou exigências no código ou em instruções permanentes. Esses dados mudam; pesquise fontes oficiais na data de cada decisão comercial.

## Preparação de publicação

Planeje como artefatos separados, no momento apropriado:

- página de cada loja escolhida em português do Brasil e inglês;
- cápsulas, screenshots e trailers sem texto fixo ou com variantes localizadas;
- descrição, avisos, suporte e notas de atualização nos dois idiomas;
- builds/depot por sistema operacional realmente suportado;
- conquistas e textos de conquistas localizados;
- política de saves locais e sincronização da loja, se adotada;
- testes sem cliente de loja e em conta de desenvolvimento.

Requisitos e capacidades das lojas mudam. Antes de implementar ou publicar, confira a documentação oficial vigente e registre as decisões de versão/configuração no projeto.
