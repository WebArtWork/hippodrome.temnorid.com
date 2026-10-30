using ITKamianets.Engine.UI;
using System;
using System.Text;
using ITKamianets.Engine.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// All of the game's screens, drawn with plain uGUI built in code: the start screen, the race HUD,
	/// the pause menu and the results. It only shows what <see cref="RaceController"/> says.
	/// </summary>
	public sealed class RaceHud : MonoBehaviour
	{
		public RaceController Race;

		static readonly Color Panel = new Color(0.06f, 0.05f, 0.09f, 0.82f);
		static readonly Color Accent = new Color(0.84f, 0.63f, 0.23f);

		UiBuilder _ui;
		GameObject _menu, _hud, _pause, _results;
		Text _records, _lap, _place, _time, _bestLap, _center, _boostState;
		Image _stamina;
		Text _resultsTitle, _resultsNames, _resultsTimes, _resultsLaps, _resultsNote;
		RacePhase _shown = (RacePhase)(-1);

		void Start()
		{
			_ui = new UiBuilder
			{
				ButtonHighlight = new Color(1f, 0.9f, 0.6f),
				ButtonSelected = new Color(1f, 0.85f, 0.45f),
				ButtonPressed = new Color(0.8f, 0.65f, 0.3f),
			};
			EnsureEventSystem();

			var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasObject.transform.SetParent(transform, false);
			var canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.matchWidthOrHeight = 0.5f;

			BuildMenu(canvas.transform);
			BuildHud(canvas.transform);
			BuildPause(canvas.transform);
			BuildResults(canvas.transform);
		}

		void Update()
		{
			if (Race == null || _menu == null) return;
			var phase = Race.Phase;

			if (phase != _shown)
			{
				_shown = phase;
				_menu.SetActive(phase == RacePhase.Menu);
				_hud.SetActive(phase != RacePhase.Menu);
				_pause.SetActive(phase == RacePhase.Paused);
				_results.SetActive(phase == RacePhase.Finished);
				if (phase == RacePhase.Menu) _records.text = RecordsText();
				SelectFirstButton(phase == RacePhase.Menu ? _menu : phase == RacePhase.Paused ? _pause : phase == RacePhase.Finished ? _results : null);
			}

			if (phase == RacePhase.Menu) return;

			var progress = Race.Rules.Racers[0];
			_lap.text = $"Lap {Mathf.Min(progress.Lap + 1, Race.Laps)} / {Race.Laps}";
			_place.text = $"Position {Race.PlayerPlace} / {Race.Racers.Length}";
			_time.text = FormatTime(Race.RaceTime);
			_bestLap.text = progress.BestLap > 0f ? $"Best lap {FormatTime(progress.BestLap)}" : "";

			var stamina = Race.Player.Horse.Stamina;
			UiBuilder.SetFill(_stamina, stamina.Value);
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

		void BuildMenu(Transform parent)
		{
			_menu = Box("StartScreen", parent, new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.85f), Panel);
			Label(_menu.transform, "Title", "TEMNORID HIPPODROME", 52, TextAnchor.MiddleCenter, new Vector2(0f, 0.78f), new Vector2(1f, 0.98f), Accent);
			Label(_menu.transform, "Tagline", "Ride. Race. Cross the line first.", 32, TextAnchor.MiddleCenter, new Vector2(0f, 0.68f), new Vector2(1f, 0.78f), Color.white);
			_records = Label(_menu.transform, "Records", RecordsText(), 30, TextAnchor.MiddleCenter, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.66f), Color.white);
			Button(_menu.transform, "Start Race", new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.42f), () => Race.StartRace());
			Button(_menu.transform, "Quit", new Vector2(0.25f, 0.13f), new Vector2(0.75f, 0.24f), () => Race.Quit());
			Label(_menu.transform, "Hint", "W gallop  ·  S brake  ·  A / D steer\nShift boost  ·  Space jump  ·  Esc pause", 24, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.1f), new Color(1f, 1f, 1f, 0.7f));
		}

		void BuildHud(Transform parent)
		{
			_hud = new GameObject("RaceHud", typeof(RectTransform));
			_hud.transform.SetParent(parent, false);
			Stretch((RectTransform)_hud.transform);
			_lap = Label(_hud.transform, "Lap", "", 44, TextAnchor.UpperLeft, new Vector2(0.02f, 0.9f), new Vector2(0.35f, 0.99f), Color.white);
			_bestLap = Label(_hud.transform, "BestLap", "", 28, TextAnchor.UpperLeft, new Vector2(0.02f, 0.85f), new Vector2(0.35f, 0.9f), new Color(1f, 1f, 1f, 0.8f));
			_place = Label(_hud.transform, "Place", "", 44, TextAnchor.UpperRight, new Vector2(0.65f, 0.9f), new Vector2(0.98f, 0.99f), Accent);
			_time = Label(_hud.transform, "Time", "", 44, TextAnchor.UpperCenter, new Vector2(0.38f, 0.9f), new Vector2(0.62f, 0.99f), Color.white);
			_center = Label(_hud.transform, "Center", "", 160, TextAnchor.MiddleCenter, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.75f), Accent);

			Label(_hud.transform, "StaminaLabel", "STAMINA", 24, TextAnchor.LowerLeft, new Vector2(0.02f, 0.075f), new Vector2(0.14f, 0.105f), new Color(1f, 1f, 1f, 0.8f));
			_stamina = UiBuilder.Bar("Stamina", _hud.transform, new Vector2(0.02f, 0.05f), new Vector2(0.24f, 0.07f), new Color(0.35f, 0.8f, 0.4f));
			_boostState = Label(_hud.transform, "BoostState", "", 26, TextAnchor.UpperLeft, new Vector2(0.02f, 0.012f), new Vector2(0.3f, 0.048f), Color.white);
		}

		void BuildPause(Transform parent)
		{
			_pause = Box("PauseMenu", parent, new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.7f), Panel);
			Label(_pause.transform, "Title", "PAUSED", 56, TextAnchor.MiddleCenter, new Vector2(0f, 0.68f), new Vector2(1f, 0.96f), Accent);
			Button(_pause.transform, "Resume", new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.62f), () => Race.Resume());
			Button(_pause.transform, "Restart race", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.38f), () => Race.StartRace());
			Button(_pause.transform, "Main menu", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.16f), () => Race.BackToMenu());
		}

		void BuildResults(Transform parent)
		{
			_results = Box("Results", parent, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), Panel);
			_resultsTitle = Label(_results.transform, "Title", "", 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.84f), new Vector2(1f, 0.98f), Accent);
			_resultsNote = Label(_results.transform, "Note", "", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0.77f), new Vector2(1f, 0.84f), Color.white);
			_resultsNames = Label(_results.transform, "Names", "", 34, TextAnchor.UpperLeft, new Vector2(0.06f, 0.36f), new Vector2(0.5f, 0.76f), Color.white);
			_resultsTimes = Label(_results.transform, "Times", "", 34, TextAnchor.UpperRight, new Vector2(0.5f, 0.36f), new Vector2(0.72f, 0.76f), Color.white);
			_resultsLaps = Label(_results.transform, "BestLaps", "", 34, TextAnchor.UpperRight, new Vector2(0.72f, 0.36f), new Vector2(0.95f, 0.76f), new Color(1f, 1f, 1f, 0.75f));
			Button(_results.transform, "Race again", new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.32f), () => Race.StartRace());
			Button(_results.transform, "Main menu", new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.17f), () => Race.BackToMenu());
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

		string RecordsText()
		{
			var r = Race.Records;
			if (r.races == 0) return "No races yet.\nYour best times will be kept here.";
			return $"Best lap  {FormatTime(r.bestLapSeconds)}     Best race  {FormatTime(r.bestTotalSeconds)}\nWins  {r.wins}  of  {r.races} races";
		}

		// ---- building blocks ----

		// Shared with the other games: 3d-unity-ui.
		void EnsureEventSystem() => UiBuilder.EnsureEventSystem(transform);

		static GameObject Box(string name, Transform parent, Vector2 min, Vector2 max, Color colour) => UiBuilder.Box(name, parent, min, max, colour);

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

		static void Stretch(RectTransform rect)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
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
