using Godot;
using System;
using System.Threading.Tasks;

public partial class LoginWarningButtons : LoginControl
{
    [Signal]
    public delegate void RegisterUserEventHandler();
    public override void _Ready()
    {

    }

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
