# Patient In Gown

https://polyfork.dev/asset/patient-in-gown-fdab75

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `patient-in-gown-fdab75.mjs` | the model as an ES module, with every option below |
| `patient-in-gown-fdab75.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`patient-in-gown-fdab75.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './patient-in-gown-fdab75.mjs';
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

Prefer the GLB? `new GLTFLoader().load('patient-in-gown-fdab75.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  figure: 'masculine',
  age: 'adult',
  build: 0.86,
  expression: 'neutral',
  gown: '#82a2a8',
  gownTie: '#82a2a8',
  tape: '#e7e5e1',
  hub: '#8b9298',
  line: '#a3adb4',
  skin: '#cd9667',
  hair: '#735236',
  brow: '#735236',
  beard: '#735236',
  stache: '#735236',
  eye: '#24190e',
  mouth: '#7a5334',
  briefs: '#8c8578',
  bra: '#8c8578',
  crease: '#9a7350',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `figure` | choice | `'masculine'` | `'masculine'`, `'feminine'` | the body the outfit is worn on; every garment refits itself to the figure |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | proportions ladder: the same wardrobe fitted to a kid, a teen or an adult |
| `build` | range | `0.86` | `0.86` to `1.18` | girth of the whole figure; the default is what this character ships at, and the knob runs the whole range the body solves |
| `expression` | choice | `'neutral'` | `'neutral'`, `'warm'`, `'stern'`, `'weary'`, `'alert'`, `'squint'` | the face, from the engine expression set |
| `gown` | color | `'#82a2a8'` | any hex or `THREE.Color` | albedo of the gown zone |
| `gownTie` | color | `'#82a2a8'` | any hex or `THREE.Color` | albedo of the gownTie zone |
| `tape` | color | `'#e7e5e1'` | any hex or `THREE.Color` | albedo of the tape zone |
| `hub` | color | `'#8b9298'` | any hex or `THREE.Color` | albedo of the hub zone |
| `line` | color | `'#a3adb4'` | any hex or `THREE.Color` | albedo of the line zone |
| `skin` | color | `'#cd9667'` | any hex or `THREE.Color` | albedo of the skin zone |
| `hair` | color | `'#735236'` | any hex or `THREE.Color` | albedo of the hair zone |
| `brow` | color | `'#735236'` | any hex or `THREE.Color` | albedo of the brow zone |
| `beard` | color | `'#735236'` | any hex or `THREE.Color` | albedo of the beard zone |
| `stache` | color | `'#735236'` | any hex or `THREE.Color` | albedo of the stache zone |
| `eye` | color | `'#24190e'` | any hex or `THREE.Color` | albedo of the eye zone |
| `mouth` | color | `'#7a5334'` | any hex or `THREE.Color` | albedo of the mouth zone |
| `briefs` | color | `'#8c8578'` | any hex or `THREE.Color` | albedo of the briefs zone |
| `bra` | color | `'#8c8578'` | any hex or `THREE.Color` | albedo of the bra zone |
| `crease` | color | `'#9a7350'` | any hex or `THREE.Color` | albedo of the crease zone |

## Specs

2,959 triangles, 1 material, 1.67 × 1.72 × 0.32 m, real-world scale.

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
   Patient In Gown by Polyfork (https://polyfork.dev/asset/patient-in-gown-fdab75), CC BY 4.0

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
