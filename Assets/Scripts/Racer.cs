using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>One horse and rider in the race. Index 0 is always the player.</summary>
	[RequireComponent(typeof(HorseController))]
	public sealed class Racer : MonoBehaviour
	{
		public string DisplayName = "Racer";
		public Color Colour = Color.white;
		public bool IsPlayer;

		[HideInInspector] public int Index;

		HorseController _horse;
		PlayerDriver _playerDriver;
		BotDriver _botDriver;

		// Looked up on first use rather than in Awake: the race controller reads these in its own Awake.
		public HorseController Horse => _horse != null ? _horse : (_horse = GetComponent<HorseController>());
		public PlayerDriver PlayerDriver => _playerDriver != null ? _playerDriver : (_playerDriver = GetComponent<PlayerDriver>());
		public BotDriver BotDriver => _botDriver != null ? _botDriver : (_botDriver = GetComponent<BotDriver>());
	}
}
