Gobkit Free Character Pack Vol.1 — 8 CC0 rigged & animated low-poly characters (GLB)
https://gobkit.com/freebies

Characters: Office Lady, Schoolkid, Cashier, Food Courier, Grandma Shopper, Stock Clerk, Store Manager, Shoplifter
Each .glb has its texture baked in, plus the shared atlas Texture001.png.
All of them share one skeleton, so their clips play on each other.

Animation: 7 clips per character @ 30 fps (some characters have more, listed below), each its own named clip:
  Idle | Walk | Run (loops) | Death (play once, hold) | Hit | Attack | Special
  Every character's clips are made for what it holds (one-handed, long weapon, pistol / bow, rifle, its own props). Special:
  OfficeLady.glb       one-handed    Special = wave
  Schoolkid.glb        one-handed    Special = jump
  Cashier.glb          pistol / bow  Special = jump
  FoodCourier.glb      one-handed    Special = jump
  GrandmaShopper.glb   one-handed    Special = cheer
  StockClerk.glb       pistol / bow  Special = jump
  StoreManager.glb     pistol / bow  Special = cheer
  Shoplifter.glb       one-handed    Special = jump
  Play by name — do not slice by frame range.

Three.js quick start:
  import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
  new GLTFLoader().load('OfficeLady.glb', (g) => { scene.add(g.scene);
    const mixer = new THREE.AnimationMixer(g.scene);
    mixer.clipAction(THREE.AnimationClip.findByName(g.animations, 'Walk')).play(); });

How to put it in your game
--------------------------
Each .glb holds the whole model: one mesh, its skeleton, its texture and its clips. Y is up, the model faces +Z and
stands on its origin. Sizes are in glTF units (1 unit = 1 metre in Unity, Godot and Unreal); scale each model to the
size your game needs. Play the clips that come with each model, by name.

  Unity     Package Manager > + > Add package by name > com.unity.cloud.gltfast, then drop the .glb into Assets.
            In its Inspector set Animation Method to Mecanim (Animator) or Legacy (Animation); the clips keep their names.
  Godot 4   Drop the .glb into the FileSystem dock and drag it into a scene; the clips are in its AnimationPlayer:
            $AnimationPlayer.play("Walk"). To loop a clip: select the .glb > Import dock > Advanced... > the clip >
            Loop Mode: Linear > Reimport.
  Unreal 5  Drag the .glb into the Content Browser (glTF import is built in). On 5.8 set Skeletal Meshes > Combine
            Skeletal Meshes to "Combine All That Use Same Skeleton".
  Blender   File > Import > glTF 2.0 (.glb/.gltf); the clips are Actions (Dope Sheet > Action Editor).
  Three.js  The quick start above; to make a model h units tall:
            const b = new THREE.Box3().setFromObject(g.scene, true); g.scene.scale.multiplyScalar(h / (b.max.y - b.min.y));

Sizes in the rest pose (height x width x length, units):
  Cashier 0.15 x 0.17 x 0.13  |  FoodCourier 0.15 x 0.16 x 0.11  |  GrandmaShopper 0.15 x 0.18 x 0.11
  OfficeLady 0.15 x 0.16 x 0.10  |  Schoolkid 0.13 x 0.19 x 0.10  |  Shoplifter 0.15 x 0.16 x 0.14
  StockClerk 0.13 x 0.20 x 0.083  |  StoreManager 0.14 x 0.20 x 0.079

License: CC0 1.0 (public domain) — any use, commercial or personal, no attribution required.
More free CC0 assets: https://gobkit.com/freebies
