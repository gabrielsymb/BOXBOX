# Catálogo curado de assets

Reiniciado em 19 de novembro de 2026.

## Estado atual

Não há assets integrados ao build. Treze pacotes Kenney, o pacote Gobkit
`human001` e duas famílias do Google Fonts estão preservados somente em
`review/`; nada foi copiado para o projeto Godot. Os modelos, a HUD e
as pranchas anteriores eram testes e foram removidos do projeto. A família visual
de personagens foi confirmada como cartoon/toon low-poly em 19 de novembro de
2026.

## Aprovados visualmente, pendentes de auditoria técnica

| Prioridade | Família | Oferta gratuita verificada | Licença | Papel no teste |
| --- | --- | --- | --- | --- |
| 1 | [Kenney Mini Characters](https://kenney.nl/assets/mini-characters) | versão 2.0, 40 arquivos, personagens animados e variedade corporal explícita | CC0 1.0 | referência principal para proporção, silhueta e coesão cartoon |
| 2 | [Gobkit Freebies](https://gobkit.com/freebies) | personagens low-poly animados em GLB, organizados em pequenos pacotes | CC0 1.0 | ampliar variedade de papéis sem abandonar a família toon |

Nenhuma dessas aprovações autoriza entrada no build. Ainda é necessário validar
licença embutida, arquivos, escala, rig, animações, materiais e importação no
Godot.

## Em revisão técnica

| Pacote | Origem | Conteúdo preservado | Estado |
| --- | --- | --- | --- |
| Kenney Mini Characters 2.0 | [Kenney](https://kenney.nl/assets/mini-characters) | ZIP original, licença CC0, preview e 43 modelos em cada formato GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney Mini Shop 2.1 | [Kenney](https://kenney.nl/assets/mini-shop) | ZIP original, licença CC0, previews e 30 modelos em cada formato GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney City Kit Commercial 2.1 | [Kenney](https://kenney.nl/assets/city-kit-commercial) | ZIP original, licença CC0, previews e 41 modelos em cada formato GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney City Kit Roads 2.2 | [Kenney](https://kenney.nl/assets/city-kit-roads) | ZIP original, licença CC0, previews e 95 modelos em cada formato GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney Mini Forest 1.0 | [Kenney](https://kenney.nl/assets/mini-forest) | ZIP original, licença CC0, previews e 22 modelos em cada formato GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney UI Pack 2.0 | [Kenney](https://kenney.nl/assets/ui-pack) | ZIP original, licença CC0, 434 SVGs, 870 PNGs, fontes e áudio | licença, hash e integridade básica verificados; recorte visual pendente |
| Kenney Game Icons 1.0 | [Kenney](https://kenney.nl/assets/game-icons) | ZIP original, licença CC0, 425 PNGs e fontes vetoriais | licença, hash e integridade básica verificados; recorte semântico pendente |
| Kenney Cursor Pack 1.1 | [Kenney](https://kenney.nl/assets/cursor-pack) | ZIP original, licença CC0, 364 SVGs e 729 PNGs | licença, hash e integridade básica verificados; recorte visual pendente |
| Kenney Input Prompts 1.5A | [Kenney](https://kenney.nl/assets/input-prompts) | ZIP original, licença CC0, 1.504 SVGs, 3.056 PNGs e fontes de glifos | licença, hash e integridade básica verificados; mapeamento de dispositivos pendente |
| Kenney Furniture Kit 2.0 | [Kenney](https://kenney.nl/assets/furniture-kit) | ZIP original, licença CC0 e 140 modelos em GLB/FBX/OBJ/DAE/STL | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney Food Kit 2.0 | [Kenney](https://kenney.nl/assets/food-kit) | ZIP original, licença CC0 e 200 modelos em GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney Car Kit 3.1 | [Kenney](https://kenney.nl/assets/car-kit) | ZIP original, licença CC0 e 50 modelos em GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Kenney City Kit Suburban 2.0 | [Kenney](https://kenney.nl/assets/city-kit-suburban) | ZIP original, licença CC0 e 40 modelos em GLB/FBX/OBJ | licença, hash e integridade básica verificados; inspeção 3D pendente |
| Gobkit `human001` | [Gobkit Freebies](https://gobkit.com/freebies) | ZIP original, licença CC0, README, textura e 8 GLBs | licença e integridade verificadas; inspeção 3D pendente |
| Fredoka variável | [Google Fonts](https://github.com/google/fonts/tree/main/ofl/fredoka) | TTF, OFL e metadados preservados no commit registrado | arquivo TrueType e cobertura de português verificadas; teste visual pendente |
| Nunito Sans variável | [Google Fonts](https://github.com/google/fonts/tree/main/ofl/nunitosans) | TTF normal/itálico, OFL e metadados preservados no commit registrado | arquivos TrueType e cobertura de português verificadas; teste visual pendente |

## Rejeitados por direção artística

| Família | Motivo |
| --- | --- |
| [Quaternius Ultimate Modular Women](https://quaternius.com/packs/ultimatemodularwomen.html) | proporção e tratamento visual fora da família cartoon/toon escolhida |
| [Quaternius Ultimate Modular Men](https://quaternius.com/packs/ultimatemodularcharacters.html) | proporção e tratamento visual fora da família cartoon/toon escolhida |

## Reservas, não selecionadas para a primeira comparação

- [Kenney Blocky Characters](https://kenney.nl/assets/blocky-characters): 20
  arquivos animados e CC0, mas a proporção muito cúbica pode dominar a identidade
  visual do mundo.
- [KayKit Character Animations](https://kaylousberg.itch.io/kaykit-character-animations):
  biblioteca CC0 útil para um experimento posterior de retargeting, não uma
  família civil de personagens para o BOXBOX.
