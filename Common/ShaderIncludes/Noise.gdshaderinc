#include "res://Common/ShaderIncludes/FloatHashes.gdshaderinc"

// taken from https://godotshaders.com/snippet/2d-noise/
float perlin12(vec2 uv)
{
	vec2 uv_index = floor(uv);
	vec2 uv_fract = fract(uv);

	vec2 blur = smoothstep(0.0, 1.0, uv_fract);

	vec2 right = vec2(1.0, 0.0);
	vec2 up = vec2(0.0, 1.0);
	vec2 zero = vec2(0.0, 0.0);
	vec2 one = vec2(1.0, 1.0);

	float a = mix
	(
		dot(hash22(uv_index + zero), uv_fract - zero),
		dot(hash22(uv_index + right), uv_fract - right),
		blur.x
	);
	
	float b = mix
	(
		dot(hash22(uv_index + up), uv_fract - up),
		dot(hash22(uv_index + one), uv_fract - one),
		blur.x
	);

	return mix(a, b, blur.y) + 0.5;
}

vec2 perlin22(vec2 uv)
{
	vec2 uv_index = floor(uv);
	vec2 uv_fract = fract(uv);

	vec2 blur = smoothstep(0.0, 1.0, uv_fract);

	vec2 right = vec2(1.0, 0.0);
	vec2 up = vec2(0.0, 1.0);
	vec2 zero = vec2(0.0, 0.0);
	vec2 one = vec2(1.0, 1.0);

	return mix(hash22(uv_index), hash22(uv_index + one), blur);
}

vec3 perlin33(vec3 point)
{
	vec3 point_index = floor(point);
	vec3 point_fract = fract(point);

	vec3 blur = smoothstep(0.0, 1.0, point_fract);

	vec2 right = vec2(1.0, 0.0);
	vec2 up = vec2(0.0, 1.0);
	vec2 zero = vec2(0.0, 0.0);
	vec3 one = vec3(1.0, 1.0, 1.0);

	return mix(hash33(point_index), hash33(point_index + one), blur);
}