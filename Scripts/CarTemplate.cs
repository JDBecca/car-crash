using Godot;
using System;
using System.Reflection.Metadata;

public partial class CarTemplate : CharacterBody2D, IDamageable
{

	[Export] protected float minRotationAngle = 25.0f; //- Degrees
	[Export] private float _maxRotationAngle = 70.0f;
	[Export] protected float enginePower = 270.0f; //- Units/s squared
	[Export] protected float steeringEnginePowerLost = 80.0f;
	[Export] protected float braking = 260.0f; //- Units/s squared
	[Export] protected float maxSpeedForward = 150.0f; //- Units/s
	[Export] protected float maxSpeedReverse = 120.0f; //- Units/s
	protected float carLenght;

	protected Vector2 acceleration = Vector2.Zero; //- Units/s squared

	protected float wheelDistance = 12.0f; //- Units
	private float _rotateAngle; //- Radians
	protected float minSpeedToStopDrift = 100.0f;

	protected float friction = 1.5f; //- Negative coefficient
	protected float brakingFriction = 3f;
	protected float airDrag = 0.0015f; //- Negative coefficient

	public int maxHealth { get; set; } = 100;
	public int currentHealth { get; protected set; } = 100;
	protected float selfRecoilMultiplier = 0.07f;
	protected float hitCooldown = 0.3f;
	internal float hitCooldownTimer = 0.0f;
	protected int minSpeedToDamage = 60;
	protected int maxCrashDamage = 35;
	protected float frontBumperHitMultiplier = 0.4f;
	protected float backBumperHitMultiplier = 1.6f;
	protected float sideHitMultiplier = 1.0f;
	protected float knockbackMultiplier = 0.6f;

	protected float SteerInput = 0.0f;
	protected float ThrottleInput = 0.0f;

	public Vector2 PreCollisionVelocity { get; private set; } = Vector2.Zero;

	public override void _Ready()
	{
		currentHealth = maxHealth;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (hitCooldownTimer > 0)
		{
			hitCooldownTimer -= (float)delta;
		}
		acceleration = Vector2.Zero;

		CarInputs(delta);
		ApplyEngieForces();
		CalculateRotation(delta);
		ApplyFriction();

		Velocity += acceleration * (float)delta;

		PreCollisionVelocity = Velocity;
		MoveAndSlide();

		HandleCollisions(PreCollisionVelocity);
	}


	protected virtual void CarInputs(double delta) { }

	private void ApplyEngieForces()
	{
		float rotationSpeedFactor = Mathf.Clamp(1.0f - Velocity.Length() / 120.0f, 0.0f, 1.0f);
		float currentRotationAngle = Mathf.Lerp(minRotationAngle, _maxRotationAngle, rotationSpeedFactor);
		_rotateAngle = SteerInput * Mathf.DegToRad(currentRotationAngle);

		if (ThrottleInput > 0.01f)
		{
			acceleration = Transform.X * (enginePower * ThrottleInput);
		}
		else if (ThrottleInput < -0.01f)
		{
			acceleration = Transform.X * (braking * ThrottleInput);
		}
	}

	private void CalculateRotation(double delta)
	{
		float fDelta = (float)delta;

		Vector2 rearWheel = Position - Transform.X * wheelDistance / 2.0f;
		Vector2 frontWheel = Position + Transform.X * wheelDistance / 2.0f;
		rearWheel += Velocity * fDelta;
		frontWheel += Velocity.Rotated(_rotateAngle) * fDelta;

		Vector2 newHeading = (frontWheel - rearWheel).Normalized();

		float direction = Velocity.Dot(Transform.X);

		if (direction > 0 && Velocity.Length() > minSpeedToStopDrift)
		{
			Velocity = newHeading * Mathf.Min(Velocity.Length(), maxSpeedForward);
		}
		else if (direction < 0 && Velocity.Length() > minSpeedToStopDrift)
		{
			Velocity = -newHeading * Mathf.Min(Velocity.Length(), maxSpeedReverse);
		}

		if (_rotateAngle != 0)
		{
			Velocity -= Velocity.Normalized() * steeringEnginePowerLost * fDelta;
		}

		if (newHeading != Vector2.Zero)
		{
			Rotation = newHeading.Angle();
		}
	}

	private void ApplyFriction()
	{
		if (Velocity.Length() < 2.5f)
		{
			Velocity = Vector2.Zero;
		}
		bool noInput = Mathf.IsZeroApprox(ThrottleInput) && Mathf.IsZeroApprox(SteerInput);

		float currentFriction = noInput ? brakingFriction : friction;
		float dot = Velocity.Dot(Transform.X);
		Vector2 frictionForce = Velocity * -currentFriction;
		Vector2 airDragForce = Velocity * Velocity.Length() * -airDrag;
		if (Velocity.Length() > 500)
		{
			frictionForce *= 3;
		}
		acceleration += airDragForce + frictionForce;
	}

