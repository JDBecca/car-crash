using System;
using System.Collections.Generic;
using Godot;

public partial class GameLogic : Control
{
	[Export] private CameraManager _cameraManager;
	[Export] private GoArrow _goArrow;
	[Export] private PackedScene _defaultEnemyScene;
	[Export] private Node2D _sectionsContainer;

	protected bool _hasPlayerWon = false;

	private List<LevelSection> _sections = new();
	private int _currentSectionIndex = 0;
	private int _aliveEnemies = 0;

	public override void _Ready()
	{
		_defaultEnemyScene ??= GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
		_sectionsContainer ??= GetNode<Node2D>("Arenas");

		foreach (Node child in _sectionsContainer.GetChildren())
		{
			if (child is LevelSection section)
			{
				_sections.Add(section);
			}
		}

		_cameraManager.PlayerTriggeredExit += OnPlayerTriggerExit;
		_cameraManager.CameraReachedPoint += OnCameraReachedPoint;

		foreach (CharacterBody2D node in FindChildren("*", "CharacterBody2D", true))
		{
			if (node.IsInGroup("Players"))
			{
				CarTemplate car = (CarTemplate)node;
				car.CheckGameOver += GameOverCheck;
			}
		}

		Callable.From(() => StartSection(_currentSectionIndex)).CallDeferred();
	}

	private void StartSection(int i)
	{
		_cameraManager.SetArena();
		_goArrow.HideArrow();
		SetPlayerInputEnabled(true);

		SpawnEnemiesForSection(i);
	}

	private void SpawnEnemiesForSection(int i)
	{
		_aliveEnemies = 0;
		LevelSection section = _sections[i];

		PackedScene enemyPrefab = section.EnemyPrefabOverride ?? _defaultEnemyScene;

		foreach (Transform2D spawnTransform in section.GetSpawnTransforms())
		{
			EnemyAI enemy = enemyPrefab.Instantiate<EnemyAI>();
			enemy.CheckGameOver += OnEnemyDied;

			AddChild(enemy);
			enemy.GlobalTransform = spawnTransform;
			_aliveEnemies++;
		}
	}

	private void OnEnemyDied(CarTemplate caller)
	{
		if (!caller.IsInGroup("Enemies")) return;

		_aliveEnemies--;
		if (_aliveEnemies <= 0)
		{
			OnSectionCleared();
		}
	}

	private void OnSectionCleared()
	{
		if (_currentSectionIndex >= _sections.Count - 1)
		{
			HandleGameOver(hasPlayerWon: true, 0);
			return;
		}

		CameraManager.SectionDirection exitDir = _sections[_currentSectionIndex].ExitDirection;
		_goArrow.ShowArrow(exitDir);
		_cameraManager.OpenExit(exitDir);
	}

	private void OnPlayerTriggerExit(int dirInt)
	{
		CameraManager.SectionDirection dir = (CameraManager.SectionDirection)dirInt;

		_goArrow.HideArrow();
		SetPlayerInputEnabled(false);

		Vector2 offset = dir switch
		{
			CameraManager.SectionDirection.Right => new Vector2(256, 0),
			CameraManager.SectionDirection.Left => new Vector2(-256, 0),
			CameraManager.SectionDirection.Up => new Vector2(0, -224),
			CameraManager.SectionDirection.Down => new Vector2(0, 224),
			_ => Vector2.Zero
		};

		NudgePlayerIntoNewScreen(offset.Normalized() * 32.0f);

		_cameraManager.MoveToPosition(_cameraManager.GlobalPosition + offset);
	}

	private void OnCameraReachedPoint()
	{
		_currentSectionIndex++;
		StartSection(_currentSectionIndex);
	}

	private void NudgePlayerIntoNewScreen(Vector2 pushOffset)
	{
		foreach (Node player in GetTree().GetNodesInGroup("Players"))
		{
			if (player is CharacterBody2D car)
			{
				car.GlobalPosition += pushOffset;
				car.Velocity = Vector2.Zero;
			}
		}
	}

	private void SetPlayerInputEnabled(bool enabled)
	{
		foreach (Node player in GetTree().GetNodesInGroup("Players"))
		{
			if (player is PlayerCar car)
			{
				car.IsInputEnabled = enabled;
			}
		}
	}

	protected void GameOverCheck(CarTemplate Caller)
	{
		if (Caller.IsInGroup("Players") && GetTree().GetNodeCountInGroup("Players") == 1)
		{
			HandleGameOver(_hasPlayerWon = false, 0);
		}
		else if (Caller.IsInGroup("Players") && GetTree().GetNodeCountInGroup("Players") >= 1 && GetTree().GetNodeCountInGroup("Enemies") <= 0)
		{
			int winnerPlayerID = 1;
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
