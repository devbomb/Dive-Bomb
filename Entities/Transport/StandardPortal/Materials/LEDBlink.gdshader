shader_type spatial;
render_mode vertex_lighting;

#include "res://Common/ShaderIncludes/FloatHashes.gdshaderinc"

uniform vec3 on_color : source_color;
uniform vec3 off_color : source_color;
uniform float blink_period = 1.0;
uniform float blink_time_offset = 0.0;

void fragment()
{
	float seed = hash13(NODE_POSITION_WORLD);
	ALBEDO = fract((TIME / blink_period) + blink_time_offset + seed) > 0.5
		? on_color
		: off_color;
}
