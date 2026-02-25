shader_type spatial;
render_mode cull_disabled, unshaded;
#include "res://Common/ShaderIncludes/Noise.gdshaderinc"

global uniform float UNPAUSED_TIME;

instance uniform float container_height = 8.0;
instance uniform float fill_percent = 0.0;
instance uniform float wave_height = 2.0;
instance uniform float radius = 1.0;

uniform vec3 base_color : source_color;
uniform vec3 bright_color : source_color;

float standing_wave(vec3 world_vertex, vec3 world_node_origin, float frequency, float nodes)
{
	vec3 flat_vertex = vec3(world_vertex.x, 0.0, world_vertex.z);
	vec3 flat_origin = vec3(world_node_origin.x, 0.0, world_node_origin.z);
	float r = distance(flat_vertex, flat_origin) / (radius);
	float t = UNPAUSED_TIME * 2.0 * PI * frequency;
	return cos((1.0 - (r * nodes + 1.0)) + t) * (1.0 - r);
}

void fragment()
{
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float min_h = NODE_POSITION_WORLD.y - (container_height / 2.0);
	float max_h = NODE_POSITION_WORLD.y + (container_height / 2.0);
	float height = mix(min_h, max_h, fill_percent);
	height += standing_wave(world_vertex, NODE_POSITION_WORLD, 2.0, 4.0) * wave_height * container_height;
	height += standing_wave(world_vertex, NODE_POSITION_WORLD, 5.0, 10.0) * wave_height * container_height;

	if (world_vertex.y > height)
		discard;

	vec3 molten_color = mix(base_color, bright_color, (sin(UNPAUSED_TIME * 8.0) + 1.0) / 2.0);
	ALBEDO = molten_color;
}