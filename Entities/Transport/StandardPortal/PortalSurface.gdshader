shader_type spatial;
render_mode unshaded;

uniform sampler2D viewport_texture : source_color;

void fragment() {
    ALBEDO = vec3(texture(viewport_texture, SCREEN_UV).rgb);
}
