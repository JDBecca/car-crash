using Godot;

public partial class LoginWarningLabel : VBoxContainer
{
	public void SetMessage(bool isUserNameTaken)
	{
		if (isUserNameTaken)
		{
			GetNode<Label>("%WarningLabel").Text = "User already exists!";
			GetNode<Label>("%DescriptionLabel").Text = "Pressing CONTINUE will create a new user with the same name.\nThe existing user will remain unchanged.\nPress BACK to try other password.";
		}
		else
		{
			GetNode<Label>("%WarningLabel").Text = "No user found with this name!";
			GetNode<Label>("%DescriptionLabel").Text = "Pressing CONTINUE will create a new user.";
		}
	}

}
