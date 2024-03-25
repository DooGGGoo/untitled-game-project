using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UI3D : Node3D
{
	/*
	Okay, so I don't know what in the hell is happening here, and I probably will not ever understand,
	but I think it's working.
	
	HUGE Thanks to some random youtube video, that shows how to solve this problem and this guy's github
	( https://github.com/MT-ZD/Godot-3D-VR-UI/blob/main/UI3D.cs )

	I was literally trying to achieve the same result for like 4 hours and not got anywhere near this. 
	*/
	[Export] public SubViewport subViewport;
	[Export] public MeshInstance3D quad;
	[Export] public Area3D area;
	[Export] public CollisionShape3D shape;

	private Vector2 quadSize;
	private bool isMouseHeld = false;
	private bool isMouseInside = false;
	private Vector3? lastMousePosition3D;
	private Vector2 lastMousePosition2D = Vector2.Zero;

	public override void _Ready()
	{
		area.MouseEntered += MouseEnteredArea;

		CalculateSizes();
	}

	private void CalculateSizes()
	{
		QuadMesh mesh = (QuadMesh)quad.Mesh;

		StandardMaterial3D material = new()
        {
			AlbedoTexture = subViewport.GetTexture(),
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		mesh.Material = material;

		const float scaleModifier = 1024f;

		mesh.Size = new Vector2(subViewport.Size.X / scaleModifier, subViewport.Size.Y / scaleModifier);

		(shape.Shape as BoxShape3D).Size = new Vector3(mesh.Size.X, mesh.Size.Y, .1f);
	}

	public override void _ExitTree()
	{
		area.MouseEntered -= MouseEnteredArea;
	}

	private void MouseEnteredArea()
	{
		isMouseInside = true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsInsideTree()) return;

		bool isMouseEvent = false;

		if (@event is InputEventMouse || @event is InputEventMouseButton || @event is InputEventMouseMotion)
		{
			isMouseEvent = true;
		}

		if (isMouseEvent && (isMouseInside || isMouseHeld))
		{
			HandleMouse((InputEventMouse)@event);
		}
		else if (!isMouseEvent)
		{
			subViewport.PushInput(@event);
		}
	}

	private void HandleMouse(InputEventMouse @event)
	{
		if (!IsInsideTree()) return;

		isMouseInside = FindMouse(@event.GlobalPosition, out Vector3 position);

		HandleMouseInPosition(@event, position);
	}

	public void HandleSyntheticMouseMotion(Vector3 position)
	{
		InputEventMouseMotion ev = new();

		isMouseInside = true;

		HandleMouseInPosition(ev, position);
	}

	public void HandleSyntheticMouseClick(Vector3 position, bool pressed)
	{
		InputEventMouseButton ev = new() { ButtonIndex = MouseButton.Left, Pressed = pressed };

		isMouseInside = true;

		HandleMouseInPosition(ev, position);
	}

	private void HandleMouseInPosition(InputEventMouse @event, Vector3 position)
	{
		quadSize = (quad.Mesh as QuadMesh).Size;

		if (@event is InputEventMouseButton)
		{
			isMouseHeld = @event.IsPressed();
		}

		Vector3 mousePosition3D;

		if (isMouseInside)
		{
			mousePosition3D = area.GlobalTransform.AffineInverse() * position;
			lastMousePosition3D = mousePosition3D;
		}
		else
		{
			if (lastMousePosition3D != null)
			{
				mousePosition3D = (Vector3)lastMousePosition3D;
			}
			else
			{
				mousePosition3D = Vector3.Zero;
			}
		}

		Vector2 mousePosition2D = new(mousePosition3D.X, -mousePosition3D.Y);

		mousePosition2D.X += quadSize.X / 2;
		mousePosition2D.Y += quadSize.Y / 2;

		mousePosition2D.X /= quadSize.X;
		mousePosition2D.Y /= quadSize.Y;

		mousePosition2D.X *= subViewport.Size.X;
		mousePosition2D.Y *= subViewport.Size.Y;

		@event.Position = mousePosition2D;
		@event.GlobalPosition = mousePosition2D;

		if (@event is InputEventMouseMotion)
		{
			(@event as InputEventMouseMotion).Relative = mousePosition2D - lastMousePosition2D;
		}

		lastMousePosition2D = mousePosition2D;

		if(IsInsideTree() && subViewport.IsInsideTree())
			subViewport.PushInput(@event);
	}

	private bool FindMouse(Vector2 globalPosition, out Vector3 position)
	{
		Camera3D camera = GetViewport().GetCamera3D();

		Vector3 from = camera.ProjectRayOrigin(globalPosition);
		float dist = FindFurtherDistanceTo(camera.Transform.Origin);
		Vector3 to = from + camera.ProjectRayNormal(globalPosition) * dist;

		PhysicsRayQueryParameters3D parameters = new() { From = from, To = to, CollideWithAreas = true, CollisionMask = area.CollisionLayer, CollideWithBodies = false };

		Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(parameters);

		position = Vector3.Zero;

		if (result.Count > 0)
		{
			position = (Vector3)result["position"];

			return true;
		}
		else
		{
			return false;
		}
	}

	private float FindFurtherDistanceTo(Vector3 origin)
	{
		Vector3[] edges = [
			area.ToGlobal(new Vector3(quadSize.X / 2, quadSize.Y / 2, 0)),
			area.ToGlobal(new Vector3(quadSize.X / 2, -quadSize.Y / 2, 0)),
			area.ToGlobal(new Vector3(-quadSize.X / 2, quadSize.Y / 2, 0)),
			area.ToGlobal(new Vector3(-quadSize.X / 2, -quadSize.Y / 2, 0)),
		];

		float farDistance = 0;

		foreach (Vector3 edge in edges)
		{
			float tempDistance = origin.DistanceTo(edge);

			if (tempDistance > farDistance)
			{
				farDistance = tempDistance;
			}
		}

		return farDistance;
	}
}