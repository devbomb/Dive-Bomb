shader_type spatial;
render_mode cull_disabled, unshaded;
#include "res://Common/ShaderIncludes/Noise.gdshaderinc"

global uniform float UNPAUSED_TIME;

instance uniform float min_height = -4.0;
instance uniform float max_height = 4.0;
instance uniform float height_percent = 0.0;
instance uniform float wave_height = 2.0;

uniform sampler3D surface_noise;

uniform vec3 base_color : source_color;
uniform vec3 bright_color : source_color;

float wave(vec3 world_vertex)
{
	const float coord_scale = 0.1;

	vec3 sample_coords = vec3(world_vertex.xz * coord_scale, UNPAUSED_TIME);
	return texture(surface_noise, sample_coords).r;
}

void fragment()
{
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float height = mix(min_height, max_height, height_percent);
	height += wave(world_vertex) * wave_height;

	if (world_vertex.y > height)
		discard;
	
	vec3 molten_color = mix(base_color, bright_color, (sin(UNPAUSED_TIME * 8.0) + 1.0) / 2.0);
	ALBEDO = molten_color;
}