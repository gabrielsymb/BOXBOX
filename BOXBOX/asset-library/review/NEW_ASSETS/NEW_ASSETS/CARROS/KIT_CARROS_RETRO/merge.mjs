// Polyfork merge.mjs. THIS FILE IS THE API. The implementation is ./_internal/merge.mjs: merging, hull maths, caching
// and shaders, none of which changes how you call it. Commented source: https://polyfork.dev/cdn/merge.mjs
/**
 * mergeAssets([{ object, rig }], { keepDynamic = true }) -> { merged, dynamic }
 * One mesh per material out of many objects. buildKit already does this for a place; call it for your own scatter.
 */
export * from './_internal/merge.mjs';
