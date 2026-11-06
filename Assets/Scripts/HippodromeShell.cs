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
				// One player, straight into the village and its ring track after signing in: no landing screen, no lobby, no Profile for now.
				Multiplayer = false,
				Profile = false,
				SkipLanding = true,
				GameScene = "Main",
				Accent = new Color(0.84f, 0.63f, 0.23f),
			};
		}
	}
}
