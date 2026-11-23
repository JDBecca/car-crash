using Godot;

public partial class HealPickup : Node2D, IPickup
{
	[Export] private int _healingAmount = 50;

	public override void _Ready()
	{
		AddToGroup("Pickups");
		AddToGroup("Heal");
	}
	private void OnPickupBodyEntered(PhysicsBody2D body)
	{
		if (body is CarTemplate target)
		{
			ApplyEffect(target);
		}
	}

	public void ApplyEffect(CarTemplate target)
	{
		if (target.CurrentHealth >= 100) { return; }
		target.Heal(_healingAmount);
		if (target.IsInGroup("Players"))
		{
			ScoreManager.Instance.AddScore(50);
		}
		QueueFree();
	}
}