	protected float GetHitZoneMultiplier(Vector2 worldPosition)
	{
		Vector2 localPos = ToLocal(worldPosition);
		if (GetNode<CollisionShape2D>("CollisionShape2D").Shape is RectangleShape2D rectangleShape)
		{
			float carLenght = rectangleShape.Size.X;
		}

		float BumperThreshold = (carLenght / 2.0f) * 0.8f;
		if (localPos.X > BumperThreshold)
		{
			return frontBumperHitMultiplier;
		}
		else if (localPos.X < -BumperThreshold)
		{
			return backBumperHitMultiplier;
		}

		return sideHitMultiplier;
	}

	protected virtual void HandleCollisions(Vector2 myPreCollisionVelocity)
	{
		if (hitCooldownTimer > 0) { return; }

		int collisionCount = GetSlideCollisionCount();
		for (int i = 0; i < collisionCount; i++)
		{
			KinematicCollision2D collision2D = GetSlideCollision(i);
			Node collider = (Node)collision2D.GetCollider();

			if (collider is CarTemplate otherCar && collider != this)
			{

				if (otherCar.hitCooldownTimer > 0) { continue; }

				Vector2 relativeVelocity = myPreCollisionVelocity - otherCar.PreCollisionVelocity;
				float crashSpeed = relativeVelocity.Length();
				if (crashSpeed < minSpeedToDamage) { continue; }

				Vector2 normal = collision2D.GetNormal();
				Vector2 dirTowardsOtherCar = -normal;

				float myPush = Mathf.Max(0.0f, myPreCollisionVelocity.Dot(dirTowardsOtherCar));
				float otherCarPush = Mathf.Max(0.0f, otherCar.PreCollisionVelocity.Dot(normal));
				float totalPush = myPush + otherCarPush;

				if (totalPush <= 1.0f) { continue; }

				float myPushShare = myPush / totalPush;
				float otherCarPushShare = otherCarPush / totalPush;

				Vector2 collisionPoint = collision2D.GetPosition();
				float baseDamage = Mathf.Clamp(crashSpeed / maxSpeedForward, 0f, 1f) * maxCrashDamage;
				float damageToOtherCar;
				float selfDamage;

				float otherCarHitMultiplier = otherCar.GetHitZoneMultiplier(collisionPoint);

				float myHitMultiplier = GetHitZoneMultiplier(collisionPoint);

				if (otherCarPushShare <= 0.3f)
				{
					damageToOtherCar = baseDamage * otherCarHitMultiplier;
					selfDamage = baseDamage * myHitMultiplier * selfRecoilMultiplier;
				}
				else if (myPushShare <= 0.3f)
				{
					selfDamage = baseDamage * myHitMultiplier;
					damageToOtherCar = baseDamage * selfRecoilMultiplier * otherCarHitMultiplier;
				}
				else
				{
					selfDamage = baseDamage * myPushShare * myHitMultiplier;
					damageToOtherCar = baseDamage * otherCarPushShare * otherCarHitMultiplier;
				}
				int finalSelfDamage = Mathf.Clamp((int)Math.Floor(selfDamage), 0, maxCrashDamage);
				int finalOtherCarDamage = Mathf.Clamp((int)Math.Ceiling(damageToOtherCar), 0, maxCrashDamage);


				if (finalOtherCarDamage > 0)
				{
					otherCar.TakeDamage(finalOtherCarDamage, dirTowardsOtherCar * (crashSpeed * 0.4f), this);
				}

				if (finalSelfDamage > 0)
				{
					TakeDamage(finalSelfDamage, Vector2.Zero, otherCar);
				}

				hitCooldownTimer = hitCooldown;
				otherCar.hitCooldownTimer = hitCooldown;

				Velocity = normal * (crashSpeed * knockbackMultiplier);

				myPreCollisionVelocity = Vector2.Zero;
				otherCar.PreCollisionVelocity = Vector2.Zero;

				OnCarCrashed(otherCar, finalOtherCarDamage);
				break;
			}
		}
	}

	public virtual void TakeDamage(int damage, Vector2 impactForce, Node2D attacker)
	{
		currentHealth -= damage;
		Velocity += impactForce;

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	protected virtual void OnCarCrashed(IDamageable target, float damageDealt) { }

	protected virtual void Die()
	{
		QueueFree();
	}
}