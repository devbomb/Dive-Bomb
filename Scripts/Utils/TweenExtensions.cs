using Godot;

namespace FastDragon
{
    public static class TweenExtensions
    {
        public static MethodTweener TweenAngleRad(
            this Tween tween,
            Node node,
            StringName property,
            float toRad,
            double duration
        )
        {
            float fromRad = (float)node.Get(property);
            System.Action<float> setter = (float t) =>
            {
                float angle = Mathf.LerpAngle(fromRad, toRad, t);
                node.Set(property, angle);
            };

            return tween.TweenMethod(
                Callable.From(setter),
                0f,
                1f,
                duration
            );
        }

        public static MethodTweener TweenAngleRadSinusoidal(
            this Tween tween,
            Node node,
            StringName property,
            float toRad,
            double duration
        )
        {
            float fromRad = (float)node.Get(property);
            System.Action<float> setter = (float t) =>
            {
                float angle = MathUtils.LerpAngleSinusoidal(fromRad, toRad, t);
                node.Set(property, angle);
            };

            return tween.TweenMethod(
                Callable.From(setter),
                0f,
                1f,
                duration
            );
        }
    }
}