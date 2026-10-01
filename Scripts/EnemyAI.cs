using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyAI : CarTemplate
{
	public enum AIState
	{
		SeekAndRam,
		TakeDistance,
		Unstuck
	}

	private float _desiredRetreatDistance = 320.0f;
	private float _maxRetreatTime = 2.5f;
	private float _retargetInterval = 1.0f;
	private float _stuckCheckInterval = 0.5f;

	public AIState CurrentState { get; private set; } = AIState.SeekAndRam;

	private float _trackingAccuracy = 3.5f;
	private float _steeringReactionSpeed = 4.0f;
	private Vector2 _aimPosition = Vector2.Zero;

	private Node2D _currentTarget;
	private float _retargetTimer = 0.0f;
	private float _stateTimer = 0.0f;
	private float _stuckTimer = 0.0f;
	private float _pursuitTimer = 0.0f;
	private Vector2 _retreatDirection = Vector2.Zero;

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

		if (_retargetTimer >= _retargetInterval)
		{
			_retargetTimer = 0.0f;
			if (!IsInstanceValid(_currentTarget) || CurrentState == AIState.SeekAndRam)
			{
				SetNewTarget(null);
			}
		}

		switch (CurrentState)
		{
			case AIState.SeekAndRam:
				RammingState(fdelta);
				break;
			case AIState.TakeDistance:
				TakeDistanceState();
				break;
			case AIState.Unstuck:
				UnstuckState();
				break;
		}
	}

	private void RammingState(float fdelta)
	{
		if (!IsInstanceValid(_currentTarget))
		{
			SetNewTarget(null);
			ThrottleInput = 0.0f;
			SteerInput = Mathf.MoveToward(SteerInput, 0.0f, _steeringReactionSpeed * fdelta);
			return;
		}

		_pursuitTimer += fdelta;

		if (_aimPosition == Vector2.Zero)
		{
			_aimPosition = _currentTarget.GlobalPosition;
		}

		_aimPosition = _aimPosition.Lerp(_currentTarget.GlobalPosition, _trackingAccuracy * fdelta);

		Vector2 toAimedPoint = _aimPosition - GlobalPosition;
		Vector2 forward = Transform.X;

		float cross = forward.Cross(toAimedPoint.Normalized());
		float desiredSteer = Mathf.Clamp(cross * 2.5f, -1.0f, 1.0f);

		SteerInput = Mathf.MoveToward(SteerInput, desiredSteer, _steeringReactionSpeed * fdelta);

		ThrottleInput = 1.0f;
	}

	private void TakeDistanceState()
	{
		if (!IsInstanceValid(_currentTarget))
		{
			SwitchState(AIState.SeekAndRam);
			return;
		}

		float distanceToTarget = GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);

		Vector2 forward = Transform.X;
		float cross = forward.Cross(_retreatDirection);
		SteerInput = Mathf.Clamp(cross * 2.5f, -1.0f, 1.0f);

		ThrottleInput = 1.0f;

		if (distanceToTarget >= _desiredRetreatDistance || _stateTimer >= _maxRetreatTime)
		{
			SwitchState(AIState.SeekAndRam);
		}
	}

	private void UnstuckState()
	{
		ThrottleInput = -1.0f;
		SteerInput = 1.0f;

		if (_stateTimer >= 1.0f)
		{
			SwitchState(AIState.SeekAndRam);
		}
	}

	private void CheckStuck(float fdelta)
	{
		float nowTime = Time.GetTicksMsec() / 1000;

		if (CurrentState == AIState.Unstuck) { return; }

		if (Mathf.Abs(ThrottleInput) > 0.5f && Velocity.Length() < 25.0f)
		{
			_stuckTimer += fdelta;
			if (_stuckTimer >= _stuckCheckInterval)
			{
				_stuckTimer = 0.0f;
				SwitchState(AIState.Unstuck);
			}
		}
		else if (CurrentState == AIState.SeekAndRam && _pursuitTimer >= 10.0f && GetTree().GetNodeCountInGroup("Cars") != 1)
		{
			_retargetTimer = -2.0f;
			_pursuitTimer = 0.0f;
			SetNewTarget(_currentTarget);
		}
		else
		{
			_stuckTimer = Mathf.Max(0.0f, _stuckTimer - fdelta);
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
			_retreatDirection = away.Rotated(randomAngle);
		}
	}

	protected override void OnCarCrashed(IDamageable target, float damageDealt)
	{
		_pursuitTimer = 0.0f;
		SwitchState(AIState.TakeDistance);
	}

	private void SetNewTarget(Node2D ignoredTarget)
	{
		var cars = GetTree().GetNodesInGroup("Cars");
		Node2D bestTarget = null;
		float shortestDist = float.MaxValue;

		foreach (Node node in cars)
		{
			if (node is Node2D car && car != this && car != ignoredTarget && IsInstanceValid(car))
			{
				float dist = GlobalPosition.DistanceSquaredTo(car.GlobalPosition);
				if (dist < shortestDist)
				{
					shortestDist = dist;
					bestTarget = car;
				}
			}
		}
		_currentTarget = bestTarget;
	}
}
