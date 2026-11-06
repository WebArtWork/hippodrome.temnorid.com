using System.Collections;
using System.Threading.Tasks;
using ITKamianets.Engine.Account;
using ITKamianets.Engine.Core;
using Temnorid.Village;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// The village scene: draws the player's Lords village (or the standard one for a guest), builds the ring track round its walls and
	/// starts the ride. The village is in the middle and cannot be entered; the way out of the ring leads to the races.
	/// </summary>
	public sealed class HippodromeWorld : MonoBehaviour
	{
		public RaceController Race;
		public VillageView View;
		public RaceCamera Camera;
		[Tooltip("A lit material of the scene: the ring's pieces are copies of it in other colours.")]
		public Material Lit;
		[Tooltip("The text material the gate's sign uses.")]
		public Material TextMaterial;

		const float AccountWaitSeconds = 5f;
		const string Log = "Hippodrome";

		public RingTrack Track { get; private set; }

		void Start()
		{
			StartCoroutine(Load());
		}

		IEnumerator Load()
		{
			// The shell restores a saved sign-in in the background; give it a few seconds, never longer.
			var end = Time.unscaledTime + AccountWaitSeconds;
			while (GameAccount.Busy && Time.unscaledTime < end) yield return null;

			VillageSession session;
			if (GameAccount.IsSignedIn && !GameAccount.IsGuest)
			{
				Task<VillageSession> task = null;
				try { task = VillageSource.ForAccount().LoadAsync(null); }
				catch (System.Exception) { }
				while (task != null && !task.IsCompleted) yield return null;
				session = task != null && !task.IsFaulted && !task.IsCanceled ? task.Result : null;
			}
			else
			{
				session = null;
			}
			if (session == null) session = VillageSource.CreateDefault(null, "Standard village.", SharedStock.SystemClock());

			// The village always has its stone wall, as in Craft: a village that has not built one yet is shown with the first level.
			if (session.Data.WallLevel <= 0) session.Data.WallLevel = 1;
			View.Build(session.Data);
			Track = HippodromeTrack.Build(transform, session.Data.VillageRx, session.Data.VillageRz, Lit, TextMaterial);
			ClearScenery(session.Data.VillageRx, session.Data.VillageRz);
			// The farm's fields lie where the ring runs: only the village and its wall are shown.
			foreach (var plot in View.Plots) if (plot.Root != null) plot.Root.gameObject.SetActive(false);
			Race.InitTrack(Track.Waypoints, Track.Checkpoints, Track.GridSlots);
			if (Camera != null) Camera.Snap();
			EngineLog.Info(Log, $"{(session.IsLords ? "Lords" : "Standard")} village, ring {session.Data.VillageRx:0} x {session.Data.VillageRz:0} m inside the track.");
		}

		/// <summary>The village scatters trees and rocks round its walls: the ones on the ring, or too near it to ride past, are taken away.</summary>
		void ClearScenery(float villageRx, float villageRz)
		{
			var scenery = View.Root != null ? View.Root.Find("Scenery") : null;
			if (scenery == null) return;
			var rx = villageRx + HippodromeTrack.WallGap + HippodromeTrack.TrackWidth * 0.5f;
			var rz = villageRz + HippodromeTrack.WallGap + HippodromeTrack.TrackWidth * 0.5f;
			var half = HippodromeTrack.TrackWidth * 0.5f + 7f;
			for (var i = scenery.childCount - 1; i >= 0; i--)
			{
				var child = scenery.GetChild(i);
				var p = child.localPosition;
				var f = Mathf.Sqrt(p.x * p.x / (rx * rx) + p.z * p.z / (rz * rz));
				if (Mathf.Abs(f - 1f) * Mathf.Min(rx, rz) < half) Destroy(child.gameObject);
			}
		}
	}
}
