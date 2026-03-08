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
                        // Stop processing further slide collisions.
                        // onCollision() should have moved us to wherever it
                        // thinks we should be.
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