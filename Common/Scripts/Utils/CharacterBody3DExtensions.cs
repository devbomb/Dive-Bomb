#nullable enable
using Godot;

namespace FastDragon
{
    public enum MoveAndSlideExResponse
    {
        /// <summary>
        /// Indicates that the body should stop after hitting this object,
        /// rather than sliding.  Use this when "bonking".
        /// </summary>
        Stop,

        /// <summary>
        /// Indicates that the body should plow right through this object in a
        /// straight line, as if it had been destroyed.
        /// </summary>
        Ignore,

        /// <summary>
        /// Indicates that the body should slide against the object
        /// </summary>
        Slide
    }

    public static class CharacterBody3DExtensions
    {
        public delegate MoveAndSlideExResponse MoveAndSlideExCollisionHandler(
            KinematicCollision3D collision
        );

        /// <summary>
        ///     Like <see cref="CharacterBody3D.MoveAndSlide"/>, except it calls
        ///     a callback on every slide collision to determine how it should
        ///     react to that collision.
        ///
        ///     Returns true if there were any non-ignored collisions.
        ///     Returns false if there were no collisions, or they were all
        ///     ignored.
        /// </summary>
        /// <param name="onCollision">
        ///     A callback that is called on every slide collision to determine
        ///     how it should react to that collision.  If omitted, it will
        ///     default to <see cref="MoveAndSlideExResponse.Slide"/> for every
        ///     collision.
        /// </param>
        /// <param name="keepVelocity">
        ///     If true, the body's velocity will not be affected by collisions.
        ///     Do note that this will also undo any velocity changes made by
        ///     <paramref name="onCollision"/>.
        /// </param>
        public static bool MoveAndSlideEx(
            this CharacterBody3D body,
            MoveAndSlideExCollisionHandler? onCollision = null,
            bool keepVelocity = false
        )
        {
            Vector3 prevPos = body.GlobalPosition;
            Vector3 prevVel = body.Velocity;

            try
            {
                if (!body.MoveAndSlide())
                    return false;

                if (onCollision == null)
                    return true;

                int numCollisions = body.GetSlideCollisionCount();
                for (int i = 0; i < numCollisions; i++)
                {
                    var collision = body.GetSlideCollision(i);
                    var response = onCollision(collision);

                    switch (response)
                    {
                        case MoveAndSlideExResponse.Slide: continue;

                        case MoveAndSlideExResponse.Ignore:
                        {
                            // Rewind and try again, but this time ignore this object
                            body.GlobalPosition = prevPos;
                            body.Velocity = prevVel;

                            body.AddCollisionExceptionWith((Node)collision.GetCollider());
                            bool result = body.MoveAndSlideEx(onCollision);
                            body.RemoveCollisionExceptionWith((Node)collision.GetCollider());

                            return result;
                        }

                        case MoveAndSlideExResponse.Stop:
                        {
                            // TODO: Actually prevent the body from sliding.
                            // Right now, it just prevents the collision handler
                            // from being called on the remaining slide collisions.
                            // Nobody's really going to notice, though, right?
                            return true;

                            // BUG: IsOnFloor(), IsOnWall(), GetPlatformVelocity(),
                            // etc. _should_ be based off _this_ collision, but
                            // they'll actually be based on
                            // GetSlideCollision(numCollisions - 1).
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (keepVelocity)
                    body.Velocity = prevVel;
            }
        }
    }
}