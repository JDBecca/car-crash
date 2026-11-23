using Godot;
using System;

public partial class CarTemplate : CharacterBody2D, IDamageable
{
	[Signal] public delegate void CheckGameOverEventHandler(CarTemplate Caller);
	[Signal] public delegate void HealthChangedEventHandler(int currentHealth);

	[Export] protected float _minRotationAngle = 25.0f; //- Degrees
	[Export] private float _maxRotationAngle = 70.0f;
	[Export] protected float _enginePower = 270.0f; //- Units/s squared
	[Export] protected float _steeringEnginePowerLost = 80.0f;
	[Export] protected float _braking = 260.0f; //- Units/s squared
	[Export] protected float _maxSpeedForward = 150.0f; //- Units/s
	[Export] protected float _maxSpeedReverse = 120.0f; //- Units/s

	private float _hitCooldown = 0.3f;
	private float _hitCooldownTimer = 0.0f;
	public bool isInHitCooldown = false;

	protected int _minSpeedToDamage = 60;
	[Export] protected int _maxCrashDamage = 40;

	protected float _carLength;
	protected float _carWidth;

	protected Vector2 acceleration = Vector2.Zero; //- Units/s squared

	protected float _wheelDistance = 12.0f; //- Units
	private float _rotateAngle; //- Radians
	protected float _minSpeedToStopDrift = 100.0f;

	protected float _friction = 1.5f; //- Negative coefficient
	protected float _externalFriction = 0f;
	protected float _brakingFriction = 3f;
	protected float _airDrag = 0.0015f; //- Negative coefficient

	[Export] public int MaxHealth { get; protected set; } = 100;
	public int CurrentHealth { get; protected set; } = 100;
	protected float _recoilMultiplier = 0.07f;
	[Export] protected float _frontBumperHitMultiplier = 0.8f;
	[Export] protected float _backBumperHitMultiplier = 1.6f;
	[Export] protected float _sideHitMultiplier = 1.0f;
	[Export] protected float _minKnockbackMultiplier = 0.3f;
	[Export] protected float _maxKnockbackMultiplier = 1.0f;

	protected float _steerInput = 0.0f;
	protected float _throttleInput = 0.0f;

	public Node2D LastAttacker { get; private set; }

	public Vector2 PreCollisionVelocity { get; private set; } = Vector2.Zero;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;

