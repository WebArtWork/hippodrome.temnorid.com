using ITKamianets.Engine.Input;
using ITKamianets.Engine.Shell;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// The player's hands on the reins: W and S gallop and brake, A and D steer (or a gamepad stick),
	/// Shift gives a burst of speed and Space jumps.
	/// </summary>
	public sealed class PlayerDriver : HorseDriver
	{
		bool _boost;
		bool _jump;

		/// <summary>On-screen buttons on a phone or tablet. Null elsewhere. Steering is by two buttons; the horse gallops unless Brake is held.</summary>
		public TouchButton SteerLeft, SteerRight, Brake, Boost, Jump;

		public override Vector2 Drive()
		{
			var move = EngineInput.Default.Player.Move.ReadValue<Vector2>();
			if (SteerLeft == null) return move;
			var steer = (SteerRight.Held ? 1f : 0f) - (SteerLeft.Held ? 1f : 0f);
			return new Vector2(Mathf.Abs(steer) > 0f ? steer : move.x, Brake.Held ? -1f : Mathf.Max(move.y, 1f));
		}

		// A key press lasts one frame and the horse reads input once per physics step, so a press is kept until it is used.
		void Update()
		{
			if (Time.timeScale == 0f) return;
			var player = EngineInput.Default.Player;
			if (player.Sprint.WasPressedThisFrame()) _boost = true;
			if (player.Jump.WasPressedThisFrame()) _jump = true;
			if (Boost != null && Boost.ConsumePress()) _boost = true;
			if (Jump != null && Jump.ConsumePress()) _jump = true;
		}

		void OnEnable()
		{
			_boost = false;
			_jump = false;
		}

		public override bool ConsumeBoost()
		{
			var pressed = _boost;
			_boost = false;
			return pressed;
		}

		public override bool ConsumeJump()
		{
			var pressed = _jump;
			_jump = false;
			return pressed;
		}
	}
}
