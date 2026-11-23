using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class ScoreboardUI : Control
{
	private GridContainer _grid;
	[Export] private Color _noEntryTextColor;
	private int _maxRows = 8;
	private readonly struct ScoreboardLabelEntry(Label Rank, Label Username, Label LastScore, Label HighScore)
	{
		public Label Rank { get; } = Rank;
		public Label Username { get; } = Username;
		public Label LastScore { get; } = LastScore;
		public Label HighScore { get; } = HighScore;
	}

	private readonly List<ScoreboardLabelEntry> _rows = new();

	public override void _Ready()
	{
		_grid = GetNode<GridContainer>("%GridContainer");
		SetUpScoreboard();
		RefreshScoreboard();
	}

	private void SetUpScoreboard()
	{
		for (int i = 0; i < _maxRows; i++)
		{
			var rankLabel = new Label { Text = "----", Theme = default, HorizontalAlignment = HorizontalAlignment.Center };
			var userLabel = new Label { Text = "----", Theme = default, HorizontalAlignment = HorizontalAlignment.Center };
			var lastScoreLabel = new Label { Text = "----", Theme = default, HorizontalAlignment = HorizontalAlignment.Center };
			var highScoreLabel = new Label { Text = "----", Theme = default, HorizontalAlignment = HorizontalAlignment.Center };

			_grid.AddChild(rankLabel);
			_grid.AddChild(userLabel);
			_grid.AddChild(lastScoreLabel);
			_grid.AddChild(highScoreLabel);

			_rows.Add(new ScoreboardLabelEntry(rankLabel, userLabel, lastScoreLabel, highScoreLabel));
		}

		for (int i = 0; i < _rows.Count; i++)
		{
			_rows[i].Rank.AddThemeColorOverride("font_color", _noEntryTextColor);
			_rows[i].Username.AddThemeColorOverride("font_color", _noEntryTextColor);
			_rows[i].LastScore.AddThemeColorOverride("font_color", _noEntryTextColor);
			_rows[i].HighScore.AddThemeColorOverride("font_color", _noEntryTextColor);

			_rows[i].Rank.AddThemeFontSizeOverride("font_size", 11);
			_rows[i].Username.AddThemeFontSizeOverride("font_size", 11);
			_rows[i].LastScore.AddThemeFontSizeOverride("font_size", 11);
			_rows[i].HighScore.AddThemeFontSizeOverride("font_size", 11);
		}
	}

	public async Task RefreshScoreboard()
	{
		List<MySqlManager.ScoreboardEntry> scoreboard = await MySqlManager.GetScoreboard(_maxRows);

		if (scoreboard is null) { return; }
		for (int i = 0; i < scoreboard.Count && i < _rows.Count; i++)
		{
			_rows[i].Rank.Text = Convert.ToString(i + 1);
			_rows[i].Username.Text = scoreboard[i].Username;
			_rows[i].LastScore.Text = Convert.ToString(scoreboard[i].LastScore);
			_rows[i].HighScore.Text = Convert.ToString(scoreboard[i].HighScore);

			_rows[i].Rank.RemoveThemeColorOverride("font_color");
			_rows[i].Username.RemoveThemeColorOverride("font_color");
			_rows[i].LastScore.RemoveThemeColorOverride("font_color");
			_rows[i].HighScore.RemoveThemeColorOverride("font_color");

			_rows[i].Rank.HorizontalAlignment = HorizontalAlignment.Center;
			_rows[i].Username.HorizontalAlignment = HorizontalAlignment.Center;
			_rows[i].LastScore.HorizontalAlignment = HorizontalAlignment.Center;
			_rows[i].HighScore.HorizontalAlignment = HorizontalAlignment.Center;

			_rows[i].Rank.ThemeTypeVariation = "Rank";
			_rows[i].Username.ThemeTypeVariation = "NameOnScoreboard";
			_rows[i].LastScore.ThemeTypeVariation = "LastScore";
			_rows[i].HighScore.ThemeTypeVariation = "Score";
		}
	}
}
