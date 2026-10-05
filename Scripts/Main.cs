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
		var Nodes = GetTree().GetNodesInGroup("Players");
		var P1 = Nodes[0] as CarTemplate;
		var P2 = Nodes[1] as CarTemplate;
		GetNode<Label>("Label").Text = P1.currentHealth.ToString();
		GetNode<Label>("Label2").Text = P2.currentHealth.ToString();
	}
}
