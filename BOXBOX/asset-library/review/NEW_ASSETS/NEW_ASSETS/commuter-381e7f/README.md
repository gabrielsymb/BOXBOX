# Commuter

https://polyfork.dev/asset/commuter-381e7f

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `commuter-381e7f.mjs` | the model as an ES module, with every option below |
| `commuter-381e7f.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`commuter-381e7f.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './commuter-381e7f.mjs';
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

Prefer the GLB? `new GLTFLoader().load('commuter-381e7f.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  figure: 'masculine',
  age: 'adult',
  build: 0.86,
  expression: 'neutral',
  shirt: '#43413e',
  cuff: '#5e5d5b',
  cuffFold: '#393837',
  coat: '#525150',
  coatTrim: '#474645',
  ventShade: '#2c2b2a',
  scarf: '#b6b4b1',
  denim: '#443933',
  waistband: '#2f2925',
  boot: '#4a372a',
  sole: '#2e211a',
  skin: '#eed3b8',
  hair: '#a68657',
  brow: '#4e3a22',
  eye: '#2a2320',
  mouth: '#9e8d7a',
  briefs: '#a59e89',
  bra: '#a59e89',
  crease: '#8e6a4e',
  case: '#323029',
  cup: '#ddd6cf',
  cupSleeve: '#d5af8a',
  cupLid: '#937a61',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `figure` | choice | `'masculine'` | `'masculine'`, `'feminine'` | the body the outfit is worn on; every garment refits itself to the figure |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | proportions ladder: the same wardrobe fitted to a kid, a teen or an adult |
| `build` | range | `0.86` | `0.86` to `1.18` | girth of the whole figure; the default is what this character ships at, and the knob runs the whole range the body solves |
| `expression` | choice | `'neutral'` | `'neutral'`, `'warm'`, `'stern'`, `'weary'`, `'alert'`, `'squint'` | the face, from the engine expression set |
| `shirt` | color | `'#43413e'` | any hex or `THREE.Color` | albedo of the shirt zone |
| `cuff` | color | `'#5e5d5b'` | any hex or `THREE.Color` | albedo of the cuff zone |
| `cuffFold` | color | `'#393837'` | any hex or `THREE.Color` | albedo of the cuffFold zone |
| `coat` | color | `'#525150'` | any hex or `THREE.Color` | albedo of the coat zone |
| `coatTrim` | color | `'#474645'` | any hex or `THREE.Color` | albedo of the coatTrim zone |
| `ventShade` | color | `'#2c2b2a'` | any hex or `THREE.Color` | albedo of the ventShade zone |
| `scarf` | color | `'#b6b4b1'` | any hex or `THREE.Color` | albedo of the scarf zone |
| `denim` | color | `'#443933'` | any hex or `THREE.Color` | albedo of the denim zone |
| `waistband` | color | `'#2f2925'` | any hex or `THREE.Color` | albedo of the waistband zone |
| `boot` | color | `'#4a372a'` | any hex or `THREE.Color` | albedo of the boot zone |
| `sole` | color | `'#2e211a'` | any hex or `THREE.Color` | albedo of the sole zone |
| `skin` | color | `'#eed3b8'` | any hex or `THREE.Color` | albedo of the skin zone |
| `hair` | color | `'#a68657'` | any hex or `THREE.Color` | albedo of the hair zone |
| `brow` | color | `'#4e3a22'` | any hex or `THREE.Color` | albedo of the brow zone |
| `eye` | color | `'#2a2320'` | any hex or `THREE.Color` | albedo of the eye zone |
| `mouth` | color | `'#9e8d7a'` | any hex or `THREE.Color` | albedo of the mouth zone |
| `briefs` | color | `'#a59e89'` | any hex or `THREE.Color` | albedo of the briefs zone |
| `bra` | color | `'#a59e89'` | any hex or `THREE.Color` | albedo of the bra zone |
| `crease` | color | `'#8e6a4e'` | any hex or `THREE.Color` | albedo of the crease zone |
| `case` | color | `'#323029'` | any hex or `THREE.Color` | albedo of the case zone |
| `cup` | color | `'#ddd6cf'` | any hex or `THREE.Color` | albedo of the cup zone |
| `cupSleeve` | color | `'#d5af8a'` | any hex or `THREE.Color` | albedo of the cupSleeve zone |
| `cupLid` | color | `'#937a61'` | any hex or `THREE.Color` | albedo of the cupLid zone |

## Specs

3,801 triangles, 1 material, 2.03 × 1.73 × 0.49 m, real-world scale.

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
   Commuter by Polyfork (https://polyfork.dev/asset/commuter-381e7f), CC BY 4.0

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
