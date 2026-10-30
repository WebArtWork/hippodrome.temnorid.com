using System.Collections.Generic;
using ITKamianets.Engine.Core;
using ITKamianets.Engine.Input;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	public enum RacePhase
	{
		/// <summary>Start screen: the horses stand on the grid.</summary>
		Menu,
		Countdown,
		Racing,
		Paused,
		/// <summary>The player has crossed the line; the others may still be riding in.</summary>
		Finished,
	}

	/// <summary>
	/// Runs one race: puts the racers on the grid, counts down, lets them go, feeds checkpoints into
	/// <see cref="RaceRules"/>, ends the race when the player finishes and keeps the player's records.
	/// </summary>
	public sealed class RaceController : MonoBehaviour
	{
		[Header("Scene")]
		public Racer[] Racers;
		public Checkpoint[] Checkpoints;
		public Transform[] Waypoints;
		public Transform[] GridSlots;

		[Header("Race")]
		public int Laps = 3;
		public float CountdownSeconds = 3f;

		[Header("Pacing")]
		[Tooltip("Computer riders quietly ease or push a few per cent so the race stays close. Only for races against computer riders: a race against other people must turn this off.")]
		public bool PacingAssist = true;

		[Header("Testing")]
		[Tooltip("The player's horse rides itself, like a bot. Used by automated tests.")]
		public bool PlayerAutopilot;
		[Tooltip("Write records to the save file when the player finishes. Tests turn this off.")]
		public bool SaveEnabled = true;

		const string Log = "Hippodrome";

		RacePhase _phase = RacePhase.Menu;
		float _countdown;
		float _timeScaleBeforePause = 1f;
		RecordsStore _store;

		// Distance along the track, kept for every racer so the race can tell who really leads.
		float[] _cumulative;
		float _perimeter;
		int[] _segment;
		int[] _laps;
		float[] _distance;

		public RacePhase Phase => _phase;
		public float CountdownRemaining => _countdown;
		public float RaceTime { get; private set; }
		public RaceRules Rules { get; private set; }

		/// <summary>Racer indexes from first to last, kept up to date during the race.</summary>
		public int[] Order { get; private set; } = new int[0];

		public HippodromeRecords Records { get; private set; } = new HippodromeRecords();
		public bool NewBestLap { get; private set; }
		public bool NewBestTotal { get; private set; }

		/// <summary>Metres ridden along the track by a racer, counting laps. Index 0 is the player.</summary>
		public float DistanceOf(int racer) => _distance != null && racer < _distance.Length ? _distance[racer] : 0f;

		public Racer Player => Racers != null && Racers.Length > 0 ? Racers[0] : null;

		/// <summary>The player's place right now, 1 for first.</summary>
		public int PlayerPlace
		{
			get
			{
				for (var i = 0; i < Order.Length; i++) if (Order[i] == 0) return i + 1;
				return 1;
			}
		}

		void Awake()
		{
			_store = new RecordsStore();
			Records = _store.Load();

			foreach (var checkpoint in Checkpoints) checkpoint.Race = this;

			var path = Waypoints;
			for (var i = 0; i < Racers.Length; i++)
			{
				var racer = Racers[i];
				racer.Index = i;
				racer.Horse.Locked = true;
				if (racer.BotDriver != null) racer.BotDriver.SetPath(path);
			}

			// Horses race each other, they do not bump each other off the track.
			for (var i = 0; i < Racers.Length; i++)
			{
				for (var j = i + 1; j < Racers.Length; j++)
				{
					foreach (var a in Racers[i].GetComponentsInChildren<Collider>())
						foreach (var b in Racers[j].GetComponentsInChildren<Collider>())
							Physics.IgnoreCollision(a, b);
				}
			}

			BuildPathLengths();
			PrepareGrid();
		}

		void FixedUpdate()
		{
			if (_phase != RacePhase.Racing && _phase != RacePhase.Finished) return;
			UpdateProgress();
			if (_phase == RacePhase.Racing) ApplyPacing(Time.fixedDeltaTime);
		}

		void Update()
		{
			var input = EngineInput.Default;

			switch (_phase)
			{
				case RacePhase.Countdown:
					_countdown -= Time.deltaTime;
					if (_countdown <= 0f) Go();
					break;

				case RacePhase.Racing:
					RaceTime += Time.deltaTime;
					UpdateOrder();
					if (input.UI.Pause.WasPressedThisFrame()) Pause();
					break;

				case RacePhase.Paused:
					if (input.UI.Pause.WasPressedThisFrame()) Resume();
					break;

				case RacePhase.Finished:
					RaceTime += Time.deltaTime;
					UpdateOrder();
					break;
			}
		}

		/// <summary>Starts a new race from the grid. Also used to race again.</summary>
		public void StartRace()
		{
			Time.timeScale = 1f;
			PrepareGrid();
			_countdown = CountdownSeconds;
			_phase = RacePhase.Countdown;
		}

		public void Pause()
		{
			if (_phase != RacePhase.Racing) return;
			_timeScaleBeforePause = Time.timeScale;
			Time.timeScale = 0f;
			_phase = RacePhase.Paused;
		}

		public void Resume()
		{
			if (_phase != RacePhase.Paused) return;
			Time.timeScale = _timeScaleBeforePause;
			_phase = RacePhase.Racing;
		}

		/// <summary>Back to the start screen, horses on the grid.</summary>
		public void BackToMenu()
		{
			Time.timeScale = 1f;
			PrepareGrid();
			_phase = RacePhase.Menu;
		}

		public void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		/// <summary>Called by a checkpoint when a racer rides through it.</summary>
		public void OnCheckpoint(int racer, int checkpoint)
		{
			if (Rules == null || (_phase != RacePhase.Racing && _phase != RacePhase.Finished)) return;
			if (!Rules.Reach(racer, checkpoint, RaceTime)) return;

			if (racer == 0 && Rules.Racers[0].Finished) PlayerFinished();
		}

		void PrepareGrid()
		{
			var names = new List<string>();
			foreach (var racer in Racers) names.Add(racer.DisplayName);
			Rules = new RaceRules(Checkpoints.Length, Laps, names);
			RaceTime = 0f;
			NewBestLap = false;
			NewBestTotal = false;

			for (var i = 0; i < Racers.Length; i++)
			{
				var racer = Racers[i];
				var slot = GridSlots[i % GridSlots.Length];
				racer.Horse.Locked = true;
				racer.Horse.ResetRace();
				racer.Horse.Teleport(slot.position, slot.rotation);
				racer.Horse.Driver = DriverFor(racer);
				if (racer.BotDriver != null) racer.BotDriver.enabled = racer.Horse.Driver == racer.BotDriver;
				if (racer.PlayerDriver != null) racer.PlayerDriver.enabled = racer.Horse.Driver == racer.PlayerDriver;
			}
			StartProgress();
			UpdateOrder();
		}

		HorseDriver DriverFor(Racer racer)
		{
			if (racer.IsPlayer && !PlayerAutopilot) return racer.PlayerDriver;
			return racer.BotDriver;
		}

		void Go()
		{
			_countdown = 0f;
			_phase = RacePhase.Racing;
			RaceTime = 0f;
			foreach (var racer in Racers)
			{
				racer.Horse.Locked = false;
				if (racer.BotDriver != null && racer.BotDriver.enabled) racer.BotDriver.SetPath(Waypoints);
			}
		}

		// ---- progress and pacing ----

		void BuildPathLengths()
		{
			var n = Waypoints.Length;
			_cumulative = new float[n + 1];
			for (var i = 0; i < n; i++)
			{
				var a = Waypoints[i].position;
				var b = Waypoints[(i + 1) % n].position;
				_cumulative[i + 1] = _cumulative[i] + Flat(b - a).magnitude;
			}
			_perimeter = _cumulative[n];
			_segment = new int[Racers.Length];
			_laps = new int[Racers.Length];
			_distance = new float[Racers.Length];
		}

		static Vector3 Flat(Vector3 v)
		{
			v.y = 0f;
			return v;
		}

		void StartProgress()
		{
			if (_cumulative == null) return;
			for (var i = 0; i < Racers.Length; i++)
			{
				_segment[i] = NearestSegment(Racers[i].transform.position, -1, out var along);
				_laps[i] = 0;
				_distance[i] = along;
			}
		}

		void UpdateProgress()
		{
			if (_cumulative == null) return;
			var n = Waypoints.Length;
			for (var i = 0; i < Racers.Length; i++)
			{
				var previous = _segment[i];
				var segment = NearestSegment(Racers[i].transform.position, previous, out var along);

				// Crossing the line where the path starts over adds or removes a lap.
				if (segment < previous - n / 2) _laps[i]++;
				else if (segment > previous + n / 2) _laps[i]--;

				_segment[i] = segment;
				_distance[i] = _laps[i] * _perimeter + along;
			}
		}

		// The segment of the path closest to a position. With a previous segment, only the ones near it are looked at.
		int NearestSegment(Vector3 position, int near, out float along)
		{
			var n = Waypoints.Length;
			var best = 0;
			var bestDistance = float.MaxValue;
			var bestAlong = 0f;
			var from = near < 0 ? 0 : -3;
			var to = near < 0 ? n - 1 : 6;

			for (var k = from; k <= to; k++)
			{
				var i = near < 0 ? k : ((near + k) % n + n) % n;
				var a = Waypoints[i].position;
				var b = Waypoints[(i + 1) % n].position;
				var ab = Flat(b - a);
				var length = ab.magnitude;
				var t = length > 0.001f ? Mathf.Clamp01(Vector3.Dot(Flat(position - a), ab) / (length * length)) : 0f;
				var closest = a + ab * t;
				var distance = Flat(position - closest).sqrMagnitude;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
					bestAlong = _cumulative[i] + t * length;
				}
			}
			along = bestAlong;
			return best;
		}

		/// <summary>
		/// Keeps the race close for a single player: riders behind the player chase up to a short distance behind it,
		/// riders ahead of the player ease in proportion to their lead. A few per cent, changing slowly.
		/// </summary>
		void ApplyPacing(float dt)
		{
			if (_distance == null || Racers.Length < 2) return;

			for (var i = 1; i < Racers.Length; i++)
			{
				var horse = Racers[i].Horse;
				var target = PacingAssist ? RacePacing.Target(_distance[0] - _distance[i]) : 1f;
				horse.SpeedScale = RacePacing.Step(horse.SpeedScale, target, dt);
			}
		}

		void UpdateOrder()
		{
			if (Rules == null) return;
			Order = Rules.Ranking(i =>
			{
				var next = Checkpoints[Rules.Racers[i].NextCheckpoint].transform.position;
				return (Racers[i].transform.position - next).magnitude;
			});
		}

		void PlayerFinished()
		{
			_phase = RacePhase.Finished;
			var progress = Rules.Racers[0];

			// Ride on after the line instead of stopping dead.
			var player = Racers[0];
			if (player.BotDriver != null)
			{
				player.BotDriver.enabled = true;
				player.Horse.Driver = player.BotDriver;
			}

			Records.races++;
			if (progress.FinishPlace == 1) Records.wins++;
			if (progress.BestLap > 0f && (Records.bestLapSeconds <= 0f || progress.BestLap < Records.bestLapSeconds))
			{
				Records.bestLapSeconds = progress.BestLap;
				NewBestLap = true;
			}
			if (Records.bestTotalSeconds <= 0f || progress.FinishTime < Records.bestTotalSeconds)
			{
				Records.bestTotalSeconds = progress.FinishTime;
				NewBestTotal = true;
			}
			if (SaveEnabled) _store.Save(Records);

			EngineLog.Info(Log, $"Player finished {progress.FinishPlace} in {progress.FinishTime:0.00}s, best lap {progress.BestLap:0.00}s.");
		}
	}
}
