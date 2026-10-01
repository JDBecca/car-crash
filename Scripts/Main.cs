using Godot;
using System;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var enemy = GetTree().GetFirstNodeInGroup("Enemies") as CarTemplate;
		GetNode<Label>("Label").Text = enemy.currentHealth.ToString();
		var player = GetTree().GetFirstNodeInGroup("Players") as CarTemplate;
		GetNode<Label>("Label2").Text = player.currentHealth.ToString();
		GetNode<Label>("Label3").Text = player.Velocity.Length().ToString();
	}
}
