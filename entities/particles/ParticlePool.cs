using Godot;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class ParticlePool : Node3D
{
	[Export] private bool useRandom;
	private List<ParticleQueue> particleQueues = [];
	private RandomNumberGenerator random = new();
	

	[Signal] public delegate void FinishedEmittingParticlesEventHandler();

	public override void _Ready()
	{
		foreach (var child in GetChildren())
		{
			if (child is ParticleQueue particleQueue)
			{
				particleQueues.Add(particleQueue);
			}
		}
	}

	public void EmitParticles()
	{
		if (useRandom)
		{
			int index = random.RandiRange(0, particleQueues.Count - 1);
			particleQueues[index].EmitParticles();
			return;
		}

		foreach (var particleQueue in particleQueues)
		{
			particleQueue.EmitParticles();
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		int numberOfPQChildren = 0;
		foreach (var child in GetChildren())
		{
			if (child is ParticleQueue)
			{
				numberOfPQChildren++;
			}
		}

		if (numberOfPQChildren < 2)
		{
			return ["ParticlePool needs at least 2 ParticleQueue children"];
		}

		return base._GetConfigurationWarnings();
	}
}
