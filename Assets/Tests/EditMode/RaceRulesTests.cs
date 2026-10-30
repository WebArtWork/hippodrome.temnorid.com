using System.IO;
using NUnit.Framework;

namespace Temnorid.Hippodrome.Tests
{
	public class RaceRulesTests
	{
		static readonly string[] Names = { "You", "Ember", "Willow" };

		// Rides one full lap of a 4-checkpoint track in order, 10 seconds apart, starting at `start`.
		static void RideLap(RaceRules rules, int racer, float start)
		{
			Assert.IsTrue(rules.Reach(racer, 1, start + 2f));
			Assert.IsTrue(rules.Reach(racer, 2, start + 5f));
			Assert.IsTrue(rules.Reach(racer, 3, start + 8f));
			Assert.IsTrue(rules.Reach(racer, 0, start + 10f));
		}

		[Test]
		public void A_lap_counts_only_when_every_checkpoint_is_passed_in_order()
		{
			var rules = new RaceRules(4, 3, Names);
			RideLap(rules, 0, 0f);
			Assert.AreEqual(1, rules.Racers[0].Lap);
			Assert.AreEqual(10f, rules.Racers[0].BestLap, 0.001f);
		}

		[Test]
		public void A_shortcut_or_the_wrong_way_does_not_count()
		{
			var rules = new RaceRules(4, 3, Names);
			Assert.IsFalse(rules.Reach(0, 3, 1f), "skipping ahead");
			Assert.IsFalse(rules.Reach(0, 0, 2f), "the finish line before a lap");
			Assert.IsTrue(rules.Reach(0, 1, 3f));
			Assert.IsFalse(rules.Reach(0, 1, 3.1f), "the same gate twice");
			Assert.IsFalse(rules.Reach(0, 0, 4f), "back over the line");
			Assert.AreEqual(0, rules.Racers[0].Lap);
		}

		[Test]
		public void Finishing_the_last_lap_gives_places_in_order_of_arrival()
		{
			var rules = new RaceRules(4, 2, Names);
			for (var lap = 0; lap < 2; lap++) RideLap(rules, 1, lap * 10f);
			for (var lap = 0; lap < 2; lap++) RideLap(rules, 0, 3f + lap * 10f);

			Assert.IsTrue(rules.Racers[1].Finished);
			Assert.AreEqual(1, rules.Racers[1].FinishPlace);
			Assert.AreEqual(20f, rules.Racers[1].FinishTime, 0.001f);
			Assert.AreEqual(2, rules.Racers[0].FinishPlace);
			Assert.IsFalse(rules.AllFinished);
			Assert.IsFalse(rules.Reach(1, 1, 99f), "a finished racer no longer scores");
		}

		[Test]
		public void The_order_puts_finished_racers_first_then_the_furthest_ahead()
		{
			var rules = new RaceRules(4, 1, Names);
			rules.Reach(0, 1, 1f);
			rules.Reach(0, 2, 2f);
			rules.Reach(1, 1, 1f);

			// Racer 2 has passed nothing. Racer 0 is two gates in, racer 1 only one.
			var order = rules.Ranking(i => 10f);
			CollectionAssert.AreEqual(new[] { 0, 1, 2 }, order);

			RideLap(rules, 2, 0f);
			order = rules.Ranking(i => 10f);
			Assert.AreEqual(2, order[0], "the finished racer leads");
		}

		[Test]
		public void Racers_on_the_same_gate_are_ordered_by_distance_then_by_index()
		{
			var rules = new RaceRules(4, 3, Names);
			var distances = new[] { 30f, 12f, 12f };
			var order = rules.Ranking(i => distances[i]);
			CollectionAssert.AreEqual(new[] { 1, 2, 0 }, order);
		}

		[Test]
		public void Records_survive_a_restart_and_a_damaged_file_is_replaced_by_empty_ones()
		{
			var folder = Path.Combine(Path.GetTempPath(), "hippodrome-test-" + System.Guid.NewGuid());
			try
			{
				var store = new RecordsStore(folder);
				Assert.AreEqual(0, store.Load().races, "nothing saved yet");

				store.Save(new HippodromeRecords { bestLapSeconds = 21.5f, bestTotalSeconds = 66f, wins = 2, races = 5 });
				var loaded = new RecordsStore(folder).Load();
				Assert.AreEqual(21.5f, loaded.bestLapSeconds, 0.001f);
				Assert.AreEqual(5, loaded.races);

				File.WriteAllText(Path.Combine(folder, "hippodrome", "default.json"), "{ not json");
				Assert.AreEqual(0, new RecordsStore(folder).Load().races, "damaged file: start fresh");
				Assert.AreEqual(1, Directory.GetFiles(Path.Combine(folder, "hippodrome"), "default.corrupt-*.json").Length, "the damaged file is kept");
			}
			finally
			{
				if (Directory.Exists(folder)) Directory.Delete(folder, true);
			}
		}
	}
}
