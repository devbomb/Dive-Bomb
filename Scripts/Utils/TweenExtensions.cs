using Godot;

namespace FastDragon
{
    public static class TweenExtensions
    {
        public static MethodTweener TweenRotRadSinusoidal(
            this Tween tween,
            Node node,
            StringName property,
            Vector3 toRad,
            double duration
        )
        {
            var fromRad = (Vector3)node.Get(property);
            System.Action<float> setter = (float t) =>
            {
                Vector3 rotRad = fromRad.LerpEulerRadSinusoidal(toRad, t);
                node.Set(property, rotRad);
            };

            return tween.TweenMethod(
                Callable.From(setter),
                0f,
                1f,
                duration
            );
        }

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

        public static MethodTweener TweenVector3Bezier(
            this Tween tween,
            Node node,
            StringName property,
            Vector3 to,
            Vector3 control,
            double duration
        )
        {
            Vector3 from = (Vector3)node.Get(property);
            System.Action<float> setter = (float t) =>
            {
                Vector3 pos = from.LerpBezier(to, control, t);
                node.Set(property, pos);
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