using ITKamianets.Engine.Input;
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

		public override Vector2 Drive()
		{
			return EngineInput.Default.Player.Move.ReadValue<Vector2>();
		}

		// A key press lasts one frame and the horse reads input once per physics step, so a press is kept until it is used.
		void Update()
		{
			if (Time.timeScale == 0f) return;
			var player = EngineInput.Default.Player;
			if (player.Sprint.WasPressedThisFrame()) _boost = true;
			if (player.Jump.WasPressedThisFrame()) _jump = true;
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
