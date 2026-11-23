using Godot;
using System;

public partial class GameUI : Control
{
	[Export] private GoArrow _goArrow;
	private Label _gameOverLabel;
	private Label _hpLabelP1;
	private Label _bossHpLabel;
	private Label _scoreLabel;

	public override void _Ready()
	{
		_gameOverLabel = GetNode<Label>("%GameOverLabel");
		_hpLabelP1 = GetNode<Label>("%HPLabel_P1");
		_bossHpLabel = GetNode<Label>("%BossHPLabel");
		_scoreLabel = GetNode<Label>("%ScoreLabel");

		_gameOverLabel.Visible = false;
		GetNode<Label>("%BossHPLabel").Visible = false;
		ScoreManager.Instance.ScoreChanged += UpdateScoreDisplay;
		UpdateScoreDisplay(ScoreManager.Instance.Score);
	}

	public void GoArrowHide()
	{
		_goArrow.HideArrow();
	}
	public void GoArrowShow(CameraManager.SectionDirection exitDir)
	{
		_goArrow.ShowArrow(exitDir);
	}

	public void ShowEndLabel(bool hasPlayerWon)
	{
		if (hasPlayerWon)
		{
			_gameOverLabel.Text = "YOU WIN!";
			_gameOverLabel.ThemeTypeVariation = "Win";
		}
		else
		{
			_gameOverLabel.Text = "YOU LOSE!";
			_gameOverLabel.ThemeTypeVariation = "Lose";
		}
		_gameOverLabel.Visible = true;
	}

	public void BindPlayer(CarTemplate player)
	{
		player.HealthChanged += (current) =>
		{
			_hpLabelP1.Text = $"HP {Math.Max(0, current)}";
		};
		_hpLabelP1.Text = $"HP {Math.Max(0, player.CurrentHealth)}";
	}

	public void BindBoss(CarTemplate boss)
	{
		boss.HealthChanged += (current) =>
		{
			_bossHpLabel.Text = $"BOSS HP {Math.Max(0, current)}";
		};
		_bossHpLabel.Text = $"BOSS HP {Math.Max(0, boss.CurrentHealth)}";
		if (!_bossHpLabel.Visible) { _bossHpLabel.Visible = true; }
	}

	private void UpdateScoreDisplay(int newScore)
	{
		_scoreLabel.Text = newScore.ToString();
	}

	public override void _ExitTree()
	{
		if (ScoreManager.Instance != null) { ScoreManager.Instance.ScoreChanged -= UpdateScoreDisplay; }
	}

}
