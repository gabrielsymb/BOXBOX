# Clean-Room Technician

https://polyfork.dev/asset/clean-room-technician-da290a

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `clean-room-technician-da290a.mjs` | the model as an ES module, with every option below |
| `clean-room-technician-da290a.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`clean-room-technician-da290a.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './clean-room-technician-da290a.mjs';
scene.add(createAsset());
```

The bare `three` specifiers resolve through any bundler (Vite, webpack, esbuild),
or through an importmap in your page:

```html
<script type="importmap">
{ "imports": { "three": "https://unpkg.com/three@0.180.0/build/three.module.js",
               "three/addons/": "https://unpkg.com/three@0.180.0/examples/jsm/" } }
</script>
```

Prefer the GLB? `new GLTFLoader().load('clean-room-technician-da290a.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  shift: 0.7,
  build: 0.86,
  age: 'adult',
  figure: 'masculine',
  expression: 'stern',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `shift` | range | `0.7` | `0` to `1` | How far into the experiment the lab is. People change least: from 0.0 to 0.9 the goggles sit pushed up on the hood's brow and the hands are bare; at 1.0 the goggles are down over the eyes and nitrile gloves are pulled on over the suit cuffs. The suit, hood, mask and overboots are the same build at every value. |
| `build` | range | `0.86` | `0.86` to `1.18` | Girth at one height: 0.86 is the tall lean technician of the reference and 1.18 a heavy one. The smock, hood and overboots refit themselves, so the hem stays clear of the legs and the goggle band stays on the hood. |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | The proportion ladder, and the standing height scales with it from this character's tall 1.80 m adult. Everything refits; `old` greys the hair, which is under the hood anyway. |
| `figure` | choice | `'masculine'` | `'masculine'`, `'feminine'` | Shoulder-to-hip ratio and the trunk section. The suit is one cloth over either. |
| `expression` | choice | `'stern'` | `'neutral'`, `'warm'`, `'stern'`, `'weary'`, `'alert'`, `'squint'` | Baked into the vertices; the brow and eyes are the only part of the face the hood and mask leave showing. stern is the reference: brows angled in over small square eyes. |

## Specs

3,527 triangles, 1 material, 1.77 × 1.86 × 0.36 m, real-world scale.

## Animating this character

It is a **skinned humanoid**: 31 bones, Mixamo names (`Hips`, `Spine`, `Spine1`,
`Spine2`, `Neck`, `Head`, `Left`/`RightShoulder`, `Arm`, `ForeArm`, `Hand`,
`Left`/`RightUpLeg`, `Leg`, `Foot`, `ToeBase`), rigidly bound one bone per vertex
so the flat-shaded facets stay crisp. Rest pose is a T-pose with identity local
rotations, facing +Z, feet at y=0.

The GLB carries 3 clip(s): `gltf.animations`.

## Building this with an AI agent?

Point it at https://polyfork.dev/prompt.txt: the whole catalogue as working
instructions, no key needed to browse. There is an MCP server too, at https://polyfork.dev/mcp.

## License

```
Licence

THE MESH FILES (.glb, .fbx, .usdz, .usda, .obj, .mtl) are yours under
either licence, your choice:

1. CC BY 4.0, https://creativecommons.org/licenses/by/4.0/
   Use, share, modify, re-upload and sell them, commercially too, as long as you
   credit Polyfork and say if you changed them. The credit line:
   Clean-Room Technician by Polyfork (https://polyfork.dev/asset/clean-room-technician-da290a), CC BY 4.0

2. The Polyfork licence, below: no credit needed, but no redistributing the files.

EVERYTHING ELSE here, the ES modules (.mjs) above all, is under the
Polyfork licence only. CC BY does not cover it.

The Polyfork licence

Use these files in personal and commercial projects: games, apps, websites, client
work, products you sell. Modify freely. No attribution required. Do not resell or
redistribute the files themselves as assets, or use them to build or train a
COMMERCIAL 3D asset generator: a model, service or pipeline that produces 3D assets
and that you sell or offer to others. Personal experiments, research and learning
are fine. Breaking these terms can end your licence and your access, without a
refund.

Full terms: https://polyfork.dev/licensing
```
