// Polyfork clips.mjs. THIS FILE IS THE API. The implementation is ./_internal/clips.mjs: merging, hull maths, caching
// and shaders, none of which changes how you call it. Commented source: https://polyfork.dev/cdn/clips.mjs
/**
 * animate(humanoid, { clips = ['Idle', 'Walk', 'Run'], inPlace = true, fade = 0.2 }) -> Promise<anim>
 * anim: play(name, { loop, fade, timeScale })   locomotion(metresPerSecond) picks Idle / Walk / Run   update(delta) every frame
 *       actions, mixer, names
 * also exported: CLIP_NAMES (all seventeen). Humanoids only; the clips are fetched from polyfork.dev/cdn.
 */
export * from './_internal/clips.mjs';
export { default } from './_internal/clips.mjs';
