// taken from https://godotshaders.com/snippet/2d-noise/, but the actual
// origin of this algorithm seems to have been lost to the sands of time.
vec2 hash22(vec2 uv)
{
	uv = vec2
	(
		dot(uv, vec2(127.1,311.7)),
		dot(uv, vec2(269.5,183.3))
	);
	return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

// not taken from anywhere; I just followed the pattern to make hash22 3D.
// There is no logic to choosing those magic numbers, other than "big number
// that's a few decimals away from a prime number".
vec3 hash33(vec3 point)
{
	point = vec3
	(
		dot(point, vec3(127.1, 311.7, 137.3)),
		dot(point, vec3(269.5, 183.3, 420.69)),
		dot(point, vec3(223.96, 349.9, 867.5309))
	);
	return -1.0 + 2.0 * fract(sin(point) * 43758.5453123);
}

// Taken and modified from https://godotshaders.com/snippet/random-value/ to
// extend it to 3D
float hash13(vec3 point)
{
	float a = dot(point.xyz, vec3(12.9898, 78.233, 867.5309));
	return fract(sin(a) * 43758.5453123);
}