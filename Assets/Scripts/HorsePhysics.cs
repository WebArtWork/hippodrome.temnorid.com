using System;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// How horses lose speed when they crowd each other or scrape a wall, with no Unity in it.
	/// Horses do not collide with each other; they feel each other instead, so a rider who closes in
	/// on another has to ease off and steer round.
	/// </summary>
	public static class HorsePhysics
	{
		/// <summary>Half the width and length of the space a horse keeps to itself, in metres.</summary>
		public const float PersonalWidth = 1.2f;
		public const float PersonalLength = 2.8f;
		/// <summary>The horse behind is braked hard, the one in front or beside only a little.</summary>
		public const float RearEndDeceleration = 26f;
		public const float SideDeceleration = 7f;
		/// <summary>Sideways speed with which touching horses are pushed apart.</summary>
		public const float PushSpeed = 2.5f;
		/// <summary>Share of speed kept after a bar is hit (too low a jump).</summary>
		public const float StumbleKeeps = 0.4f;
		/// <summary>Share of speed lost per second while scraping along a wall.</summary>
		public const float ScrapeLossPerSecond = 0.7f;

		/// <summary>
		/// How much another horse is in this one's way, 0 (clear) to 1 (overlapping). The offsets are
		/// the other horse's position in this horse's own frame: <paramref name="lateral"/> to the right,
		/// <paramref name="longitudinal"/> ahead.
		/// </summary>
		public static float Closeness(float lateral, float longitudinal)
		{
			var x = lateral / PersonalWidth;
			var z = longitudinal / PersonalLength;
			var d = (float)Math.Sqrt(x * x + z * z);
			return Clamp01((1.8f - d) / (1.8f - 0.7f));
		}

		/// <summary>Braking, in metres per second squared, for a horse with another close by.</summary>
		public static float ContactDeceleration(float lateral, float longitudinal)
		{
			var closeness = Closeness(lateral, longitudinal);
			if (closeness <= 0f) return 0f;
			var otherIsAhead = longitudinal > 0f && Math.Abs(lateral) < PersonalWidth;
			return closeness * (otherIsAhead ? RearEndDeceleration : SideDeceleration);
		}

		/// <summary>
		/// Sideways push (positive to the right) that parts two horses that are touching. Horses exactly in line
		/// are pushed to the side given by <paramref name="tieBreak"/> (1 or -1), which must differ between the two.
		/// </summary>
		public static float ContactPush(float lateral, float longitudinal, float tieBreak = 1f)
		{
			var closeness = Closeness(lateral, longitudinal);
			if (closeness <= 0f) return 0f;
			// The other is on the right: this horse is pushed left, and the other way round.
			var away = Math.Abs(lateral) < 0.05f ? tieBreak : lateral > 0f ? -1f : 1f;
			return away * closeness * PushSpeed;
		}

		/// <summary>How wide, either side of a horse, another counts as being in its lane, in metres.</summary>
		public const float LaneHalfWidth = 1.4f;
		/// <summary>The length of a horse, nose to tail, in metres.</summary>
		public const float BodyLength = 2.6f;
		/// <summary>The gap a rider aims to keep when following, in metres.</summary>
		public const float FollowGap = 1.0f;
		/// <summary>How far ahead a slower horse starts to matter, in metres.</summary>
		public const float FollowHorizon = 15f;
		public const float MaxFollowDeceleration = 40f;

		/// <summary>
		/// Braking, in metres per second squared, for a horse that is catching up another in its own lane: just
		/// enough to match the other's speed by the time the gap has closed to <see cref="FollowGap"/>.
		/// <paramref name="closingSpeed"/> is how much faster this horse is going.
		/// </summary>
		public static float FollowDeceleration(float lateral, float longitudinal, float closingSpeed)
		{
			if (longitudinal <= 0f || Math.Abs(lateral) >= LaneHalfWidth || closingSpeed <= 0f) return 0f;
			var gap = longitudinal - BodyLength;
			if (gap > FollowHorizon) return 0f;
			var room = Math.Max(gap - FollowGap, 0.25f);
			return Math.Min(MaxFollowDeceleration, closingSpeed * closingSpeed / (2f * room));
		}

		/// <summary>Speed after hitting a wall: <paramref name="into"/> is how fast the horse was heading into it.</summary>
		public static float AfterWallImpact(float speed, float into)
		{
			if (speed <= 0f) return speed;
			var loss = Math.Min(speed * 0.5f, into * 0.6f + speed * 0.05f);
			return speed - loss;
		}

		/// <summary>Speed after <paramref name="dt"/> seconds of scraping along a wall.</summary>
		public static float AfterWallScrape(float speed, float dt)
		{
			return speed * Math.Max(0f, 1f - ScrapeLossPerSecond * dt);
		}

		static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
	}

	/// <summary>
	/// The quiet help that keeps a race against computer riders close and lets skill decide it. A rider behind
	/// the player chases, but only up to a short distance behind, so it stays on the player's tail and passes
	/// only if the player slips. A rider ahead of the player eases in proportion to how far ahead it is, so a
	/// player who rides well from behind can win. Pace changes by a few per cent and only gradually, so it is
	/// not noticed. It is for races against computer riders only; a race against other people must not use it.
	/// </summary>
	public static class RacePacing
	{
		/// <summary>The most speed a computer rider adds while chasing the player, as a share of normal.</summary>
		public const float MaxChase = 0.12f;
		/// <summary>The most speed a computer rider gives up while ahead of the player.</summary>
		public const float MaxEase = 0.18f;
		/// <summary>A rider this close behind the player (metres) has no reason to push harder.</summary>
		public const float HoverGap = 6f;
		/// <summary>How much further back than the hover gap (metres) the full chase applies.</summary>
		public const float ChaseGap = 25f;
		/// <summary>How far ahead of the player (metres) the full easing applies.</summary>
		public const float EaseGap = 22f;
		/// <summary>How fast the pace may move, per second, so it is never a sudden jolt.</summary>
		public const float Rate = 0.05f;

		/// <summary>
		/// The pace a computer rider should aim for, as a share of normal. <paramref name="playerAhead"/> is how far
		/// the player is in front of this rider in metres (negative when this rider is the one in front).
		/// </summary>
		public static float Target(float playerAhead)
		{
			if (playerAhead > HoverGap) return 1f + MaxChase * Clamp01((playerAhead - HoverGap) / ChaseGap);
			if (playerAhead < 0f) return 1f - MaxEase * Clamp01(-playerAhead / EaseGap);
			return 1f;
		}

		public static float Step(float current, float target, float dt)
		{
			var max = Rate * dt;
			if (target > current) return Math.Min(target, current + max);
			return Math.Max(target, current - max);
		}

		static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
	}
}
