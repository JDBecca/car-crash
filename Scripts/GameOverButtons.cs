using Godot;
using System;
using System.Threading.Tasks;

public partial class GameOverButtons : LoginControl
{
	public override void _Ready()
	{
		LoginCompleted += OnLoginCompleted;
	}

	private void OnExitButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	private async void OnContinueButtonPressed()
	{
		string username = GetNode<LineEdit>("%UserNameInput").Text.Trim();
		string password = GetNode<LineEdit>("%UserPasswordInput").Text;
		string passwordHash = string.IsNullOrWhiteSpace(password) ? null : password.Sha256Text();
		GetNode<Button>("%ContinueButton").Disabled = true;

		bool success = await Authenticate(username, passwordHash);

		if (!success)
		{
			GetNode<Button>("%ContinueButton").Disabled = false;
		}
	}

	private void OnLoginCompleted(bool success, string message)
	{
		if (success) { GD.Print($"Login: {message}"); }
		else { GD.PrintErr($"Login: {message}"); }
	}
}
