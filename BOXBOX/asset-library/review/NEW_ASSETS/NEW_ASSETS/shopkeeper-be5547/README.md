# Shopkeeper

https://polyfork.dev/asset/shopkeeper-be5547

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `shopkeeper-be5547.mjs` | the model as an ES module, with every option below |
| `shopkeeper-be5547.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`shopkeeper-be5547.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './shopkeeper-be5547.mjs';
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

Prefer the GLB? `new GLTFLoader().load('shopkeeper-be5547.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  figure: 'masculine',
  age: 'adult',
  build: 1,
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `figure` | choice | `'masculine'` | `'masculine'`, `'feminine'` | The body under the apron. The shirt, the apron, the trousers and the towel refit themselves to the shoulder-to-hip ratio and the chest and waist section; the skeleton does not move by a bone. |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | The proportion ladder, not a scale factor: the shop's son helping out after school, or the old man who has kept it forty years. Every garment is sized off the body's own stations. |
| `build` | range | `1` | `0.85` to `1.2` | Girth of the figure inside the clothes: 1.0 is the stocky shopkeeper both references draw, 0.85 a lean one, 1.20 a heavy one. The garments wrap whatever is inside them. |

## Specs

4,347 triangles, 1 material, 1.6 × 1.67 × 0.38 m, real-world scale.

Detachable parts (the surface behind stays closed): attach-hat, attach-coat.

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
   Shopkeeper by Polyfork (https://polyfork.dev/asset/shopkeeper-be5547), CC BY 4.0

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
