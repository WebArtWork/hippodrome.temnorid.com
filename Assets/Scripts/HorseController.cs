using System.Collections.Generic;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// Decides what a horse does: x steers (-1 left, 1 right), y is the throttle (-1 brake, 1 full gallop).
	/// Boost and jump are one-shot presses: the horse asks for them once per physics step and a press is used up.
	/// </summary>
	public abstract class HorseDriver : MonoBehaviour
	{
		public abstract Vector2 Drive();

		/// <summary>True once per press of the boost key.</summary>
		public virtual bool ConsumeBoost() => false;

		/// <summary>True once per press of the jump key.</summary>
		public virtual bool ConsumeJump() => false;
	}

	/// <summary>
	/// An arcade horse: it gallops forward, steers while it moves, slides along walls, can burst into a faster
	/// run for a moment and can jump. It feels other horses close to it and slows down, and a wall takes speed
	/// away. It knows nothing about who rides it; a <see cref="HorseDriver"/> gives the commands.
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

		[Header("Boost")]
		[Tooltip("Top speed during a burst, as a multiple of normal.")]
		public float BoostSpeedFactor = 1.3f;
		[Tooltip("Acceleration during a burst, as a multiple of normal.")]
		public float BoostAccelerationFactor = 2.2f;

		[Header("Jump")]
		public float JumpSpeed = 6.6f;
		public float Gravity = 18f;
		public float MinJumpSpeed = 3f;

		/// <summary>The rider and horse shapes, bobbed a little while galloping.</summary>
		public Transform Visual;

		public HorseDriver Driver { get; set; }

		/// <summary>True before the start: the horse stands still.</summary>
		public bool Locked { get; set; } = true;

		/// <summary>Forward speed in metres per second (negative when backing).</summary>
		public float Speed { get; private set; }

		/// <summary>Scales top speed. Computer riders get a little more or less from the race; players stay at 1.</summary>
		public float SpeedScale { get; set; } = 1f;

		public HorseStamina Stamina { get; } = new HorseStamina();

		/// <summary>Metres above the ground.</summary>
		public float Height { get; private set; }
		public bool Airborne { get; private set; }

		/// <summary>Bars cleared and bars hit with a jump, walls hit and jumps made, this race.</summary>
		public int BarsCleared { get; private set; }
		public int BarsHit { get; private set; }
		public int WallHits { get; private set; }
		public int Jumps { get; private set; }

		/// <summary>The closest another horse got to this one this race, in the horse's own measure (below 1 is touching).</summary>
		public float MaxCloseness { get; private set; }

		public static readonly List<HorseController> All = new List<HorseController>();

		Rigidbody _body;
		float _bobPhase;
		float _groundY;
		float _verticalSpeed;

		void Awake()
		{
			_body = GetComponent<Rigidbody>();
			_body.useGravity = false;
			_body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
			_body.interpolation = RigidbodyInterpolation.Interpolate;
			_body.collisionDetectionMode = CollisionDetectionMode.Continuous;
			_body.linearDamping = 0f;
			_body.angularDamping = 0f;
			_groundY = transform.position.y;
		}

		void OnEnable()
		{
			if (!All.Contains(this)) All.Add(this);
		}

		void OnDisable()
		{
			All.Remove(this);
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
			var flatVelocity = _body.linearVelocity;
			flatVelocity.y = 0f;
			var along = Vector3.Dot(flatVelocity, forward);
			if (Mathf.Abs(along) < Mathf.Abs(Speed)) Speed = along;

			var command = Driver != null ? Driver.Drive() : Vector2.zero;
			var steer = Mathf.Clamp(command.x, -1f, 1f);
			var throttle = Mathf.Clamp(command.y, -1f, 1f);

			if (Driver != null)
			{
				if (Driver.ConsumeBoost()) Stamina.PressBoost();
				if (Driver.ConsumeJump()) TryJump();
			}
			Stamina.Tick(dt, throttle > 0.9f && Speed > MaxSpeed * 0.8f);

			var boosting = Stamina.Boosting;
			var top = MaxSpeed * SpeedScale * Stamina.PaceFactor * (boosting ? BoostSpeedFactor : 1f);
			var acceleration = Acceleration * (boosting ? BoostAccelerationFactor : 1f);

			if (Airborne)
			{
				// Nothing to push against in the air: the horse keeps its speed.
				Speed = Mathf.MoveTowards(Speed, 0f, 0.3f * dt);
			}
			else if (throttle > 0f)
			{
				Speed = Mathf.MoveTowards(Speed, top * throttle, (Speed < top * throttle ? acceleration : CoastDeceleration * 2f) * dt);
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

			var push = FeelOthers(dt, forward);

			// Steering works a little even when standing, so a horse facing a wall can turn away.
			var grip = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(Mathf.Abs(Speed) / 5f)) * (Airborne ? 0.3f : 1f);
			var heavy = 1f - 0.3f * Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
			var direction = Speed < -0.1f ? -1f : 1f;
			var yaw = transform.eulerAngles.y + steer * TurnRate * grip * heavy * direction * dt;
			_body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

			var vertical = StepVertical(dt);
			_body.linearVelocity = forward * Speed + push + Vector3.up * vertical;
			_body.angularVelocity = Vector3.zero;
		}

		/// <summary>Slows the horse when another is close, and returns a sideways push that parts them.</summary>
		Vector3 FeelOthers(float dt, Vector3 forward)
		{
			var right = Vector3.Cross(Vector3.up, forward);
			var push = Vector3.zero;
			var position = transform.position;

			foreach (var other in All)
			{
				if (other == this || other.Locked) continue;
				var offset = other.transform.position - position;
				if (Mathf.Abs(offset.y) > 0.9f) continue; // one of them is well above the other, in a jump
				offset.y = 0f;
				var lateral = Vector3.Dot(offset, right);
				var longitudinal = Vector3.Dot(offset, forward);

				var closeness = HorsePhysics.Closeness(lateral, longitudinal);
				if (closeness > MaxCloseness) MaxCloseness = closeness;
				if (longitudinal > HorsePhysics.BodyLength + HorsePhysics.FollowHorizon || Mathf.Abs(lateral) > 3f) continue;
				if (longitudinal < -HorsePhysics.PersonalLength * 2f) continue;

				var brake = Mathf.Max(HorsePhysics.ContactDeceleration(lateral, longitudinal), HorsePhysics.FollowDeceleration(lateral, longitudinal, Speed - other.Speed));
				if (Speed > 0f) Speed = Mathf.Max(0f, Speed - brake * dt);
				push += right * HorsePhysics.ContactPush(lateral, longitudinal, All.IndexOf(this) < All.IndexOf(other) ? 1f : -1f);
			}
			return push;
		}

		float StepVertical(float dt)
		{
			Height = Mathf.Max(0f, _body.position.y - _groundY);

			if (Airborne)
			{
				_verticalSpeed -= Gravity * dt;
				if (_verticalSpeed < 0f && Height + _verticalSpeed * dt <= 0f)
				{
					// Lands during this step: come down exactly onto the ground.
					Airborne = false;
					_verticalSpeed = 0f;
					return -Height / dt;
				}
				return _verticalSpeed;
			}

			// On the ground: hold the height.
			return Height > 0.001f ? -Height / dt : 0f;
		}

		/// <summary>Takes off if the horse is on the ground and moving.</summary>
		public bool TryJump()
		{
			if (Locked || Airborne || Speed < MinJumpSpeed) return false;
			Airborne = true;
			_verticalSpeed = JumpSpeed;
			Jumps++;
			Stamina.Jumped();
			return true;
		}

		/// <summary>The front of the horse crossed a bar too low: it stumbles and loses most of its speed.</summary>
		public void HitBar()
		{
			BarsHit++;
			Speed *= HorsePhysics.StumbleKeeps;
		}

		public void ClearedBar()
		{
			BarsCleared++;
		}

		void OnCollisionEnter(Collision collision)
		{
			if (Locked || collision.contactCount == 0) return;
			var normal = collision.GetContact(0).normal;
			var into = Mathf.Max(0f, -Vector3.Dot(transform.forward * Speed, normal));
			Speed = HorsePhysics.AfterWallImpact(Speed, into);
			WallHits++;
		}

		void OnCollisionStay(Collision collision)
		{
			if (Locked) return;
			Speed = HorsePhysics.AfterWallScrape(Speed, Time.fixedDeltaTime);
		}

		void Update()
		{
			if (Visual == null) return;
			_bobPhase += Mathf.Abs(Speed) * 0.55f * Time.deltaTime;
			var amount = Airborne ? 0f : Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
			Visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.12f * amount, 0f);
			var pitch = Airborne ? Mathf.Clamp(-_verticalSpeed * 2.2f, -14f, 10f) : Mathf.Sin(_bobPhase) * 2.5f * amount;
			Visual.localRotation = Quaternion.Euler(pitch, 0f, 0f);
		}

		/// <summary>Puts the horse somewhere standing still, rested and with a clean race record.</summary>
		public void Teleport(Vector3 position, Quaternion rotation)
		{
			if (_body == null) _body = GetComponent<Rigidbody>();
			Speed = 0f;
			_groundY = position.y;
			_verticalSpeed = 0f;
			Airborne = false;
			Height = 0f;
			transform.SetPositionAndRotation(position, rotation);
			_body.position = position;
			_body.rotation = rotation;
			_body.linearVelocity = Vector3.zero;
			_body.angularVelocity = Vector3.zero;
		}

		/// <summary>Clears everything a race adds up: breath, counters, pace.</summary>
		public void ResetRace()
		{
			Stamina.Reset();
			SpeedScale = 1f;
			BarsCleared = 0;
			BarsHit = 0;
			WallHits = 0;
			Jumps = 0;
			MaxCloseness = 0f;
		}
	}
}
