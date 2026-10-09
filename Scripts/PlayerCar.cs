using Godot;

public partial class PlayerCar : CarTemplate
{

	private string _forward = "Forward";
	private string _left = "Left";
	private string _right = "Right";
	private string _backward = "Backward";
	
	public override void _Ready()
	{
		playerID = 1;
		base._Ready();
		AddToGroup("Players");
		AddToGroup("Cars");
		SetInputs();
	}

	private void SetInputs()
	{
		if (GetTree().GetFirstNodeInGroup("Players") != this)
		{
			_right = "Right_P2";
			_left = "Left_P2";
			_forward = "Forward_P2";
			_backward = "Backward_P2";
			playerID = 2;
			Sprite2D sprite2D = GetNode<Sprite2D>("Sprite2D");
			Vector2 textureSize = sprite2D.Scale;
			sprite2D.Texture = GD.Load<Texture2D>("res://Assets/Sprites/red_car_sprite.png");
			sprite2D.Scale = textureSize;
		}
	}
	protected override void CarInputs(double delta)
	{
		base.CarInputs(delta);
		SteerInput = Input.GetActionStrength(_right) - Input.GetActionStrength(_left);
		ThrottleInput = Input.GetActionStrength(_forward) - Input.GetActionStrength(_backward);
	}

}