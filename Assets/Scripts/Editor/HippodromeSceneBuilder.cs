using ITKamianets.Engine.Graybox;
using ITKamianets.Engine.Graybox.EditorTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Temnorid.Hippodrome.EditorTools
{
	/// <summary>
	/// Builds the whole game from nothing but Unity primitives: the track, the horses, the camera, the
	/// light and the race. Run it from the menu (Temnorid > Hippodrome > Build Scene) or from the command
	/// line (<c>-executeMethod Temnorid.Hippodrome.EditorTools.HippodromeSceneBuilder.Build</c>); it can
	/// be run again at any time and rebuilds <c>Assets/Scenes/Main.unity</c> from scratch.
	/// </summary>
	public static class HippodromeSceneBuilder
	{
		const string ScenePath = "Assets/Scenes/Main.unity";
		const string MaterialDir = "Assets/Materials";

		// The track is a stadium: two straights joined by two half circles, ridden counter-clockwise from above.
		const float StraightHalf = 60f;
		const float CenterRadius = 38f;
		const float TrackWidth = 14f;
		const int CheckpointCount = 8;
		const int PointsPerCheckpoint = 9;

		[MenuItem("Temnorid/Hippodrome/Build Scene")]
		public static void Build()
		{
			SetupProject();

			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.74f);

			var points = BuildPath();
			var slide = SlideMaterial();

			BuildEnvironment(points, slide);
			BuildNarrowPasses(points, slide);
			var waypoints = BuildWaypoints(points);
			var checkpoints = BuildCheckpoints(points);
			var slots = BuildGrid(points);
			var racers = BuildRacers(slots, slide);
			BuildCameraAndLight(racers[0].transform);

			var game = new GameObject("Game");
			var race = game.AddComponent<RaceController>();
			race.Racers = racers;
			race.Checkpoints = checkpoints;
			race.Waypoints = waypoints;
			race.GridSlots = slots;
			race.Laps = 3;
			var hud = game.AddComponent<RaceHud>();
			hud.Race = race;

			EditorSceneManager.SaveScene(scene, ScenePath);
			EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
			AssetDatabase.SaveAssets();
			Debug.Log("[Hippodrome] Scene built: " + ScenePath);
		}

		static void SetupProject()
		{
			PlayerSettings.productName = "Temnorid Hippodrome";
			PlayerSettings.companyName = "Temnorid";
			PlayerSettings.bundleVersion = "0.2.0";
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.itkamianets.hippodrome");
			PlayerSettings.runInBackground = true;
			PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
		}

		// ---- track ----

		static Vector3[] BuildPath()
		{
			var points = new List<Vector3>();
			var h = StraightHalf;
			var r = CenterRadius;

			// Index 0 is the finish line, in the middle of the bottom straight, riders heading +x.
			for (var i = 0; i < 6; i++) points.Add(new Vector3(i * 10f, 0f, -r));
			for (var k = 0; k < 24; k++)
			{
				var a = (-90f + k * 7.5f) * Mathf.Deg2Rad;
				points.Add(new Vector3(h + r * Mathf.Cos(a), 0f, r * Mathf.Sin(a)));
			}
			for (var i = 0; i < 12; i++) points.Add(new Vector3(h - i * 10f, 0f, r));
			for (var k = 0; k < 24; k++)
			{
				var a = (90f + k * 7.5f) * Mathf.Deg2Rad;
				points.Add(new Vector3(-h + r * Mathf.Cos(a), 0f, r * Mathf.Sin(a)));
			}
			for (var i = 0; i < 6; i++) points.Add(new Vector3(-h + i * 10f, 0f, -r));
			return points.ToArray();
		}

		static Vector3 Tangent(Vector3[] p, int i)
		{
			var n = p.Length;
			return (p[(i + 1) % n] - p[(i - 1 + n) % n]).normalized;
		}

		static Vector3 Right(Vector3[] p, int i) => Vector3.Cross(Vector3.up, Tangent(p, i));

		static void BuildEnvironment(Vector3[] p, PhysicsMaterial slide)
		{
			var root = new GameObject("Environment").transform;
			var grass = Mat("Grass", new Color(0.30f, 0.52f, 0.26f));
			var dirt = Mat("Dirt", new Color(0.55f, 0.40f, 0.26f));
			var rail1 = Mat("RailRed", new Color(0.78f, 0.18f, 0.16f));
			var rail2 = Mat("RailWhite", new Color(0.92f, 0.92f, 0.9f));
			var black = Mat("Black", new Color(0.08f, 0.08f, 0.08f));

			// No collider on the ground or the track: horses are held at one height and never fall.
			Prim(PrimitiveType.Cube, "Ground", root, new Vector3(0f, -0.5f, 0f), new Vector3(420f, 1f, 300f), grass);

			var trackRoot = new GameObject("Track").transform;
			trackRoot.SetParent(root, false);
			trackRoot.gameObject.AddComponent<AssetIdTag>().Id = "track/surface";
			var railRoot = new GameObject("Rails").transform;
			railRoot.SetParent(root, false);
			railRoot.gameObject.AddComponent<AssetIdTag>().Id = "track/rail";

			var n = p.Length;
			for (var i = 0; i < n; i++)
			{
				var j = (i + 1) % n;
				var a = p[i];
				var b = p[j];

				var segment = Prim(PrimitiveType.Cube, "Track_" + i, trackRoot, (a + b) * 0.5f + Vector3.up * 0.05f, new Vector3(TrackWidth + 0.6f, 0.1f, (b - a).magnitude + 0.9f), dirt);
				segment.transform.rotation = Quaternion.LookRotation(b - a);

				// A rail on each side. Each is measured between its own two end points, so on the outside
				// of a bend (where the rail is longer than the centre line) there are no gaps to slip through.
				foreach (var side in new[] { -1f, 1f })
				{
					var offset = side * (TrackWidth * 0.5f + 0.45f);
					var pa = a + Right(p, i) * offset;
					var pb = b + Right(p, j) * offset;
					var rail = Prim(PrimitiveType.Cube, "Rail_" + i + (side < 0 ? "_in" : "_out"), railRoot, (pa + pb) * 0.5f + Vector3.up * 0.55f, new Vector3(0.5f, 1.1f, (pb - pa).magnitude + 0.25f), (i / 2) % 2 == 0 ? rail1 : rail2, true);
					rail.transform.rotation = Quaternion.LookRotation(pb - pa);
					// The rail is 1.1 m tall to look at but its collider reaches 4 m, so a jumping horse cannot hop out of the track.
					var railCollider = rail.GetComponent<BoxCollider>();
					railCollider.sharedMaterial = slide;
					railCollider.size = new Vector3(1f, 4f / 1.1f, 1f);
					railCollider.center = new Vector3(0f, (2f - 0.55f) / 1.1f, 0f);
				}
			}

			// Finish line painted across the track at point 0: a chequered strip.
			var line = new GameObject("FinishLine").transform;
			line.SetParent(root, false);
			line.position = p[0] + Vector3.up * 0.11f;
			line.rotation = Quaternion.LookRotation(Tangent(p, 0));
			line.gameObject.AddComponent<AssetIdTag>().Id = "track/finish-line";
			var white = Mat("White", Color.white);
			for (var row = 0; row < 2; row++)
			{
				for (var col = 0; col < 14; col++)
				{
					var x = -TrackWidth * 0.5f + 0.5f + col;
					Prim(PrimitiveType.Cube, "Check", line, new Vector3(x, 0f, (row - 0.5f) * 0.6f), new Vector3(1f, 0.02f, 0.6f), (col + row) % 2 == 0 ? white : black);
				}
			}
		}

		// ---- narrow passes with a bar to jump ----

		const float PassGapHalf = 4f;

		/// <summary>Two places where blocks narrow the track to a gap, with a bar across it. Riders jump the bar.</summary>
		static void BuildNarrowPasses(Vector3[] p, PhysicsMaterial slide)
		{
			var root = new GameObject("NarrowPasses").transform;
			var hay = Mat("HayBale", new Color(0.82f, 0.68f, 0.30f));
			var wood = Mat("HurdleWood", new Color(0.50f, 0.32f, 0.18f));
			var stripe = Mat("HurdleStripe", new Color(0.92f, 0.92f, 0.9f));

			BuildNarrowPass(root, p, 36, "A", hay, wood, stripe, slide);
			BuildNarrowPass(root, p, 69, "B", hay, wood, stripe, slide);
		}

		static void BuildNarrowPass(Transform parent, Vector3[] p, int index, string name, Material block, Material wood, Material stripe, PhysicsMaterial slide)
		{
			var pass = new GameObject("Pass_" + name);
			pass.transform.SetParent(parent, false);
			pass.transform.SetPositionAndRotation(p[index], Quaternion.LookRotation(Tangent(p, index)));
			pass.AddComponent<AssetIdTag>().Id = "track/narrow-pass";

			var half = TrackWidth * 0.5f + 0.45f;
			foreach (var side in new[] { -1f, 1f })
			{
				// A long block either side of the gap, and a shorter, thinner one before and after it so the track narrows gently.
				AddBlock(pass.transform, "Block", new Vector3(side * (PassGapHalf + (half - PassGapHalf) * 0.5f), 1f, 0f), new Vector3(half - PassGapHalf, 2f, 8f), block, slide);
				foreach (var end in new[] { -1f, 1f })
				{
					var inner = PassGapHalf + 1.5f;
					AddBlock(pass.transform, "Taper", new Vector3(side * (inner + (half - inner) * 0.5f), 1f, end * 5.5f), new Vector3(half - inner, 2f, 3f), block, slide);
				}
			}

			// The hurdle: two posts and two bars across the gap. Only the top bar matters, and it falls when hit.
			var hurdle = new GameObject("Hurdle");
			hurdle.transform.SetParent(pass.transform, false);
			hurdle.AddComponent<AssetIdTag>().Id = "track/hurdle";
			var component = hurdle.AddComponent<Hurdle>();
			component.GapHalfWidth = PassGapHalf;
			component.ZoneHalfLength = 8f;
			component.ClearHeight = 0.9f;

			foreach (var x in new[] { -PassGapHalf + 0.1f, PassGapHalf - 0.1f })
				Prim(PrimitiveType.Cylinder, "Post", hurdle.transform, new Vector3(x, 0.55f, 0f), new Vector3(0.2f, 0.55f, 0.2f), wood);
			Prim(PrimitiveType.Cube, "LowBar", hurdle.transform, new Vector3(0f, 0.4f, 0f), new Vector3(PassGapHalf * 2f - 0.2f, 0.14f, 0.14f), wood);
			var top = Prim(PrimitiveType.Cube, "TopBar", hurdle.transform, new Vector3(0f, 0.85f, 0f), new Vector3(PassGapHalf * 2f - 0.2f, 0.16f, 0.16f), stripe);
			component.Bar = top.transform;
		}

		static void AddBlock(Transform parent, string name, Vector3 position, Vector3 size, Material material, PhysicsMaterial slide)
		{
			var block = Prim(PrimitiveType.Cube, name, parent, position, size, material, true);
			block.GetComponent<BoxCollider>().sharedMaterial = slide;
		}

		static Transform[] BuildWaypoints(Vector3[] p)
		{
			var root = new GameObject("Waypoints").transform;
			var list = new Transform[p.Length];
			for (var i = 0; i < p.Length; i++)
			{
				var waypoint = new GameObject("Waypoint_" + i.ToString("00")).transform;
				waypoint.SetParent(root, false);
				waypoint.position = p[i];
				waypoint.rotation = Quaternion.LookRotation(Tangent(p, i));
				list[i] = waypoint;
			}
			return list;
		}

		static Checkpoint[] BuildCheckpoints(Vector3[] p)
		{
			var root = new GameObject("Checkpoints").transform;
			var poleMaterial = Mat("PoleOrange", new Color(0.95f, 0.55f, 0.12f));
			var bannerMaterial = Mat("White", Color.white);
			var list = new Checkpoint[CheckpointCount];

			for (var k = 0; k < CheckpointCount; k++)
			{
				var index = k * PointsPerCheckpoint;
				var gate = new GameObject("Checkpoint_" + k);
				gate.transform.SetParent(root, false);
				gate.transform.position = p[index] + Vector3.up * 2f;
				gate.transform.rotation = Quaternion.LookRotation(Tangent(p, index));
				gate.AddComponent<AssetIdTag>().Id = "track/checkpoint";

				var trigger = gate.AddComponent<BoxCollider>();
				trigger.isTrigger = true;
				trigger.size = new Vector3(TrackWidth + 1f, 4f, 3f);

				var checkpoint = gate.AddComponent<Checkpoint>();
				checkpoint.Index = k;
				list[k] = checkpoint;

				var x = TrackWidth * 0.5f + 1.1f;
				Prim(PrimitiveType.Cylinder, "PoleLeft", gate.transform, new Vector3(-x, 0f, 0f), new Vector3(0.25f, 2f, 0.25f), poleMaterial);
				Prim(PrimitiveType.Cylinder, "PoleRight", gate.transform, new Vector3(x, 0f, 0f), new Vector3(0.25f, 2f, 0.25f), poleMaterial);
				if (k == 0) Prim(PrimitiveType.Cube, "Banner", gate.transform, new Vector3(0f, 1.7f, 0f), new Vector3(TrackWidth + 2.7f, 0.7f, 0.3f), bannerMaterial);
			}
			return list;
		}

		/// <summary>Four places just past the line: the player at the back on the outside, three bots around.</summary>
		static Transform[] BuildGrid(Vector3[] p)
		{
			var root = new GameObject("GridSlots").transform;
			var forward = Tangent(p, 0);
			var right = Right(p, 0);
			var rotation = Quaternion.LookRotation(forward);
			var spots = new[] { (5f, 3f), (12f, -3f), (12f, 3f), (5f, -3f) };

			var slots = new Transform[spots.Length];
			for (var i = 0; i < spots.Length; i++)
			{
				var slot = new GameObject("Slot_" + i).transform;
				slot.SetParent(root, false);
				slot.SetPositionAndRotation(p[0] + forward * spots[i].Item1 + right * spots[i].Item2 + Vector3.up * 0.12f, rotation);
				slots[i] = slot;
			}
			return slots;
		}

		// ---- horses ----

		static Racer[] BuildRacers(Transform[] slots, PhysicsMaterial slide)
		{
			var root = new GameObject("Racers").transform;
			return new[]
			{
				Horse(root, slots[0], "You", new Color(0.45f, 0.30f, 0.18f), new Color(0.18f, 0.45f, 0.95f), true, 1f, 3f, slide),
				Horse(root, slots[1], "Ember", new Color(0.62f, 0.34f, 0.16f), new Color(0.85f, 0.2f, 0.2f), false, 0.97f, -3.5f, slide),
				Horse(root, slots[2], "Willow", new Color(0.25f, 0.20f, 0.17f), new Color(0.25f, 0.7f, 0.3f), false, 0.94f, 3.5f, slide),
				Horse(root, slots[3], "Storm", new Color(0.72f, 0.68f, 0.6f), new Color(0.95f, 0.8f, 0.2f), false, 0.91f, 0f, slide),
			};
		}

		static Racer Horse(Transform parent, Transform slot, string name, Color coat, Color rider, bool player, float skill, float lane, PhysicsMaterial slide)
		{
			var root = new GameObject("Horse_" + name);
			root.transform.SetParent(parent, false);
			root.transform.SetPositionAndRotation(slot.position, slot.rotation);

			var body = root.AddComponent<Rigidbody>();
			body.mass = 450f;
			body.useGravity = false;

			var collider = root.AddComponent<BoxCollider>();
			collider.center = new Vector3(0f, 1.1f, 0f);
			collider.size = new Vector3(1.0f, 2.0f, 2.6f);
			collider.sharedMaterial = slide;

			var controller = root.AddComponent<HorseController>();
			var racer = root.AddComponent<Racer>();
			racer.DisplayName = name;
			racer.Colour = rider;
			racer.IsPlayer = player;
			root.AddComponent<AssetIdTag>().Id = "horses/basic";

			if (player) root.AddComponent<PlayerDriver>();
			var bot = root.AddComponent<BotDriver>();
			bot.LaneOffset = lane;
			bot.Skill = skill;
			bot.JumpTimingError = player ? 1f : Mathf.Lerp(0.8f, 4.2f, Mathf.InverseLerp(0.97f, 0.91f, skill));
			bot.enabled = !player;

			var visual = new GameObject("Visual").transform;
			visual.SetParent(root.transform, false);
			controller.Visual = visual;

			var coatMaterial = Mat("Coat_" + name, coat);
			var maneMaterial = Mat("Mane", new Color(0.12f, 0.09f, 0.07f));
			var riderMaterial = Mat("Rider_" + name, rider);
			var skin = Mat("Skin", new Color(0.88f, 0.70f, 0.55f));

			Prim(PrimitiveType.Cube, "Body", visual, new Vector3(0f, 1.25f, 0f), new Vector3(0.9f, 0.9f, 1.9f), coatMaterial);
			var neck = Prim(PrimitiveType.Cube, "Neck", visual, new Vector3(0f, 1.8f, 1.0f), new Vector3(0.45f, 0.95f, 0.5f), coatMaterial);
			neck.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
			var head = Prim(PrimitiveType.Cube, "Head", visual, new Vector3(0f, 2.15f, 1.55f), new Vector3(0.38f, 0.42f, 0.85f), coatMaterial);
			head.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
			Prim(PrimitiveType.Cube, "Mane", visual, new Vector3(0f, 2.2f, 0.95f), new Vector3(0.12f, 0.8f, 0.3f), maneMaterial).transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
			var tail = Prim(PrimitiveType.Cylinder, "Tail", visual, new Vector3(0f, 1.2f, -1.1f), new Vector3(0.1f, 0.45f, 0.1f), maneMaterial);
			tail.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
			foreach (var x in new[] { -0.32f, 0.32f })
			{
				foreach (var z in new[] { -0.7f, 0.7f })
					Prim(PrimitiveType.Cylinder, "Leg", visual, new Vector3(x, 0.62f, z), new Vector3(0.18f, 0.62f, 0.18f), coatMaterial);
			}
			Prim(PrimitiveType.Capsule, "Rider", visual, new Vector3(0f, 2.1f, -0.15f), new Vector3(0.5f, 0.5f, 0.5f), riderMaterial);
			Prim(PrimitiveType.Sphere, "RiderHead", visual, new Vector3(0f, 2.8f, -0.1f), new Vector3(0.42f, 0.42f, 0.42f), skin);

			return racer;
		}

		// ---- camera, light, materials ----

		static void BuildCameraAndLight(Transform target)
		{
			var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
			var camera = cameraObject.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.56f, 0.72f, 0.9f);
			camera.fieldOfView = 65f;
			camera.nearClipPlane = 0.3f;
			camera.farClipPlane = 600f;
			cameraObject.AddComponent<AudioListener>();
			var follow = cameraObject.AddComponent<RaceCamera>();
			follow.Target = target;

			var lightObject = new GameObject("Sun");
			lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
			var light = lightObject.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.15f;
			light.shadows = LightShadows.Soft;
		}

		static PhysicsMaterial SlideMaterial()
		{
			EnsureMaterialFolder();
			var path = MaterialDir + "/Slide.asset";
			var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
			if (material == null)
			{
				material = new PhysicsMaterial("Slide");
				AssetDatabase.CreateAsset(material, path);
			}
			material.dynamicFriction = 0f;
			material.staticFriction = 0f;
			material.bounciness = 0f;
			material.frictionCombine = PhysicsMaterialCombine.Minimum;
			material.bounceCombine = PhysicsMaterialCombine.Minimum;
			EditorUtility.SetDirty(material);
			return material;
		}

		static void EnsureMaterialFolder()
		{
			if (!AssetDatabase.IsValidFolder(MaterialDir)) AssetDatabase.CreateFolder("Assets", "Materials");
		}

		// Shared with the other games: 3d-unity-graybox.
		static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = false) => GrayboxBuilder.Prim(type, name, parent, position, scale, material, keepCollider);

		static Material Mat(string name, Color colour) => GrayboxBuilder.Mat(name, colour, MaterialDir, 0.15f);
	}
}
