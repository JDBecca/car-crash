using Godot;
using System;

public partial class LoginWarningLabel : LoginControl
{
	public override void _Draw()
	{
		if (isUserNameTaken)
		{
			GetNode<Label>("%WarningLabel").Text = "User already exists!";
			GetNode<Label>("%DescriptionLabel").Text = "Pressing CONTINUE will create a new user with the same name.\nThe existing user will remaing unchanged.\nPress BACK to try other password.";
		}
		else if (!isUserNameTaken)
		{
			GetNode<Label>("%WarningLabel").Text = "No user found with this name!";
			GetNode<Label>("%DescriptionLabel").Text = "Pressing CONTINUE will create a new user.";
		}
	}

}
