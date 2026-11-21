using Godot;
using System;

public partial class HealPickup : Node2D
{
	private int maxHealing = 50;

	public override void _Ready()
	{
		AddToGroup("Pickups");
		AddToGroup("HealingPickups");
	}
	private void OnPickupBodyEntered(PhysicsBody2D body)
	{
		if (body is CarTemplate target)
		{
			Heal(target);
		}
	}

	private void Heal(CarTemplate target)
	{
		int currentHealth = target.currentHealth;
		if (target.currentHealth > 0 && target.currentHealth <= 50)
		{
			target.Heal(maxHealing);
		}
		else if (target.currentHealth > 0 && target.currentHealth != 100)
		{
			int healing = target.maxHealth - currentHealth;
			target.Heal(healing);
		}
		else
		{
			return;
		}

		if (target.IsInGroup("Players"))
		{
			ScoreManager.Instance.AddScore(50);
		}
		QueueFree();
	}
}
