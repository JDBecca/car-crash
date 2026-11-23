using Godot;

public partial class GameOverUI : Control
{

	public bool GameOverWin = false;
	public int WinnerPlayerID = 0;

	public override void _Ready()
	{
		Label gameOverLabel = GetNode<Label>("%GameOverStateLabel");
		if (GameOverWin)
		{
			gameOverLabel.Text = "YOU WIN!";
			gameOverLabel.ThemeTypeVariation = "Win";
			if (WinnerPlayerID != 0)
			{
				gameOverLabel.Text = "Player " + WinnerPlayerID + " WON";
			}
		}
		else
		{
			gameOverLabel.Text = "YOU LOSE";
			gameOverLabel.ThemeTypeVariation = "Lose";
		}

		GetNode<Label>("%ScorePointsLabel").Text = $"{ScoreManager.Instance.Score}";

	}
}
