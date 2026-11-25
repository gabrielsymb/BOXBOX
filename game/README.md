# BOXBOX — jogo novo

Este diretório é a raiz do projeto novo do BOXBOX. Código de produto, cenas, conteúdo
e assets novos ficam aqui. O protótipo antigo em `../BOXBOX/` e o experimento em
`../BOXBOX/reboot/` são referências de pesquisa, não dependências nem destinos para
novas funcionalidades.

## Stack

- Unity 6.3 LTS (`6000.3.29f1`)
- C#
- Execução local e offline
- `pt-BR` padrão e `en` como segundo idioma

## Projeto

Abra esta pasta no Unity Hub usando a versão indicada em
`ProjectSettings/ProjectVersion.txt`. A cena de entrada é um bootstrap vazio e
deliberado; ela só deve conectar os adaptadores necessários para iniciar o jogo.

O domínio será independente da engine. Os scripts do produto ficam sob
`Assets/BoxBox/`; crie cada assembly definition junto com os primeiros scripts daquela
camada para evitar assemblies vazios no projeto inicial.

## Fronteiras

- `Assets/BoxBox/Core/Domain`: regras puras e invariantes.
- `Assets/BoxBox/Core/Application`: comandos e casos de uso transacionais.
- `Assets/BoxBox/Core/Simulation`: tempo, agenda e execução determinística.
- `Assets/BoxBox/Core/AI`: contratos e políticas substituíveis; propostas não mutam estado.
- `Assets/BoxBox/Infrastructure`: adaptadores de engine, arquivo e recursos externos.
- `Assets/BoxBox/Presentation`: cenas, entrada, view models, UI e localização apresentada.
- `Assets/BoxBox/Content`: catálogos e regras declarativas versionadas.
- `Assets/BoxBox/ArtAssets`: assets próprios/aprovados do jogo.
- `Assets/BoxBox/Tests`: testes do núcleo e integrações da Unity.

A arquitetura e as regras obrigatórias para agentes estão em [AGENTS.md](./AGENTS.md).
O escopo e os critérios de conclusão do primeiro slice estão em
[Docs/VERTICAL_SLICE.md](./Docs/VERTICAL_SLICE.md).

## Ambiente de Desenvolvimento e SandboxLab

O projeto utiliza Unity 6.3 LTS (`6000.3.29f1`) com Universal Render Pipeline (URP).
O protótipo de cubos preliminar foi retirado para dar lugar à implementação limpa (clean-room)
com física real e arquitetura desacoplada.

### Decisão Técnica: Sistema de Entrada (Input)
- **Input System 1.13.0 removido:** O pacote `com.unity.inputsystem: 1.13.0` introduz quebras de compilação no Unity 6.3 LTS (erro de símbolos ausentes em `BuildTarget` em `InputSystemPluginControl.cs` gerando falhas em cascata).
- **Solução Vigente:** Utilizamos o **Input Manager legado** (`Input.GetAxisRaw`, `Input.GetKey`), atendido diretamente pelo `VehicleInputAdapter.cs`.
- **Evolução Futura:** Quando houver necessidade estrita do novo Input System (mapeamento avançado de controle/gamepad ou hot-plug), instalar a versão verificada para Unity 6 (pacote 2.0+ ou versão recomendada pelo Package Manager do Unity 6).

### Como abrir e testar

1. No Unity, abra `Assets/BoxBox/Presentation/Scenes/SandboxLab.unity`.
2. A cena contém:
   - Luz solar direcionada (`Directional Light`) calibrada para diorama;
   - Câmera em perspectiva inclinada (`Main Camera`);
   - Chão de testes com colisor físico sólido (`PhysicsGround` com `BoxCollider`), pronto para testes de física de veículos e active ragdoll.
3. As regras puras e imutáveis de economia e dinheiro continuam isoladas sob `Assets/BoxBox/Core/Domain/`.