		if (GetNode<CollisionShape2D>("CollisionShape2D").Shape is RectangleShape2D carShape)
		{
			_carLength = carShape.Size.X;
			_carWidth = carShape.Size.Y;
		}
	}

	public void ResetHitCooldown() { _hitCooldownTimer = _hitCooldown; }

	public override void _PhysicsProcess(double delta)
	{
		if (_hitCooldownTimer > 0)
		{
			_hitCooldownTimer -= (float)delta;
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
		float currentRotationAngle = Mathf.Lerp(_minRotationAngle, _maxRotationAngle, rotationSpeedFactor);
		_rotateAngle = _steerInput * Mathf.DegToRad(currentRotationAngle);

		if (_throttleInput > 0.01f)
		{
			acceleration = Transform.X * (_enginePower * _throttleInput);
		}
		else if (_throttleInput < -0.01f)
		{
			acceleration = Transform.X * (_braking * _throttleInput);
		}
	}

	private void CalculateRotation(double delta)
	{
		float fDelta = (float)delta;

		float forwardSpeed = Velocity.Dot(Transform.X);

		Vector2 rearWheel = Position - Transform.X * _wheelDistance / 2.0f;
		Vector2 frontWheel = Position + Transform.X * _wheelDistance / 2.0f;
		rearWheel += Velocity * fDelta;
		frontWheel += Velocity.Rotated(_rotateAngle) * fDelta;

		Vector2 newHeading = (frontWheel - rearWheel).Normalized();

		if (forwardSpeed > 0 && Velocity.Length() > _minSpeedToStopDrift)
		{
			Velocity = newHeading * Mathf.Min(Velocity.Length(), _maxSpeedForward);
		}
		else if (forwardSpeed < 0 && Velocity.Length() > _minSpeedToStopDrift)
		{
			Velocity = -newHeading * Mathf.Min(Velocity.Length(), _maxSpeedReverse);
		}

		if (_rotateAngle != 0)
		{
			Velocity -= Velocity.Normalized() * _steeringEnginePowerLost * fDelta;
		}

		if (newHeading != Vector2.Zero)
		{
			Rotation = newHeading.Angle();
		}
	}

	private void ApplyFriction()
	{
		if (Velocity.Length() < 2.5f) { Velocity = Vector2.Zero; }

		bool noInput = Mathf.IsZeroApprox(_throttleInput) && Mathf.IsZeroApprox(_steerInput);

		float currentFriction = noInput ? _brakingFriction : _friction;
		if (_externalFriction > 0) { currentFriction += _externalFriction; }
		Vector2 frictionForce = Velocity * -currentFriction;
		Vector2 airDragForce = Velocity * Velocity.Length() * -_airDrag;
		if (Velocity.Length() > 500) { frictionForce *= 3; }

		acceleration += airDragForce + frictionForce;
	}


	protected void HandleCollisions(Vector2 preCollisionVelocity, double delta)
	{
		_externalFriction = 0;
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
	}

	protected void HandleWallCollision(KinematicCollision2D collision2D, Vector2 normal, double delta)
	{
		bool isFrontRearCollision = false;
		float roundedThreshold = (_carWidth / 2.0f) * 0.6f;
		float bumperThreshold = (_carLength / 2.0f) * 0.4f;
		_externalFriction = 1.5f;
		Vector2 collisionPoint = collision2D.GetPosition();
		Vector2 localCollision = ToLocal(collision2D.GetPosition());
		Vector2 rearWheel = Position - Transform.X * _wheelDistance / 2.0f;

		if (localCollision.X > bumperThreshold) { isFrontRearCollision = true; }
		else if (localCollision.X < -bumperThreshold) { isFrontRearCollision = true; }

		Vector2 wallTangent = new Vector2(-normal.Y, normal.X);
		Vector2 wallDirection = Transform.X.Dot(wallTangent) >= 0 ? wallTangent : -wallTangent;
		float wantedWallAngle = wallDirection.Angle();

		float angleToWallRad = Mathf.AngleDifference(Rotation, wantedWallAngle);
		float wantedSteerAngle = Mathf.RadToDeg(Mathf.Abs(angleToWallRad));

		float collisionAngle = Mathf.Abs(collisionPoint.AngleToPoint(rearWheel) * 180 / Mathf.Pi);
		if (collisionAngle > 110.0f) { collisionAngle = 110; }

		if (collisionAngle > wantedSteerAngle) { collisionAngle = wantedSteerAngle; }

		if (localCollision.Y > roundedThreshold && isFrontRearCollision && _throttleInput != 0)
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
		else if (localCollision.Y < -roundedThreshold && isFrontRearCollision && _throttleInput != 0)
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
		if (_hitCooldownTimer > 0) { return; }

		if (otherCar._hitCooldownTimer > 0) { return; }

		Vector2 relativeVelocity = myPreCollisionVelocity - otherCar.PreCollisionVelocity;
		float crashSpeed = relativeVelocity.Length();
		if (crashSpeed < _minSpeedToDamage) { Velocity *= 0.8f; return; }

		Vector2 normal = collision2D.GetNormal();
		Vector2 dirTowardsOtherCar = -normal;

		float myCarVelocity = myPreCollisionVelocity.Dot(dirTowardsOtherCar);
		float otherCarVelocity = otherCar.PreCollisionVelocity.Dot(dirTowardsOtherCar);

		float closingSpeed = myCarVelocity - otherCarVelocity;

		float impulseMagnitude = 0f;
		if (closingSpeed > 0)
		{
			float restitution = 0.2f;
			impulseMagnitude = closingSpeed * (1.0f + restitution) * 0.5f;
		}

		Vector2 impulseVector = dirTowardsOtherCar * impulseMagnitude;

		float impact = Math.Clamp(crashSpeed / _maxSpeedForward, 0f, 1f);
		float otherCarKnockback = Mathf.Lerp(otherCar._minKnockbackMultiplier, otherCar._maxKnockbackMultiplier, impact);
		float myKnockback = Mathf.Lerp(_minKnockbackMultiplier, _maxKnockbackMultiplier, impact);

		Vector2 otherCarImpulse = impulseVector * otherCarKnockback;
		Vector2 myImpulse = impulseVector * myKnockback;


		float myPush = Mathf.Max(0.0f, myPreCollisionVelocity.Dot(dirTowardsOtherCar));
		float otherCarPush = Mathf.Max(0.0f, otherCar.PreCollisionVelocity.Dot(normal));
		float totalPush = myPush + otherCarPush;

		if (totalPush <= 1.0f) { return; }

		float myPushShare = myPush / totalPush;
		float otherCarPushShare = otherCarPush / totalPush;

		Vector2 collisionPoint = collision2D.GetPosition();
		float baseDamage = Mathf.Clamp(crashSpeed / _maxSpeedForward, 0f, 1f) * _maxCrashDamage;
		float damageToOtherCar;
		float selfDamage;

		float otherCarHitMultiplier = otherCar.GetHitZoneMultiplier(collisionPoint);

		float myHitMultiplier = GetHitZoneMultiplier(collisionPoint);

		if (otherCarPushShare <= 0.3f)
		{
			damageToOtherCar = baseDamage * otherCarHitMultiplier;
			selfDamage = baseDamage * myHitMultiplier * _recoilMultiplier;

		}
		else if (myPushShare <= 0.3f)
		{
			selfDamage = baseDamage * myHitMultiplier;
			damageToOtherCar = baseDamage * _recoilMultiplier * otherCarHitMultiplier;

		}
		else
		{
			selfDamage = baseDamage * otherCarPushShare * myHitMultiplier;
			damageToOtherCar = baseDamage * myPushShare * otherCarHitMultiplier;
		}
		int finalSelfDamage = Mathf.Clamp((int)Math.Floor(selfDamage), 0, _maxCrashDamage);
		int finalOtherCarDamage = Mathf.Clamp((int)Math.Ceiling(damageToOtherCar), 0, _maxCrashDamage);


		if (finalOtherCarDamage > 0)
		{
			otherCar.TakeDamage(finalOtherCarDamage, otherCarImpulse, this);
		}

		if (finalSelfDamage > 0)
		{
			TakeDamage(finalSelfDamage, Vector2.Zero, otherCar);
		}

		Velocity -= myImpulse;


		PreCollisionVelocity = Vector2.Zero;
		otherCar.PreCollisionVelocity = Vector2.Zero;

		OnCarCrashed(otherCar, finalOtherCarDamage, otherCarHitMultiplier);

		ResetHitCooldown();
		otherCar.ResetHitCooldown();
	}

	protected float GetHitZoneMultiplier(Vector2 worldPosition)
	{
		Vector2 localPos = ToLocal(worldPosition);

		float BumperThreshold = (_carLength / 2.0f) * 0.8f;
		if (localPos.X > BumperThreshold) { return _frontBumperHitMultiplier; }
		else if (localPos.X < -BumperThreshold) { return _backBumperHitMultiplier; }

		return _sideHitMultiplier;
	}

	public virtual void TakeDamage(int damage, Vector2 impactForce, Node2D attacker)
	{
		if (CurrentHealth <= 0) { return; }

		LastAttacker = attacker;
		CurrentHealth -= damage;
		Velocity += impactForce;
		EmitSignal(SignalName.HealthChanged, CurrentHealth);

		if (CurrentHealth <= 0) { EmitSignal(SignalName.CheckGameOver, this); Die(); }
	}

	protected virtual void OnCarCrashed(IDamageable target, float damageDealt, float hitZoneMultiplier) { }

	public void Heal(int healing)
	{
		CurrentHealth = Math.Min(CurrentHealth + healing, MaxHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth);
	}

	protected virtual void Die()
	{
		QueueFree();
	}
}