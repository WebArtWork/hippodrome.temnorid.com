using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Hippodrome.Tests
{
	/// <summary>
	/// Plays whole races with computer riders standing in for a good, an average and a sloppy player, and
	/// writes how they fared to <c>Builds/balance.txt</c>. It is how the difficulty is tuned: a good player should
	/// usually win, and a sloppy one should still be able to. Run it on its own (it takes a few minutes):
	/// <c>-runTests -testPlatform PlayMode -testFilter Temnorid.Hippodrome.Tests.BalanceTests</c>.
	/// </summary>
	public class BalanceTests
	{
		const int RacesPerStyle = 6;

		struct Style
		{
			public string Name;
			public float Skill;
			public float JumpError;
			public bool Boost;
			public float BoostEvery;
			public float Lane;
		}

		[UnityTest]
		[Explicit("Takes a few minutes; run it when tuning the difficulty.")]
		[Timeout(1800000)]
		public IEnumerator A_good_player_usually_wins_and_a_sloppy_one_still_can()
		{
			yield return SceneManager.LoadSceneAsync("Main");
			yield return null;

			var race = Object.FindAnyObjectByType<RaceController>();
			race.PlayerAutopilot = true;
			race.SaveEnabled = false;
			race.Laps = 3;

			var styles = new[]
			{
				new Style { Name = "good", Skill = 1f, JumpError = 1f, Boost = true, BoostEvery = 3.6f, Lane = -4f },
				new Style { Name = "average", Skill = 0.95f, JumpError = 4.5f, Boost = true, BoostEvery = 5.5f, Lane = -1f },
				new Style { Name = "sloppy", Skill = 0.88f, JumpError = 6f, Boost = false, BoostEvery = 9f, Lane = 3f },
			};

			var report = new StringBuilder();
			var detail = new StringBuilder();
			foreach (var style in styles)
			{
				var bot = race.Player.BotDriver;
				bot.Skill = style.Skill;
				bot.JumpTimingError = style.JumpError;
				bot.UseBoost = style.Boost;
				bot.BoostEvery = style.BoostEvery;
				bot.LaneOffset = style.Lane;

				var wins = 0;
				var places = 0f;
				var margins = new StringBuilder();
				for (var i = 0; i < RacesPerStyle; i++)
				{
					race.StartRace();
					Time.timeScale = 8f;
					var deadline = Time.realtimeSinceStartup + 240f;
					while (!race.Rules.AllFinished && Time.realtimeSinceStartup < deadline) yield return null;
					Time.timeScale = 1f;

					var progress = race.Rules.Racers[0];
					var best = float.MaxValue;
					for (var r = 1; r < race.Racers.Length; r++)
					{
						var other = race.Rules.Racers[r];
						// Riders still on the track when the player finishes have not set a time; they are behind.
						if (other.Finished) best = Mathf.Min(best, other.FinishTime);
					}
					if (progress.FinishPlace == 1) wins++;
					places += progress.FinishPlace;
					margins.Append(best == float.MaxValue ? "+" : (progress.FinishTime - best).ToString("+0.0;-0.0")).Append(' ');
					for (var r = 0; r < race.Racers.Length; r++)
					{
						var racer = race.Racers[r];
						var info = race.Rules.Racers[r];
						detail.AppendLine($"  {style.Name} race {i + 1}: {racer.DisplayName,-7} place {info.FinishPlace} time {info.FinishTime:0.0} bestLap {info.BestLap:0.00} cleared {racer.Horse.BarsCleared} hit {racer.Horse.BarsHit} walls {racer.Horse.WallHits} closeness {racer.Horse.MaxCloseness:0.00}");
					}
					yield return null;
				}
				report.AppendLine($"{style.Name,-8} wins {wins}/{RacesPerStyle}  average place {places / RacesPerStyle:0.0}  margin to best bot (s, - means ahead): {margins}");
			}

			Directory.CreateDirectory("Builds");
			File.WriteAllText("Builds/balance.txt", report.ToString() + "\n" + detail);
			Debug.Log("[Balance]\n" + report);
			Assert.Pass(report.ToString());
		}
	}
}
