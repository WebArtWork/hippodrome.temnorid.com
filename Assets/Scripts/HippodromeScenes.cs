using UnityEngine;
using UnityEngine.SceneManagement;

namespace Temnorid.Hippodrome
{
	/// <summary>The game's two places: the village with the ring round its walls, and the races a rider reaches by leaving that ring.</summary>
	public static class HippodromeScenes
	{
		public const string Village = "Main";
		public const string Races = "Race";

		public static void LoadVillage() => Load(Village);

		public static void LoadRaces() => Load(Races);

		static void Load(string scene)
		{
			Time.timeScale = 1f;
			SceneManager.LoadScene(scene);
		}
	}

	/// <summary>The gap in the ring's outer rail: a horse that rides out through it goes on to the races.</summary>
	[RequireComponent(typeof(BoxCollider))]
	public sealed class RaceExitGate : MonoBehaviour
	{
		bool _used;

		void OnTriggerEnter(Collider other)
		{
			if (_used) return;
			var racer = other.GetComponentInParent<Racer>();
			if (racer == null || !racer.IsPlayer) return;
			_used = true;
			HippodromeScenes.LoadRaces();
		}
	}
}
