using Godot;

namespace FastDragon
{
    public static class AccelMath
    {
        /// <summary>
        /// Returns the speed that would be required to send an object a given
        /// distance with a given amount of friction, using a timestep of 1/60.
        ///
        /// The answer is only an estimate, because some combinations of
        /// distances/frictions are impossible to hit exactly with a fixed
        /// timestep.
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="friction"></param>
        /// <returns></returns>
        public static float SpeedNeededForDistance(float distance, float friction)
        {
            float tolerance = 0.1f;
            float speed = 1;
            float predictedDist = CalculateDistance(speed);
            float error = float.MaxValue;

            for (int i = 0; i < 10_000 && error > tolerance; i++)
            {
                float mult = predictedDist > distance
                    ? 0.75f
                    : 2f;

                speed *= mult;
                predictedDist = CalculateDistance(speed);
                error = Mathf.Abs(predictedDist - distance);
            }

            if (error >= tolerance)
            {
                GD.PrintErr("Took too many iterations");
            }

            return speed;

            float CalculateDistance(float s)
            {
                float delta = 1f / 60;
                float v = s;
                float d = 0;

                while (v > 0)
                {
                    v = Mathf.MoveToward(v, 0, friction * delta);
                    d += v * delta;
                }

                return d;
            }
        }

        public static float FrictionNeededForDistance(float distance, float initialSpeed)
        {
            float tolerance = 0.1f;
            float friction = 1;
            float predictedDist = CalculateDistance(friction);
            float error = float.MaxValue;

            for (int i = 0; i < 10_000 && error > tolerance; i++)
            {
                float mult = predictedDist < distance
                    ? 0.75f
                    : 2f;

                friction *= mult;
                predictedDist = CalculateDistance(friction);
                error = Mathf.Abs(predictedDist - distance);
            }

            if (error >= tolerance)
            {
                GD.PrintErr("Took too many iterations");
            }

            return friction;

            float CalculateDistance(float f)
            {
                float delta = 1f / 60;
                float v = initialSpeed;
                float d = 0;

                while (v > 0)
                {
                    v = Mathf.MoveToward(v, 0, f * delta);
                    d += v * delta;
                }

                return d;
            }
        }

        public static (float speed, float friction) SpeedAndFrictionNeededForDistanceAndTime(
            float distance,
            float time
        )
        {
            float friction = (2 * distance) / (time * time);
            float speed = SpeedNeededForDistance(distance, friction);

            return (speed, friction);
        }

        public static float DistanceTraveledWithFriction(float initialSpeed, float friction)
        {
            if (friction <= 0 || Mathf.IsZeroApprox(friction))
                throw new System.Exception("Friction must be greater than 0");

            float delta = 1f / 60;
            float speed = initialSpeed;
            float distance = 0;

            while (speed > 0)
            {
                speed = Mathf.MoveToward(speed, 0, friction * delta);
                distance += speed * delta;
            }

            return distance;
        }
    }
}
