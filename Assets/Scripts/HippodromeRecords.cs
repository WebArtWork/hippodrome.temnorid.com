using System;
using ITKamianets.Engine.Save;

namespace Temnorid.Hippodrome
{
	/// <summary>What is kept between sessions. A value of 0 for a time means none yet.</summary>
	[Serializable]
	public sealed class HippodromeRecords
	{
		public float bestLapSeconds;
		public float bestTotalSeconds;
		public int wins;
		public int races;
	}

	/// <summary>Reads and writes <see cref="HippodromeRecords"/> in the game's save slot.</summary>
	public sealed class RecordsStore
	{
		const string Slot = "default";
		const int PayloadVersion = 1;

		readonly SaveStore _store;

		public RecordsStore(string rootDirectory = null)
		{
			_store = new SaveStore("hippodrome", rootDirectory);
		}

		public HippodromeRecords Load()
		{
			var result = _store.Load<HippodromeRecords>(Slot, PayloadVersion);
			return result.Ok ? result.Value : new HippodromeRecords();
		}

		public void Save(HippodromeRecords records)
		{
			_store.Save(Slot, records, PayloadVersion);
		}
	}
}
