using Godot;
using System;


public partial class Button : StaticBody3D, IInteractable
{
    [Export] public bool ToggleState = false;
    [Export] private ButtonType buttonType;

    private enum ButtonType
    {
        PUSH,
        TOGGLE
    }

    [Signal] public delegate void ButtonToggledEventHandler(bool state);
    [Signal] public delegate void ButtonPressedEventHandler();

    public void Interact(CharacterBody3D interactor)
    {
        switch (buttonType)
        {
            case ButtonType.PUSH:
                EmitSignal(SignalName.ButtonPressed);
                break;
            case ButtonType.TOGGLE:
                EmitSignal(SignalName.ButtonToggled, ToggleState);
                ToggleState = !ToggleState;
                break;
            default:
                break;
        }
    }
}