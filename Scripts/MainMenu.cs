using Godot;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	private void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnBackButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	private void OnScoreboardButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Scoreboard.tscn");
	}
}
