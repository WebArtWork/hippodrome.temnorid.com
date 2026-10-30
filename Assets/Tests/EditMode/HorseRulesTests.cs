using NUnit.Framework;

namespace Temnorid.Hippodrome.Tests
{
	public class HorseRulesTests
	{
		static void Rest(HorseStamina stamina, float seconds)
		{
			for (var t = 0f; t < seconds; t += 0.02f) stamina.Tick(0.02f, false);
		}

		// ---- boost and stamina ----

		[Test]
		public void A_rested_horse_answers_the_boost_key_with_a_burst_then_needs_a_rest()
		{
			var stamina = new HorseStamina();
			Assert.IsTrue(stamina.Ready);
			Assert.AreEqual(BoostResult.Started, stamina.PressBoost());
			Assert.IsTrue(stamina.Boosting);
			Assert.Less(stamina.Value, 1f, "a burst costs stamina");

			Rest(stamina, HorseStamina.BoostDuration + 0.1f);
			Assert.IsFalse(stamina.Boosting, "the burst ends");
			Assert.IsFalse(stamina.Ready, "and the horse has to rest before the next");

			Rest(stamina, HorseStamina.BoostCooldown);
			Assert.IsTrue(stamina.Ready);
		}

		[Test]
		public void Pressing_too_soon_gives_no_speed_and_tires_the_horse()
		{
			var stamina = new HorseStamina();
			stamina.PressBoost();
			Rest(stamina, 2f);
			var before = stamina.Value;
			Assert.AreEqual(BoostResult.Strained, stamina.PressBoost());
			Assert.AreEqual(before - HorseStamina.StrainCost, stamina.Value, 0.001f);
			Assert.IsFalse(stamina.Boosting);
		}

		[Test]
		public void Pressing_all_the_time_exhausts_the_horse_which_then_slows_and_ignores_the_key()
		{
			var stamina = new HorseStamina();
			for (var i = 0; i < 40 && !stamina.Exhausted; i++)
			{
				stamina.PressBoost();
				Rest(stamina, 0.25f);
			}
			Assert.IsTrue(stamina.Exhausted, "mashing the key wears the horse out");
			Assert.Less(stamina.PaceFactor, 0.9f, "an exhausted horse runs slower");
			Assert.AreEqual(BoostResult.Ignored, stamina.PressBoost());
			Assert.IsFalse(stamina.Boosting, "and gets no burst at all");

			Rest(stamina, 20f);
			Assert.IsFalse(stamina.Exhausted, "it catches its breath in time");
			Assert.AreEqual(BoostResult.Started, stamina.PressBoost());
		}

		[Test]
		public void Using_the_boost_each_time_it_is_ready_can_go_on_all_race()
		{
			var stamina = new HorseStamina();
			var bursts = 0;
			for (var t = 0f; t < 120f; t += 0.02f)
			{
				if (stamina.Ready && stamina.PressBoost() == BoostResult.Started) bursts++;
				stamina.Tick(0.02f, true);
			}
			Assert.GreaterOrEqual(bursts, 30);
			Assert.IsFalse(stamina.Exhausted);
			Assert.Greater(stamina.Value, 0.4f, "normal use does not wear the horse out");
		}

		[Test]
		public void A_tired_horse_is_slower_a_fresh_one_is_not()
		{
			var stamina = new HorseStamina();
			Assert.AreEqual(1f, stamina.PaceFactor, 0.0001f);
			for (var i = 0; i < 3; i++) { stamina.PressBoost(); stamina.PressBoost(); }
			Assert.Less(stamina.Value, HorseStamina.TiredBelow);
			Assert.Less(stamina.PaceFactor, 1f);
			Assert.GreaterOrEqual(stamina.PaceFactor, HorseStamina.WornOutPace);
		}

		// ---- crowding and walls ----

		[Test]
		public void Horses_far_apart_do_not_feel_each_other()
		{
			Assert.AreEqual(0f, HorsePhysics.ContactDeceleration(6f, 0f), 0.0001f);
			Assert.AreEqual(0f, HorsePhysics.ContactDeceleration(0f, 12f), 0.0001f);
			Assert.AreEqual(0f, HorsePhysics.ContactPush(6f, 0f), 0.0001f);
		}

