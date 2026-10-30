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

		Transform[] _waypoints;
		int _target;
		float _stuckTime;
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
		}

		public override Vector2 Drive()
		{
			if (_waypoints == null || _waypoints.Length < 3) return Vector2.zero;

			var position = transform.position;
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

			if (_horse != null && !_horse.Locked && _horse.Speed < 1.5f) _stuckTime += Time.fixedDeltaTime;
			else _stuckTime = 0f;
			if (_stuckTime > 2.5f) Recover();

			return new Vector2(steer, throttle);
		}

		Vector3 TargetPoint(int index)
		{
			var count = _waypoints.Length;
			var tangent = _waypoints[(index + 1) % count].position - _waypoints[(index - 1 + count) % count].position;
			tangent.y = 0f;
			var right = Vector3.Cross(Vector3.up, tangent.normalized);
			return _waypoints[index].position + right * LaneOffset;
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
