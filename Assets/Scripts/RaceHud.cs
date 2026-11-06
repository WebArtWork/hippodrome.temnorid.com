using ITKamianets.Engine.Shell;
using ITKamianets.Engine.UI;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// The game's screens, drawn with plain uGUI built in code: the HUD (laps, time, place, and stamina in the footer), the pause menu
	/// and, in the races, the results. It only shows what <see cref="RaceController"/> says. There is no start screen: both the village
	/// ring and the races begin as soon as the track is there.
	/// </summary>
	public sealed class RaceHud : MonoBehaviour
	{
		public RaceController Race;

		/// <summary>The name in the middle of the top bar.</summary>
		public string Title = "Village";

		static readonly Color Panel = new Color(0.06f, 0.05f, 0.09f, 0.82f);
		static readonly Color Accent = new Color(0.84f, 0.63f, 0.23f);

		UiBuilder _ui;
		GameObject _hud, _pause, _results;
		ShellChrome _chrome;
		Text _lap, _place, _time, _bestLap, _center, _boostState, _staminaText;
		Image _stamina;
		Text _resultsTitle, _resultsNames, _resultsTimes, _resultsLaps, _resultsNote;
		RacePhase _shown = (RacePhase)(-1);
		bool _started;

		/// <summary>One horse alone (the ring round the village) or a race against others.</summary>
		bool IsRace => Race.Racers.Length > 1;

		void Start()
		{
			_ui = new UiBuilder
			{
				ButtonHighlight = new Color(1f, 0.9f, 0.6f),
				ButtonSelected = new Color(1f, 0.85f, 0.45f),
				ButtonPressed = new Color(0.8f, 0.65f, 0.3f),
			};
			UiBuilder.EnsureEventSystem(transform);

			var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);
			var canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 0.5f;

			_chrome = ShellChrome.Create(transform);
			_chrome.SetTitle(Title);

			BuildHud(canvas.transform);
			BuildPause(canvas.transform);
			BuildResults(canvas.transform);
			if (TouchButton.IsNeeded) BuildTouch(_hud.transform);
		}

		// On a phone or tablet: steer with two thumbs on the left, boost and jump on the right, brake in between. The horse gallops by itself.
		void BuildTouch(Transform parent)
		{
			var driver = Race.Player.PlayerDriver;
			if (driver == null) return;
			driver.SteerLeft = TouchButton.Create(_ui, parent, "<", new Vector2(0.02f, 0.14f), new Vector2(0.12f, 0.34f), 60);
			driver.SteerRight = TouchButton.Create(_ui, parent, ">", new Vector2(0.13f, 0.14f), new Vector2(0.23f, 0.34f), 60);
			driver.Brake = TouchButton.Create(_ui, parent, "Brake", new Vector2(0.38f, 0.14f), new Vector2(0.52f, 0.28f), 30);
			driver.Boost = TouchButton.Create(_ui, parent, "Boost", new Vector2(0.88f, 0.14f), new Vector2(0.98f, 0.3f), 30);
			driver.Jump = TouchButton.Create(_ui, parent, "Jump", new Vector2(0.76f, 0.14f), new Vector2(0.86f, 0.3f), 30);
		}

		void Update()
		{
			if (Race == null || _hud == null) return;
			var phase = Race.Phase;

			if (phase != _shown)
			{
				_shown = phase;
				_hud.SetActive(phase != RacePhase.Menu);
				_pause.SetActive(phase == RacePhase.Paused);
				_results.SetActive(phase == RacePhase.Finished);
				SelectFirstButton(phase == RacePhase.Paused ? _pause : phase == RacePhase.Finished ? _results : null);
			}

			if (phase == RacePhase.Menu)
			{
				// The track is made when the game starts (round the village): the ride begins the moment it is ready.
				if (!_started && Race.Rules != null && Race.Net == null)
				{
					_started = true;
					Race.RequestStart();
				}
				return;
			}

			var progress = Race.Rules.Racers[0];
			_lap.text = Race.Laps >= 999 ? $"Lap {progress.Lap + 1}" : $"Lap {Mathf.Min(progress.Lap + 1, Race.Laps)} / {Race.Laps}";
			_place.text = IsRace ? $"Position {Race.PlayerPlace} / {Race.Racers.Length}" : "";
			_time.text = FormatTime(Race.RaceTime);
			_bestLap.text = progress.BestLap > 0f ? $"Best lap {FormatTime(progress.BestLap)}" : "";

			var stamina = Race.Player.Horse.Stamina;
			UiBuilder.SetFill(_stamina, stamina.Value);
			UiBuilder.SetText(_staminaText, $"Stamina {Mathf.RoundToInt(stamina.Value * 100f)}");
			_stamina.color = stamina.Exhausted ? new Color(0.85f, 0.25f, 0.2f) : stamina.Value < HorseStamina.TiredBelow ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.35f, 0.8f, 0.4f);
			if (stamina.Exhausted) { _boostState.text = "EXHAUSTED"; _boostState.color = new Color(0.95f, 0.35f, 0.3f); }
			else if (stamina.Boosting) { _boostState.text = "BOOST"; _boostState.color = Accent; }
			else if (stamina.Ready) { _boostState.text = "Shift: boost ready"; _boostState.color = Color.white; }
			else { _boostState.text = "Shift: resting"; _boostState.color = new Color(1f, 1f, 1f, 0.55f); }

			if (phase == RacePhase.Countdown) _center.text = Mathf.CeilToInt(Race.CountdownRemaining).ToString();
			else if (phase == RacePhase.Racing && Race.RaceTime < 1f) _center.text = "GO!";
			else _center.text = "";

			if (phase == RacePhase.Finished) FillResults();
		}

		// ---- screens ----

		void BuildHud(Transform parent)
		{
			_hud = new GameObject("RaceHud", typeof(RectTransform));
			_hud.transform.SetParent(parent, false);
			var hudRect = (RectTransform)_hud.transform;
			hudRect.anchorMin = new Vector2(0f, _chrome.BottomFraction);
			hudRect.anchorMax = new Vector2(1f, 1f - _chrome.TopFraction);
			hudRect.offsetMin = Vector2.zero;
			hudRect.offsetMax = Vector2.zero;

			_lap = Label(_hud.transform, "Lap", "", 44, TextAnchor.UpperLeft, new Vector2(0.02f, 0.9f), new Vector2(0.35f, 0.99f), Color.white);
			_bestLap = Label(_hud.transform, "BestLap", "", 28, TextAnchor.UpperLeft, new Vector2(0.02f, 0.85f), new Vector2(0.35f, 0.9f), new Color(1f, 1f, 1f, 0.8f));
			_place = Label(_hud.transform, "Place", "", 44, TextAnchor.UpperRight, new Vector2(0.65f, 0.9f), new Vector2(0.98f, 0.99f), Accent);
			_time = Label(_hud.transform, "Time", "", 44, TextAnchor.UpperCenter, new Vector2(0.38f, 0.9f), new Vector2(0.62f, 0.99f), Color.white);
			_center = Label(_hud.transform, "Center", "", 160, TextAnchor.MiddleCenter, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.75f), Accent);

			// The footer: the horse's stamina in the middle of the bottom edge, its number written inside the bar.
			var footer = UiBuilder.Box("Footer", _hud.transform, new Vector2(0.3f, 0.015f), new Vector2(0.7f, 0.075f), Panel);
			footer.GetComponent<Image>().raycastTarget = false;
			_stamina = UiBuilder.Bar("Stamina", footer.transform, new Vector2(0.03f, 0.2f), new Vector2(0.97f, 0.8f), new Color(0.35f, 0.8f, 0.4f));
			_staminaText = Label(_stamina.transform.parent, "StaminaText", "", 26, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Color.white);
			_boostState = Label(_hud.transform, "BoostState", "", 26, TextAnchor.LowerCenter, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.12f), Color.white);
		}

		void BuildPause(Transform parent)
		{
			_pause = UiBuilder.Box("PauseMenu", parent, new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.7f), Panel);
			Label(_pause.transform, "Title", "PAUSED", 56, TextAnchor.MiddleCenter, new Vector2(0f, 0.68f), new Vector2(1f, 0.96f), Accent);
			Button(_pause.transform, "Resume", new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.62f), () => Race.Resume());
			if (IsRace) Button(_pause.transform, "Back to the village", new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.38f), () => HippodromeScenes.LoadVillage());
			Button(_pause.transform, "Save and quit", new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.18f), () => Race.Quit());
		}

		void BuildResults(Transform parent)
		{
			_results = UiBuilder.Box("Results", parent, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), Panel);
			_resultsTitle = Label(_results.transform, "Title", "", 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.84f), new Vector2(1f, 0.98f), Accent);
			_resultsNote = Label(_results.transform, "Note", "", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0.77f), new Vector2(1f, 0.84f), Color.white);
			_resultsNames = Label(_results.transform, "Names", "", 34, TextAnchor.UpperLeft, new Vector2(0.06f, 0.36f), new Vector2(0.5f, 0.76f), Color.white);
			_resultsTimes = Label(_results.transform, "Times", "", 34, TextAnchor.UpperRight, new Vector2(0.5f, 0.36f), new Vector2(0.72f, 0.76f), Color.white);
			_resultsLaps = Label(_results.transform, "BestLaps", "", 34, TextAnchor.UpperRight, new Vector2(0.72f, 0.36f), new Vector2(0.95f, 0.76f), new Color(1f, 1f, 1f, 0.75f));
			Button(_results.transform, "Race again", new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.32f), () => Race.RequestStart());
			Button(_results.transform, "Back to the village", new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.17f), () => HippodromeScenes.LoadVillage());
		}

		void FillResults()
		{
			var rules = Race.Rules;
			var player = rules.Racers[0];
			_resultsTitle.text = player.Finished ? $"You finished {Ordinal(player.FinishPlace)}" : "Race over";
			var note = new StringBuilder();
			if (Race.NewBestTotal) note.Append("New best time!  ");
			if (Race.NewBestLap) note.Append("New best lap!");
			_resultsNote.text = note.ToString();

			var names = new StringBuilder();
			var times = new StringBuilder();
			var laps = new StringBuilder();
			for (var place = 0; place < Race.Order.Length; place++)
			{
				var racer = rules.Racers[Race.Order[place]];
				names.Append(place + 1).Append(".  ").Append(racer.Name).Append('\n');
				times.Append(racer.Finished ? FormatTime(racer.FinishTime) : "riding").Append('\n');
				laps.Append(racer.BestLap > 0f ? FormatTime(racer.BestLap) : "-").Append('\n');
			}
			_resultsNames.text = names.ToString();
			_resultsTimes.text = times.ToString();
			_resultsLaps.text = laps.ToString();
		}

		// ---- building blocks ----

		Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 min, Vector2 max, Color colour) => _ui.Label(name, parent, text, size, anchor, min, max, colour);

		void Button(Transform parent, string text, Vector2 min, Vector2 max, System.Action onClick) => _ui.Button(text, parent, min, max, onClick);

		static void SelectFirstButton(GameObject screen)
		{
			if (EventSystem.current == null) return;
			EventSystem.current.SetSelectedGameObject(null);
			if (screen == null) return;
			var button = screen.GetComponentInChildren<UnityEngine.UI.Button>();
			if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
		}

		// ---- text ----

		public static string FormatTime(float seconds)
		{
			if (seconds <= 0f) return "--";
			var minutes = (int)(seconds / 60f);
			return $"{minutes}:{seconds - minutes * 60f:00.00}";
		}

		static string Ordinal(int place)
		{
			switch (place)
			{
				case 1: return "1st";
				case 2: return "2nd";
				case 3: return "3rd";
				default: return place + "th";
			}
		}
	}
}