		[Test]
		public void A_horse_close_behind_another_is_braked_harder_than_one_beside_it()
		{
			var behind = HorsePhysics.ContactDeceleration(0f, 2.5f); // the other is 2.5 m dead ahead
			var beside = HorsePhysics.ContactDeceleration(1.6f, 0f);  // the other is alongside
			Assert.Greater(behind, 0f);
			Assert.Greater(beside, 0f);
			Assert.Greater(behind, beside);
		}

		[Test]
		public void A_horse_catching_another_in_its_lane_brakes_in_time_to_match_its_speed()
		{
			Assert.AreEqual(0f, HorsePhysics.FollowDeceleration(0f, 30f, 10f), 0.0001f, "too far ahead to matter");
			Assert.AreEqual(0f, HorsePhysics.FollowDeceleration(3f, 6f, 10f), 0.0001f, "in another lane");
			Assert.AreEqual(0f, HorsePhysics.FollowDeceleration(0f, 6f, -2f), 0.0001f, "the other is pulling away");

			var far = HorsePhysics.FollowDeceleration(0f, 12f, 10f);
			var near = HorsePhysics.FollowDeceleration(0f, 5f, 10f);
			Assert.Greater(far, 0f);
			Assert.Greater(near, far, "the nearer the gap, the harder it brakes");
			Assert.Greater(HorsePhysics.FollowDeceleration(0f, 5f, 12f), near, "and the faster it is closing, the harder");

			// Braking at that rate from the start of the gap stops the closing exactly at the following distance.
			var gap = 8f;
			var stopsIn = 10f * 10f / (2f * HorsePhysics.FollowDeceleration(0f, gap + HorsePhysics.BodyLength, 10f));
			Assert.AreEqual(gap - HorsePhysics.FollowGap, stopsIn, 0.01f);
		}

		[Test]
		public void The_closer_the_harder_and_touching_horses_are_pushed_apart()
		{
			Assert.Greater(HorsePhysics.ContactDeceleration(0f, 1f), HorsePhysics.ContactDeceleration(0f, 3f));
			Assert.Less(HorsePhysics.ContactPush(0.5f, 0f), 0f, "the other is on the right, so this horse goes left");
			Assert.Greater(HorsePhysics.ContactPush(-0.5f, 0f), 0f);
		}

		[Test]
		public void A_wall_takes_speed_away_head_on_and_while_scraping()
		{
			var head = HorsePhysics.AfterWallImpact(20f, 20f);
			var glance = HorsePhysics.AfterWallImpact(20f, 2f);
			Assert.Less(head, 20f);
			Assert.Less(head, glance, "a head-on hit costs more than a glancing one");
			Assert.Greater(glance, 0f);
			Assert.Less(HorsePhysics.AfterWallScrape(20f, 1f), 20f);
			Assert.GreaterOrEqual(HorsePhysics.AfterWallScrape(20f, 10f), 0f);
		}

		// ---- pacing ----

		[Test]
		public void The_player_in_front_makes_the_others_a_little_faster_never_more_than_a_few_per_cent()
		{
			Assert.AreEqual(1f, RacePacing.Target(true, 0f, false), 0.0001f);
			var far = RacePacing.Target(true, 500f, false);
			Assert.Greater(far, 1f);
			Assert.LessOrEqual(far, 1f + RacePacing.MaxChange + 0.0001f);
			Assert.Greater(RacePacing.Target(true, 40f, false), RacePacing.Target(true, 10f, false));
		}

		[Test]
		public void The_player_behind_slows_only_the_leader_a_little()
		{
			Assert.Less(RacePacing.Target(false, -30f, true), 1f);
			Assert.GreaterOrEqual(RacePacing.Target(false, -500f, true), 1f - RacePacing.MaxChange - 0.0001f);
			Assert.AreEqual(1f, RacePacing.Target(false, -30f, false), 0.0001f, "the others ride as they are");
		}

		[Test]
		public void The_pace_changes_slowly()
		{
			var scale = 1f;
			for (var t = 0f; t < 1f; t += 0.02f) scale = RacePacing.Step(scale, 1.06f, 0.02f);
			Assert.Less(scale, 1.03f, "one second moves it by a fraction of a per cent");
			for (var t = 0f; t < 10f; t += 0.02f) scale = RacePacing.Step(scale, 1.06f, 0.02f);
			Assert.AreEqual(1.06f, scale, 0.0001f);
		}
	}
}
