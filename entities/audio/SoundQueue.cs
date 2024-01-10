using Godot;
using System;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class SoundQueue : Node3D
{
    private int next = 0;
    private List<AudioStreamPlayer3D> audioPlayers = new List<AudioStreamPlayer3D>();

    [Export] public int InstancesCount = 1;
    [Export] public AudioStream AudioStream;
    private Global Global = Global.Instance();

    public override void _Ready()
    {
        AudioStreamPlayer3D newAudioPlayer = new AudioStreamPlayer3D()
        {
            Stream = AudioStream
        };

        AddChild(newAudioPlayer);

        if (GetChildCount() == 0)
        {
            GD.Print($"SoundQueue {Name} (at {GetPath()}) has no children");
            return;
        }

        var child = GetChild(0);
        if (child is AudioStreamPlayer3D audioPlayer)
        {
            audioPlayers.Add(audioPlayer);

            for (int i = 0; i < InstancesCount; i++)
            {
                AudioStreamPlayer3D duplicate = audioPlayer.Duplicate() as AudioStreamPlayer3D;
                AddChild(duplicate);
                audioPlayers.Add(duplicate);
            }
        }
    }

    public void PlaySound()
    {
        //GD.Print($"Playing sound {Name} (at {GetPath()})");

        if (!audioPlayers[next].Playing)
        {
            //GD.Print("Playing");
            audioPlayers[next++].Play();
            next %= audioPlayers.Count;
        }

    }

    public override string[] _GetConfigurationWarnings()
    {
        if (AudioStream == null)
        {
            return new string[] { "SoundQueue has no AudioStream" };
        }

        if (GetChildCount() == 0)
        {
            return new string[] { "SoundQueue has no children. Expected new AudioStreamPlayer3D" };
        }

        return base._GetConfigurationWarnings();
    }
}