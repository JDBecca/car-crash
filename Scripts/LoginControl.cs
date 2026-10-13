using Godot;
using System;
using System.Threading.Tasks;

public partial class LoginControl : VBoxContainer
{
	[Signal]
	public delegate void LoginCompletedEventHandler(bool success, string message);

	protected bool isUserNameTaken = false;
	public override void _Ready()
	{
	}

	protected async Task<bool> Authenticate(string username, string passwordHash)
	{
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(passwordHash))
		{
			EmitSignal(SignalName.LoginCompleted, false, "Empty fields.");
			return false;
		}

		bool isAuthenticated = await MySqlManager.UserValidationAsync(username, passwordHash);

		if (isAuthenticated)
		{
			EmitSignal(SignalName.LoginCompleted, true, "Login successful!");
			GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
		}
		else
		{
			isUserNameTaken = await MySqlManager.UsernameExists(username);
			if (isUserNameTaken) { EmitSignal(SignalName.LoginCompleted, false, "Name already taken!"); }
			else { EmitSignal(SignalName.LoginCompleted, false, "non-existent Name"); }

			var LoginWarning = GD.Load<PackedScene>("res://Scenes/LoginWarning.tscn");
			CanvasLayer LoginWarningInstance = LoginWarning.Instantiate<CanvasLayer>();
			GetTree().Root.AddChild(LoginWarningInstance);
			LoginWarningLabel loginWarningLabels = LoginWarningInstance.GetNode<LoginWarningLabel>("%LabelsContainer");
			LoginWarningButtons loginWarningButtons = LoginWarningInstance.GetNode<LoginWarningButtons>("%ButtonsContainer");

			loginWarningLabels.isUserNameTaken = isUserNameTaken;
			loginWarningButtons.RegisterUser += OnRegisterNewUser;
		}

		return isAuthenticated;
	}

	protected async void OnRegisterNewUser()
	{
		string username = GetNode<LineEdit>("%UserNameInput").Text.Trim();
		string passwordHash = GetNode<LineEdit>("%UserPasswordInput").Text.Sha256Text();


		bool isRegister = await MySqlManager.UserRegisterAsync(username, passwordHash);

		if (isRegister)
		{
			GD.Print("Register OK");

			await Authenticate(username, passwordHash);
		}
		else
		{
			GD.PrintErr("Register ERROR");
		}
	}
}