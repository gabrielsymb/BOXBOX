# Estudo e Registro de Decisão Técnica: Compatibilidade de Input no Unity 6.3 LTS

- **Data:** 2026-11-23
- **Engine / Versão:** Unity 6.3 LTS (`6000.3.29f1`), Linux x86_64
- **Renderizador:** Universal Render Pipeline (URP 17.0.3)
- **Status:** Resolvido / Decisão aplicada

## Problema Observado

Ao importar o pacote `com.unity.inputsystem: "1.13.0"` no `Packages/manifest.json` do Unity 6.3 LTS, a compilação do editor quebrava na inicialização com erros em cascata:

```text
[16:36:55] Library/PackageCache/com.unity.inputsystem@.../InputSystem/Editor/InputSystemPluginControl.cs(47,25): error CS0117: 'BuildTarget' does not contain a definition...
[16:37:54] TypeLoadException: Could not load type 'UnityEngine.InputSystem.InputActionAsset' from assembly 'Unity.InputSystem'.
```

A API interna do Unity 6 alterou ou descontinuou propriedades de `BuildTarget` esperadas pela versão 1.13.x do pacote, disparando falhas no backend de scripts (`Compiling Scripts: Running Backend`), bloqueando o Play Mode e gerando centenas de erros no console.

## Decisão Adotada

1. **Remoção do pacote incompatível:**
   - O `com.unity.inputsystem: 1.13.0` foi removido de `Packages/manifest.json`.
2. **Utilização do Input Manager Legado:**
   - O controlador do veículo (`VehicleInputAdapter.cs`) consome diretamente `Input.GetAxisRaw("Horizontal")`, `Input.GetAxisRaw("Vertical")` e `Input.GetKey(KeyCode.Space)`.
   - Essa camada atende plenamente ao teclado (WASD, setas, espaço) e joysticks configurados pelo gerenciador padrão da Unity, sem dependências externas e com zero custo de pacotes quebrados.
3. **Plano de Atualização Futura:**
   - Quando o projeto demandar explicitamente remapeamento dinâmico em runtime ou controle multi-dispositivo avançado, a migração para o novo Input System deverá ser realizada utilizando a versão verificada e homologada para o Unity 6 (versão 2.0+ ou instalada diretamente via Package Manager oficial conectado ao registro da Unity).
