using ITKamianets.Engine.Input;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>The player's hands on the reins: W and S gallop and brake, A and D steer (or a gamepad stick).</summary>
	public sealed class PlayerDriver : HorseDriver
	{
		public override Vector2 Drive()
		{
			return EngineInput.Default.Player.Move.ReadValue<Vector2>();
		}
	}
}
