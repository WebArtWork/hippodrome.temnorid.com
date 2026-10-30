using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Hippodrome.Tests
{
	/// <summary>
	/// How horses behave on the real track: they cannot ride through each other, walls and bars slow them,
	/// a jump clears a bar and boost makes a horse faster for a moment.
	/// </summary>
	public class RidingTests
	{
		const float Ground = 0.12f;

		RaceController _race;

		IEnumerator Load()
		{
			yield return SceneManager.LoadSceneAsync("Main");
			yield return null;
			_race = Object.FindAnyObjectByType<RaceController>();
			Assert.IsNotNull(_race);
			_race.SaveEnabled = false;
		}

		static ScriptedDriver Ride(Racer racer, Vector3 position, float heading, float throttle)
		{
			var horse = racer.Horse;
			horse.ResetRace();
			horse.Teleport(position, Quaternion.Euler(0f, heading, 0f));
			var driver = racer.GetComponent<ScriptedDriver>();
			if (driver == null) driver = racer.gameObject.AddComponent<ScriptedDriver>();
			driver.Throttle = throttle;
			driver.Steer = 0f;
			driver.JumpAt = null;
			horse.Driver = driver;
			horse.Locked = false;
			return driver;
		}

		static Hurdle HurdleNear(Vector3 point)
		{
			Hurdle best = null;
			var bestDistance = float.MaxValue;
			foreach (var hurdle in Hurdle.All)
			{
				var distance = (hurdle.transform.position - point).magnitude;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = hurdle;
				}
			}
			return best;
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator A_faster_horse_cannot_ride_through_a_slower_one()
		{
			yield return Load();

			// Open grass beside the track, so nothing but the other horse is in the way.
			var slow = _race.Racers[1];
			var fast = _race.Racers[2];
			Ride(slow, new Vector3(0f, Ground, 100f), 90f, 0.5f);
			Ride(fast, new Vector3(-30f, Ground, 100f), 90f, 1f);

			// Whenever the two overlap lengthways they must be side by side, never one inside the other.
			var narrowest = float.MaxValue;
			var slowest = float.MaxValue;
			var end = Time.realtimeSinceStartup + 7f;
			while (Time.realtimeSinceStartup < end)
			{
				var offset = slow.transform.position - fast.transform.position;
				if (Mathf.Abs(offset.x) < 2.4f) narrowest = Mathf.Min(narrowest, Mathf.Abs(offset.z));
				if (offset.x < 14f) slowest = Mathf.Min(slowest, fast.Horse.Speed);
				yield return null;
			}

			Assert.Greater(fast.Horse.MaxCloseness, 0f, "it did catch up and feel the other horse");
			Assert.Greater(narrowest, 0.9f, "the two were never on top of each other (narrowest side gap " + narrowest + " m)");
			Assert.Less(slowest, 18f, "the chaser had to ease off when it came up behind");

			slow.Horse.Locked = true;
			fast.Horse.Locked = true;
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator Horses_side_by_side_slow_down_and_move_apart()
		{
			yield return Load();

			var a = _race.Racers[1];
			var b = _race.Racers[2];
			Ride(a, new Vector3(0f, Ground, 100f), 90f, 1f);
			Ride(b, new Vector3(0f, Ground, 100.9f), 90f, 1f);

			yield return new WaitForSeconds(3f);

			var gap = Mathf.Abs(a.transform.position.z - b.transform.position.z);
			Assert.Greater(gap, 1.6f, "they were pushed apart");
			Assert.Greater(a.Horse.MaxCloseness, 0.5f, "they felt each other first");
			Assert.Greater(b.Horse.MaxCloseness, 0.5f);

			a.Horse.Locked = true;
			b.Horse.Locked = true;
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator Hitting_a_wall_slows_the_horse()
		{
			yield return Load();

			var racer = _race.Racers[1];
			var driver = Ride(racer, new Vector3(-45f, Ground, -38f), 90f, 1f);
			driver.Steer = 0.35f; // drifts toward the outer rail on the bottom straight

			var before = 0f;
			var deadline = Time.realtimeSinceStartup + 8f;
			while (racer.Horse.WallHits == 0 && Time.realtimeSinceStartup < deadline)
			{
				before = racer.Horse.Speed;
				yield return null;
			}
			Assert.Greater(racer.Horse.WallHits, 0, "the horse reached the rail");
			Assert.Greater(before, 10f, "at a gallop");

			driver.Steer = 0f;
			yield return new WaitForSeconds(0.2f);
			Assert.Less(racer.Horse.Speed, before * 0.85f, "the rail took speed away");

			racer.Horse.Locked = true;
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator A_jump_clears_the_bar_and_without_one_the_horse_stumbles()
		{
			yield return Load();

			var hurdle = HurdleNear(new Vector3(-30f, 0f, -38f));
			Assert.IsNotNull(hurdle, "the scene has a hurdle");
			var racer = _race.Racers[1];
			var start = new Vector3(-58f, Ground, -38f);

			// Jumping at the right moment clears the bar.
			var driver = Ride(racer, start, 90f, 1f);
			driver.JumpAt = hurdle;
			var deadline = Time.realtimeSinceStartup + 15f;
			while (racer.transform.position.x < -15f && Time.realtimeSinceStartup < deadline) yield return null;
			Assert.AreEqual(1, racer.Horse.BarsCleared, "bar cleared");
			Assert.AreEqual(0, racer.Horse.BarsHit);
			Assert.AreEqual(1, racer.Horse.Jumps);
			Assert.AreEqual(0, racer.Horse.WallHits, "and the gap was wide enough to ride through");
			Assert.Greater(racer.Horse.Speed, 15f, "no speed lost");

			// Not jumping: the bar is hit and most of the speed is lost.
			driver = Ride(racer, start, 90f, 1f);
			var peak = 0f;
			deadline = Time.realtimeSinceStartup + 15f;
			while (racer.Horse.BarsHit == 0 && Time.realtimeSinceStartup < deadline)
			{
				peak = Mathf.Max(peak, racer.Horse.Speed);
				yield return null;
			}
			Assert.AreEqual(1, racer.Horse.BarsHit, "bar hit");
			Assert.AreEqual(0, racer.Horse.BarsCleared);
			Assert.Less(racer.Horse.Speed, peak * 0.6f, "the stumble cost most of the speed");

			racer.Horse.Locked = true;
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator A_boost_makes_the_horse_faster_for_a_moment_and_costs_stamina()
		{
			yield return Load();

			var racer = _race.Racers[1];
			var driver = Ride(racer, new Vector3(0f, Ground, 120f), 90f, 1f);

			yield return new WaitForSeconds(3.5f); // up to full speed
			var normal = racer.Horse.Speed;
			Assert.AreEqual(racer.Horse.MaxSpeed, normal, 0.6f);

			driver.BoostNow = true;
			yield return new WaitForSeconds(1.1f);
			Assert.Greater(racer.Horse.Speed, normal * 1.1f, "faster during the burst");
			Assert.Less(racer.Horse.Stamina.Value, 1f, "and it cost stamina");

			racer.Horse.Locked = true;
		}

		[UnityTest]
		[Timeout(300000)]
		public IEnumerator Riders_in_a_real_race_jump_the_bars_and_do_not_ride_the_rails()
		{
			yield return Load();
			_race.PlayerAutopilot = true;
			_race.Laps = 2;

			_race.StartRace();
			Time.timeScale = 6f;
			var deadline = Time.realtimeSinceStartup + 240f;
			while (!_race.Rules.AllFinished && Time.realtimeSinceStartup < deadline) yield return null;
			Time.timeScale = 1f;

			Assert.IsTrue(_race.Rules.AllFinished, "everybody finished");

			var cleared = 0;
			var hit = 0;
			foreach (var racer in _race.Racers)
			{
				cleared += racer.Horse.BarsCleared;
				hit += racer.Horse.BarsHit;
			}
			// 4 riders x 2 laps x 2 bars
			Assert.GreaterOrEqual(cleared + hit, 14, "nearly every bar was crossed on the way");
			Assert.Greater(cleared, hit, "most were jumped cleanly");

			foreach (var racer in _race.Racers)
				Assert.Less(racer.Horse.WallHits, 20, racer.DisplayName + " did not ride along the rails");
		}
	}
}
