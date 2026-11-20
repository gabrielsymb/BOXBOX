# BOXBOX — opções de distribuição para PC

Pesquisa atualizada em **19 de novembro de 2026**. Taxas, regras e recursos podem mudar; confirme novamente antes de publicar.

## Recomendação provisória

Não há necessidade de escolher uma única loja agora. Para o BOXBOX, a estratégia mais equilibrada é:

1. desenvolver o jogo sem dependência de loja;
2. usar itch.io, se for útil, para protótipos, testes fechados ou uma demo inicial;
3. preparar a Steam como candidata ao lançamento comercial principal;
4. avaliar Epic Games Store e GOG depois que o build, o suporte e a operação de lançamento estiverem maduros.

Essa recomendação é revisável. O primeiro compromisso técnico deve ser uma porta `PlatformServices` com implementação local/nula, não uma integração Steamworks.

## Atenção ao nome

Já existe na Steam um jogo chamado exatamente **BoxBox**, lançado em 10 de julho de 2026. BOXBOX pode continuar como nome interno do projeto, mas o título comercial precisa ser verificado antes da página pública, do registro de marca e de um investimento grande em identidade. Capitalização diferente, sozinha, não melhora a descoberta. Uma possibilidade é adotar futuramente um subtítulo distintivo, sem decidir isso agora.

Fonte: [BoxBox já publicado na Steam](https://store.steampowered.com/app/4836160/BoxBox/).

## Comparação curta

### Steam

Boa candidata ao lançamento principal de um jogo indie para PC. Oferece página “coming soon”, wishlists e recursos Steamworks. Atualmente cobra **US$ 100 por aplicativo**; a taxa é recuperada depois que o produto atinge US$ 1.000 de receita bruta ajustada. Para os primeiros títulos, há espera mínima de 30 dias após o pagamento e página pública “coming soon” por pelo menos duas semanas.

Fonte: [Steam Direct e taxa de publicação](https://partner.steamgames.com/doc/gettingstarted/appfee?l=english&language=english).

### itch.io

Útil para protótipos, demos, builds de teste, early access pequeno e venda direta. Não há custo para criar a página; o criador escolhe a porcentagem destinada ao itch.io, além das taxas do processador de pagamento.

Fontes: [FAQ para criadores](https://itch.io/docs/creators/faq.amp) e [pagamentos e revenue share](https://itch.io/docs/creators/payments).

### Epic Games Store

Pode ser uma expansão comercial posterior. A publicação é self-service e independente de engine. Na política oficial vigente, o desenvolvedor mantém 100% do primeiro US$ 1 milhão de receita líquida por produto por ano; depois, aplica-se a divisão 88%/12%.

Fonte: [distribuição na Epic Games Store](https://store.epicgames.com/distribution/?lang=en-US).

### GOG

É uma alternativa DRM-free e curada. A própria GOG recomenda submeter o jogo quando estiver próximo da versão final ou já tiver sido lançado em outra plataforma, portanto não precisa orientar o primeiro protótipo.

Fontes: [envio de jogos à GOG](https://lp.gog.com/submit-your-game/en) e [FAQ de lançamento](https://support.gog.com/hc/en-us/articles/11382878039197-Releasing-your-game-on-GOG-FAQ).

## Critérios para decidir mais tarde

- onde o público de simuladores de gestão demonstra interesse;
- desempenho da demo, seguidores e wishlists;
- custo de manter páginas, builds, conquistas e suporte em várias lojas;
- países, moedas e métodos de pagamento relevantes;
- recursos realmente necessários: cloud saves, conquistas, workshop, controle e comunidade;
- exigências de DRM, exclusividade ou paridade;
- capacidade de oferecer suporte em pt-BR e inglês.

## Decisão que pode ser tomada agora

O BOXBOX é um jogo de PC independente de loja. Steam é a candidata principal; itch.io é uma candidata para protótipos e demo. Epic e GOG ficam como opções de expansão, sem trabalho de integração antecipado.
