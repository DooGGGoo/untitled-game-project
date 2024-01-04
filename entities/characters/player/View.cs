using System;
using Godot;

public partial class View : Node3D
{
    private Player player;
    [ExportGroup("Camera")]
    [Export] public Camera3D PlayerCamera;
    [Export] public float Sensitivity = 0.25f;
    [Export] public float CameraRotationLimit = 0.25f;
    [Export] private float cameraRollAngle = .5f;
    [Export] private float cameraRollSpeed = 3f;
    [Export] public bool enableHeadbob = true;
    [Export] private float headbobTimer = 10f;  // Speed
    [Export] private float headbobScale = 0.1f; // Magnitude
    [Export] private float cameraShakeReductionRate = 1f;
    [Export] private FastNoiseLite noise = new();
    [Export] private float noiseSpeed = 50f;
    [Export] private Vector3 maxShakeRotation;
    [Export] public bool LockCameraRotation;
    private float cameraShake;
    private float time;
    private Vector3 targetRotation, cameraTargetRotation, shakeInitialRotation, viewmodelInitialPosition, oldPosition;
    private Vector2 mouseInput;

    [ExportSubgroup("Viewmodel")]
    [Export] private Node3D viewmodel;
    [Export] private float bobCycle;
    [Export] private float bobUp;
    [Export] private float bobAmount;
    private Vector3 bobTimes, bobOffsets;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        PlayerCamera.MakeCurrent();

        viewmodelInitialPosition = viewmodel.Position;

        // We disabling that to fix "jumping" values at low framerate for example in Lerp function
        Input.UseAccumulatedInput = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && !player.GrabMouseLock)
        {
            InputEventMouseMotion mouseMotionEvent = @event as InputEventMouseMotion;
            targetRotation.X -= mouseMotionEvent.Relative.Y * Sensitivity;
            targetRotation.Y -= mouseMotionEvent.Relative.X * Sensitivity;

            mouseInput = Vector2.Zero;
            mouseInput = -mouseMotionEvent.Relative;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
        {
            player = Global.Instance().CurrentLevel.CurrentPlayer;
            return;
        }

        time += (float)delta;


        ProcessCameraMovement(delta);
		ProcessViewmodel();
    }

    #region Camera
    private void ProcessCameraMovement(double delta)
    {
        // Camera roll
        float sign, side, angle;

        side = player.Velocity.Dot(-GlobalBasis.X);
        sign = Mathf.Sign(side);
        side = Mathf.Abs(side);
        angle = cameraRollAngle;

        if (side < cameraRollSpeed)
            side = side * angle / cameraRollSpeed;
        else
            side = angle;

        float cameraZRotation = side * sign;

        // Camera bob
        if (enableHeadbob == true && player.IsOnFloor())
        {
            Vector2 offset;

            offset.Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(player.Velocity.Length()) * headbobScale / 10f;

            targetRotation.Y += offset.Y;
        }

        ProcessCameraShake(delta);

        // Apply all rotation changes
        targetRotation.X = Mathf.Clamp(targetRotation.X, -89.9f, 89.9f);

        // Just to be sure
        PlayerCamera.GlobalRotation = new Vector3(
            Mathf.Clamp(PlayerCamera.GlobalRotation.X, -89.9f, 89.9f), 
            PlayerCamera.GlobalRotation.Y, 
            PlayerCamera.GlobalRotation.Z
        );

        // Return to camera's initial rotation
        if (!LockCameraRotation)
        {
            cameraTargetRotation = cameraTargetRotation.Lerp(Vector3.Zero, 0.15f);
        }

        PlayerCamera.RotationDegrees = cameraTargetRotation;
        RotationDegrees = new Vector3(targetRotation.X, targetRotation.Y, cameraZRotation);
    }

    private void ProcessCameraShake(double delta)
    {
        cameraShake = Mathf.Max(cameraShake - (float)delta * cameraShakeReductionRate, 0f);

        cameraTargetRotation.X += shakeInitialRotation.X + maxShakeRotation.X * GetCameraShakeIntensity() * GetNoiseFromSeed(0);
        cameraTargetRotation.Y += shakeInitialRotation.Y + maxShakeRotation.Y * GetCameraShakeIntensity() * GetNoiseFromSeed(1);
        cameraTargetRotation.Z += shakeInitialRotation.Z + maxShakeRotation.Z * GetCameraShakeIntensity() * GetNoiseFromSeed(2);
    }

    #region Shake
    public void AddCameraShake(float amount)
    {
        cameraShake = Mathf.Clamp(cameraShake + amount, 0f, 1f);
    }

    private float GetCameraShakeIntensity()
    {
        return cameraShake * cameraShake;
    }

    private float GetNoiseFromSeed(int seed)
    {
        noise.Seed = seed;
        return noise.GetNoise1D(time * noiseSpeed);
    }

    #endregion

    public void ViewPunch(Vector3 angle, bool? useSmoothing = false)
    {
        if (useSmoothing == true)
        {
            cameraTargetRotation = cameraTargetRotation.Slerp(cameraTargetRotation + angle, 0.35f);
        }
        else
        {
            cameraTargetRotation += angle;
        }
    }

    // TODO
    #region Viewmodel
    private void ProcessViewmodel()
    {
        Vector3 offset = new()
        {
            //Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
            //X = Mathf.Cos(time * headbobTimer / 2f) * Mathf.Abs(Velocity.Length()) * headbobScale / 400f,
            Y = Mathf.Sin(time * headbobTimer) * Mathf.Abs(player.Velocity.Length()) * headbobScale / 400f,
            X = Mathf.Sin((time * headbobTimer + Mathf.Pi * 3f) / -2f) * Mathf.Abs(player.Velocity.Length()) * headbobScale / 400f,
        };

        viewmodel.Position += offset;
        viewmodel.Position = viewmodel.Position.Lerp(viewmodelInitialPosition, 0.125f);

        Vector3 viewmodelRotation = viewmodel.RotationDegrees;

        viewmodelRotation.X = Mathf.Lerp(viewmodel.Rotation.X, mouseInput.Y * .9f, 0.125f);
        viewmodelRotation.Y = Mathf.Lerp(viewmodel.Rotation.Y, mouseInput.X * .9f, 0.125f);

        viewmodel.RotationDegrees += viewmodelRotation;

        viewmodel.RotationDegrees = viewmodel.RotationDegrees.Clamp(new Vector3(-6f, -6f, -6f), new Vector3(6f, 6f, 6f));
        viewmodel.RotationDegrees = viewmodel.RotationDegrees.Lerp(Vector3.Zero, 0.125f);
    }

    #endregion

    #endregion
}