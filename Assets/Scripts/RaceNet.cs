using System.Collections.Generic;
using ITKamianets.Engine.Net;
using UnityEngine;

namespace Temnorid.Hippodrome
{
	/// <summary>
	/// Makes the race shared. Every person rides their own horse on their own machine and publishes where it is; every other
	/// machine shows it. The computer riders belong to the host, which publishes them; the others only show them. Each machine
	/// keeps the race rules for itself, so a place on the screen is what that machine sees.
	/// <para>
	/// A grid slot belongs to a person by their place in the roster (the host first), so the same horse stands in the same
	/// slot everywhere. The local rider is always <c>Racers[0]</c> here, whatever their slot.
	/// </para>
	/// </summary>
	public sealed class RaceNet : MonoBehaviour
	{
		const float PublishEvery = 1f / 15f;
		const string StartEvent = "hip.start";
		const string HereEvent = "hip.here";

		RaceController _race;
		NetSession _net;
		int[] _slot;
		ulong[] _owner;
		bool[] _human;
		float _next;
		readonly HashSet<ulong> _here = new HashSet<ulong>();

		/// <summary>The grid slot of a racer.</summary>
		public int SlotOf(int racer) => _slot != null && racer < _slot.Length ? _slot[racer] : racer;

		/// <summary>True once every other person has loaded the track, so the host can start.</summary>
		public bool EveryoneHere
		{
			get
			{
				foreach (var player in _net.Players)
					if (player.Id != _net.LocalId && !_here.Contains(player.Id)) return false;
				return true;
			}
		}

		public void Init(RaceController race)
		{
			_race = race;
			_net = NetSession.Instance;
			var racers = race.Racers;
			var players = _net.Players;
			_slot = new int[racers.Length];
			_owner = new ulong[racers.Length];
			_human = new bool[racers.Length];

			var taken = new bool[racers.Length];
			var nextRacer = 1;
			for (var p = 0; p < players.Count && p < racers.Length; p++)
			{
				var player = players[p];
				var racerIndex = player.Id == _net.LocalId ? 0 : nextRacer++;
				_slot[racerIndex] = p;
				_owner[racerIndex] = player.Id;
				_human[racerIndex] = true;
				taken[p] = true;
				var racer = racers[racerIndex];
				racer.DisplayName = player.Name;
				racer.Colour = ShellColour(player.Colour);
				racer.Horse.SetRemote(racerIndex != 0);
				if (racerIndex != 0 && racer.BotDriver != null) racer.BotDriver.enabled = false;
			}

			// The computer riders take the slots nobody sits in, in order. Only the host simulates them.
			var freeSlot = 0;
			for (var i = nextRacer; i < racers.Length; i++)
			{
				while (freeSlot < racers.Length && taken[freeSlot]) freeSlot++;
				_slot[i] = freeSlot;
				taken[freeSlot] = true;
				_owner[i] = ulong.MaxValue;
				racers[i].Horse.SetRemote(!_net.IsHost);
			}

			_net.On(StartEvent, OnStart);
			_net.On(HereEvent, OnHere);
			_net.Disconnected += OnDisconnected;
			_net.Send(HereEvent, "");
		}

		static Color ShellColour(int index)
		{
			return ITKamianets.Engine.Shell.ShellConfig.Current.ColourOf(index);
		}

		void OnDestroy()
		{
			if (_net == null) return;
			_net.Off(StartEvent, OnStart);
			_net.Off(HereEvent, OnHere);
			_net.Disconnected -= OnDisconnected;
		}

		/// <summary>The host tells everybody to start the race.</summary>
		public void AnnounceStart()
		{
			_net.Send(StartEvent, "");
		}

		void OnStart(ulong sender, string payload)
		{
			if (_net.IsClient) _race.StartRace();
		}

		void OnHere(ulong sender, string payload)
		{
			_here.Add(sender);
		}

		void OnDisconnected(string reason)
		{
			// The host closed the game or the connection dropped: nothing to race against any more.
			ITKamianets.Engine.Shell.ShellFlow.QuitToMenu();
		}

		void Update()
		{
			if (_net == null || !_net.IsOnline) return;
			ShowOthers();

			if (Time.unscaledTime < _next) return;
			_next = Time.unscaledTime + PublishEvery;
			PublishMine();
		}

		void PublishMine()
		{
			var horse = _race.Racers[0].Horse;
			_net.PublishPose(PoseOf(horse));

			if (!_net.IsHost) return;
			var list = new List<NetEntity>();
			for (var i = 1; i < _race.Racers.Length; i++)
			{
				if (_human[i]) continue;
				var racer = _race.Racers[i];
				var pose = PoseOf(racer.Horse);
				list.Add(new NetEntity { Id = _slot[i], Kind = 0, Position = pose.Position, Yaw = pose.Yaw, Value = pose.Speed, State = pose.State });
			}
			_net.PublishEntities(list);
		}

		static NetPose PoseOf(HorseController horse)
		{
			var transform = horse.transform;
			return new NetPose
			{
				Position = transform.position,
				Yaw = transform.eulerAngles.y,
				Speed = horse.Speed,
				State = (horse.Airborne ? 1 : 0) | (horse.Stamina.Boosting ? 2 : 0),
				A = horse.Stamina.Value,
				B = horse.Height,
			};
		}

		void ShowOthers()
		{
			var racers = _race.Racers;
			for (var i = 1; i < racers.Length; i++)
			{
				var horse = racers[i].Horse;
				if (!horse.Remote) continue;
				var body = horse.GetComponent<RemoteBody>() ?? horse.gameObject.AddComponent<RemoteBody>();

				if (_human[i])
				{
					if (!_net.TryGetPose(_owner[i], out var pose)) continue;
					body.Apply(pose);
					horse.ShowRemote(pose.Speed, (pose.State & 1) != 0, pose.B);
				}
				else if (_net.Entities.TryGetValue(_slot[i], out var entity))
				{
					body.Apply(entity);
					horse.ShowRemote(entity.Value, (entity.State & 1) != 0, 0f);
				}
			}
		}
	}
}
