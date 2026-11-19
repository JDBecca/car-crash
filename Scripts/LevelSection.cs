using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class LevelSection : Node2D
{
	[Export] public CameraManager.SectionDirection ExitDirection { get; set; } = CameraManager.SectionDirection.Right;
	[Export] public PackedScene EnemyPrefabOverride { get; set; }

	public List<Transform2D> GetSpawnTransforms()
	{
		var transforms = new List<Transform2D>();

		Node container = GetNodeOrNull("Spawns") ?? this;

		foreach (Node child in container.GetChildren())
		{
			if (child is Marker2D marker)
			{
				transforms.Add(marker.GlobalTransform);
			}
		}

		return transforms;
	}
}
