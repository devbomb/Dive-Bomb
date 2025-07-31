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