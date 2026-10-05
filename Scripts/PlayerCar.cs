using Godot;

public partial class PlayerCar : CarTemplate
{
		private string _forward = "Forward";
		private string _left = "Left";
		private string _right = "Right";
		private string _backward = "Backward";
	public override void _Ready()
	{
		base._Ready();
		AddToGroup("Players");
		AddToGroup("Cars");
		SetInputs();
	}

	private void SetInputs()
	{
	if ( GetTree().GetFirstNodeInGroup("Players") != this)
		{
			_right = "Right_P2";
			_left = "Left_P2";
			_forward = "Forward_P2"; 
			_backward = "Backward_P2";
		}
	}
	protected override void CarInputs(double delta)
	{
		base.CarInputs(delta);
		SteerInput = Input.GetActionStrength(_right) - Input.GetActionStrength(_left);
		ThrottleInput = Input.GetActionStrength(_forward) - Input.GetActionStrength(_backward);
	}

}