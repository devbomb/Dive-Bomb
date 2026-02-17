shader_type spatial;
render_mode cull_disabled, shadows_disabled;
#include "res://Common/ShaderIncludes/Noise.gdshaderinc"

uniform float min_height = -4;
uniform float max_height = 4;
uniform float height_percent = 0.0;
uniform float wave_height = 2.0;
uniform sampler3D surface_noise;

float wave(vec3 world_vertex)
{
	const float coord_scale = 0.1;
	const float bubble_time_scale = 0.5;
	const float flow_speed = 2.0;
	const float flow_period = 2.0;

	vec3 sample_coords = vec3(world_vertex.xz * coord_scale, TIME * bubble_time_scale);
	return length(texture(surface_noise, sample_coords));
}

void fragment()
{
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float waviness = 1.0 - pow(2.0 * height_percent - 1.0, 4.0);
	float height = mix(min_height, max_height, height_percent);
	height += wave(world_vertex) * wave_height * waviness;

	if (world_vertex.y > height)
		discard;
}