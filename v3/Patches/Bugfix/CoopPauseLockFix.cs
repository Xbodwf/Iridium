using System.Collections.Generic;
using UnityEngine;
using ADOFAI;

namespace Iridium.Patches.Bugfix
{
	public static class CoopPauseLockFix
	{
		internal static readonly Dictionary<int, float> _playerPauseEndTimes = new();

		public static void SetPause(scrPlayer player, float lockTime)
		{
			if (player == null || lockTime <= 0f) return;
			float pitch = Mathf.Max(ADOBase.conductor.song.pitch, 0.001f);
			_playerPauseEndTimes[player.playerID] = Time.time + lockTime / pitch;
		}

		public static bool IsPaused(scrPlayer player)
		{
			if (player == null) return false;
			if (_playerPauseEndTimes.TryGetValue(player.playerID, out var endTime))
			{
				if (Time.time >= endTime)
				{
					_playerPauseEndTimes.Remove(player.playerID);
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
