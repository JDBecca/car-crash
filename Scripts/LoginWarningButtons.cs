using Godot;
using System;

public partial class LoginWarningButtons : LoginControl
{
    [Signal]
    public delegate void RegisterUserEventHandler();

    private void OnNewUserButtonPressed()
    {
        EmitSignal(SignalName.RegisterUser);
        UnloadCurrentScene();
    }

    private void OnBackButtonPressed()
    {
        UnloadCurrentScene();
    }

    private void UnloadCurrentScene()
    {
        var parentScene = GetTree().CurrentScene.GetParent();
        if (parentScene != null)
        {
            var thisScene = parentScene.GetChild(1, true);
            thisScene.QueueFree();
        }
    }
}
