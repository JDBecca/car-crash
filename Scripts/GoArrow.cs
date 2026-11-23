using Godot;

public partial class GoArrow : Control
{

	private Tween _blinkTween;
	private Sprite2D _currentSprite;

	public override void _Ready()
	{
		Visible = false;
		_currentSprite = GetNode<Sprite2D>("GoArrowSprite");
	}

	public void ShowArrow(CameraManager.SectionDirection dir)
	{
		int arrowWidth = _currentSprite.Texture.GetWidth();
		int arrowHeight = _currentSprite.Texture.GetHeight();
		Visible = true;

		switch (dir)
		{
			case CameraManager.SectionDirection.Right:
				RotationDegrees = 0;
				Position = new Vector2(256 - arrowWidth, 136 - arrowHeight);
				break;
			case CameraManager.SectionDirection.Left:
				RotationDegrees = 180;
				Position = new Vector2(arrowWidth, 136);
				break;
			case CameraManager.SectionDirection.Down:
				RotationDegrees = 90;
				Position = new Vector2(128 + (arrowHeight / 2), 224 - arrowWidth);
				break;
			case CameraManager.SectionDirection.Up:
				RotationDegrees = 270;
				Position = new Vector2(128 - (arrowHeight / 2), 24 + arrowWidth);
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
