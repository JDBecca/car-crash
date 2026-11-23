using Godot;
using System;
using System.Threading.Tasks;

public partial class LoginControl : Node
{
	[Export] private GameOverButtons _buttonsView = null!;

	private readonly AuthService _authService = new();

	public override void _Ready()
	{
		_buttonsView.LoginRequest += OnLoginRequest;
		_buttonsView.ExitRequest += OnExitRequest;
	}

	private void OnExitRequest()
	{
		ScoreManager.Instance.ResetScore();
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	private async void OnLoginRequest(string username, string password)
	{
		string passwordHash = password.Sha256Text();
		DateTime currentdate = DateTime.Now;
		int currentScore = ScoreManager.Instance.Score;

		bool success = await _authService.AuthenticateAndSaveScore(username, passwordHash, currentScore, currentdate);

		if (success)
		{
			OnLoginComplete();
		}
		else
		{
			GetNode<Button>("%ContinueButton").Disabled = false;
			await ShowWarning(username, passwordHash, currentScore, currentdate);
		}
	}

	private async Task ShowWarning(string username, string passwordHash, int currentScore, DateTime currentDate)
	{
		var lookup = await _authService.LookupUsername(username);
		bool userExist = lookup == MySqlManager.UsernameLookup.Found;

		var warningScene = GD.Load<PackedScene>("res://Scenes/LoginWarning.tscn");
		CanvasLayer warningInstance = warningScene.Instantiate<CanvasLayer>();
		GetTree().CurrentScene.AddChild(warningInstance);

		var labels = warningInstance.GetNode<LoginWarningLabel>("%LabelsContainer");
		var buttons = warningInstance.GetNode<LoginWarningButtons>("%ButtonsContainer");

		labels.SetMessage(userExist);

		buttons.RegisterUser += async () =>
		{
			bool registered = await _authService.Register(username, passwordHash, currentDate);
			if (registered)
			{
				bool logged = await _authService.AuthenticateAndSaveScore(username, passwordHash, currentScore, currentDate);
				if (logged)
				{
					OnLoginComplete();
				}
			}
		};
	}

	private void OnLoginComplete()
	{
		ScoreManager.Instance.ResetScore();
		GetTree().ChangeSceneToFile("res://Scenes/Scoreboard.tscn");
	}
}
