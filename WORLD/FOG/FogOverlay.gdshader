shader_type canvas_item;
render_mode unshaded;
uniform sampler2D fog_mask : filter_linear, repeat_disable;
uniform vec2 mask_size;
uniform vec2 world_size;
uniform float unexplored_opacity = 1.0;
uniform float explored_opacity = 0.7;
uniform float edge_softness = 48.0;

void fragment() {
    vec2 cell_uv = (floor(UV * mask_size) + vec2(0.5)) / mask_size;
    vec2 logical = texture(fog_mask, cell_uv).rg;
    vec2 step_uv = vec2(edge_softness) / world_size * 0.5;
    float vision = texture(fog_mask, UV).r * 0.4;
    vision += texture(fog_mask, UV + vec2(step_uv.x, 0.0)).r * 0.15;
    vision += texture(fog_mask, UV - vec2(step_uv.x, 0.0)).r * 0.15;
    vision += texture(fog_mask, UV + vec2(0.0, step_uv.y)).r * 0.15;
    vision += texture(fog_mask, UV - vec2(0.0, step_uv.y)).r * 0.15;
    // Cosmetic interpolation never exposes an unexplored logical cell.
    float opacity = logical.g < 0.5 ? unexplored_opacity : explored_opacity * (1.0 - vision);
    COLOR = vec4(0.0, 0.0, 0.0, opacity);
}
