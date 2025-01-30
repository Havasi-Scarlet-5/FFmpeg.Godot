using System;
using FFmpeg.Godot;
using Godot;

partial class Player : Node2D
{
	[Export]
	private FFPlayGodot fFPlayGodot;

	[Export]
	private string path;

	[Export]
	private HSlider hSlider;

	[Export]
	private Label label;

	[Export]
	private Label label2;

	public override void _Ready()
	{
		fFPlayGodot.Play(path, path);
		hSlider.MaxValue = fFPlayGodot.GetLength();
		label2.Text = TimeSpan.FromSeconds(fFPlayGodot.GetLength()).ToString("mm':'ss':'fff");
		// fFPlayGodot.Pause();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey inputEventKey && inputEventKey.IsPressed())
		{
			switch (inputEventKey.PhysicalKeycode)
			{
				case Key.Left:
					fFPlayGodot.Seek(fFPlayGodot.PlaybackTime - 1);
					break;
				case Key.Right:
					fFPlayGodot.Seek(fFPlayGodot.PlaybackTime + 1);
					break;
			}
		}
	}

	public override void _Process(double delta)
	{
		hSlider.Value = fFPlayGodot.PlaybackTime;
		label.Text = TimeSpan.FromSeconds(fFPlayGodot.PlaybackTime).ToString("mm':'ss':'fff");
		// GD.Print(string.Format("Time: {0:0.000} / Length: {1:0.000}", FFPlayGodot.TimeAsDouble, fFPlayGodot.GetLength()));
	}
}