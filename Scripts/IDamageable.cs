using Godot;

public interface IDamageable
{
	int CurrentHealth { get; }
	int MaxHealth { get; }
	void TakeDamage(int damage, Vector2 impactForce, Node2D attacker);
}
