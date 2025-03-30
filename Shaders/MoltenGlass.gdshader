shader_type spatial;
render_mode
	diffuse_lambert,
	specular_toon,
	shadows_disabled,
	ambient_light_disabled,
	world_vertex_coords;

uniform float amplitude = 0.05;
uniform float frequency = 1.5;

vec3 displaceVertex(vec3 vertex, mat4 viewMatrix)
{
	vec3 viewPos = (vec4(vertex, 0.0) * viewMatrix).xyz;
	float timeOffset = length(vec2(vertex.x, vertex.z)) + (2.0 * vertex.y);
	
	float t = TIME + timeOffset;
	float wave = 0.5 * (1.0 - cos(frequency * PI * t));
	float height = amplitude * wave;
	return vertex + vec3(0, 1, 0) * height;
}

vec3 recalculateNormal(vec3 vertex, vec3 tangent, vec3 bitangent, mat4 viewMatrix)
{
	vertex = displaceVertex(vertex, viewMatrix);
	vec3 neighbor1 = displaceVertex(vertex + (1.0 * tangent), viewMatrix);
	vec3 neighbor2 = displaceVertex(vertex + (1.0 * bitangent), viewMatrix);
	
	vec3 newTangent = neighbor1 - vertex;
	vec3 newBitangent = neighbor2 - vertex;
	return normalize(cross(newTangent, newBitangent));
}

void vertex() 
{
	NORMAL = recalculateNormal(VERTEX, TANGENT, BINORMAL, VIEW_MATRIX);
	VERTEX = displaceVertex(VERTEX, VIEW_MATRIX);
}

void fragment() 
{
	ALBEDO = vec3(1.0, 0.76, 0);
	METALLIC = 0.0;
	SPECULAR = 0.54;
	ROUGHNESS = 0.78;
	EMISSION = vec3(1.0, 0, 0);
	RIM = 0.2;
	RIM_TINT = 0.45;
	CLEARCOAT = 1.0;
	CLEARCOAT_ROUGHNESS = 0.27;
}