using Godot;
using System;

public partial class GameOverUI : Control
{

	public bool GameOverWin = false;
	public int playerWinID = 0;

	public override void _Ready()
	{
		if (GameOverWin == true)
		{
			GetNode<Label>("%GameOverStateLabel").Text = "YOU WON";
			if (playerWinID != 0)
			{
				GetNode<Label>("%GameOverStateLabel").Text = "Player " + playerWinID + " WON";
			}
		}
		else
		{
			GetNode<Label>("%GameOverStateLabel").Text = "YOU LOSE";
		}
	}
}
