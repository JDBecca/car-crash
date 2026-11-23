using Godot;

public partial class ScoreManager : Node
{
	[Signal] public delegate void ScoreChangedEventHandler(int newScore);
	public static ScoreManager Instance { get; private set; }
	public int Score { get; private set; } = 0;

	public override void _Ready()
	{
		Instance = this;
	}

	public void AddScore(int points)
	{
		Score += points;
		EmitSignal(SignalName.ScoreChanged, Score);
	}

	public void ResetScore()
	{
		Score = 0;
		EmitSignal(SignalName.ScoreChanged, Score);
	}
}
