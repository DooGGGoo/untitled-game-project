using Godot;

public partial class GroundCharacter : CharacterBody3D
{
    [ExportGroup("Movement")]
    [Export] protected float MovementAcceleration = 6f;
    [Export] protected float MovementFriction = 8f;
    [Export] public float WalkSpeed = 4f;
    [Export] public float SprintSpeed = 5.8f;
    [Export] public float CrouchSpeed = 2.3f;
    [Export] public float JumpVelocity = 1.2f;
    [Export] protected float gravity = 16.8f;
    [Export] protected float StepHeight = 0.45f;
    protected float currentSpeed;
    protected Vector3 wishDir;

    bool StartedProcessOnFloor = false;
    public override void _PhysicsProcess(double delta)
    {
        currentSpeed = WalkSpeed;
        ProcessMovement(delta);

        PushRigidBodies(delta);
        StartedProcessOnFloor = IsOnFloor();
        MoveAndClimbStairs((float)delta, false);
    }

    #region Movement

    protected virtual void ProcessMovement(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        if (wishDir != Vector3.Zero)
        {
            float movementAcceleration = IsOnFloor() ? MovementAcceleration : 1f;
            velocity.X = Mathf.Lerp(velocity.X, wishDir.X * currentSpeed, movementAcceleration * (float)delta);
            velocity.Z = Mathf.Lerp(velocity.Z, wishDir.Z * currentSpeed, movementAcceleration * (float)delta);
        }
        else
        {
            float movementFriction = IsOnFloor() ? MovementFriction : 1f;
            velocity.X = Mathf.Lerp(velocity.X, 0f, movementFriction * (float)delta);
            velocity.Z = Mathf.Lerp(velocity.Z, 0f, movementFriction * (float)delta);
        }

        Velocity = velocity;
    }

    protected virtual void Jump()
    {
        if (!IsOnFloor()) return;
        float jMult = Mathf.Sqrt(2f * gravity * JumpVelocity);
        Vector3 vel = Velocity;
        vel.Y += jMult;
        Velocity = vel;
    }

    protected virtual void PushRigidBodies(double delta)
    {
        KinematicCollision3D collision = MoveAndCollide(Velocity * (float)delta, true);

        if (collision != null && collision.GetCollider() is RigidBody3D rigidbody)
        {
            Vector3 pushVector = collision.GetNormal() * (Velocity.Length() * 2f / rigidbody.Mass);
            rigidbody.ApplyImpulse(-pushVector, collision.GetPosition() - rigidbody.GlobalPosition);
        }
    }

    #region Step handling code, Do not Touch, Breathe or Stare at
    // Source: (https://github.com/wareya/GodotStairTester/blob/main/player/SimplePlayer.gd), Huge thanks! :3

    bool foundStairs = false;
    Vector3 wallTestTravel = Vector3.Zero;
    Vector3 wallRemainder = Vector3.Zero;
    Vector3 ceilingPosition = Vector3.Zero;
    float ceilingTravelDistance = 0f;
    KinematicCollision3D wallCollision = null;
    KinematicCollision3D floorCollision = null;

    protected virtual bool MoveAndClimbStairs(float delta, bool allowStairSnapping = true)
    {
        Vector3 startPosition = GlobalPosition;
        Vector3 startVelocity = Velocity;

        foundStairs = false;
        wallTestTravel = Vector3.Zero;
        wallRemainder = Vector3.Zero;
        ceilingPosition = Vector3.Zero;
        ceilingTravelDistance = 0f;
        wallCollision = null;
        floorCollision = null;

        // do MoveAndSlide and check if we hit a wall
        MoveAndSlide();
        Vector3 slideVelocity = Velocity;
        Vector3 slidePosition = GlobalPosition;
        bool hitWall = false;
        float floorNormal = Mathf.Cos(FloorMaxAngle);
        int maxSlide = GetSlideCollisionCount() - 1;
        Vector3 accumulatedPosition = startPosition;
        foreach (int slide in GD.Range(maxSlide + 1))
        {
            KinematicCollision3D collision = GetSlideCollision(slide);
            float y = collision.GetNormal().Y;
            if (y < floorNormal && y > -floorNormal)
            {
                hitWall = true;
            }
            accumulatedPosition += collision.GetTravel();
        }
        Vector3 slideSnapOffset = accumulatedPosition - GlobalPosition;

        // if we hit a wall, check for simple stairs; three steps
        if (hitWall && (startVelocity.X != 0f || startVelocity.Z != 0f))
        {
            GlobalPosition = startPosition;
            Velocity = startVelocity;

            // step 1: upwards trace
            float upHeight = StepHeight;
            //changed to true
            KinematicCollision3D ceilingCollision = MoveAndCollide(upHeight * Vector3.Up);
            ceilingTravelDistance = StepHeight;
            if (ceilingCollision != null)
                ceilingTravelDistance = Mathf.Abs(ceilingCollision.GetTravel().Y);
            ceilingPosition = GlobalPosition;

            // step 2: "check if there's a wall" trace
            wallTestTravel = Velocity * delta;
            object[] info = MoveAndCollideNTimes(Velocity, delta, 2);
            Velocity = (Vector3)info[0];
            wallRemainder = (Vector3)info[1];
            wallCollision = (KinematicCollision3D)info[2];

            // step 3: downwards trace
            floorCollision = MoveAndCollide(Vector3.Down * (ceilingTravelDistance + (StartedProcessOnFloor ? StepHeight : 0f)));
            if (floorCollision != null)
            {
                if (floorCollision.GetNormal(0).Y > floorNormal)
                    foundStairs = true;
            }
        }
        // (this section is more complex than it needs to be, because of MoveAndSlide taking velocity and delta for granted)
        // if we found stairs, climb up them
        if (foundStairs)
        {
            Vector3 vel = Velocity;
            bool StairsCauseFloorSnap = true;
            if (allowStairSnapping && StairsCauseFloorSnap == true)
                vel.Y = 0f;
            Velocity = vel;
            Vector3 oldvel = Velocity;
            Velocity = wallRemainder / delta;
            MoveAndSlide();
            Velocity = oldvel;
        }
        // no stairs, do "normal" non-stairs movement
        else
        {
            GlobalPosition = slidePosition;
            Velocity = slideVelocity;
        }
        return foundStairs;
    }

    protected virtual object[] MoveAndCollideNTimes(Vector3 vector, float delta, int slideCount, bool skipRejectIfCeiling = true)
    {
        KinematicCollision3D collision = null;
        Vector3 remainder = vector;
        Vector3 adjustedVector = vector * delta;
        float floorNormal = Mathf.Cos(FloorMaxAngle);
        foreach (int i in GD.Range(slideCount))
        {
            KinematicCollision3D newCollision = MoveAndCollide(adjustedVector);
            if (newCollision != null)
            {
                collision = newCollision;
                remainder = collision.GetRemainder();
                adjustedVector = remainder;
                if (!skipRejectIfCeiling || collision.GetNormal().Y >= -floorNormal)
                {
                    adjustedVector = adjustedVector.Slide(collision.GetNormal());
                    vector = vector.Slide(collision.GetNormal());
                }
            }
            else
            {
                remainder = Vector3.Zero;
                break;
            }
        }
        return [vector, remainder, collision];
    }
    #endregion

    #endregion
}