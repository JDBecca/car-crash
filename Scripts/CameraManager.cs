using Godot;

public partial class CameraManager : Node2D
{
	public enum SectionDirection
	{
		Right,
		Down,
		Left,
		Up
	}

	[Signal] public delegate void PlayerTriggeredExitEventHandler(int direction);
	[Signal] public delegate void CameraReachedPointEventHandler();

	[Export] private CollisionShape2D _upWall;
	[Export] private CollisionShape2D _rightWall;
	[Export] private CollisionShape2D _downWall;
	[Export] private CollisionShape2D _leftWall;

	[Export] private Area2D _upTrigger;
	[Export] private Area2D _rightTrigger;
	[Export] private Area2D _downTrigger;
	[Export] private Area2D _leftTrigger;

	[Export] public float ScrollDuration = 2.0f;

	public override void _Ready()
	{
		_upTrigger.BodyEntered += (body) => OnBodyEnteredTrigger(body, SectionDirection.Up);
		_downTrigger.BodyEntered += (body) => OnBodyEnteredTrigger(body, SectionDirection.Down);
		_rightTrigger.BodyEntered += (body) => OnBodyEnteredTrigger(body, SectionDirection.Right);
		_leftTrigger.BodyEntered += (body) => OnBodyEnteredTrigger(body, SectionDirection.Left);
	}

	private void OnBodyEnteredTrigger(Node2D body, SectionDirection dir)
	{
		if (body.IsInGroup("Players"))
		{
			EmitSignal(SignalName.PlayerTriggeredExit, (int)dir);
		}
	}

	public void SetArena()
	{
		_upWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_downWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_rightWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_leftWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);

		_upTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_downTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_rightTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_leftTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
	}

	public void DisableWalls()
	{
		_upWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_downWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_rightWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		_leftWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

		_upTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_downTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_rightTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_leftTrigger.SetDeferred(Area2D.PropertyName.Monitoring, false);
	}

	public void OpenExit(SectionDirection dir)
	{
		SetArena();

		switch (dir)
		{
			case SectionDirection.Up:
				_upWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
				_upTrigger.SetDeferred(Area2D.PropertyName.Monitoring, true);
				break;
			case SectionDirection.Down:
				_downWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
				_downTrigger.SetDeferred(Area2D.PropertyName.Monitoring, true);
				break;
			case SectionDirection.Right:
				_rightWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
				_rightTrigger.SetDeferred(Area2D.PropertyName.Monitoring, true);
				break;
			case SectionDirection.Left:
				_leftWall.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
				_leftTrigger.SetDeferred(Area2D.PropertyName.Monitoring, true);
				break;
		}
	}

	public void MoveToPosition(Vector2 targetPostion)
	{
		DisableWalls();

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.InOut);

		tween.TweenProperty(this, "global_position", targetPostion, ScrollDuration);
		tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.CameraReachedPoint)));
	}
}
