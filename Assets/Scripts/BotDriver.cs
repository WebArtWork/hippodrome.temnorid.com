using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// A computer rider: follows the track's waypoints along its own lane, easing off in sharp turns.
	/// <see cref="Skill"/> scales its speed, so bots are not all equally fast.
	/// </summary>
	public sealed class BotDriver : HorseDriver
	{
		/// <summary>Sideways offset from the track's centre line, in metres (positive is toward the outer rail).</summary>
		public float LaneOffset;
		/// <summary>Share of full throttle this rider uses on a straight, 0 to 1.</summary>
		public float Skill = 0.95f;
		/// <summary>How far ahead the rider aims, in metres.</summary>
		public float Lookahead = 12f;
		/// <summary>How far off, in metres, this rider may misjudge where to take off for a jump. A weak rider sometimes hits the bar.</summary>
		public float JumpTimingError;
		/// <summary>Whether this rider uses the boost key. A good rider uses it every time the horse is rested.</summary>
		public bool UseBoost = true;
		/// <summary>Seconds this rider waits between bursts. The horse itself allows one every 3.5 s.</summary>
		public float BoostEvery = 3.9f;
		/// <summary>Whether this rider goes round a slower horse in front of it instead of queueing behind it.</summary>
		public bool Overtakes = true;

		Transform[] _waypoints;
		int _target;
		float _stuckTime;
		float _steer;
		float _nextBoost;
		float _lastJump = -10f;
		float _jumpError;
		float _lane;
		float _passLane;
		float _passUntil;
		HorseController _horse;

		public void SetPath(Transform[] waypoints)
		{
			_waypoints = waypoints;
			_target = NearestAhead();
		}

		void Awake()
		{
			_horse = GetComponent<HorseController>();
		}

		void OnEnable()
		{
			if (_waypoints != null) _target = NearestAhead();
			_stuckTime = 0f;
			_nextBoost = Time.time + Random.Range(1f, 3f);
			_lane = LaneOffset;
			_passUntil = 0f;
		}

		public override Vector2 Drive()
		{
			if (_waypoints == null || _waypoints.Length < 3) return Vector2.zero;

			var position = transform.position;
			_lane = Mathf.MoveTowards(_lane, ChooseLane(), 3.5f * Time.fixedDeltaTime);
			Vector3 toTarget = Vector3.zero;
			for (var guard = 0; guard < 4; guard++)
			{
				toTarget = TargetPoint(_target) - position;
				toTarget.y = 0f;
				if (toTarget.magnitude < Lookahead * 0.6f) _target = (_target + 1) % _waypoints.Length;
				else break;
			}

			var angle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
			var steer = Mathf.Clamp(angle / 30f, -1f, 1f);
			var throttle = Skill * (1f - 0.5f * Mathf.Abs(steer));
			_steer = steer;

			if (_horse != null && !_horse.Locked && _horse.Speed < 1.5f) _stuckTime += Time.fixedDeltaTime;
			else _stuckTime = 0f;
			if (_stuckTime > 2.5f) Recover();

			return new Vector2(steer, throttle);
		}

		/// <summary>A rested rider asks for a burst whenever the horse is ready, not in the middle of a sharp turn.</summary>
		public override bool ConsumeBoost()
		{
			if (!UseBoost || _horse == null || Time.time < _nextBoost) return false;
			if (!_horse.Stamina.Ready || _horse.Stamina.Value < 0.5f || Mathf.Abs(_steer) > 0.35f) return false;
			_nextBoost = Time.time + Random.Range(BoostEvery, BoostEvery * 1.25f);
			return true;
		}

		/// <summary>
		/// The lane to ride in: its own, unless a slower horse is in the way ahead, in which case it moves
		/// out to the side with more room for a moment and goes round.
		/// </summary>
		float ChooseLane()
		{
			if (!Overtakes || _horse == null || Time.time < _passUntil) return Time.time < _passUntil ? _passLane : LaneOffset;

			var forward = transform.forward;
			var right = Vector3.Cross(Vector3.up, forward);
			foreach (var other in HorseController.All)
			{
				if (other == _horse || other.Locked) continue;
				var offset = other.transform.position - transform.position;
				offset.y = 0f;
				var ahead = Vector3.Dot(offset, forward);
				var side = Vector3.Dot(offset, right);
				if (ahead < 0.5f || ahead > 18f || Mathf.Abs(side) > 1.7f || other.Speed > _horse.Speed + 1.5f) continue;

				// Its lane is about ours plus the sideways offset. Go round on the side with more track.
				var theirs = _lane + side;
				var left = theirs - 2.6f;
				var rightLane = theirs + 2.6f;
				var canLeft = left >= -4.8f;
				var canRight = rightLane <= 4.8f;
				if (!canLeft && !canRight) return LaneOffset;
				_passLane = canLeft && (!canRight || Mathf.Abs(left - _lane) <= Mathf.Abs(rightLane - _lane)) ? left : rightLane;
				_passUntil = Time.time + 2.2f;
				return _passLane;
			}
			return LaneOffset;
		}

		/// <summary>Takes off so that the front of the horse is high over the bar when it gets there.</summary>
		public override bool ConsumeJump()
		{
			if (_horse == null || _horse.Airborne || _horse.Speed < _horse.MinJumpSpeed || Time.time - _lastJump < 1.2f) return false;

			foreach (var hurdle in Hurdle.All)
			{
				if (!hurdle.Faces(_horse)) continue;
				var distance = hurdle.DistanceAhead(_horse);
				if (distance <= 0f) continue;

				// The jump peaks about 0.37 s after take-off.
				if (distance <= _horse.Speed * 0.37f + _jumpError)
				{
					_lastJump = Time.time;
					_jumpError = Random.Range(-JumpTimingError, JumpTimingError);
					return true;
				}
			}
			return false;
		}

		Vector3 TargetPoint(int index)
		{
			var count = _waypoints.Length;
			var tangent = _waypoints[(index + 1) % count].position - _waypoints[(index - 1 + count) % count].position;
			tangent.y = 0f;
			var right = Vector3.Cross(Vector3.up, tangent.normalized);
			var point = _waypoints[index].position + right * _lane;

			// Where the track is narrowed to a gap, keep to the middle of it.
			foreach (var hurdle in Hurdle.All)
			{
				var local = hurdle.transform.InverseTransformPoint(point);
				// Only the stretch of track the hurdle is on: not a point further along, nor on the far side of the infield.
				if (Mathf.Abs(local.z) > hurdle.ZoneHalfLength + Lookahead || Mathf.Abs(local.x) > hurdle.GapHalfWidth + 6f) continue;
				var limit = hurdle.GapHalfWidth - 1.6f;
				if (Mathf.Abs(local.x) <= limit) continue;
				local.x = Mathf.Clamp(local.x, -limit, limit);
				point = hurdle.transform.TransformPoint(local);
			}
			return point;
		}

		int NearestAhead()
		{
			var best = 0;
			var bestDistance = float.MaxValue;
			for (var i = 0; i < _waypoints.Length; i++)
			{
				var distance = (_waypoints[i].position - transform.position).sqrMagnitude;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
				}
			}
			return (best + 2) % _waypoints.Length;
		}

		/// <summary>Stuck against something: set the horse back on the line, facing the way round.</summary>
		void Recover()
		{
			_stuckTime = 0f;
			var best = (NearestAhead() - 2 + _waypoints.Length) % _waypoints.Length;
			var next = _waypoints[(best + 1) % _waypoints.Length].position;
			var here = _waypoints[best].position;
			var direction = next - here;
			direction.y = 0f;
			_horse.Teleport(new Vector3(here.x, transform.position.y, here.z), Quaternion.LookRotation(direction));
			_target = (best + 2) % _waypoints.Length;
		}
	}
}
