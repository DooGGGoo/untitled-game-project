using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Level : Node
{
	[Export] public Node3D[] PlayerSpawn;
	[Export(PropertyHint.File, "*.tscn")] public string PlayerScene = "res://entities/characters/player/player.tscn";
	[Export(PropertyHint.File, "*.tscn")] public string ReturnToScene = "res://maps/map_test_1.tscn";
	[Export(PropertyHint.File, "*.tscn")] public string ExplosionParticlesScene = "res://assets/particles/scenes/explosion_particles.tscn";
	
	public Player CurrentPlayer;

	[Signal] public delegate void PlayerSpawnedEventHandler(Player player);

	public override void _EnterTree()
	{
		Global.Instance.CurrentLevel = this;
		MissionSystem.Instance.ActiveMission?.CallDeferred(Mission.MethodName.Start);
	}

	public override void _Ready()
	{
		PlayerSpawned += (Player player) => GD.Print("Player spawned" + player.GlobalPosition);
		
		GD.Print(ReturnToScene);

		CurrentPlayer = SpawnPlayer();
	}

	public Player SpawnPlayer(int spawn = 0)
	{
		if (PlayerSpawn.Length == 0)
		{
			GD.PrintErr("No player spawn points found");
			return null;
		}

		if (PlayerScene == null)
		{
			GD.PrintErr("No player scene found");
			return null;
		}
		
		if (spawn == -1)
		{
			spawn = GD.RandRange(0, PlayerSpawn.Length);
		}
		
		PackedScene scene = ResourceLoader.Load<PackedScene>(PlayerScene);
		Player player = scene.Instantiate<Player>();
		AddChild(player);
		GD.Print(scene, player, PlayerSpawn);
		player.GlobalPosition = PlayerSpawn[spawn].GlobalPosition;

		EmitSignal(SignalName.PlayerSpawned, player);

		return player;
	}

	public void ReturnToBase()
	{
		GetTree().ChangeSceneToFile(ReturnToScene);
	}

	public void ChangeLevel(PackedScene level)
	{
		GetTree().ChangeSceneToPacked(level);
	}

	public MeshInstance3D DrawDebugSphere(Vector3 position, float radius = 0.05f)
	{
		StandardMaterial3D MaterialOverride = new()
        {
			AlbedoColor = Colors.DarkRed,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
		};

		MeshInstance3D debugMesh = new()
        {
			Mesh = new SphereMesh()
			{
				Rings = 4,
				RadialSegments = 8,
				Radius = radius,
				Height = radius * 2f,
			},
			MaterialOverride = MaterialOverride,
			TopLevel = true
		};

		AddChild(debugMesh);
		debugMesh.GlobalPosition = position;
		return debugMesh;
	}

	public void SpawnExplosion(Vector3 position, float explosionForce = 5f)
	{
		PhysicsDirectSpaceState3D spaceState = CurrentPlayer.GetWorld3D().DirectSpaceState;

		Transform3D transform = Transform3D.Identity;
		transform.Origin = position;

		PhysicsShapeQueryParameters3D query = new()
		{
			Shape = new SphereShape3D() { Radius = explosionForce * 0.8f },
			Transform = transform
		};
		Array<Dictionary> result = spaceState.IntersectShape(query);

		foreach (Dictionary hit in result)
		{
			if ((Node3D)hit["collider"] != null)
			{
				Node3D node = (Node3D)hit["collider"];
				
				if (node is Player player)
				{
					player.PlayerView.AddCameraShake(explosionForce / Mathf.Max(player.GlobalPosition.DistanceTo(position), 1f));
				}

				if (node is RigidBody3D body)
				{
					body.ApplyCentralImpulse(-body.GlobalPosition.DirectionTo(position) * explosionForce * explosionForce / body.Mass);
				}
			}
		}

		PackedScene explosionScene = ResourceLoader.Load<PackedScene>(ExplosionParticlesScene);
		ParticlePool explosionParticles = explosionScene.Instantiate<ParticlePool>();
		AddChild(explosionParticles);
		explosionParticles.GlobalPosition = position;
		explosionParticles.EmitParticles();
		
		// HACK
		SceneTreeTimer particleLifetimeTimer = GetTree().CreateTimer(8f);
		particleLifetimeTimer.Timeout += () => explosionParticles.QueueFree();
	}
}
