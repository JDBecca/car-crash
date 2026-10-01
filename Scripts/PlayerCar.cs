using Godot;

public partial class PlayerCar : CarTemplate
{
	public override void _Ready()
	{
		base._Ready();
		AddToGroup("Players");
		AddToGroup("Cars");
	}

	protected override void CarInputs(double delta)
	{
		base.CarInputs(delta);
		SteerInput = Input.GetActionStrength("Right") - Input.GetActionStrength("Left");
		ThrottleInput = Input.GetActionStrength("Forward") - Input.GetActionStrength("Backward");
	}
}