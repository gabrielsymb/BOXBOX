# Follow-Me Car

https://polyfork.dev/asset/follow-me-car-b3c6a5

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `follow-me-car-b3c6a5.mjs` | the model as an ES module, with every option below |
| `follow-me-car-b3c6a5.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`follow-me-car-b3c6a5.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './follow-me-car-b3c6a5.mjs';
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

Prefer the GLB? `new GLTFLoader().load('follow-me-car-b3c6a5.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  colorway: 'service-yellow',
  wear: 0.2,
  cab: 'crew',
  bed: 'open',
  roofSign: 'sign',
  message: 'follow-me',
  body: '#e8be30',
  chequer: '#282f3f',
  trim: '#696568',
  tyre: '#575451',
  rim: '#948f8f',
  metal: '#8fa5b1',
  glass: '#3a4d6b',
  plate: '#eeeded',
  lamp: '#f5f3f4',
  board: '#4c3a43',
  tail: '#b94c34',
  beacon: '#e0902c',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `colorway` | choice | `'service-yellow'` | `'service-yellow'`, `'handler-blue'`, `'apron-white'` | Fleet livery. service-yellow: the airfield follow-me yellow with a black chequer band; handler-blue: handling-agent blue with a white chequer; apron-white: white with a red chequer. Lamps and beacons keep their colours in every colorway. |
| `wear` | range | `0.2` | `0` to `1` | 0 freshly painted and clean, 1 old and weathered: faded paint, exhaust soot, rubber marks, grime and small chips; the object itself never changes. |
| `cab` | choice | `'crew'` | `'crew'`, `'single'` | crew: four doors, two side windows a side and a 1.45 m bed; single: two long doors, one side window, a short roof and a 2.15 m bed. Length, wheelbase and the sign height stay the same. |
| `bed` | choice | `'open'` | `'open'`, `'canopy'` | open: open load bed with a stainless sports bar (hoop plus two diagonal stays) carrying the roof sign; canopy: a body-coloured hard-top over the bed at roof height with side and rear windows, the sign carried on its front edge. |
| `roofSign` | choice | `'sign'` | `'sign'`, `'beacons'` | sign: a dark message board with lit LED lettering, flanked by two amber beacons; beacons: two amber beacons on a bare light bar, no board. |
| `message` | choice | `'follow-me'` | `'follow-me'`, `'stop'` | The lit words on both faces of the roof board, modelled as LED letters: follow-me reads FOLLOW ME across the whole board; stop reads a short, larger STOP. No effect when Roof sign is beacons. |
| `body` | color | `'#e8be30'` | any hex or `THREE.Color` | Main body paint: bonnet, cab, bed, arch flares, front bumper, canopy. |
| `chequer` | color | `'#282f3f'` | any hex or `THREE.Color` | The dark squares of the two-row chequer band on doors and bed sides. |
| `trim` | color | `'#696568'` | any hex or `THREE.Color` | Black plastic: bumpers, grille back, mirrors, bed liner, arch liners, sign housing, window reveals. |
| `tyre` | color | `'#575451'` | any hex or `THREE.Color` | Tyre rubber. |
| `rim` | color | `'#948f8f'` | any hex or `THREE.Color` | Five-spoke cast wheel rims. |
| `metal` | color | `'#8fa5b1'` | any hex or `THREE.Color` | Polished stainless sports bar and grille bars. |
| `glass` | color | `'#3a4d6b'` | any hex or `THREE.Color` | Tinted window glass and mirror glass. |
| `plate` | color | `'#eeeded'` | any hex or `THREE.Color` | Blank number plates, front and rear. |
| `lamp` | color | `'#f5f3f4'` | any hex or `THREE.Color` | Headlamp and fog lamp lenses and the roof board lettering; glows warm white at night. |
| `board` | color | `'#4c3a43'` | any hex or `THREE.Color` | Dark face of the roof message board behind the lit letters. |
| `tail` | color | `'#b94c34'` | any hex or `THREE.Color` | Rear lamp clusters; glows dim red at night. |
| `beacon` | color | `'#e0902c'` | any hex or `THREE.Color` | Amber roof beacon lenses; glow amber at night. |

## Specs

3,010 triangles, 1 material, 2.2 × 2.04 × 4.73 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): wheel-fl, steer-fl, wheel-fr, steer-fr, wheel-rl, wheel-rr.

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
   Follow-Me Car by Polyfork (https://polyfork.dev/asset/follow-me-car-b3c6a5), CC BY 4.0

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
