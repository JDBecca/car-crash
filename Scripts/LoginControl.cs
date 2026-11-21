using Godot;
using System;
using System.Threading.Tasks;

public partial class LoginControl : VBoxContainer
{
	[Signal]
	public delegate void LoginCompletedEventHandler(bool success, string message);

	protected bool isUserNameTaken = false;

	protected async Task<bool> Authenticate(string username, string passwordHash, DateTime currentdate)
	{
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(passwordHash))
		{
			EmitSignal(SignalName.LoginCompleted, false, "Empty fields.");
			return false;
		}

		bool isAuthenticated = await MySqlManager.UserValidationAsync(username, passwordHash, currentdate);

		if (isAuthenticated)
		{
			EmitSignal(SignalName.LoginCompleted, true, "Login successful!");
			int? user_id = await MySqlManager.GetUserId(username, passwordHash);
			if (user_id != null)
			{
				bool isScoreSaved = await MySqlManager.SaveScore((int)user_id, currentdate);
				if (isScoreSaved) { ScoreManager.Instance.ResetScore(); GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn"); }
				else { return false; }
			}
			else { return false; }
		}
		else
		{
			isUserNameTaken = await MySqlManager.UsernameExists(username);
			if (isUserNameTaken) { EmitSignal(SignalName.LoginCompleted, false, "Name already taken!"); }
			else { EmitSignal(SignalName.LoginCompleted, false, "Non-existent Name"); }

			var LoginWarning = GD.Load<PackedScene>("res://Scenes/LoginWarning.tscn");
			CanvasLayer LoginWarningInstance = LoginWarning.Instantiate<CanvasLayer>();
			GetTree().CurrentScene.AddChild(LoginWarningInstance);
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
		DateTime currentdate = DateTime.Now;


		bool isRegister = await MySqlManager.UserRegisterAsync(username, passwordHash, currentdate);

		if (isRegister)
		{
			GD.Print("Register OK");

			await Authenticate(username, passwordHash, currentdate);
		}
		else
		{
			GD.PrintErr("Register ERROR");
		}
	}
}