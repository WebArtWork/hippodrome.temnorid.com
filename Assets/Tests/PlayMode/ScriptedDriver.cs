using UnityEngine;

namespace Temnorid.Hippodrome.Tests
{
	/// <summary>A rider for tests: steers and gallops as told, and can jump a given hurdle at the right moment.</summary>
	public sealed class ScriptedDriver : HorseDriver
	{
		public float Steer;
		public float Throttle = 1f;
		public Hurdle JumpAt;
		public bool BoostNow;

		HorseController _horse;

		void Awake()
		{
			_horse = GetComponent<HorseController>();
		}

		public override Vector2 Drive() => new Vector2(Steer, Throttle);

		public override bool ConsumeBoost()
		{
			var pressed = BoostNow;
			BoostNow = false;
			return pressed;
		}

		public override bool ConsumeJump()
		{
			if (JumpAt == null || _horse.Airborne || _horse.Speed < _horse.MinJumpSpeed) return false;
			var distance = JumpAt.DistanceAhead(_horse);
			if (distance <= 0f || distance > _horse.Speed * 0.37f) return false;
			JumpAt = null;
			return true;
		}
	}
}
