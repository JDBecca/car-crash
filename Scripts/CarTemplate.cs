using Godot;
using System;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

public partial class CarTemplate : CharacterBody2D, IDamageable
{
	[Signal]
	public delegate void CheckGameOverEventHandler(CarTemplate Caller);

	[Export] protected float minRotationAngle = 25.0f; //- Degrees
	[Export] private float _maxRotationAngle = 70.0f;
	[Export] protected float enginePower = 270.0f; //- Units/s squared
	[Export] protected float steeringEnginePowerLost = 80.0f;
	[Export] protected float braking = 260.0f; //- Units/s squared
	[Export] protected float maxSpeedForward = 150.0f; //- Units/s
	[Export] protected float maxSpeedReverse = 120.0f; //- Units/s

	private float hitCooldown = 0.3f;
	internal float hitCooldownTimer = 0.0f;
	protected int minSpeedToDamage = 60;
	protected int maxCrashDamage = 35;

	protected float carLenght;
	protected float carWidth;

	protected Vector2 acceleration = Vector2.Zero; //- Units/s squared

	protected float wheelDistance = 12.0f; //- Units
	private float _rotateAngle; //- Radians
	protected float minSpeedToStopDrift = 100.0f;

	protected float friction = 1.5f; //- Negative coefficient
	protected float externalFriction = 0f;
	protected float brakingFriction = 3f;
	protected float airDrag = 0.0015f; //- Negative coefficient

	public int maxHealth { get; protected set; } = 100;
	public int currentHealth { get; protected set; } = 100;
	protected float recoilMultiplier = 0.07f;
	protected float frontBumperHitMultiplier = 0.4f;
	protected float backBumperHitMultiplier = 1.6f;
	protected float sideHitMultiplier = 1.0f;
	protected float minKnockbackMultiplier = 0.3f;
	protected float maxKnockbackMultiplier = 0.7f;

	protected float SteerInput = 0.0f;
	protected float ThrottleInput = 0.0f;

	public Vector2 PreCollisionVelocity { get; private set; } = Vector2.Zero;

	public override void _Ready()
	{
		currentHealth = maxHealth;

		if (GetNode<CollisionShape2D>("CollisionShape2D").Shape is RectangleShape2D carShape)
		{
			carLenght = carShape.Size.X;
			carWidth = carShape.Size.Y;
		}
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

		HandleCollisions(PreCollisionVelocity, delta);
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
		if (Velocity.Length() < 2.5f) { Velocity = Vector2.Zero; }

		bool noInput = Mathf.IsZeroApprox(ThrottleInput) && Mathf.IsZeroApprox(SteerInput);

		float currentFriction = noInput ? brakingFriction : friction;
		if (externalFriction > 0) { currentFriction += externalFriction; }
		float dot = Velocity.Dot(Transform.X);
		Vector2 frictionForce = Velocity * -currentFriction;
		Vector2 airDragForce = Velocity * Velocity.Length() * -airDrag;
		if (Velocity.Length() > 500) { frictionForce *= 3; }

		acceleration += airDragForce + frictionForce;
	}


	protected void HandleCollisions(Vector2 preCollisionVelocity, double delta)
	{
		int collisionCount = GetSlideCollisionCount();
		for (int i = 0; i < collisionCount; i++)
		{
			KinematicCollision2D collision2D = GetSlideCollision(i);
			Node collider = (Node)collision2D.GetCollider();
			Vector2 normal = collision2D.GetNormal();

			if (collider is StaticBody2D)
			{
				HandleWallCollision(collision2D, normal, delta);
			}
			else if (collider is CarTemplate otherCar && collider != this)
			{
				HandleCarRamming(preCollisionVelocity, collision2D, otherCar);
			}
		}
		if (collisionCount == 0) { externalFriction = 0; }
	}

