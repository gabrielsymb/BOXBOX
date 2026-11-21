# Pré-visualizações do rifle fiel

Arquivos combinados para inspeção no Blender/Godot. Eles incluem o personagem fiel, rig, locomoção (`idle`, `walk`, `run`, `jump`) e o rifle fiel como filho do osso correspondente:

- `boxbox-character-rifle-hand.glb`: rifle preso ao `mixamorig:RightHand` para a pose armada.
- `boxbox-character-rifle-back.glb`: rifle preso ao `mixamorig:Spine2` para a pose guardada/corrida.

São arquivos de referência visual. O jogo usa o prop separado e os mesmos sockets no `PlayerController.cs`, permitindo alternar os estados sem duplicar o personagem na cena.
