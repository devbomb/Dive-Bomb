shader_type spatial;
render_mode
	diffuse_lambert,
	specular_toon,
	shadows_disabled,
	ambient_light_disabled,
	world_vertex_coords;

uniform float minAmplitude = 0.09;
uniform float minAmplitudeDepth = 1.0;

uniform float maxAmplitude = 0.15;
uniform float maxAmplitudeDepth = 20.0;

uniform float frequency = 1.5;

uniform sampler3D albedoTexture;

uniform sampler3D warbleDirectionMapX;
uniform sampler3D warbleDirectionMapY;
uniform sampler3D warbleDirectionMapZ;

float inverseLerp(float value, float start, float end)
{
	float shiftedValue = value - start;
	float shiftedEnd = end - start;
	return shiftedValue / shiftedEnd;
}

vec3 warbleDirection(vec3 worldPos)
{
	float x = length(texture(warbleDirectionMapX, worldPos)) - 0.5;
	float y = length(texture(warbleDirectionMapY, worldPos)) - 0.5;
	float z = length(texture(warbleDirectionMapZ, worldPos)) - 0.5;

	return normalize(vec3(x, y, z));
}

vec3 displaceVertex(vec3 vertex, mat4 viewMatrix, float depth)
{
	vec3 viewPos = (vec4(vertex, 0.0) * viewMatrix).xyz;
	float timeOffset = length(vec2(vertex.x, vertex.z)) + (2.0 * vertex.y);

	depth = clamp(depth, minAmplitudeDepth, maxAmplitudeDepth);
	float depthPercent = inverseLerp(depth, minAmplitudeDepth, maxAmplitudeDepth);
	float amplitude = mix(minAmplitude, maxAmplitude, depthPercent);

	float t = TIME + timeOffset;
	float wave = 0.5 * (1.0 - cos(frequency * PI * t));
	float height = amplitude * wave;
	return vertex + warbleDirection(vertex) * height;
}

void vertex()
{
	float depth = distance(VERTEX, CAMERA_POSITION_WORLD);
	VERTEX = displaceVertex(VERTEX, VIEW_MATRIX, depth);
}

void fragment()
{
	vec3 globalPos = ((INV_VIEW_MATRIX * vec4(VERTEX, 1)).xyz);

	// Pull the albedo from a scrolling noise texture
	vec3 scrolledGlobalPos = (globalPos * 0.025) + vec3(0, TIME * 0.1, TIME * 0.05);
	ALBEDO = texture(albedoTexture, scrolledGlobalPos).xyz;

	// Add some noise to the normal
	vec3 warble = warbleDirection(scrolledGlobalPos);
	NORMAL = (0.5 * NORMAL) + (0.5 * warble);

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