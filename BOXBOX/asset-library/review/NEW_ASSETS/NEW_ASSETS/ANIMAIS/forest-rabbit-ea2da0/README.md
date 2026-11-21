# Forest Rabbit

https://polyfork.dev/asset/forest-rabbit-ea2da0

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `forest-rabbit-ea2da0.mjs` | the model as an ES module, with every option below |
| `forest-rabbit-ea2da0.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`forest-rabbit-ea2da0.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './forest-rabbit-ea2da0.mjs';
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

Prefer the GLB? `new GLTFLoader().load('forest-rabbit-ea2da0.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  coat: '#998D7D',
  pale: '#E6E2DA',
  ring: '#D2C6B2',
  inner: '#B89C92',
  eye: '#161310',
  iris: '#5A3C2A',
  nose: '#5E4639',
  withers: 0.24,
  build: 1,
  length: 1,
  width: 1,
  head: 1,
  cheeks: 1,
  ears: 1,
  earSpread: 1,
  feet: 1,
  tail: 1,
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `coat` | color | `'#998D7D'` | any hex or `THREE.Color` | The grey-brown agouti coat: body, haunches, head, ear backs and rims, legs and feet. |
| `pale` | color | `'#E6E2DA'` | any hex or `THREE.Color` | The pale grey-white cottontail and the white stomach, from the chest between the forelegs back between the haunches. |
| `ring` | color | `'#D2C6B2'` | any hex or `THREE.Color` | The pale facets round each eye and the catchlight. |
| `inner` | color | `'#B89C92'` | any hex or `THREE.Color` | The soft pink-mauve inside of the ear cups. |
| `eye` | color | `'#161310'` | any hex or `THREE.Color` | The near-black pupil and lid line of the eyes. |
| `iris` | color | `'#5A3C2A'` | any hex or `THREE.Color` | The dark brown iris round each pupil. |
| `nose` | color | `'#5E4639'` | any hex or `THREE.Color` | The dark rose-brown nose, the split lip and the closed mouth line. |
| `withers` | range | `0.24` | `0.17` to `0.32` | Height of the back in metres in the crouch; the whole rabbit and its rig scale with it. |
| `build` | range | `1` | `0.88` to `1.12` | Size of the round haunches that make the wide pear-shaped base. |
| `length` | range | `1` | `0.92` to `1.08` | Length of the back from the shoulders to the rump. |
| `width` | range | `1` | `0.9` to `1.1` | Width of the whole body and the stance of the feet. |
| `head` | range | `1` | `0.9` to `1.1` | Scale of the skull, cheeks and muzzle about the back of the head. |
| `cheeks` | range | `1` | `0.85` to `1.15` | Fullness of the round cheeks and jowls under the eyes. |
| `ears` | range | `1` | `0.85` to `1.15` | Length of the upright ears. |
| `earSpread` | range | `1` | `0.6` to `1.4` | Outward V of the pair: low holds them parallel, high splays them. |
| `feet` | range | `1` | `0.9` to `1.1` | Length of the long flat hind feet. |
| `tail` | range | `1` | `0.8` to `1.2` | Size of the pale cottontail. |

## Specs

2,074 triangles, 1 material, 0.2 × 0.37 × 0.4 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): Head, Neck, Jaw, LeftEar, RightEar, Tail, TailTip, LeftFrontUpLeg, LeftFrontElbow, LeftFrontLeg, LeftFrontFoot, LeftHindUpLeg, LeftHindLeg, LeftHindHock, LeftHindFoot, RightFrontUpLeg, RightFrontElbow, RightFrontLeg, RightFrontFoot, RightHindUpLeg, RightHindLeg, RightHindHock, RightHindFoot.

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

Measured on this model: a natural step of **0.073 m**, and a planted foot that does not move at all (0 mm of drift on level ground, a slope and a ripple).

Not using three.js? The same numbers are in `https://polyfork.dev/api/assets/forest-rabbit-ea2da0`
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
   Forest Rabbit by Polyfork (https://polyfork.dev/asset/forest-rabbit-ea2da0), CC BY 4.0

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
