using System;
using System.Collections;
using ITKamianets.Engine.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Hippodrome.Tests
{
	/// <summary>
	/// The real track with this machine as the host and a second player (a detached session in the same process) as the guest:
	/// the guest's horse is shown where the guest says, the computer riders are published, and the host's start reaches the guest.
	/// </summary>
	public class OnlineRaceTests
	{
		NetSession _guest;

		static IEnumerator Until(Func<bool> condition, string what, float seconds = 10f)
		{
			var end = Time.realtimeSinceStartup + seconds;
			while (!condition())
			{
				if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
				yield return null;
			}
		}

		[TearDown]
		public void TearDown()
		{
			Time.timeScale = 1f;
			if (_guest != null) { _guest.Leave(); UnityEngine.Object.Destroy(_guest.gameObject); }
			NetSession.Instance.Leave();
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator A_guest_rides_on_the_hosts_track_and_the_start_reaches_them()
		{
			var host = NetSession.Instance;
			host.Leave();
			host.GameId = "hippodrome";
			host.GameVersion = Application.version;
			host.LocalName = "Hosty";
			host.MaxPlayers = 4;
			Assert.IsTrue(host.HostDirect(7795), host.LastError);

			_guest = NetSession.CreateDetached();
			_guest.GameId = "hippodrome";
			_guest.GameVersion = Application.version;
			_guest.LocalName = "Guesty";
			_guest.LocalColour = 3;
			Assert.IsTrue(_guest.JoinDirect("127.0.0.1:7795"), _guest.LastError);
			yield return Until(() => host.Players.Count == 2 && _guest.Players.Count == 2 && !_guest.Connecting, "the guest to join");

			string started = null;
			_guest.On("hip.start", (who, text) => started = "yes");

			yield return SceneManager.LoadSceneAsync("Main");
			yield return null;

			var race = UnityEngine.Object.FindAnyObjectByType<RaceController>();
			race.SaveEnabled = false;
			Assert.IsNotNull(race.Net, "an online race has the link to other players");
			Assert.IsFalse(race.PacingAssist, "no catch-up pacing against people");
			Assert.AreEqual("Guesty", race.Racers[1].DisplayName);
			Assert.IsTrue(race.Racers[1].Horse.Remote, "the guest's horse is only shown here");
			Assert.IsFalse(race.Racers[0].Horse.Remote);
			Assert.IsFalse(race.Racers[2].Horse.Remote, "the host simulates the computer riders");
			Assert.AreEqual(0, race.Net.SlotOf(0), "the host starts in the first slot");
			Assert.AreEqual(1, race.Net.SlotOf(1), "the guest in the second");
			Assert.AreEqual(2, race.Net.SlotOf(2));

			// The guest says where they are.
			var target = new Vector3(12f, 0.5f, -7f);
			for (var i = 0; i < 6; i++)
			{
				_guest.PublishPose(new NetPose { Position = target, Yaw = 40f, Speed = 9f, State = 1, A = 0.8f, B = 0.4f });
				yield return new WaitForSeconds(0.1f);
			}
			yield return Until(() => (race.Racers[1].transform.position - target).magnitude < 0.5f, "the guest's horse to be shown");
			Assert.AreEqual(9f, race.Racers[1].Horse.Speed, 0.01f);
			Assert.IsTrue(race.Racers[1].Horse.Airborne);

			// The guest has loaded the track; the host starts the race and the guest hears of it.
			Assert.IsFalse(race.Net.EveryoneHere);
			_guest.Send("hip.here", "");
			yield return Until(() => race.Net.EveryoneHere, "the guest to be counted as loaded");
			race.RequestStart();
			Assert.AreEqual(RacePhase.Countdown, race.Phase);
			yield return Until(() => started != null, "the guest to be told to start");

			// The computer riders are published to the guest, in the slots the guest's own grid uses.
			yield return Until(() => _guest.Entities.Count >= 2, "the computer riders' states");
			Assert.IsTrue(_guest.Entities.ContainsKey(2));
			Assert.IsTrue(_guest.Entities.ContainsKey(3));
		}
	}
}
