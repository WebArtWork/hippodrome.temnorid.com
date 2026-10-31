using ITKamianets.Engine.Shell;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>What the shared shell (splash, landing, lobby, top and bottom bars) needs to know about this game.</summary>
	public static class HippodromeShell
	{
		public const string Repo = "WebArtWork/hippodrome.temnorid.com";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Configure()
		{
			ShellConfig.Current = new ShellConfig
			{
				Title = "Temnorid Hippodrome",
				Tagline = "Ride. Race. Cross the line first.",
				GameId = "hippodrome",
				Repo = Repo,
				InstallerSuffix = "-Setup.exe",
				MaxPlayers = 4,
				Multiplayer = true,
				GameScene = "Main",
				Accent = new Color(0.84f, 0.63f, 0.23f),
				Places =
				{
					new ShellPlace("race", "Race", "R"),
					new ShellPlace("records", "Records", "T"),
				},
			};
		}
	}
}
