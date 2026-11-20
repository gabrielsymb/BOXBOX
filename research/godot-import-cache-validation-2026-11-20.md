# Validação do cache de importação do Godot

Data: 2026-11-20

Ambiente observado:

- Godot 4.7.2 Stable .NET (`ed1daf0bf`);
- renderizador Compatibility / OpenGL 4.2;
- Mesa 25.2.8, Intel HD Graphics 4000;
- .NET SDK 8.0.428.

## Fontes oficiais

- Godot 4.7, `ResourceLoader`: <https://docs.godotengine.org/en/4.7/classes/class_resourceloader.html>
- Godot, processo de importação: <https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html>

## Evidência reproduzida

A primeira importação gráfica do worktree encerrou com `SIGSEGV` durante a
pré-reimportação. Uma execução posterior da cena abriu sem a cidade e repetiu a
cada frame falhas para quatro caminhos antigos:

- `res://Assets/World/Buildings/building-a.glb`;
- `res://Assets/World/Buildings/building-c.glb`;
- `res://Assets/World/Buildings/building-type-a.glb`;
- `res://Assets/World/Buildings/building-type-d.glb`.

Os quatro GLBs não existem nesses caminhos. Os modelos reais estão nas pastas
`Commercial/` e `Suburban/`, onde foram importados com sucesso. Restavam somente
arquivos `.glb.import` órfãos na pasta antiga. Como o catálogo percorre recursos
com `ResourceLoader.ListDirectory()`, essas entradas antigas eram descobertas e
enviadas ao carregamento assíncrono.

## Conclusão adotada

Remover apenas os quatro metadados `.import` órfãos. Nenhum GLB, textura,
transformação, esqueleto, colisor ou fator de escala é alterado. Recriar o cache
local `.godot/` é seguro porque a documentação o define como derivado e
reimportável; ele não deve ser versionado.

Critério de aceitação:

1. a cena principal inicia com ruas, lotes e edifícios visíveis;
2. o log não contém `Cannot open file` nem `Threaded world asset failed` para os
   quatro caminhos antigos;
3. o catálogo continua consumindo as variantes reais de `Commercial/` e
   `Suburban/`;
4. nenhuma dimensão visual ou transformação de asset muda.

## Limitações

O core sem símbolos do crash inicial não identifica a função interna do Godot.
A importação headless completa foi bem-sucedida, o que isola o primeiro crash na
execução gráfica da importação, mas não prova uma causa específica no driver ou
na engine.
