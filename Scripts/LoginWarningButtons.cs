using Godot;

public partial class LoginWarningButtons : VBoxContainer
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
        Owner?.QueueFree();
    }
}
