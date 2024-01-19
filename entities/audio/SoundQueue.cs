using Godot;
using System;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class SoundQueue : Node3D
{
    private int next = 0;
    private List<AudioStreamPlayer3D> audioPlayers = new();

    private Global Global = Global.Instance();

    public override void _Ready()
    {
        if (GetChildCount() == 0)
        {
            GD.Print($"SoundQueue {Name} (at {GetPath()}) has no children");
            return;
        }

        var child = GetChild(0);
        if (child is AudioStreamPlayer3D audioPlayer)
        {
            audioPlayers.Add(audioPlayer);
        }
    }

    public void PlaySound()
    {
        audioPlayers[next++].Play();
        next %= audioPlayers.Count;
    }

    public override string[] _GetConfigurationWarnings()
    {
        if (GetChildCount() == 0)
        {
            return new string[] { "SoundQueue has no children. Expected new AudioStreamPlayer3D" };
        }

        return base._GetConfigurationWarnings();
    }
}