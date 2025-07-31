#include "res://Common/ShaderIncludes/HashWithoutSine.gdshaderinc"

struct VertexWarbleSettings
{
	float frequency;
	
	float min_amplitude;
	float max_amplitude;
	
	float min_amplitude_depth;
	float max_amplitude_depth;
};

VertexWarbleSettings default_vertex_warble_settings()
{
	return VertexWarbleSettings
	(
		1.5,	// frequency
		0.09,	// min_amplitude
		0.15,	// max_amplitude
		1.0,	// min_amplitude_depth
		20.0	// max_amplitude_depth
	);
}

float inverse_lerp(float value, float start, float end)
{
	float shiftedValue = value - start;
	float shiftedEnd = end - start;
	return shiftedValue / shiftedEnd;
}

vec3 warble_direction(vec3 worldPos)
{
	return normalize(hash33(worldPos));
}

vec3 warble_vertex(VertexWarbleSettings settings, vec3 vertex, mat4 viewMatrix, float depth)
{
	vec3 viewPos = (vec4(vertex, 0.0) * viewMatrix).xyz;
	float timeOffset = length(vec2(vertex.x, vertex.z)) + (2.0 * vertex.y);

	depth = clamp(depth, settings.min_amplitude_depth, settings.max_amplitude_depth);
	float depthPercent = inverse_lerp(depth, settings.min_amplitude_depth, settings.max_amplitude_depth);
	float amplitude = mix(settings.min_amplitude, settings.max_amplitude, depthPercent);

	float t = TIME + timeOffset;
	float wave = 0.5 * (1.0 - cos(settings.frequency * PI * t));
	float height = amplitude * wave;
	return vertex + warble_direction(vertex) * height;
}
