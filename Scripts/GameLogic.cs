using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class GameLogic : Node
{

	[Export] private CameraManager _cameraManager;
	[Export] private PackedScene _defaultEnemyScene;
	[Export] private PackedScene _healPickupScene;
	[Export] private GameUI _gameUIControl;
	[Export] private Node2D _sectionsContainer;
	[Export] private PlayerCar _player;

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

		foreach (CharacterBody2D node in GetTree().GetNodesInGroup("Players"))
		{
			CarTemplate car = (CarTemplate)node;
			car.CheckGameOver += GameOverCheck;
			_gameUIControl.BindPlayer(car);
		}

		Callable.From(() => StartSection(_currentSectionIndex)).CallDeferred();
	}

	private void StartSection(int i)
	{
		_cameraManager.SetArena();
		_gameUIControl.GoArrowHide();
		SetPlayerInputEnabled(true);

		SpawnEnemiesForSection(i);
		SpawnPickupsForSection(i);
	}

	private void SpawnEnemiesForSection(int i)
	{
		_aliveEnemies = 0;
		LevelSection section = _sections[i];

		PackedScene enemyPrefab = section.EnemyPrefabOverride ?? _defaultEnemyScene;

		if (section.GetSpawnsTransforms().Count <= 0)
		{
			OnSectionCleared();
			return;
		}
		foreach (Transform2D spawnTransform in section.GetSpawnsTransforms())
		{
			EnemyAI enemy = enemyPrefab.Instantiate<EnemyAI>();
			enemy.CheckGameOver += OnEnemyDied;

			if (enemy.SceneFilePath == "res://Scenes/Boss.tscn")
			{
				enemy.AddToGroup("Boss");
				_gameUIControl.BindBoss(enemy);
			}
			AddChild(enemy);
			enemy.GlobalTransform = spawnTransform;
			_aliveEnemies++;
		}
	}

	private void SpawnPickupsForSection(int i)
	{
		LevelSection section = _sections[i];
		PackedScene healPickup = _healPickupScene;

		foreach (Transform2D spawnTransform in section.GetPickupsTransforms())
		{
			HealPickup pickup = healPickup.Instantiate<HealPickup>();

			AddChild(pickup);
			pickup.GlobalTransform = spawnTransform;
		}
	}

	private void OnEnemyDied(CarTemplate caller)
	{
		if (caller.LastAttacker != null && caller.LastAttacker.IsInGroup("Players"))
		{
			if (caller.IsInGroup("Boss"))
			{
				ScoreManager.Instance.AddScore(3000);
			}
			else
			{
				ScoreManager.Instance.AddScore(1000);
			}
		}

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
		_gameUIControl.GoArrowShow(exitDir);
		_cameraManager.OpenExit(exitDir);
	}

	private void OnPlayerTriggerExit(int dirInt)
	{
		_player.Heal(20);
		CameraManager.SectionDirection dir = (CameraManager.SectionDirection)dirInt;

		_gameUIControl.GoArrowHide();
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
		ScoreManager.Instance.AddScore(5000);
		_currentSectionIndex++;
		foreach (Node2D node in GetTree().GetNodesInGroup("Pickups"))
		{
			node.QueueFree();
		}
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

	protected void GameOverCheck(CarTemplate caller)
	{

		if (caller.IsInGroup("Players") && GetTree().GetNodeCountInGroup("Players") >= 1 && GetTree().GetNodeCountInGroup("Enemies") <= 0)
		{
			int winnerPlayerID = 1;
			if (GetTree().GetFirstNodeInGroup("Players") != caller)
			{
				winnerPlayerID = 2;
			}
			HandleGameOver(true, winnerPlayerID);
		}
		else
		{
			HandleGameOver(false, 0);
		}
	}

	protected async Task HandleGameOver(bool hasPlayerWon, int winnerPlayerID)
	{
		if (hasPlayerWon) { ScoreManager.Instance.AddScore(10000); }


		await Wait();

		async Task Wait()
		{
			var gameScene = GetTree().CurrentScene;
			_gameUIControl.ShowEndLabel(hasPlayerWon);

			await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);

			var gameOverScene = GD.Load<PackedScene>("res://Scenes/GameOver.tscn");
			GameOverUI gameOverInstance = gameOverScene.Instantiate<GameOverUI>();
			gameOverInstance.GameOverWin = hasPlayerWon;
			gameOverInstance.WinnerPlayerID = winnerPlayerID;

			GetTree().Root.AddChild(gameOverInstance);
			GetTree().CurrentScene = gameOverInstance;
			gameScene.QueueFree();
		}
	}
}
