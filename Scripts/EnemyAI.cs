using System;
using Godot;

public partial class EnemyAI : CarTemplate
{
	public enum AIState
	{
		SeekAndRam,
		SeekForHeal,
		TakeDistance,
		Unstuck
	}

	private enum StuckType
	{
		FrontStuck,
		LoopStuck
	}

	private float _desiredMaxRetreatDistance = 60.0f;
	private float _desiredRetreatDistance = 60.0f;
	private float _retargetInterval = 1.0f;
	private float _stuckCheckInterval = 0.5f;

	public AIState CurrentState { get; private set; } = AIState.SeekAndRam;
	private StuckType UnstuckType = StuckType.FrontStuck;

	private float _trackingAccuracy = 3.5f;
	private float _steeringReactionSpeed = 4.0f;
	private Vector2 _aimPosition = Vector2.Zero;

	private Node2D _currentTarget;
	private float _retargetTimer = 0.0f;
	private float _stateTimer = 0.0f;
	private float _stuckTimer = 0.0f;
	private float _pursuitTimer = 0.0f;
	[Export] private int _minHealthForHeal = 60;
	private int _retreatDirection = -1;
	private Vector2 _retreatPoint = Vector2.Zero;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("Enemies");
		AddToGroup("Cars");
		SetNewTarget(null);
	}

	protected override void CarInputs(double delta)
	{
		float fdelta = (float)delta;
		_retargetTimer += fdelta;
		_stateTimer += fdelta;

		CheckStuck(fdelta);
		CheckHealth();

		if (_retargetTimer >= _retargetInterval)
		{
			_retargetTimer = 0.0f;
			if (CurrentHealth > _minHealthForHeal || GetTree().GetNodeCountInGroup("Heal") <= 0)
			{
				SwitchState(AIState.SeekAndRam);
				SetNewTarget(null);
			}
			else
			{
				SwitchState(AIState.SeekForHeal);
				SetNewPickupTarget();
			}
		}

		switch (CurrentState)
		{
			case AIState.SeekAndRam:
				RammingState(fdelta);
				break;
			case AIState.SeekForHeal:
				HealingState(fdelta);
				break;
			case AIState.TakeDistance:
				TakeDistanceState();
				break;
			case AIState.Unstuck:
				UnstuckState();
				break;
		}
	}

	private void SteerToward(Vector2 worldPoint, float fdelta)
	{
		if (_aimPosition == Vector2.Zero) { _aimPosition = worldPoint; }

		_aimPosition = _aimPosition.Lerp(worldPoint, _trackingAccuracy * fdelta);

		Vector2 toAimedPoint = _aimPosition - GlobalPosition;
		float desiredSteer = 0.0f;
		if (toAimedPoint.LengthSquared() > 0.001f)
		{
			float cross = Transform.X.Cross(toAimedPoint.Normalized());
			desiredSteer = Mathf.Clamp(cross * 2.5f, -1.0f, 1.0f);
		}
		_steerInput = Mathf.MoveToward(_steerInput, desiredSteer, _steeringReactionSpeed * fdelta);
		_throttleInput = 1.0f;
	}

	private void RammingState(float fdelta)
	{
		if (!IsInstanceValid(_currentTarget))
		{
			SetNewTarget(null);
			if (!IsInstanceValid(_currentTarget))
			{
				_throttleInput = 0.0f;
				_steerInput = Mathf.MoveToward(_steerInput, 0.0f, _steeringReactionSpeed * fdelta);
				return;
			}
		}

		_pursuitTimer += fdelta;
		SteerToward(_currentTarget.GlobalPosition, fdelta);
	}

	private void HealingState(float fdelta)
	{
		if (!IsInstanceValid(_currentTarget) || CurrentHealth > _minHealthForHeal)
		{
			SetNewPickupTarget();
			if (!IsInstanceValid(_currentTarget) || CurrentHealth > _minHealthForHeal)
			{
				SwitchState(AIState.SeekAndRam);
				return;
			}
		}

		SteerToward(_currentTarget.GlobalPosition, fdelta);
	}

	private void TakeDistanceState()
	{
		if (!IsInstanceValid(_currentTarget))
		{
			if (CurrentHealth <= _minHealthForHeal)
			{
				SwitchState(AIState.SeekForHeal);
			}
			else
			{
				SwitchState(AIState.SeekAndRam);
			}
			return;
		}

		float distanceToTarget = GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);

		Vector2 forward = Transform.X;
		float cross = forward.Cross(_retreatPoint);
		_steerInput = Mathf.Clamp(cross * 2.5f, -1.0f, 1.0f);

		_throttleInput = 1.0f;

		if (distanceToTarget >= _desiredRetreatDistance)
		{
			SwitchState(AIState.SeekAndRam);
		}
	}

	private void UnstuckState()
	{
		if (UnstuckType == StuckType.FrontStuck)
		{
			_throttleInput = -1.0f;
			_steerInput = 1.0f;
		}
		else if (UnstuckType == StuckType.LoopStuck)
		{
			_retreatDirection = 1;
			_desiredRetreatDistance = 35.0f;
			SwitchState(AIState.TakeDistance);
		}
		if (_stateTimer >= 1.0f)
		{
			SwitchState(AIState.SeekAndRam);
		}
	}

	private void CheckStuck(float fdelta)
	{
		if (CurrentState == AIState.Unstuck) { return; }

		if (Mathf.Abs(_throttleInput) > 0.5f && Velocity.Length() < 25.0f)
		{
			_stuckTimer += fdelta;
			if (_stuckTimer >= _stuckCheckInterval)
			{
				_stuckTimer = 0.0f;
				UnstuckType = StuckType.FrontStuck;
				SwitchState(AIState.Unstuck);
			}
		}
		else if (CurrentState == AIState.SeekAndRam && _pursuitTimer >= 8.0f)
		{
			_retargetTimer = -2.0f;
			_pursuitTimer = 0.0f;
			if (GetTree().GetNodeCountInGroup("Cars") > 2)
			{
				SetNewTarget(_currentTarget);
			}
			else
			{
				UnstuckType = StuckType.LoopStuck;
				SwitchState(AIState.Unstuck);
			}
		}
		else
		{
			_stuckTimer = Mathf.Max(0.0f, _stuckTimer - fdelta);
		}
	}

	private void CheckHealth()
	{
		if (CurrentHealth <= _minHealthForHeal && GetTree().GetNodeCountInGroup("Heal") > 0)
		{
			if (CurrentState != AIState.SeekForHeal) { SwitchState(AIState.SeekForHeal); }
		}
	}

	private void SwitchState(AIState state)
	{
		CurrentState = state;
		_stateTimer = 0.0f;

		if (state == AIState.TakeDistance && IsInstanceValid(_currentTarget))
		{
			Vector2 away = (GlobalPosition - _currentTarget.GlobalPosition).Normalized();
			float randomAngle = (float)GD.RandRange(-Mathf.Pi / 3.0f, Mathf.Pi / 3.0f);
			if (_retreatDirection > 0) { randomAngle = Math.Abs(randomAngle); }
			_retreatPoint = away.Rotated(randomAngle);
		}
	}

	protected override void OnCarCrashed(IDamageable target, float damageDealt, float hitZoneMultiplier)
	{
		_pursuitTimer = 0.0f;
		_retreatDirection = -1;
		_desiredRetreatDistance = _desiredMaxRetreatDistance;
		SwitchState(AIState.TakeDistance);
	}

	private void SetNewTarget(Node2D ignoredTarget)
	{
		var cars = GetTree().GetNodesInGroup("Cars");
		Node2D bestTarget = null;
		float shortestDist = float.MaxValue;
		foreach (Node node in cars)
		{
			if (node is Node2D car && car != this && car != ignoredTarget && IsInstanceValid(car) && !car.IsQueuedForDeletion())
			{
				if (car is CarTemplate carTemplate && carTemplate.CurrentHealth <= 0) { continue; }

				float dist = GlobalPosition.DistanceSquaredTo(car.GlobalPosition);
				if (dist < shortestDist)
				{
					shortestDist = dist;
					bestTarget = car;
				}
			}
		}
		_currentTarget = bestTarget;

		if (_currentTarget != null) { _aimPosition = _currentTarget.GlobalPosition; }
	}

	private void SetNewPickupTarget()
	{
		var heals = GetTree().GetNodesInGroup("Heal");
		Node2D bestTarget = null;
		float shortestDist = float.MaxValue;
		if (CurrentHealth <= _minHealthForHeal)
		{
			foreach (Node node in heals)
			{
				if (node is Node2D heal && IsInstanceValid(heal) && !heal.IsQueuedForDeletion())
				{
					float dist = GlobalPosition.DistanceSquaredTo(heal.GlobalPosition);
					if (dist < shortestDist)
					{
						shortestDist = dist;
						bestTarget = heal;
					}
				}
			}
		}
		_currentTarget = bestTarget;

		if (_currentTarget != null) { _aimPosition = _currentTarget.GlobalPosition; }
	}
}
