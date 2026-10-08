using Godot;
using System;

public partial class GameOverButtons : VBoxContainer
{
	private void OnExitButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}
}
