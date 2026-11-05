shader_type canvas_item;
render_mode unshaded;
uniform sampler2D fog_mask : filter_nearest, repeat_disable;
uniform vec2 mask_size;
uniform vec2 world_origin;
uniform vec2 world_size;
uniform bool fog_enabled = true;
varying vec2 world_position;
varying vec2 anchor_position;
void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
    anchor_position = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy;
}
float visible_at(vec2 position) {
    vec2 uv = (position - world_origin) / world_size;
    if (any(lessThan(uv, vec2(0.0))) || any(greaterThanEqual(uv, vec2(1.0)))) return 0.0;
    return texture(fog_mask, (floor(uv * mask_size) + vec2(0.5)) / mask_size).r;
}
void fragment() {
    // Same center-cell rule for ground/flying enemies, shots and short-lived effects.
    if (fog_enabled && (visible_at(anchor_position) < 0.5 || visible_at(world_position) < 0.5)) discard;
}
