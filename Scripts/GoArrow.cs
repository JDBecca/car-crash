using Godot;
using System;

public partial class GoArrow : Control
{

	private Tween _blinkTween;

	public override void _Ready()
	{
		Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void ShowArrow(CameraManager.SectionDirection dir)
	{
		Visible = true;

		switch (dir)
		{
			case CameraManager.SectionDirection.Right:
				RotationDegrees = 0;
				Position = new Vector2(210, 105);
				break;
			case CameraManager.SectionDirection.Left:
				RotationDegrees = 180;
				Position = new Vector2(45, 120);
				break;
			case CameraManager.SectionDirection.Down:
				RotationDegrees = 90;
				Position = new Vector2(135, 195);
				break;
			case CameraManager.SectionDirection.Up:
				RotationDegrees = 270;
				Position = new Vector2(120, 30);
				break;
		}

		StartBlinking();
	}

	public void HideArrow()
	{
		Visible = false;
		_blinkTween?.Kill();
	}

	private void StartBlinking()
	{
		_blinkTween?.Kill();
		_blinkTween = CreateTween().SetLoops();
		_blinkTween.TweenProperty(this, "modulate:a", 0.1f, 0.25f);
		_blinkTween.TweenProperty(this, "modulate:a", 1.0f, 0.25f);
	}
}
