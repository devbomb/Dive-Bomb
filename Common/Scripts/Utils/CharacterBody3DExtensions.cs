using Godot;

namespace FastDragon
{
    public enum MoveAndSlideExResponse
    {
        Stop,
        Ignore,
        Slide
    }

    public static class CharacterBody3DExtensions
    {
        public delegate MoveAndSlideExResponse MoveAndSlideExCollisionHandler(
            KinematicCollision3D collision
        );

        public static bool MoveAndSlideEx(
            this CharacterBody3D body,
            MoveAndSlideExCollisionHandler onCollision,
            double delta
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
                        bool result = body.MoveAndSlideEx(onCollision, delta);
                        body.RemoveCollisionExceptionWith((Node)collision.GetCollider());

                        return result;
                    }

                    case MoveAndSlideExResponse.Stop:
                    {
                        // Stop processing further slide collisions.
                        // Rewind back to the start and move to the point of
                        // contact.
                        body.GlobalPosition = prevPos;
                        body.MoveAndCollide(prevVel * (float)delta);
                        return true;

                        // BUG: This always moves you to the FIRST slide's
                        // collision point, not the CURRENT one.

                        // BUG: If the onCollision handler moved the body, then
                        // we just undid that move.

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