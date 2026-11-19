using Godot;
using System;

public partial class HUDControl : Control
{
	public override void _Process(double delta)
	{
		if (GetTree().GetNodeCountInGroup("Players") >= 1)
		{
			CarTemplate Player1 = (CarTemplate)GetTree().GetFirstNodeInGroup("Players");
			GetNode<Label>("HPLabel_P1").Text = Player1.currentHealth.ToString();
		}
	}
}
