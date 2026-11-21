# Surveyor

https://polyfork.dev/asset/surveyor-82e5c7

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `surveyor-82e5c7.mjs` | the model as an ES module, with every option below |
| `surveyor-82e5c7.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`surveyor-82e5c7.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './surveyor-82e5c7.mjs';
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

Prefer the GLB? `new GLTFLoader().load('surveyor-82e5c7.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  wear: 0.3,
  fleece: 'zipped',
  drawings: 'none',
  figure: 'feminine',
  age: 'adult',
  build: 0.86,
  coat: '#ef6a1e',
  reflect: '#afb2ac',
  shirt: '#8c9190',
  helmet: '#e8ebea',
  denim: '#8e7859',
  boot: '#56442f',
  pencil: '#ffc24a',
  instrument: '#868f6c',
  tripod: '#c2cace',
  skin: '#cfa686',
  hair: '#c8a05a',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `wear` | range | `0.3` | `0` to `1` | 0 new and clean, 1 old and site-worn: faded paint, mud, dust, rust and small chips; the object itself never changes. |
| `fleece` | choice | `'zipped'` | `'zipped'`, `'open'` | The grey fleece under the vest. zipped: done up to the stand collar round her throat, as both references draw her. open: unzipped at the neck, the collar's two halves parted with her throat between them, and the vest's own front parted a little further to show it. Nothing else on her changes. |
| `drawings` | choice | `'none'` | `'none'`, `'pocket'` | A folded setting-out drawing. none: just the pencil in her left breast pocket, as both references draw her. pocket: a second breast pocket on the right of the vest with the folded drawing standing out of it. Nothing else on her changes. |
| `figure` | choice | `'feminine'` | `'masculine'`, `'feminine'` | The frame under the kit. Every garment refits itself, and the arms are held on the instrument on either frame. |
| `age` | choice | `'adult'` | `'kid'`, `'teen'`, `'adult'`, `'old'` | The proportion ladder, not a scale factor: adult is the reference. The tripod is sized and placed against her own eye height and chest, so a younger surveyor stands behind a smaller instrument. |
| `build` | range | `0.86` | `0.86` to `1.18` | Girth of the frame the fleece and vest hang off. 0.86 is the slight build the brief describes; the tripod stands off her chest by the same distance at every value. |
| `coat` | color | `'#ef6a1e'` | any hex or `THREE.Color` | Albedo of the hi-vis vest. |
| `reflect` | color | `'#afb2ac'` | any hex or `THREE.Color` | Albedo of the vest's reflective tape. |
| `shirt` | color | `'#8c9190'` | any hex or `THREE.Color` | Albedo of the fleece, its collar and cuffs. |
| `helmet` | color | `'#e8ebea'` | any hex or `THREE.Color` | Albedo of the hard hat. |
| `denim` | color | `'#8e7859'` | any hex or `THREE.Color` | Albedo of the work trousers. |
| `boot` | color | `'#56442f'` | any hex or `THREE.Color` | Albedo of the work boots. |
| `pencil` | color | `'#ffc24a'` | any hex or `THREE.Color` | Albedo of the pencil in the vest pocket. |
| `instrument` | color | `'#868f6c'` | any hex or `THREE.Color` | Albedo of the total station's body. |
| `tripod` | color | `'#c2cace'` | any hex or `THREE.Color` | Albedo of the tripod's aluminium legs. |
| `skin` | color | `'#cfa686'` | any hex or `THREE.Color` | Albedo of the skin. |
| `hair` | color | `'#c8a05a'` | any hex or `THREE.Color` | Albedo of the hair. |

## Specs

4,341 triangles, 1 material, 1.62 × 1.82 × 1.09 m, real-world scale.

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
   Surveyor by Polyfork (https://polyfork.dev/asset/surveyor-82e5c7), CC BY 4.0

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
