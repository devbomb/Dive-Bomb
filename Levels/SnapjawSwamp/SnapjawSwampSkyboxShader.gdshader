shader_type sky;

uniform vec3 sky_color : source_color;
uniform sampler2D trunks_texture : filter_nearest, source_color, hint_default_black;
uniform sampler2D leaves_texture : filter_nearest, source_color, hint_default_black;
uniform float exposure : hint_range(0, 128) = 1.0;


void sky()
{
	vec2 leaf_uv = SKY_COORDS;
	leaf_uv.y += 0.001 * sin((2.0 * TIME) + (leaf_uv.x * 10.0));
	
	COLOR = sky_color;
	
	vec4 trunk_color = texture(trunks_texture, SKY_COORDS);
	COLOR = mix(COLOR, trunk_color.rgb, trunk_color.a);
	
	vec4 leaf_color = texture(leaves_texture, leaf_uv);
	COLOR = mix(COLOR, leaf_color.rgb, leaf_color.a);
	
	COLOR *= exposure;
}
