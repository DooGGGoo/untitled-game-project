using Godot;
using System;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class SoundPool : Node3D
{
    private List<SoundQueue> sounds = new();
    private RandomNumberGenerator random = new();

    public override void _Ready()
    {
        foreach (var child in GetChildren())
        {
            if (child is SoundQueue sound)
            {
                sounds.Add(sound);
            }
        }
    }

    public virtual void PlayRandomSound()
    {
        int index = random.RandiRange(0, sounds.Count - 1);
        sounds[index].PlaySound();
    }

    public override string[] _GetConfigurationWarnings()
    {
        int numberOfSQChildren = 0;
        foreach (var child in GetChildren())
        {
            if (child is SoundQueue sound)
            {
                numberOfSQChildren++;
            }
        }

        if (numberOfSQChildren < 2)
        {
            return new string[] { "SoundPool needs at least 2 SoundQueue children" };
        }

        return base._GetConfigurationWarnings();
    }
}

