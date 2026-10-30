using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>Decides what a horse does: x steers (-1 left, 1 right), y is the throttle (-1 brake, 1 full gallop).</summary>
	public abstract class HorseDriver : MonoBehaviour
	{
		public abstract Vector2 Drive();
	}

	/// <summary>
	/// An arcade horse: it gallops forward, steers while it moves, and slides along walls.
	/// It knows nothing about who rides it; a <see cref="HorseDriver"/> gives the commands.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public sealed class HorseController : MonoBehaviour
	{
		public float MaxSpeed = 22f;
		public float Acceleration = 9f;
		public float BrakeDeceleration = 20f;
		public float CoastDeceleration = 2.5f;
		public float MaxReverseSpeed = 4f;
		/// <summary>Degrees per second at full steer.</summary>
		public float TurnRate = 105f;

		/// <summary>The rider and horse shapes, bobbed a little while galloping.</summary>
		public Transform Visual;

		public HorseDriver Driver { get; set; }

		/// <summary>True before the start: the horse stands still.</summary>
		public bool Locked { get; set; } = true;

		/// <summary>Forward speed in metres per second (negative when backing).</summary>
		public float Speed { get; private set; }

		Rigidbody _body;
		float _bobPhase;

		void Awake()
		{
			_body = GetComponent<Rigidbody>();
			_body.useGravity = false;
			_body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
			_body.interpolation = RigidbodyInterpolation.Interpolate;
			_body.collisionDetectionMode = CollisionDetectionMode.Continuous;
			_body.linearDamping = 0f;
			_body.angularDamping = 0f;
		}

		void FixedUpdate()
		{
			var dt = Time.fixedDeltaTime;

			if (Locked)
			{
				Speed = 0f;
				_body.linearVelocity = Vector3.zero;
				_body.angularVelocity = Vector3.zero;
				return;
			}

			// A wall takes speed away: the real forward speed can only be lower than what we asked for.
			var forward = transform.forward;
			var along = Vector3.Dot(_body.linearVelocity, forward);
			if (Mathf.Abs(along) < Mathf.Abs(Speed)) Speed = along;

			var command = Driver != null ? Driver.Drive() : Vector2.zero;
			var steer = Mathf.Clamp(command.x, -1f, 1f);
			var throttle = Mathf.Clamp(command.y, -1f, 1f);

			if (throttle > 0f)
			{
				Speed = Mathf.MoveTowards(Speed, MaxSpeed * throttle, (Speed < MaxSpeed * throttle ? Acceleration : CoastDeceleration * 2f) * dt);
			}
			else if (throttle < 0f)
			{
				if (Speed > 0.1f) Speed = Mathf.MoveTowards(Speed, 0f, BrakeDeceleration * dt);
				else Speed = Mathf.MoveTowards(Speed, -MaxReverseSpeed * -throttle, Acceleration * dt);
			}
			else
			{
				Speed = Mathf.MoveTowards(Speed, 0f, CoastDeceleration * dt);
			}

			// Steering works a little even when standing, so a horse facing a wall can turn away.
			var grip = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(Mathf.Abs(Speed) / 5f));
			var heavy = 1f - 0.3f * Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
			var direction = Speed < -0.1f ? -1f : 1f;
			var yaw = transform.eulerAngles.y + steer * TurnRate * grip * heavy * direction * dt;
			_body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

			_body.linearVelocity = forward * Speed;
			_body.angularVelocity = Vector3.zero;
		}

		void Update()
		{
			if (Visual == null) return;
			_bobPhase += Mathf.Abs(Speed) * 0.55f * Time.deltaTime;
			var amount = Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
			Visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.12f * amount, 0f);
			Visual.localRotation = Quaternion.Euler(Mathf.Sin(_bobPhase) * 2.5f * amount, 0f, 0f);
		}

		/// <summary>Puts the horse somewhere standing still.</summary>
		public void Teleport(Vector3 position, Quaternion rotation)
		{
			if (_body == null) _body = GetComponent<Rigidbody>();
			Speed = 0f;
			transform.SetPositionAndRotation(position, rotation);
			_body.position = position;
			_body.rotation = rotation;
			_body.linearVelocity = Vector3.zero;
			_body.angularVelocity = Vector3.zero;
		}
	}
}
