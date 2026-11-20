# BOXBOX — pesquisa e integração de assets

## Status

Auditoria e curadoria inicial realizadas em 19 de novembro de 2026. Kenney Mini
Characters e Gobkit Freebies foram aprovados como referências da família visual
cartoon/toon low-poly. Quaternius Ultimate Modular Men/Women foi rejeitado. Os
treze pacotes Kenney, Gobkit `human001`, Fredoka e Nunito Sans estão preservados
em `review/`, mas ainda não há asset integrado ao projeto.

A HUD, os modelos e as pranchas anteriores eram testes e foram removidos por
decisão explícita do usuário na mesma data.

## Decisões confirmadas

- orçamento absoluto de aquisição: R$ 0;
- diorama isométrico 3D low-poly, com geometria real para o mundo;
- personagens com estilo cartoon/toon, formas simplificadas, silhuetas legíveis e
  proporções compactas;
- Kenney Mini Characters e Gobkit representam a direção desejada;
- Quaternius Ultimate Modular Men/Women não representa essa direção;
- Godot 4.7.2 .NET/C# e Blender disponível para inspeção;
- pt-BR como idioma padrão e inglês como segundo idioma;
- origem e licença precisam ser verificadas antes do download e preservadas junto
  ao pacote;
- aprovação visual não equivale a aprovação técnica nem autoriza entrada no build.
- toda aparência final vem de assets curados: mundo 3D, protagonista, HUD, setas,
  ícones, tipografia, painéis, botões, cursores e efeitos;
- código e bibliotecas implementam lógica, layout, interação, localização e troca
  de estados, mas não criam a identidade visual nem deixam controles, fontes ou
  formas padrão aparentes no produto final;
- o protagonista será selecionado depois de receber os requisitos de sua
  funcionalidade, para que rig, animações e acessórios não sejam escolhidos por
  aparência isolada.

## Auditoria do projeto

- O núcleo headless permanece independente da apresentação e passou nos 43 checks
  existentes após a remoção visual.
- O projeto Godot abre em `Presentation/Bootstrap/EmptyBootstrap.tscn`, uma cena
  intencionalmente vazia.
- Não há modelos, materiais, fontes, ícones ou HUD ativos.
- Candidatos ficam em `BOXBOX/asset-library/review/`, fora do importador do Godot.
  Dezesseis pacotes/famílias estão preservados para auditoria, sem entrada no build.
- O executável local verificado é Godot 4.7.2 Stable .NET; o Blender instalado é
  3.6.23 e o SDK .NET é 8.0.428.

## Formato e pipeline recomendados

Use GLB como formato de entrega para modelos novos: é compacto, agrupa malha,
materiais e texturas e é o formato 3D recomendado pela documentação do Godot.
Aceite glTF com arquivos externos quando isso facilitar a inspeção de texturas ou
controle de versão. Preserve FBX/Blend apenas como fonte de trabalho fora da pasta
ativa. OBJ serve para objetos estáticos simples, mas não preserva esqueleto,
animações, pivôs ou materiais PBR com a mesma capacidade.

O fluxo de entrada é:

1. registrar URL oficial, autor, pacote, versão/data, licença e hash do download;
2. extrair somente em `asset-library/review/<autor>/<pacote>/`;
3. inspecionar no Blender escala, +Y vertical, frente, origem, pivô, materiais,
   triângulos, armature, nomes de clipes e dependências;
4. testar importação no Godot sem copiar o pacote inteiro para `res://`;
5. gerar uma prancha com câmera, luz, chão e escala idênticos;
6. confirmar a escolha artística do recorte;
7. copiar apenas o subconjunto aprovado para `boxbox/Assets/ThirdParty/`, junto da
   licença e do manifesto;
8. criar cenas-wrapper do BOXBOX para normalizar escala, colisão e materiais sem
   editar o arquivo original.

