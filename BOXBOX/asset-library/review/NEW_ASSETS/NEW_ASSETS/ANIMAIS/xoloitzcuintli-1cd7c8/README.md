# Xoloitzcuintli

https://polyfork.dev/asset/xoloitzcuintli-1cd7c8

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `xoloitzcuintli-1cd7c8.mjs` | the model as an ES module, with every option below |
| `xoloitzcuintli-1cd7c8.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`xoloitzcuintli-1cd7c8.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './xoloitzcuintli-1cd7c8.mjs';
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

Prefer the GLB? `new GLTFLoader().load('xoloitzcuintli-1cd7c8.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  coat: '#7A736B',
  white: '#AC9A8F',
  tan: '#9A7C62',
  inner: '#BFA3A0',
  dark: '#2B2523',
  marigold: '#e8840f',
  marigoldDeep: '#bf5e0a',
  wilt: '#a86a26',
  leaf: '#55712a',
  brass: '#b08a3a',
  vigil: 'ofrenda',
  tone: 0.45,
  bell: 'brass',
  withers: 0.5,
  build: 0.65,
  length: 1,
  width: 1,
  legs: 1,
  crest: 0.68,
  neck: 1,
  head: 1,
  muzzle: 0.92,
  ears: 1,
  tail: 0.5,
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `coat` | color | `'#7A736B'` | any hex or `THREE.Color` | The bare slate-grey hide over the whole dog. |
| `white` | color | `'#AC9A8F'` | any hex or `THREE.Color` | Pinkish toes, pasterns and the pale throat where the hide lightens. |
| `tan` | color | `'#9A7C62'` | any hex or `THREE.Color` | The irregular copper patches scattered over the hide and the brows. |
| `inner` | color | `'#BFA3A0'` | any hex or `THREE.Color` | The pink inside of the tall bat ears. |
| `dark` | color | `'#2B2523'` | any hex or `THREE.Color` | Nose leather, eyes and lip line. |
| `marigold` | color | `'#e8840f'` | any hex or `THREE.Color` | albedo of the bright cempasúchil orange: the crown of most flower heads |
| `marigoldDeep` | color | `'#bf5e0a'` | any hex or `THREE.Color` | albedo of the deep cempasúchil orange: the band under each crown and the crowns the tone knob deals deep |
| `wilt` | color | `'#a86a26'` | any hex or `THREE.Color` | albedo of browning marigold at dawn: the spent heads |
| `leaf` | color | `'#55712a'` | any hex or `THREE.Color` | albedo of the green leaves tucked between the heads |
| `brass` | color | `'#b08a3a'` | any hex or `THREE.Color` | albedo of the brass bell hanging at the breast |
| `vigil` | choice | `'ofrenda'` | `'before'`, `'ofrenda'`, `'dawn'` | the kit-wide vigil: before builds no garland; ofrenda a ring of fresh cempasúchil round the neck; dawn the same ring with two heads a side browned |
| `tone` | range | `0.45` | `0` to `1` | how many marigold heads run to the deeper orange: 0 every head the bright orange, 1 every head the deep one; dealt per head off a fixed hash, so no two neighbours are guaranteed to match |
| `bell` | choice | `'brass'` | `'brass'`, `'none'` | brass hangs the small bell under the front of the garland; none leaves the flowers alone |
| `withers` | range | `0.5` | `0.35` to `0.85` | Scale the complete dog, including its rig. |
| `build` | range | `0.65` | `0` to `1` | Lean to fuller ribs, keeping the back height fixed. |
| `length` | range | `1` | `0.85` to `1.15` | Length of the trunk and the stance. |
| `width` | range | `1` | `0.85` to `1.15` | Chest width and leg spacing. |
| `legs` | range | `1` | `0.85` to `1.15` | Leg length with paws on the ground. |
| `crest` | range | `0.68` | `0` to `1` | Relaxed to alert neck carriage. |
| `neck` | range | `1` | `0.85` to `1.2` | Length of the raised canine neck. |
| `head` | range | `1` | `0.85` to `1.2` | Scale the head, face and ears together. |
| `muzzle` | range | `0.92` | `0.85` to `1.15` | Length of the tapered collie muzzle. |
| `ears` | range | `1` | `0.8` to `1.15` | Height of the upright ears below their folded black tips. |
| `tail` | range | `0.5` | `0` to `1` | Lower or raise the curved tail. |

## Specs

2,994 triangles, 1 material, 0.25 × 0.88 × 1.07 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): Head, Neck, Jaw, LeftEar, RightEar, Tail, TailTip, LeftFrontUpLeg, LeftFrontElbow, LeftFrontLeg, LeftFrontFoot, LeftHindUpLeg, LeftHindLeg, LeftHindHock, LeftHindFoot, RightFrontUpLeg, RightFrontElbow, RightFrontLeg, RightFrontFoot, RightHindUpLeg, RightHindLeg, RightHindHock, RightHindFoot.

Detachable parts (the surface behind stays closed): attach-collar.

## Walking this machine

It has **4 legs** (2-link), and `walk.mjs` in this folder walks it:

```js
import { walker, raycastGround } from './walk.mjs';

const machine = createAsset();
scene.add(machine);
const gait = walker(machine, { ground: raycastGround([yourTerrain]) });

// in your render loop, after you have moved it yourself:
distance += speed * delta;
machine.position.z = distance;
gait.poseAt(distance);
```

You keep the steering; it owns the legs, the ride height and the body tilt. It
finds the ground under each foothold (an analytic height field if you have one, a
downward ray otherwise), plants each foot so it does not slide while the body
passes over it, and leans the chassis onto the plane through the feet that are
down. The phase comes from DISTANCE, not from a clock, so the walk is identical
at any frame rate and can be scrubbed or rendered offline.

Measured on this model: a natural step of **0.053 m**, and a planted foot that does not move at all (0 mm of drift on level ground, a slope and a ripple).

Not using three.js? The same numbers are in `https://polyfork.dev/api/assets/xoloitzcuintli-1cd7c8`
under `gait`: leg names, segment lengths, foot offsets, phase order, step and
duty. It is about thirty lines to reimplement against that.

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
   Xoloitzcuintli by Polyfork (https://polyfork.dev/asset/xoloitzcuintli-1cd7c8), CC BY 4.0

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
