using Godot;

public partial class GameLogic : Control
{
	protected bool _hasPlayerWon = false;

    public override void _Ready()
    {
        foreach (CharacterBody2D node in FindChildren("*", "CharacterBody2D", true))
		{
			CarTemplate car = (CarTemplate)node;
			car.CheckGameOver += GameOverCheck;
		}
    }

	protected void GameOverCheck(CarTemplate Caller)
	{
		if (Caller.IsInGroup("Players") && GetTree().GetNodeCountInGroup("Players") == 1)
		{
			HandleGameOver(_hasPlayerWon = false, 0);
		}
		int winnerPlayerID = 1;
		if (Caller.IsInGroup("Enemies") && GetTree().GetNodeCountInGroup("Enemies") == 1)
		{
			HandleGameOver(_hasPlayerWon = true, winnerPlayerID);
		}
		else if (Caller.IsInGroup("Players") && GetTree().GetNodeCountInGroup("Players") >= 1 && GetTree().GetNodeCountInGroup("Enemies") <= 0)
		{
			if (GetTree().GetFirstNodeInGroup("Players") != Caller)
			{
				winnerPlayerID = 2;
			}
			HandleGameOver(_hasPlayerWon = true, winnerPlayerID);
		}
	}

	protected void HandleGameOver(bool hasPlayerWon, int winnerPlayerID)
	{
		var GameOverScene = GD.Load<PackedScene>("res://Scenes/GameOver.tscn");
		GameOverUI GameOverInstance = GameOverScene.Instantiate<GameOverUI>();

		GameOverInstance.GameOverWin = hasPlayerWon;
		GameOverInstance.winnerPlayerID = winnerPlayerID;

		Node gameScene = GetTree().CurrentScene;
		GetTree().Root.AddChild(GameOverInstance);
		GetTree().CurrentScene = GameOverInstance;

		gameScene.QueueFree();
	}
}
