shader_type spatial;

uniform sampler2D paint_grain : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D metal_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D metal_roughness : filter_linear_mipmap, repeat_enable;
uniform sampler2D dust_grain : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform vec3 paint_color = vec3(0.85, 0.64, 0.34);
uniform float cargo_wear : hint_range(0.0, 1.0) = 0.0;

varying vec3 body_position;
varying vec3 body_normal;

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

void vertex() {
    body_position = VERTEX;
    body_normal = NORMAL;
}

void fragment() {
    // Sample in the truck's local space so dirt stays attached in motion.
    vec2 point = UV + vec2(body_position.y * 0.26, body_position.x * 0.31);
    float broad = patch_noise(point * 4.9 + vec2(2.2, 1.3));
    float mottling = broad * 0.68 + patch_noise(point * 16.0) * 0.32;
    float upward = smoothstep(0.42, 0.83, body_normal.y);
    float sun = upward * smoothstep(0.86, 1.46, body_position.y);
    float faded = smoothstep(0.27, 0.69, mottling + sun * 0.29) * mix(0.39, 0.93, upward);

    float lower = 1.0 - smoothstep(0.53, 1.05, body_position.y);
    float dust = smoothstep(0.36, 0.75, 0.73 - mottling + lower * 0.24 + cargo_wear * 0.18);
    dust *= mix(0.31, 0.8, max(lower, cargo_wear));

    vec3 grain = texture(paint_grain, UV).rgb;
    vec3 worn_paint = mix(paint_color, vec3(0.79, 0.71, 0.51), faded) * grain;
    vec3 soil = texture(dust_grain, UV * 1.7 + vec2(0.21, 0.37)).rgb;
    vec3 road_dust = mix(soil, vec3(0.55, 0.46, 0.32), 0.43);
    float scuffs = (1.0 - smoothstep(0.27, 0.49, mottling))
        * mix(0.36, 0.69, max(upward, cargo_wear));
    vec3 old_primer = vec3(0.45, 0.34, 0.23) * grain;
    ALBEDO = mix(mix(worn_paint, road_dust, dust), old_primer, scuffs);
    METALLIC = mix(0.26, 0.035, max(dust, faded * 0.52));
    ROUGHNESS = clamp(mix(0.43, 0.94, max(max(dust, faded), scuffs))
        + (texture(metal_roughness, UV).r - 0.5) * 0.14, 0.28, 1.0);
    NORMAL_MAP = texture(metal_normal, UV).rgb;
    NORMAL_MAP_DEPTH = mix(0.15, 0.34, max(dust, cargo_wear));
}
