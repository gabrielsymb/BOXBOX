# Crew Minivan

https://polyfork.dev/asset/crew-minivan-2ed5f1

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `crew-minivan-2ed5f1.mjs` | the model as an ES module, with every option below |
| `crew-minivan-2ed5f1.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`crew-minivan-2ed5f1.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './crew-minivan-2ed5f1.mjs';
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

Prefer the GLB? `new GLTFLoader().load('crew-minivan-2ed5f1.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  colorway: 'charcoal',
  doors: 0,
  roof: 'high',
  roofBox: true,
  wear: 0.2,
  body: '#696568',
  trim: '#575451',
  glass: '#3a4d6b',
  tyre: '#282f3f',
  hub: '#dddbdc',
  interior: '#7b787c',
  headlight: '#f5f3f4',
  tail: '#b94c34',
  beacon: '#cbc852',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `colorway` | choice | `'charcoal'` | `'charcoal'`, `'fleet-white'`, `'handler-blue'` | Curated fleet paint sets. charcoal: dark grey crew shuttle (the hero); fleet-white: white airside fleet van with dark trim; handler-blue: handling-agent blue. Lamps never change. |
| `doors` | range | `0` | `0` to `1` | How far every door stands open, driving the door rig. 0: all doors shut flush. 1: both front doors swung 70 degrees out on their A-pillar hinges, both sliding side doors run back along their tracks over the rear quarters, and the rear barn doors swung 100 degrees open, showing the empty cab seats, crew benches and cabin. In between, every door is partly open. Legacy values closed/open = 0/1. |
| `roof` | choice | `'high'` | `'high'`, `'standard'` | high: tall crew-shuttle roof, roof crown at 2.48 m (the hero). standard: low roof, crown at 2.10 m; shorter side windows, doors and rear doors, same length, width and wheels. |
| `roofBox` | toggle | true | `true`, `false` | On: a low chamfered air-conditioning pod over the rear half of the roof. Off: clean flat roof with only the beacon. |
| `wear` | range | `0.2` | `0` to `1` | 0 freshly painted and clean, 1 old and weathered: faded paint, exhaust soot, rubber marks, grime and small chips; the object itself never changes. |
| `body` | color | `'#696568'` | any hex or `THREE.Color` | Painted steel body, doors, bonnet, roof, roof pod and grille bars. |
| `trim` | color | `'#575451'` | any hex or `THREE.Color` | Black plastic: bumpers, sill cladding, mirrors, handles, beacon base, window seals, door seams, grille surround and wheel wells. |
| `glass` | color | `'#3a4d6b'` | any hex or `THREE.Color` | All windows and mirror glass. |
| `tyre` | color | `'#282f3f'` | any hex or `THREE.Color` | Tyre rubber. |
| `hub` | color | `'#dddbdc'` | any hex or `THREE.Color` | Domed wheel covers. |
| `interior` | color | `'#7b787c'` | any hex or `THREE.Color` | Cab seats, crew benches, dash, floor, inner walls and door cards; seen through any open door. |
| `headlight` | color | `'#f5f3f4'` | any hex or `THREE.Color` | Headlight and fog-lamp lenses (lit warm white at night). |
| `tail` | color | `'#b94c34'` | any hex or `THREE.Color` | Rear corner lamps and the high brake light (lit red at night). |
| `beacon` | color | `'#cbc852'` | any hex or `THREE.Color` | Amber roof beacon lens (lit amber at night). |

## Specs

3,617 triangles, 1 material, 2.28 × 2.55 × 4.98 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): steer-fl, steer-fr, wheel-fl, wheel-fr, wheel-rl, wheel-rr, door-fl, door-fr, door-sl, door-sr, door-rl, door-rr.

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
   Crew Minivan by Polyfork (https://polyfork.dev/asset/crew-minivan-2ed5f1), CC BY 4.0

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
