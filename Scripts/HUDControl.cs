using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class HUDControl : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		int players = GetTree().GetNodeCountInGroup("Players");
		if (players > 1)
		{
			var Nodes = GetTree().GetNodesInGroup("Players");
			CarTemplate player1 = Nodes[0] as CarTemplate;
			GetNode<Label>("HPLabel_P1").Text = "HP: " + player1.currentHealth.ToString();
			CarTemplate player2 = Nodes[1] as CarTemplate;
			GetNode<Label>("HPLabel_P2").Text = "HP: " + player2.currentHealth.ToString();
		}
		else if (players == 1)
		{
			CarTemplate player = GetTree().GetFirstNodeInGroup("Players") as CarTemplate;
			GetNode<Label>("HPLabel_P1").Text = "HP: " + player.currentHealth.ToString();
		}
	}
}
