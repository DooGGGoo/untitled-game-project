using Godot;
using System;



public partial class Lightswitch : StaticBody3D, IInteractable
{
    [Export] public bool SwitchState = false;

    [Signal] public delegate void SwitchLightVisibilityEventHandler(bool visible);

    public void Interact(CharacterBody3D interactor)
    {
        if (SwitchState == true)
        {
            SwitchState = false;
            EmitSignal(SignalName.SwitchLightVisibility, SwitchState);
        }
        else
        {
            SwitchState = true;
            EmitSignal(SignalName.SwitchLightVisibility, SwitchState);
        }

    }
}