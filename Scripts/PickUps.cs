using Godot;
using System;
using System.Runtime.Serialization;

public partial class PickUps : Node2D
{
	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
        HandlePlayerPicked();
	}

    private void HandlePlayerPicked()
    {
        
    }
}