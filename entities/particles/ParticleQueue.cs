using Godot;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class ParticleQueue : Node3D
{
	[Export] private int InstancesCount = 1;
    private int next = 0;
    private List<GpuParticles3D> particleEmitters = new();
    private Global Global = Global.Instance;

    public override void _Ready()
    {
        if (GetChildCount() == 0)
        {
            GD.Print($"ParticleQueue {Name} (at {GetPath()}) has no children");
            return;
        }

        Node child = GetChild(0);
        if (child is GpuParticles3D particleEmitter)
        {
            particleEmitters.Add(particleEmitter);

			for (int i = 0; i < InstancesCount; i++)
			{
				GpuParticles3D duplicate = particleEmitter.Duplicate() as GpuParticles3D;
				AddChild(duplicate);
				particleEmitters.Add(duplicate);
			}
        }
	}

    public void EmitParticles()
    {
        if (!particleEmitters[next].Emitting)
        {
			particleEmitters[next++].Restart();
            next %= particleEmitters.Count;
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (GetChildCount() == 0)
        {
            return new string[] { "ParticleQueue has no children. Expected new GpuParticles3D" };
        }

        return base._GetConfigurationWarnings();
    }
}
