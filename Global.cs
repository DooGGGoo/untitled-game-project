using System;
using Godot;

public sealed partial class Global : Node
{
    private Global() { }
    private static Global instance;

    public static Global Instance
    {
        get => instance ??= new Global();
        private set => instance = value;
    }

    public Level CurrentLevel;

    public FastNoiseLite GlobalNoise = new();
}
