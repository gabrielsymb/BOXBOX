shader_type spatial;

uniform float metres_per_uv = 5.0;

uniform sampler2D grass_color : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D dirt_color : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D dirt_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_roughness : filter_linear_mipmap, repeat_enable;
uniform sampler2D dirt_roughness : filter_linear_mipmap, repeat_enable;

float hash_cell(vec2 cell) {
    return fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);
}

float patch_noise(vec2 point) {
    vec2 cell = floor(point);
    vec2 fraction = fract(point);
    vec2 weight = fraction * fraction * (3.0 - 2.0 * fraction);
    return mix(
        mix(hash_cell(cell), hash_cell(cell + vec2(1, 0)), weight.x),
        mix(hash_cell(cell + vec2(0, 1)), hash_cell(cell + vec2(1, 1)), weight.x),
        weight.y
    );
}

void fragment() {
    vec2 metres = UV * metres_per_uv;
    vec2 grass_uv = metres / 2.4;
    float patches = patch_noise(metres * 0.66) * 0.68
        + patch_noise(metres * 1.95) * 0.32;
    float cover = mix(0.12, 0.88, smoothstep(0.28, 0.74, patches));
    vec3 grass = texture(grass_color, grass_uv).rgb * vec3(0.53, 0.62, 0.47);
    vec3 dirt = texture(dirt_color, UV).rgb * vec3(0.85, 0.79, 0.70);
    ALBEDO = mix(dirt, grass, cover);
    NORMAL_MAP = mix(texture(dirt_normal, UV).rgb, texture(grass_normal, grass_uv).rgb, cover);
    NORMAL_MAP_DEPTH = mix(0.65, 0.78, cover);
    ROUGHNESS = mix(texture(dirt_roughness, UV).r, texture(grass_roughness, grass_uv).r, cover);
}
