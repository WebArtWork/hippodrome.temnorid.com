using Temnorid.Village;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>The ring track the game builds around the village walls once the village is drawn: its path, gates and start place.</summary>
	public sealed class RingTrack
	{
		public Transform[] Waypoints;
		public Checkpoint[] Checkpoints;
		public Transform[] GridSlots;
		public Vector3 Start;
		public Quaternion StartRotation;
	}

	/// <summary>
	/// Builds the small hippodrome at run time: an oval ridden counter-clockwise just outside the village wall, with a rail on each side,
	/// eight gates (the first is the finish line) and a chequered strip. Its size follows the village, so a bigger village gets a bigger ring.
	/// </summary>
	public static class HippodromeTrack
	{
		public const float TrackWidth = 14f;
		public const int PointCount = 96;
		public const int CheckpointCount = 8;

		/// <summary>The gap between the wall and the inside edge of the track, in metres.</summary>
		public const float WallGap = 9f;

		/// <summary>Where the outer rail is open: the way out of the ring, to the races (the east side).</summary>
		public const int ExitPoint = PointCount / 4;

		public static RingTrack Build(Transform parent, float villageRx, float villageRz, Material template, Material textMaterial)
		{
			var rx = villageRx + WallGap + TrackWidth * 0.5f;
			var rz = villageRz + WallGap + TrackWidth * 0.5f;

			var slide = new PhysicsMaterial("Slide")
			{
				dynamicFriction = 0f,
				staticFriction = 0f,
				bounciness = 0f,
				frictionCombine = PhysicsMaterialCombine.Minimum,
				bounceCombine = PhysicsMaterialCombine.Minimum,
			};
			// A worn dirt road, not a racing oval: a few shades of dirt, a fence of rough timber, and a width and line that wander a little.
			var dirts = new[]
			{
				Mat(template, new Color(0.55f, 0.40f, 0.26f)),
				Mat(template, new Color(0.50f, 0.37f, 0.24f)),
				Mat(template, new Color(0.60f, 0.45f, 0.30f)),
				Mat(template, new Color(0.46f, 0.38f, 0.28f)),
			};
			var timbers = new[]
			{
				Mat(template, new Color(0.42f, 0.29f, 0.18f)),
				Mat(template, new Color(0.35f, 0.25f, 0.16f)),
				Mat(template, new Color(0.48f, 0.34f, 0.21f)),
			};
			var white = Mat(template, new Color(0.92f, 0.92f, 0.9f));
			var random = new System.Random(11);
			var black = Mat(template, new Color(0.08f, 0.08f, 0.08f));
			var pole = Mat(template, new Color(0.95f, 0.55f, 0.12f));

			var root = new GameObject("RingTrack").transform;
			root.SetParent(parent, false);

			// Point 0 is the finish line, at the bottom of the oval with the riders heading +x (counter-clockwise from above).
			var points = new Vector3[PointCount];
			var widths = new float[PointCount];
			for (var i = 0; i < PointCount; i++)
			{
				var angle = (-90f + 360f * i / PointCount) * Mathf.Deg2Rad;
				// The line and the width drift smoothly round the oval: it never looks drawn with a compass.
				var drift = 2.6f * Mathf.Sin(3f * angle + 1.3f) + 1.4f * Mathf.Sin(5f * angle + 0.4f);
				widths[i] = Mathf.Max(11f, TrackWidth + 2.2f * Mathf.Sin(4f * angle + 0.7f) + 1.2f * Mathf.Sin(7f * angle));
				points[i] = new Vector3((rx + drift) * Mathf.Cos(angle), 0f, (rz + drift) * Mathf.Sin(angle));
			}

			var surface = new GameObject("Surface").transform;
			surface.SetParent(root, false);
			var rails = new GameObject("Rails").transform;
			rails.SetParent(root, false);
			for (var i = 0; i < PointCount; i++)
			{
				var j = (i + 1) % PointCount;
				var a = points[i];
				var b = points[j];
				var width = (widths[i] + widths[j]) * 0.5f;
				var piece = Piece(PrimitiveType.Cube, "Track_" + i, surface, (a + b) * 0.5f + Vector3.up * (0.05f + 0.02f * (float)random.NextDouble()), new Vector3(width + 0.6f, 0.1f, (b - a).magnitude + 0.9f), dirts[random.Next(dirts.Length)], false);
				piece.rotation = Quaternion.LookRotation(b - a);

				foreach (var side in new[] { -1f, 1f })
				{
					// The gap to the races: two pieces of the outer rail are left out.
					if (side > 0f && (i == ExitPoint - 1 || i == ExitPoint)) continue;
					var pa = a + Right(points, i) * side * (widths[i] * 0.5f + 0.45f);
					var pb = b + Right(points, j) * side * (widths[j] * 0.5f + 0.45f);
					// Rough timber: each length a little different in height and a little crooked.
					var height = 0.9f + 0.35f * (float)random.NextDouble();
					var rail = Piece(PrimitiveType.Cube, "Rail_" + i + (side < 0 ? "_in" : "_out"), rails, (pa + pb) * 0.5f + Vector3.up * (height * 0.5f), new Vector3(0.4f, height, (pb - pa).magnitude + 0.25f), timbers[random.Next(timbers.Length)], true);
					rail.rotation = Quaternion.LookRotation(pb - pa) * Quaternion.Euler(0f, (float)(random.NextDouble() - 0.5) * 6f, (float)(random.NextDouble() - 0.5) * 8f);
					// The rail looks 1.1 m tall but its collider reaches 4 m, so a jumping horse cannot hop out of the track.
					var collider = rail.GetComponent<BoxCollider>();
					collider.sharedMaterial = slide;
					collider.size = new Vector3(1f, 4f / height, 1f);
					collider.center = new Vector3(0f, (2f - height * 0.5f) / height, 0f);
				}
			}

			var waypointRoot = new GameObject("Waypoints").transform;
			waypointRoot.SetParent(root, false);
			var waypoints = new Transform[PointCount];
			for (var i = 0; i < PointCount; i++)
			{
				var waypoint = new GameObject("Waypoint_" + i.ToString("00")).transform;
				waypoint.SetParent(waypointRoot, false);
				waypoint.position = points[i];
				waypoint.rotation = Quaternion.LookRotation(Tangent(points, i));
				waypoints[i] = waypoint;
			}

			var gateRoot = new GameObject("Checkpoints").transform;
			gateRoot.SetParent(root, false);
			var checkpoints = new Checkpoint[CheckpointCount];
			for (var k = 0; k < CheckpointCount; k++)
			{
				var index = k * (PointCount / CheckpointCount);
				var gate = new GameObject("Checkpoint_" + k);
				gate.transform.SetParent(gateRoot, false);
				gate.transform.SetPositionAndRotation(points[index] + Vector3.up * 2f, Quaternion.LookRotation(Tangent(points, index)));
				var trigger = gate.AddComponent<BoxCollider>();
				trigger.isTrigger = true;
				trigger.size = new Vector3(TrackWidth + 8f, 4f, 3f);
				var checkpoint = gate.AddComponent<Checkpoint>();
				checkpoint.Index = k;
				checkpoints[k] = checkpoint;

				var x = TrackWidth * 0.5f + 1.1f;
				Piece(PrimitiveType.Cylinder, "PoleLeft", gate.transform, gate.transform.TransformPoint(new Vector3(-x, 0f, 0f)), new Vector3(0.25f, 2f, 0.25f), pole, false);
				Piece(PrimitiveType.Cylinder, "PoleRight", gate.transform, gate.transform.TransformPoint(new Vector3(x, 0f, 0f)), new Vector3(0.25f, 2f, 0.25f), pole, false);
				if (k == 0) Piece(PrimitiveType.Cube, "Banner", gate.transform, gate.transform.TransformPoint(new Vector3(0f, 1.7f, 0f)), new Vector3(TrackWidth + 2.7f, 0.7f, 0.3f), white, false);
			}

			// The finish line: a chequered strip across the track at point 0.
			var line = new GameObject("FinishLine").transform;
			line.SetParent(root, false);
			line.SetPositionAndRotation(points[0] + Vector3.up * 0.12f, Quaternion.LookRotation(Tangent(points, 0)));
			for (var row = 0; row < 2; row++)
			{
				for (var col = 0; col < 14; col++)
				{
					var x = -TrackWidth * 0.5f + 0.5f + col;
					Piece(PrimitiveType.Cube, "Check", line, line.TransformPoint(new Vector3(x, 0f, (row - 0.5f) * 0.6f)), new Vector3(1f, 0.02f, 0.6f), (col + row) % 2 == 0 ? white : black, false)
						.rotation = line.rotation;
				}
			}

			BuildExit(root, points, widths, white, pole, textMaterial);

			// The horse stands just past the line.
			var forward = Tangent(points, 0);
			var slot = new GameObject("Slot_0").transform;
			slot.SetParent(root, false);
			var rotation = Quaternion.LookRotation(forward);
			slot.SetPositionAndRotation(points[0] + forward * 6f + Vector3.up * 0.12f, rotation);

			return new RingTrack
			{
				Waypoints = waypoints,
				Checkpoints = checkpoints,
				GridSlots = new[] { slot },
				Start = slot.position,
				StartRotation = rotation,
			};
		}

		/// <summary>Two posts either side of the gap in the outer rail, a banner over it saying where it leads, and the trigger beyond it.</summary>
		static void BuildExit(Transform root, Vector3[] points, float[] widths, Material banner, Material post, Material textMaterial)
		{
			var a = points[ExitPoint - 1] + Right(points, ExitPoint - 1) * (widths[ExitPoint - 1] * 0.5f + 0.45f);
			var b = points[ExitPoint + 1] + Right(points, ExitPoint + 1) * (widths[ExitPoint + 1] * 0.5f + 0.45f);
			var centre = points[ExitPoint] + Right(points, ExitPoint) * (widths[ExitPoint] * 0.5f + 0.45f);
			var outward = Right(points, ExitPoint);

			var gate = new GameObject("RacesGate").transform;
			gate.SetParent(root, false);
			gate.SetPositionAndRotation(centre, Quaternion.LookRotation(outward));

			Piece(PrimitiveType.Cylinder, "PostA", gate, a + Vector3.up * 2.2f, new Vector3(0.4f, 2.2f, 0.4f), post, false);
			Piece(PrimitiveType.Cylinder, "PostB", gate, b + Vector3.up * 2.2f, new Vector3(0.4f, 2.2f, 0.4f), post, false);
			var width = (b - a).magnitude;
			var bar = Piece(PrimitiveType.Cube, "Banner", gate, centre + Vector3.up * 4.3f, new Vector3(width, 1.3f, 0.3f), banner, false);
			bar.rotation = gate.rotation;

			var sign = new GameObject("Sign").transform;
			sign.SetParent(gate, false);
			// Read from inside the ring, riding towards the gap.
			sign.SetPositionAndRotation(centre + Vector3.up * 4.3f - outward * 0.2f, Quaternion.LookRotation(outward));
			sign.gameObject.AddComponent<VillageLabel>(); // turns the text to whoever looks at it
			var mesh = sign.gameObject.AddComponent<TextMesh>();
			mesh.text = "RACES  >>";
			mesh.characterSize = 0.12f;
			mesh.fontSize = 64;
			mesh.anchor = TextAnchor.MiddleCenter;
			mesh.alignment = TextAlignment.Center;
			mesh.color = new Color(0.1f, 0.1f, 0.1f);
			mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			if (textMaterial != null) sign.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;

			// Beyond the gap: a horse that gets here has left the ring.
			var trigger = new GameObject("Exit").AddComponent<BoxCollider>();
			trigger.transform.SetParent(gate, false);
			trigger.transform.localPosition = new Vector3(0f, 2f, 5f);
			trigger.size = new Vector3(width, 4f, 3f);
			trigger.isTrigger = true;
			trigger.gameObject.AddComponent<RaceExitGate>();
		}

		static Vector3 Tangent(Vector3[] p, int i)
		{
			var n = p.Length;
			return (p[(i + 1) % n] - p[(i - 1 + n) % n]).normalized;
		}

		static Vector3 Right(Vector3[] p, int i) => Vector3.Cross(Vector3.up, Tangent(p, i));

		static Material Mat(Material template, Color colour) => new Material(template) { color = colour };

		static Transform Piece(PrimitiveType type, string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material, bool keepCollider)
		{
			var piece = GameObject.CreatePrimitive(type);
			piece.name = name;
			if (!keepCollider)
			{
				var collider = piece.GetComponent<Collider>();
				if (collider != null) Object.Destroy(collider);
			}
			piece.transform.SetParent(parent, false);
			piece.transform.position = worldPosition;
			piece.transform.localScale = scale;
			piece.GetComponent<Renderer>().sharedMaterial = material;
			return piece.transform;
		}
	}
}
