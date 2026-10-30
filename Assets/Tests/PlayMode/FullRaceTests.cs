using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Hippodrome.Tests
{
	public class FullRaceTests
	{
		[UnityTest]
		[Timeout(300000)]
		public IEnumerator Every_rider_completes_the_laps_and_the_places_are_all_different()
		{
			yield return SceneManager.LoadSceneAsync("Main");
			yield return null;

			var race = Object.FindAnyObjectByType<RaceController>();
			Assert.IsNotNull(race, "the Main scene has a RaceController");
			race.PlayerAutopilot = true; // nobody is at the keyboard: the player's horse rides itself
			race.SaveEnabled = false;    // do not touch the real save file
			race.Laps = 2;

			race.StartRace();
			Time.timeScale = 6f;

			var deadline = Time.realtimeSinceStartup + 240f;
			while (!race.Rules.AllFinished && Time.realtimeSinceStartup < deadline) yield return null;
			Time.timeScale = 1f;

			Assert.AreEqual(RacePhase.Finished, race.Phase, "the race ends when the player finishes");
			Assert.IsTrue(race.Rules.AllFinished, "every rider crossed the line after 2 laps");

			var places = new bool[race.Racers.Length + 1];
			foreach (var progress in race.Rules.Racers)
			{
				Assert.AreEqual(2, progress.Lap, progress.Name + " completed both laps");
				Assert.Greater(progress.FinishTime, 5f, progress.Name + " took a believable time");
				Assert.IsFalse(places[progress.FinishPlace], "place " + progress.FinishPlace + " is given once");
				places[progress.FinishPlace] = true;
			}
			Assert.AreEqual(race.Rules.Racers[race.Order[0]].FinishPlace, 1, "the order starts with the winner");
		}
	}
}
