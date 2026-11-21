# Laundry Line Run

https://polyfork.dev/asset/laundry-line-run-dcbc64

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `laundry-line-run-dcbc64.mjs` | the model as an ES module, with every option below |
| `laundry-line-run-dcbc64.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`laundry-line-run-dcbc64.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './laundry-line-run-dcbc64.mjs';
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

Prefer the GLB? `new GLTFLoader().load('laundry-line-run-dcbc64.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  colorway: 'aegean',
  count: 7,
  sag: 0.18,
  wind: 0.5,
  whitewash: 0.3,
  linen: '#f8f6ef',
  cotton: '#e2ded7',
  blue: '#5991c1',
  red: '#a26b43',
  collar: '#c4946c',
  denim: '#2f5b91',
  sock: '#86b8dd',
  heel: '#b6d4eb',
  stripe: '#c0463e',
  rope: '#b79461',
  peg: '#7a6345',
  interior: '#34322d',
  iron: '#4e4c4c',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `colorway` | choice | `'aegean'` | `'aegean'`, `'olive-grove'`, `'harbour-grey'` | Curated laundry colours. aegean: white shirt, Aegean blue towel and T-shirt, terracotta dress, navy trousers. olive-grove: olive green and earth brown cloth. harbour-grey: steel blue, wine red and grey work trousers. The striped sky-blue socks, rope, pegs and metalwork stay the same. |
| `count` | range | `7` | `0` to `8` | How many garments hang on the line, spaced evenly along it, added in this order: 1 the white long-sleeved shirt, 2 adds the striped socks, 3 the blue bath towel, then the sheet, the trousers, the dress, the brown hand towel and last the T-shirt. 0 is a bare rope loop between the hooks and the pulley; 8 is a full wash day, front run over back run. |
| `sag` | range | `0.18` | `0.05` to `0.35` | How far the rope droops at mid-span below the 2.6 m fixings, in metres; the garments follow the rope. 0.05 is a line pulled drum-tight and nearly straight, 0.35 a slack, deeply bellied rope whose middle laundry hangs a third of a metre lower than the ends. |
| `wind` | range | `0.5` | `0` to `1` | How hard the wind fills the cloth in the still pose. 0 is a dead-calm day: garments hang plumb with only soft pleats. 1 is a gusty meltemi: every garment bellies out toward the front and its hem kicks forward about 15 cm. The animated sway rides on top of this. |
| `whitewash` | range | `0.3` | `0` to `1` | Kit-wide weathering. This part has no limewash, so it reads as sun fading: 0 is freshly dyed laundry and new rope, 1 a long summer later with the colours bleached chalky, whites yellowed, rope sun-bleached, pegs silvered grey and the metalwork rust-tinged. Colour only. |
| `linen` | color | `'#f8f6ef'` | any hex or `THREE.Color` | White laundry: the long-sleeved shirt and its collar, the white sock stripes and the towel band. |
| `cotton` | color | `'#e2ded7'` | any hex or `THREE.Color` | The unbleached cotton bed sheet on the back run, and the shirt buttons. |
| `blue` | color | `'#5991c1'` | any hex or `THREE.Color` | The accent laundry colour: the bath towel and the T-shirt. Aegean blue by default. |
| `red` | color | `'#a26b43'` | any hex or `THREE.Color` | The dress and the hand towel. |
| `collar` | color | `'#c4946c'` | any hex or `THREE.Color` | The rounded Peter Pan collar on the dress: the dress's brown a few shades lighter. |
| `denim` | color | `'#2f5b91'` | any hex or `THREE.Color` | The work trousers, their pockets and fly. |
| `sock` | color | `'#86b8dd'` | any hex or `THREE.Color` | The light sky-blue knit of the pair of socks. |
| `heel` | color | `'#b6d4eb'` | any hex or `THREE.Color` | The reinforced heel patch on the outer bump of each sock: the sock's own blue, a few shades lighter. |
| `stripe` | color | `'#c0463e'` | any hex or `THREE.Color` | The red stripe knitted between the white bands at the top of each sock. |
| `rope` | color | `'#b79461'` | any hex or `THREE.Color` | The natural-fibre washing line. |
| `peg` | color | `'#7a6345'` | any hex or `THREE.Color` | The wooden clothes pegs. |
| `interior` | color | `'#34322d'` | any hex or `THREE.Color` | The dark inside of the shirt, T-shirt, dress and trousers, seen through their open necklines, armholes, waistband, cuffs and hems (from below you look up into it). |
| `iron` | color | `'#4e4c4c'` | any hex or `THREE.Color` | The cast-iron wall plates, hooks, pulley wheel and bracket, and the trouser zip pull. |

## Specs

894 triangles, 1 material, 3.96 × 1.2 × 0.47 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): line-front, line-back, pulley.

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
   Laundry Line Run by Polyfork (https://polyfork.dev/asset/laundry-line-run-dcbc64), CC BY 4.0

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
