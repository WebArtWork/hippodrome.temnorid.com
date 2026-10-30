using System;
using System.Collections.Generic;

namespace Temnorid.Hippodrome
{
	/// <summary>One racer's progress through the race.</summary>
	public sealed class RacerProgress
	{
		public readonly string Name;

		/// <summary>The checkpoint this racer must reach next. Checkpoint 0 is the finish line.</summary>
		public int NextCheckpoint = 1;

		/// <summary>Laps completed.</summary>
		public int Lap;

		/// <summary>Valid checkpoints passed in the whole race: the measure of how far ahead a racer is.</summary>
		public int CheckpointsPassed;

		public bool Finished;
		public float FinishTime;

		/// <summary>1 for the winner, 0 until the racer has finished.</summary>
		public int FinishPlace;

		/// <summary>Fastest completed lap in seconds, 0 until one is completed.</summary>
		public float BestLap;

		public float LapStart;
		public readonly List<float> LapTimes = new List<float>();

		public RacerProgress(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// The rules of a race, with no Unity in them: which checkpoint counts, when a lap is done,
	/// who has finished, and the running order.
	///
	/// Checkpoints are numbered around the track, and 0 is the finish line. Racers start just past
	/// the line, so the first one they need is 1; a lap is complete when they cross 0 again having
	/// passed every checkpoint in order. Reaching a checkpoint out of order (a shortcut, riding
	/// backwards) does nothing.
	/// </summary>
	public sealed class RaceRules
	{
		readonly List<RacerProgress> _racers = new List<RacerProgress>();
		int _finishedCount;

		public int CheckpointCount { get; }
		public int Laps { get; }
		public IReadOnlyList<RacerProgress> Racers => _racers;
		public bool AllFinished => _finishedCount == _racers.Count;

		public RaceRules(int checkpointCount, int laps, IList<string> names)
		{
			if (checkpointCount < 2) throw new ArgumentException("a track needs at least two checkpoints", nameof(checkpointCount));
			if (laps < 1) throw new ArgumentException("a race needs at least one lap", nameof(laps));
			CheckpointCount = checkpointCount;
			Laps = laps;
			foreach (var name in names) _racers.Add(new RacerProgress(name));
		}

		/// <summary>
		/// A racer touched a checkpoint at <paramref name="time"/> (seconds since the start).
		/// Returns true when it counted.
		/// </summary>
		public bool Reach(int racer, int checkpoint, float time)
		{
			var progress = _racers[racer];
			if (progress.Finished || checkpoint != progress.NextCheckpoint) return false;

			progress.CheckpointsPassed++;
			progress.NextCheckpoint = (checkpoint + 1) % CheckpointCount;

			if (checkpoint == 0)
			{
				var lapTime = time - progress.LapStart;
				progress.LapStart = time;
				progress.Lap++;
				progress.LapTimes.Add(lapTime);
				if (progress.BestLap <= 0f || lapTime < progress.BestLap) progress.BestLap = lapTime;

				if (progress.Lap >= Laps)
				{
					progress.Finished = true;
					progress.FinishTime = time;
					progress.FinishPlace = ++_finishedCount;
				}
			}
			return true;
		}

		/// <summary>
		/// Racer indexes from first to last. Finished racers come first in the order they finished.
		/// The others are ordered by checkpoints passed, then by how close they are to the next
		/// one (<paramref name="distanceToNext"/>), and finally by index, so the order is always
		/// the same for the same situation.
		/// </summary>
		public int[] Ranking(Func<int, float> distanceToNext)
		{
			var order = new List<int>();
			for (var i = 0; i < _racers.Count; i++) order.Add(i);

			order.Sort((a, b) =>
			{
				var ra = _racers[a];
				var rb = _racers[b];
				if (ra.Finished != rb.Finished) return ra.Finished ? -1 : 1;
				if (ra.Finished) return ra.FinishPlace.CompareTo(rb.FinishPlace);
				if (ra.CheckpointsPassed != rb.CheckpointsPassed) return rb.CheckpointsPassed.CompareTo(ra.CheckpointsPassed);
				var byDistance = distanceToNext(a).CompareTo(distanceToNext(b));
				return byDistance != 0 ? byDistance : a.CompareTo(b);
			});
			return order.ToArray();
		}
	}
}
