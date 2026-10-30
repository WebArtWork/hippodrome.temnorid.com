using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>A gate across the track. Checkpoint 0 is the finish line.</summary>
	[RequireComponent(typeof(BoxCollider))]
	public sealed class Checkpoint : MonoBehaviour
	{
		public int Index;
		[HideInInspector] public RaceController Race;

		void OnTriggerEnter(Collider other)
		{
			var racer = other.GetComponentInParent<Racer>();
			if (racer != null && Race != null) Race.OnCheckpoint(racer.Index, Index);
		}
	}
}