	protected void HandleWallCollision(KinematicCollision2D collision2D, Vector2 normal, double delta)
	{
		bool isFrontRearCollision = false;
		float roundedThreshold = (carWidth / 2.0f) * 0.6f;
		float BumperThreshold = (carLenght / 2.0f) * 0.4f;
		externalFriction = 1.5f;
		Vector2 collisionPoint = collision2D.GetPosition();
		Vector2 localCollision = ToLocal(collision2D.GetPosition());
		Vector2 rearWheel = Position - Transform.X * wheelDistance / 2.0f;

		if (localCollision.X > BumperThreshold) { isFrontRearCollision = true; }
		else if (localCollision.X < -BumperThreshold) { isFrontRearCollision = true; }

		Vector2 wallTangent = new Vector2(-normal.Y, normal.X);
		Vector2 wallDirection = Transform.X.Dot(wallTangent) >= 0 ? wallTangent : -wallTangent;
		float wantedWallAngle = wallDirection.Angle();

		float angleToWallRad = Mathf.AngleDifference(Rotation, wantedWallAngle);
		float wantedSteerAngle = Mathf.RadToDeg(Mathf.Abs(angleToWallRad));

		float collisionAngle = Mathf.Abs(collisionPoint.AngleToPoint(rearWheel) * 180 / Mathf.Pi);
		if (collisionAngle > 110.0f) { collisionAngle = 110; }

		if (collisionAngle > wantedSteerAngle) { collisionAngle = wantedSteerAngle; }

		if (localCollision.Y > roundedThreshold && isFrontRearCollision && ThrottleInput != 0)
		{
			float angleDiffBefore = Mathf.AngleDifference(Rotation, wantedWallAngle);

			_rotateAngle = -Mathf.DegToRad(collisionAngle);
			CalculateRotation(delta);

			float angleDiffAfter = Mathf.AngleDifference(Rotation, wantedWallAngle);
			if (angleDiffBefore * angleDiffAfter <= 0.0f || Mathf.Abs(angleDiffAfter) < Mathf.DegToRad(1.0f))
			{
				Rotation = wantedWallAngle;
				_rotateAngle = 0.0f;
			}
		}
		else if (localCollision.Y < -roundedThreshold && isFrontRearCollision && ThrottleInput != 0)
		{
			float angleDiffBefore = Mathf.AngleDifference(Rotation, wantedWallAngle);

			_rotateAngle = Mathf.DegToRad(collisionAngle);
			CalculateRotation(delta);

			float angleDiffAfter = Mathf.AngleDifference(Rotation, wantedWallAngle);
			if (angleDiffBefore * angleDiffAfter <= 0.0f || Mathf.Abs(angleDiffAfter) < Mathf.DegToRad(1.0f))
			{
				Rotation = wantedWallAngle;
				_rotateAngle = 0.0f;
			}
		}
		else if (isFrontRearCollision)
		{
			acceleration = Vector2.Zero;
			Velocity = Vector2.Zero;
		}
	}

	protected void HandleCarRamming(Vector2 myPreCollisionVelocity, KinematicCollision2D collision2D, CarTemplate otherCar)
	{
		if (hitCooldownTimer > 0) { return; }

		if (otherCar.hitCooldownTimer > 0) { return; }

		Vector2 relativeVelocity = myPreCollisionVelocity - otherCar.PreCollisionVelocity;
		float crashSpeed = relativeVelocity.Length();
		if (crashSpeed < minSpeedToDamage) { Velocity *= 0.8f; return; }

		Vector2 normal = collision2D.GetNormal();
		Vector2 dirTowardsOtherCar = -normal;

		float myPush = Mathf.Max(0.0f, myPreCollisionVelocity.Dot(dirTowardsOtherCar));
		float otherCarPush = Mathf.Max(0.0f, otherCar.PreCollisionVelocity.Dot(normal));
		float totalPush = myPush + otherCarPush;

		if (totalPush <= 1.0f) { return; }

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
			selfDamage = baseDamage * myHitMultiplier * recoilMultiplier;

		}
		else if (myPushShare <= 0.3f)
		{
			selfDamage = baseDamage * myHitMultiplier;
			damageToOtherCar = baseDamage * recoilMultiplier * otherCarHitMultiplier;

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
			otherCar.TakeDamage(finalOtherCarDamage, dirTowardsOtherCar * (crashSpeed * Mathf.Lerp(minKnockbackMultiplier, maxKnockbackMultiplier, myPushShare)), this);
		}

		if (finalSelfDamage > 0)
		{
			TakeDamage(finalSelfDamage, Vector2.Zero, otherCar);
		}

		hitCooldownTimer = hitCooldown;
		otherCar.hitCooldownTimer = hitCooldown;

		Velocity = normal * (crashSpeed * Mathf.Lerp(minKnockbackMultiplier, maxKnockbackMultiplier, myPushShare));

		myPreCollisionVelocity = Vector2.Zero;
		otherCar.PreCollisionVelocity = Vector2.Zero;

		OnCarCrashed(otherCar, finalOtherCarDamage);
	}

	protected float GetHitZoneMultiplier(Vector2 worldPosition)
	{
		Vector2 localPos = ToLocal(worldPosition);

		float BumperThreshold = (carLenght / 2.0f) * 0.8f;
		if (localPos.X > BumperThreshold) { return frontBumperHitMultiplier; }
		else if (localPos.X < -BumperThreshold) { return backBumperHitMultiplier; }

		return sideHitMultiplier;
	}

	public virtual void TakeDamage(int damage, Vector2 impactForce, Node2D attacker)
	{
		currentHealth -= damage;
		Velocity += impactForce;

		if (currentHealth <= 0) { EmitSignal(SignalName.CheckGameOver, this); Die();}
	}

	protected virtual void OnCarCrashed(IDamageable target, float damageDealt) { }

	public void Heal(int healing)
	{
		currentHealth += healing;
	}

	protected virtual void Die()
	{
		QueueFree();
	}
}