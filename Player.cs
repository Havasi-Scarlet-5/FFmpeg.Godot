using FFmpeg.Godot;
using Godot;

partial class Player : Node2D
{
	[Export]
	private FFPlayGodot fFPlayGodot;

	[Export]
	private string path;

	public override void _Ready()
	{
		fFPlayGodot.Play(path, path);
	}

	public override void _Process(double delta)
	{
	}
}