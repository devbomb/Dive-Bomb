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

        public static bool MoveAndSlideEx(
            this CharacterBody3D body,
            MoveAndSlideExCollisionHandler onCollision
        )
        {
            Vector3 prevPos = body.GlobalPosition;
            Vector3 prevVel = body.Velocity;

            if (!body.MoveAndSlide())
                return false;

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
    }
}