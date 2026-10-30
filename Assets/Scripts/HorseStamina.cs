using System;

namespace Temnorid.Hippodrome
{
	/// <summary>What happened when the rider pressed the boost key.</summary>
	public enum BoostResult
	{
		/// <summary>A burst of speed began.</summary>
		Started,
		/// <summary>Too soon after the last burst: no speed, and the horse is strained for it.</summary>
		Strained,
		/// <summary>The horse is exhausted and does not answer the whip at all.</summary>
		Ignored,
	}

	/// <summary>
	/// A horse's wind, with no Unity in it. A burst of speed costs a chunk of stamina and then needs a
	/// short rest before the next; pressing again before that rest is over gets no speed but still tires
	/// the horse. Resting, even at a gallop, slowly gives stamina back. Below half the horse slows down,
	/// and when it is spent it is exhausted: slow, and deaf to the boost key until it has caught its breath.
	/// </summary>
	public sealed class HorseStamina
	{
		/// <summary>Seconds a burst lasts.</summary>
		public const float BoostDuration = 1.3f;
		/// <summary>Seconds from one burst to the next that a rested horse allows.</summary>
		public const float BoostCooldown = 3.5f;
		public const float BoostCost = 0.17f;
		/// <summary>What a press that came too early costs.</summary>
		public const float StrainCost = 0.07f;
		public const float JumpCost = 0.02f;
		public const float RegenPerSecond = 0.05f;
		/// <summary>Galloping flat out is not free, only very slightly.</summary>
		public const float GallopDrainPerSecond = 0.004f;
		/// <summary>Below this the horse starts to slow down.</summary>
		public const float TiredBelow = 0.5f;
		public const float ExhaustedBelow = 0.12f;
		/// <summary>An exhausted horse listens to the boost key again once it has this much back.</summary>
		public const float RecoveredAbove = 0.35f;
		/// <summary>Share of top speed a horse with nothing left can still manage.</summary>
		public const float WornOutPace = 0.78f;

		float _boostLeft;
		float _cooldown;

		/// <summary>0 (spent) to 1 (fresh).</summary>
		public float Value { get; private set; } = 1f;

		public bool Exhausted { get; private set; }
		public bool Boosting => _boostLeft > 0f;
		public float CooldownRemaining => _cooldown > 0f ? _cooldown : 0f;

		/// <summary>A press right now would start a burst.</summary>
		public bool Ready => !Exhausted && !Boosting && _cooldown <= 0f;

		/// <summary>Share of top speed the horse can hold: 1 while fresh, less once tired.</summary>
		public float PaceFactor => Value >= TiredBelow ? 1f : WornOutPace + (1f - WornOutPace) * (Value / TiredBelow);

		public void Reset()
		{
			Value = 1f;
			Exhausted = false;
			_boostLeft = 0f;
			_cooldown = 0f;
		}

		public BoostResult PressBoost()
		{
			if (Exhausted) return BoostResult.Ignored;

			if (Boosting || _cooldown > 0f)
			{
				Spend(StrainCost);
				return BoostResult.Strained;
			}

			Spend(BoostCost);
			if (Exhausted) return BoostResult.Strained;
			_boostLeft = BoostDuration;
			_cooldown = BoostCooldown;
			return BoostResult.Started;
		}

		/// <summary>Taking off for a jump tires the horse a little.</summary>
		public void Jumped() => Spend(JumpCost);

		/// <summary>Advances time. <paramref name="galloping"/> is true while the horse runs flat out.</summary>
		public void Tick(float dt, bool galloping)
		{
			if (_boostLeft > 0f) _boostLeft = Math.Max(0f, _boostLeft - dt);
			if (_cooldown > 0f) _cooldown = Math.Max(0f, _cooldown - dt);

			var change = RegenPerSecond * dt;
			if (galloping) change -= GallopDrainPerSecond * dt;
			Value = Math.Min(1f, Math.Max(0f, Value + change));

			if (Exhausted && Value >= RecoveredAbove) Exhausted = false;
		}

		void Spend(float amount)
		{
			Value = Math.Max(0f, Value - amount);
			if (Value <= ExhaustedBelow)
			{
				Exhausted = true;
				_boostLeft = 0f;
			}
		}
	}
}
