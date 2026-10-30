using System.Collections.Generic;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// A bar across a narrow part of the track. A horse has to be in the air when its front crosses the bar;
	/// one that is not stumbles and loses most of its speed. Nothing blocks the way, so nobody gets stuck:
	/// the bar is knocked flat for a moment instead. Riders cross it in the direction the hurdle faces.
	/// </summary>
	public sealed class Hurdle : MonoBehaviour
	{
		/// <summary>Half the width of the gap between the blocks that narrow the track, in metres.</summary>
		public float GapHalfWidth = 4f;
		/// <summary>How far before and after the bar the track is narrow, in metres.</summary>
		public float ZoneHalfLength = 8f;
		/// <summary>A horse must be at least this high, in metres, when its front crosses.</summary>
		public float ClearHeight = 0.9f;
		/// <summary>How far ahead of its centre a horse's front is, in metres.</summary>
		public float FrontOffset = 1f;
		/// <summary>The top bar, knocked down when a horse hits it.</summary>
		public Transform Bar;

		public static readonly List<Hurdle> All = new List<Hurdle>();

		readonly Dictionary<HorseController, float> _last = new Dictionary<HorseController, float>();
		float _knockedFor;
		Vector3 _barRest;
		Quaternion _barRestRotation;

		void OnEnable()
		{
			if (!All.Contains(this)) All.Add(this);
			if (Bar != null)
			{
				_barRest = Bar.localPosition;
				_barRestRotation = Bar.localRotation;
			}
		}

		void OnDisable()
		{
			All.Remove(this);
		}

		void FixedUpdate()
		{
			foreach (var horse in HorseController.All)
			{
				var front = horse.transform.position + horse.transform.forward * FrontOffset;
				var local = transform.InverseTransformPoint(front);

				if (_last.TryGetValue(horse, out var before)
					&& before < 0f && local.z >= 0f && local.z - before < 6f
					&& Mathf.Abs(local.x) <= GapHalfWidth + 1f)
				{
					if (horse.Height < ClearHeight)
					{
						horse.HitBar();
						_knockedFor = 1.5f;
					}
					else horse.ClearedBar();
				}
				_last[horse] = local.z;
			}
		}

		void Update()
		{
			if (Bar == null) return;
			if (_knockedFor > 0f)
			{
				_knockedFor -= Time.deltaTime;
				Bar.localPosition = Vector3.Lerp(Bar.localPosition, new Vector3(_barRest.x, 0.1f, _barRest.z + 0.4f), 12f * Time.deltaTime);
				Bar.localRotation = Quaternion.Slerp(Bar.localRotation, Quaternion.Euler(0f, 0f, 8f), 12f * Time.deltaTime);
			}
			else
			{
				Bar.localPosition = Vector3.Lerp(Bar.localPosition, _barRest, 6f * Time.deltaTime);
				Bar.localRotation = Quaternion.Slerp(Bar.localRotation, _barRestRotation, 6f * Time.deltaTime);
			}
		}

		/// <summary>The distance, in metres, from a horse's front to the bar along the hurdle's direction; negative once past it.</summary>
		public float DistanceAhead(HorseController horse)
		{
			var front = horse.transform.position + horse.transform.forward * FrontOffset;
			return -transform.InverseTransformPoint(front).z;
		}

		/// <summary>True when the horse is heading the way the hurdle faces and is lined up with the gap.</summary>
		public bool Faces(HorseController horse)
		{
			var front = horse.transform.position + horse.transform.forward * FrontOffset;
			var local = transform.InverseTransformPoint(front);
			return Vector3.Dot(horse.transform.forward, transform.forward) > 0.6f && Mathf.Abs(local.x) <= GapHalfWidth;
		}
	}
}
