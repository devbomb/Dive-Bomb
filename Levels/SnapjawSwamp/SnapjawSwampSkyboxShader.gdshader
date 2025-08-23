shader_type sky;

uniform vec3 sky_color : source_color;
uniform sampler2D trunks_texture : filter_nearest, source_color, hint_default_black;
uniform sampler2D leaves_texture : filter_nearest, source_color, hint_default_black;
uniform sampler2D water_texture : filter_nearest, source_color, hint_default_black;
uniform float exposure : hint_range(0, 128) = 1.0;


void sky()
{	
	COLOR = sky_color;
	
	vec4 trunk_color = texture(trunks_texture, SKY_COORDS);
	COLOR = mix(COLOR, trunk_color.rgb, trunk_color.a);
	
	vec2 leaf_uv = SKY_COORDS;
	leaf_uv.y += 0.001 * sin((2.0 * TIME) + (leaf_uv.x * 10.0));
	vec4 leaf_color = texture(leaves_texture, leaf_uv);
	COLOR = mix(COLOR, leaf_color.rgb, leaf_color.a);
	
	vec2 water_uv = SKY_COORDS;
	water_uv.x += 0.0001 * cos((10.1 * TIME) + (SKY_COORDS.y * 100.0));
	water_uv.y += 0.002 * sin((1.0 * TIME) + (SKY_COORDS.x * 50.0));
	water_uv.y += 0.001 * sin((3.0 * TIME) + (SKY_COORDS.y * 100.0));
	vec4 water_color = texture(water_texture, water_uv);
	COLOR = mix(COLOR, water_color.rgb, water_color.a);
	
	COLOR *= exposure;
}
