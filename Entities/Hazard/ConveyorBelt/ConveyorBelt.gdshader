shader_type spatial;

global uniform float UNPAUSED_TIME;

uniform sampler2D albedo : filter_linear_mipmap;
instance uniform vec2 scroll_speed = vec2(0.0, 0.0);

void fragment()
{
	ALBEDO = texture(albedo, UV + (scroll_speed * UNPAUSED_TIME)).xyz;
}