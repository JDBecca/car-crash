using Godot;
using System;

public partial class GameOverButtons : VBoxContainer
{
	[Signal] public delegate void LoginRequestEventHandler(string username, string password);
	[Signal] public delegate void ExitRequestEventHandler();

	private void OnExitButtonPressed()
	{
		EmitSignal(SignalName.ExitRequest);
	}

	private async void OnContinueButtonPressed()
	{
		string username = GetNode<LineEdit>("%UserNameInput").Text.Trim();
		string password = GetNode<LineEdit>("%UserPasswordInput").Text;
		if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
		{
			GD.PrintErr("Empty Inputs.");
			return;
		}
		EmitSignal(SignalName.LoginRequest, username, password);

	}

	public void SetLoading(bool isLoading)
	{
		GetNode<Button>("%ContinueButton").Disabled = isLoading;
	}
}
