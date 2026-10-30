using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>A third-person camera that rides behind the player's horse and turns with it.</summary>
	[RequireComponent(typeof(Camera))]
	public sealed class RaceCamera : MonoBehaviour
	{
		public Transform Target;
		public float Distance = 9f;
		public float Height = 4.2f;
		public float LookHeight = 1.8f;
		[Tooltip("How quickly the camera swings round behind the horse.")]
		public float Follow = 4f;

		float _yaw;
		bool _placed;

		void LateUpdate()
		{
			if (Target == null) return;

			var targetYaw = Target.eulerAngles.y;
			_yaw = _placed ? Mathf.LerpAngle(_yaw, targetYaw, 1f - Mathf.Exp(-Follow * Time.deltaTime)) : targetYaw;
			_placed = true;

			var behind = Quaternion.Euler(0f, _yaw, 0f) * Vector3.back;
			transform.position = Target.position + behind * Distance + Vector3.up * Height;
			transform.LookAt(Target.position + Vector3.up * LookHeight);
		}

		/// <summary>Jump straight behind the horse, for example after the race is reset.</summary>
		public void Snap()
		{
			_placed = false;
		}
	}
}
