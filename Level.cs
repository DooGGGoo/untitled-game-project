using Godot;
using System;

public partial class Level : Node
{
	[Export] public Node3D[] PlayerSpawn;
	[Export] public PackedScene PlayerScene;

	[Signal] public delegate void PlayerSpawnedEventHandler(Player player);

	public override void _Ready()
	{
		SpawnPlayer();

		PlayerSpawned += (Player player) => GD.Print("Player spawned" + player.GlobalPosition);
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
		Player player = PlayerScene.InstantiateOrNull<Player>();
		AddChild(player);
		player.GlobalPosition = PlayerSpawn[spawn].GlobalPosition;

		EmitSignal(SignalName.PlayerSpawned, player);

		return player;
	}
}
