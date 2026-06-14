shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;
global uniform float UNPAUSED_TIME;

uniform vec4 albedo : source_color;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;
uniform float specular : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float metallic : hint_range(0.0, 1.0, 0.01) = 0.75;

uniform float rim : hint_range(0.0, 1.0, 0.01) = 1.0;
uniform float rim_tint : hint_range(0.0, 1.0, 0.01) = 0.5;

mat3 rotate_x(float theta)
{
	float cosa = cos(theta);
	float sina = sin(theta);
	
	return mat3(
	   vec3(1.0, 0.0, 0.0),
	   vec3(0.0, cosa, -sina),
	   vec3(0.0, sina, cosa)
	);
}

mat3 rotate_y(float theta)
{
	float cosa = cos(theta);
	float sina = sin(theta);
	
	return mat3(
	   vec3(cosa, 0.0, sina),
	   vec3(0.0, 1.0, 0.0),
	   vec3(-sina, 0.0, cosa)
	);
}

void vertex() 
{
	mat3 rotation_matrix = rotate_y(UNPAUSED_TIME * radians(120)) * rotate_x(radians(45));
	VERTEX = rotation_matrix * VERTEX;
	NORMAL = rotation_matrix * NORMAL;
}

void fragment() 
{
	ALBEDO = albedo.rgb;
	METALLIC = metallic;
	SPECULAR = specular;
	ROUGHNESS = roughness;

	// Rim: Enabled
	RIM = rim;
	RIM_TINT = rim_tint;
}
