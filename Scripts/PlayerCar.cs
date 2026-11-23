using Godot;

public partial class PlayerCar : CarTemplate
{
	private ICarInputProvider _inputProvier = null!;


	public bool IsInputEnabled { get; set; } = true;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("Players");
		AddToGroup("Cars");
		SetInputs();
	}

	private void SetInputs()
	{
		bool isPlayerTwo = GetTree().GetFirstNodeInGroup("Players") != this;
		if (isPlayerTwo)
		{
			_inputProvier = new KeyboardCarInput("Right_P2", "Left_P2", "Forward_P2", "Backward_P2");
			Sprite2D sprite2D = GetNode<Sprite2D>("Sprite2D");
			sprite2D.Texture = GD.Load<Texture2D>("res://Assets/Sprites/red_car_sprite.png");
		}
		else
		{
			_inputProvier = new KeyboardCarInput("Right", "Left", "Forward", "Backward");
		}
	}

	protected override void CarInputs(double delta)
	{
		if (!IsInputEnabled)
		{
			_steerInput = 0.0f;
			_throttleInput = 0.0f;
			return;
		}
		_steerInput = _inputProvier.GetSteering();
		_throttleInput = _inputProvier.GetThrottle();
	}

	protected override void OnCarCrashed(IDamageable target, float damageDealt, float hitZoneMultiplier)
	{
		if (target is CarTemplate targetCar && targetCar.IsInGroup("Enemies"))
		{
			bool isRearHit = damageDealt > 0 && hitZoneMultiplier >= _backBumperHitMultiplier;
			int points = isRearHit ? 500 : 100;
			ScoreManager.Instance.AddScore(points);
		}
	}
}

public class KeyboardCarInput : ICarInputProvider
{
	private readonly string _right;
	private readonly string _left;
	private readonly string _forward;
	private readonly string _backward;

	public KeyboardCarInput(string rightAction, string leftAction, string forwardAction, string backwardAction)
	{
		_right = rightAction;
		_left = leftAction;
		_forward = forwardAction;
		_backward = backwardAction;
	}

	public float GetSteering() => Input.GetActionStrength(_right) - Input.GetActionStrength(_left);
	public float GetThrottle() => Input.GetActionStrength(_forward) - Input.GetActionStrength(_backward);
}