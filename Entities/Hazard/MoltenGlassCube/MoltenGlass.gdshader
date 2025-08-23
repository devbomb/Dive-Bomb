shader_type spatial;
render_mode
	diffuse_lambert,
	specular_toon,
	shadows_disabled,
	ambient_light_disabled,
	world_vertex_coords;

#include "res://Common/ShaderIncludes/VertexWarble.gdshaderinc"

uniform float minAmplitude = 0.09;
uniform float minAmplitudeDepth = 1.0;

uniform float maxAmplitude = 0.15;
uniform float maxAmplitudeDepth = 20.0;

uniform float frequency = 1.5;

uniform sampler3D albedoTexture;

void vertex()
{
	float depth = distance(VERTEX, CAMERA_POSITION_WORLD);
	VertexWarbleSettings settings = VertexWarbleSettings(
		frequency,
		minAmplitude,
		maxAmplitude,
		minAmplitudeDepth,
		maxAmplitudeDepth
	);
	VERTEX = warble_vertex(settings, VERTEX, VIEW_MATRIX, depth);
}

void fragment()
{
	vec3 globalPos = ((INV_VIEW_MATRIX * vec4(VERTEX, 1)).xyz);

	// Pull the albedo from a scrolling noise texture
	vec3 scrolledGlobalPos = (globalPos * 0.025) + vec3(0, TIME * 0.1, TIME * 0.05);
	ALBEDO = texture(albedoTexture, scrolledGlobalPos).xyz;

	// Make the edges redder
	float redMix = 1.0 - abs(dot(NORMAL, vec3(0, 0, 1)));
	vec3 rimColor = vec3(1, 0, 0);
	ALBEDO = mix(ALBEDO, rimColor, abs(redMix));

	METALLIC = 0.0;
	SPECULAR = 0.54;
	ROUGHNESS = 0.78;
	EMISSION = vec3(1.0, 0, 0);
	RIM = 0.2;
	RIM_TINT = 0.45;
}