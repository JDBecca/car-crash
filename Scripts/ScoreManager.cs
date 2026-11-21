using Godot;
using System;

public partial class ScoreManager : Node
{
	public static ScoreManager Instance { get; private set; }
	public int Score { get; private set; } = 0;

	public override void _Ready()
	{
		Instance = this;
	}

	public void AddScore(int points)
	{
		Score += points;
	}

	public void ResetScore()
	{
		Score = 0;
	}
}
