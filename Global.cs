using System;
using Godot;

public sealed partial class Global : Node
{
    private Global() { }
    private static Global instance;

    public static Global Instance()
    {
        instance ??= new Global();
        return instance;
    }

    public Level CurrentLevel;

    public FastNoiseLite GlobalNoise = new();
}
