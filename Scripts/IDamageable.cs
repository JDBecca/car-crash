using Godot;

public interface IDamageable
{
	int currentHealth { get; }
	int maxHealth { get; }
	void TakeDamage(int damage, Vector2 impactForce, Node2D attacker);
}
