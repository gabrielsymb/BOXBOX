# Police Officer

https://polyfork.dev/asset/police-officer-1cc379

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `police-officer-1cc379.mjs` | the model as an ES module, with every option below |
| `police-officer-1cc379.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`police-officer-1cc379.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './police-officer-1cc379.mjs';
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

Prefer the GLB? `new GLTFLoader().load('police-officer-1cc379.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  figure: 'masculine',
  age: 'adult',
  build: 0.86,
  expression: 'neutral',
  shirt: '#333749',
  cuff: '#333749',
  shirtCollar: '#2b2f40',
  cap: '#333749',
  capTrim: '#211f1d',
  brass: '#808080',
  button: '#333749',
  denim: '#333749',
  waistband: '#282b3b',
  boot: '#211f1d',
  sole: '#151312',
  belt: '#211f1d',
  strap: '#1c1b19',
  buckleMetal: '#7b8b8f',
  pouchStud: '#6e6e6e',
  skin: '#c8946a',
  hair: '#463b37',
  eye: '#211f1d',
  mouth: '#b98560',
  briefs: '#a59e89',
  bra: '#a59e89',
  crease: '#8a6244',
  beard: '#9e9e98',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `figure` | choice | `'masculine'` | `'masculine'`, `'feminine'` | the body the outfit is worn on; every garment refits itself to the figure |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | proportions ladder: the same wardrobe fitted to a kid, a teen or an adult |
| `build` | range | `0.86` | `0.86` to `1.18` | girth of the whole figure; the default is what this character ships at, and the knob runs the whole range the body solves |
| `expression` | choice | `'neutral'` | `'neutral'`, `'warm'`, `'stern'`, `'weary'`, `'alert'`, `'squint'` | the face, from the engine expression set |
| `shirt` | color | `'#333749'` | any hex or `THREE.Color` | albedo of the shirt zone |
| `cuff` | color | `'#333749'` | any hex or `THREE.Color` | albedo of the cuff zone |
| `shirtCollar` | color | `'#2b2f40'` | any hex or `THREE.Color` | albedo of the shirtCollar zone |
| `cap` | color | `'#333749'` | any hex or `THREE.Color` | albedo of the cap zone |
| `capTrim` | color | `'#211f1d'` | any hex or `THREE.Color` | albedo of the capTrim zone |
| `brass` | color | `'#808080'` | any hex or `THREE.Color` | albedo of the brass zone |
| `button` | color | `'#333749'` | any hex or `THREE.Color` | albedo of the button zone |
| `denim` | color | `'#333749'` | any hex or `THREE.Color` | albedo of the denim zone |
| `waistband` | color | `'#282b3b'` | any hex or `THREE.Color` | albedo of the waistband zone |
| `boot` | color | `'#211f1d'` | any hex or `THREE.Color` | albedo of the boot zone |
| `sole` | color | `'#151312'` | any hex or `THREE.Color` | albedo of the sole zone |
| `belt` | color | `'#211f1d'` | any hex or `THREE.Color` | albedo of the belt zone |
| `strap` | color | `'#1c1b19'` | any hex or `THREE.Color` | albedo of the strap zone |
| `buckleMetal` | color | `'#7b8b8f'` | any hex or `THREE.Color` | albedo of the buckleMetal zone |
| `pouchStud` | color | `'#6e6e6e'` | any hex or `THREE.Color` | albedo of the pouchStud zone |
| `skin` | color | `'#c8946a'` | any hex or `THREE.Color` | albedo of the skin zone |
| `hair` | color | `'#463b37'` | any hex or `THREE.Color` | albedo of the hair zone |
| `eye` | color | `'#211f1d'` | any hex or `THREE.Color` | albedo of the eye zone |
| `mouth` | color | `'#b98560'` | any hex or `THREE.Color` | albedo of the mouth zone |
| `briefs` | color | `'#a59e89'` | any hex or `THREE.Color` | albedo of the briefs zone |
| `bra` | color | `'#a59e89'` | any hex or `THREE.Color` | albedo of the bra zone |
| `crease` | color | `'#8a6244'` | any hex or `THREE.Color` | albedo of the crease zone |
| `beard` | color | `'#9e9e98'` | any hex or `THREE.Color` | albedo of the beard zone |

## Specs

3,499 triangles, 1 material, 1.67 × 1.79 × 0.37 m, real-world scale.

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
   Police Officer by Polyfork (https://polyfork.dev/asset/police-officer-1cc379), CC BY 4.0

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