O Godot permite importar uma biblioteca de animação separada e retargetar
humanoides com `BoneMap` e `SkeletonProfileHumanoid`. Isso torna o reuso possível,
mas não automático: nomes iguais não garantem poses de descanso ou hierarquias
compatíveis. O primeiro teste precisa provar animações locomotoras e ao menos uma
interação de loja sem deformação ou deslizamento visível.

Fontes técnicas: [formatos 3D no Godot](https://docs.godotengine.org/en/latest/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html),
[retargeting de esqueletos](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/retargeting_3d_skeletons.html).

## Famílias aprovadas visualmente

### 1. Kenney Mini Characters — referência principal

[Mini Characters](https://kenney.nl/assets/mini-characters) versão 2.0, atualizada
em 7 de novembro de 2026, declara 40 arquivos, animação e licença CC0. A família
define a referência principal para proporção, simplificação, silhueta e coesão
cartoon do elenco.

**Força:** conjunto coeso e de autor consolidado. **Risco técnico/artístico:** a
proporção “mini” pode ficar infantil ou pequena demais perto do mobiliário.
**Experimento:** medir altura visual, largura de circulação e leitura das poses na
câmera do diorama.

### 2. Gobkit Freebies — variedade complementar

[Gobkit Freebies](https://gobkit.com/freebies) oferece personagens cartoon/toon
low-poly animados em pequenos pacotes GLB sob CC0. A família pode ampliar papéis e
variedade sem abandonar a linguagem aprovada.

**Força:** variedade grande distribuída em pacotes pequenos. **Risco:** a coleção
é recente, heterogênea e contém muitos papéis sem relação com comércio; não deve
ser baixada em massa. Nomes ou aparências nunca podem associar um tipo de
personagem a culpa, honestidade ou competência. **Experimento:** escolher um
único pacote civil e inspecionar topologia, atlas, licença, rig e clipes.

## Famílias rejeitadas

[Quaternius Ultimate Modular Men](https://quaternius.com/packs/ultimatemodularcharacters.html)
e [Ultimate Modular Women](https://quaternius.com/packs/ultimatemodularwomen.html)
foram rejeitados explicitamente porque suas proporções e tratamento não
correspondem ao cartoon/toon desejado. A modularidade não compensa a divergência
artística e essas famílias não entram na próxima prancha.

## Candidatos para o HUB

Esta lista começou como pré-seleção gratuita. Os quatro pacotes Kenney marcados
como baixados foram obtidos manualmente pelo usuário e estão preservados para
inspeção; isso ainda não constitui aprovação técnica nem entrada no build.

| Camada do HUB | Candidato | Oferta gratuita verificada | Papel proposto |
| --- | --- | --- | --- |
| comércio principal | [Kenney Mini Shop](https://kenney.nl/assets/mini-shop) | baixado: versão 2.1, 30 modelos por formato, animação, variações e CC0 | loja, equipamentos e linguagem visual mais próxima dos Mini Characters |
| ruas e infraestrutura | [Kenney City Kit (Roads)](https://kenney.nl/assets/city-kit-roads) | baixado: versão 2.2, 95 modelos por formato, variações e CC0 | ruas, calçadas, placas, semáforos e elementos urbanos |
| fachadas comerciais | [Kenney City Kit (Commercial)](https://kenney.nl/assets/city-kit-commercial) | baixado: versão 2.1, 41 modelos por formato, variações e CC0 | volume externo do centro comercial e silhueta do bairro |
| bairro residencial | [Kenney City Kit (Suburban)](https://kenney.nl/assets/city-kit-suburban) | baixado: versão 2.0, 40 modelos por formato e CC0 | casas e transição entre comércio e vizinhança |
| kit urbano alternativo | [KayKit City Builder Bits](https://kaylousberg.itch.io/city-builder-bits) | versão Free com 32+ modelos, GLTF/FBX/OBJ e CC0 | peças urbanas cartoon para cobrir lacunas após comparação de escala e paleta |
| interiores | [Kenney Furniture Kit](https://kenney.nl/assets/furniture-kit) | baixado: versão 2.0, 140 modelos por formato e CC0 | mobiliário de residências, escritórios e áreas de apoio |
| alimentos e cozinha | [KayKit Restaurant Bits](https://kaylousberg.itch.io/restaurant-bits) | versão Free com 140+ modelos, GLTF/FBX/OBJ e CC0 | comida, cozinha, lanchonete e variedade de produtos |
| vegetação | [Kenney Mini Forest](https://kenney.nl/assets/mini-forest) | baixado: versão 1.0, 22 modelos por formato, animação e CC0 | árvores, vegetação e pequenas áreas verdes na família Mini |
| produtos e alimentação | [Kenney Food Kit](https://kenney.nl/assets/food-kit) | baixado: versão 2.0, 200 modelos por formato e CC0 | SKUs, refeições, mercearia e alimentação dos atores |
| circulação | [Kenney Car Kit](https://kenney.nl/assets/car-kit) | baixado: versão 3.1, 50 modelos por formato e CC0 | veículos e variedade de tráfego no mundo de bairro |

O menor conjunto capaz de prototipar o HUB é Mini Shop + City Kit (Roads) + um
kit de edifícios. KayKit entra somente onde o conjunto Kenney não cobrir a
necessidade ou quando sua aparência for aprovada na mesma câmera e iluminação.

## UI e tipografia em revisão

| Função | Asset preservado | Licença | Próxima validação |
| --- | --- | --- | --- |
| painéis, botões e controles | Kenney UI Pack 2.0 | CC0 | selecionar uma única família visual e testar nine-patch no Godot |
| ícones funcionais | Kenney Game Icons 1.0 | CC0 | mapear significado, estado e contraste sem depender de cor isolada |
| ponteiro e setas | Kenney Cursor Pack 1.1 | CC0 | escolher cursores principais e validar escala/legibilidade |
| teclado, mouse e gamepad | Kenney Input Prompts 1.5A | CC0 | mapear dispositivo ativo e remapeamento de controles |
| títulos | Fredoka variável | SIL OFL 1.1 | testar títulos, números grandes e acentos em `pt-BR`/`en` |
| corpo da interface | Nunito Sans variável normal/itálica | SIL OFL 1.1 | testar HUD densa, relatórios, valores e localização expandida |

As fontes foram obtidas do commit `cc5661d43cd4f9aad5567033a8e15ca28dc35c86`
do repositório oficial Google Fonts. `fc-query` confirmou arquivos TrueType
válidos e cobertura de português nas duas famílias.

## Personalização inicial

A primeira versão não dependerá de corpos modulares. Ela deve usar escolhas
discretas que os assets aprovados suportem sem reconstrução: personagem-base,
variante de paleta/material e acessórios em pontos seguros. Não incluir sliders
de corpo, edição facial, troca arbitrária de rig nem dezenas de peças.

O contrato da apresentação recebe IDs semânticos de aparência; domínio, saves
econômicos e IA não conhecem caminhos de asset.

## Licença e distribuição

As duas famílias aprovadas usam CC0 1.0. A Creative Commons informa que a obra
pode ser copiada, modificada e distribuída, inclusive comercialmente, sem pedir
permissão. CC0 não concede direitos de marca, patente, imagem ou endosso e não
oferece garantia; por isso o BOXBOX ainda preservará autor, página original,
arquivo de licença e alterações, mesmo quando atribuição não for obrigatória.

Referências: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/),
[FAQ de licenças da Kenney](https://kenney.nl/support).

## Próximo portão

O portão de aquisição inicial foi concluído. O próximo passo de maior aprendizado
é uma auditoria visual integrada, sem copiar pacotes inteiros para o build:

1. escolher uma variante civil para o protagonista e duas para NPCs;
2. montar uma única quadra com Mini Shop, Roads, Commercial/Suburban, Furniture,
   Food, Car e Mini Forest na mesma câmera, escala e iluminação;
3. selecionar um recorte pequeno de UI Pack, Game Icons, Cursor e Input Prompts;
4. testar Fredoka e Nunito Sans com acentos, números, BRL e expansão do inglês;
5. aprovar ou rejeitar os recortes antes de integrá-los ao Godot.
