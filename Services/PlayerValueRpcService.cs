using System;
using System.Globalization;
using Il2CppScheduleOne.PlayerScripts;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Sends Nugzz control values through the game's existing Player target RPC.
    /// Player.SendValue is client-to-server and cannot broadcast a host value.
    /// </summary>
    internal static class PlayerValueRpcService
    {
        internal static bool SendToClient(Player source, Player recipient, string name, int value)
        {
            return SendToClient(source, recipient, name,
                value.ToString(CultureInfo.InvariantCulture));
        }

        internal static bool SendToClient(Player source, Player recipient, string name, float value)
        {
            return SendToClient(source, recipient, name,
                value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        internal static bool SendToClient(Player source, Player recipient, string name, string value)
        {
            return SendToClient(source, recipient, name, value, out _);
        }

        internal static bool SendToClient(Player source, Player recipient, string name, string value,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (source == null)
            {
                diagnostic = "source player is null";
                return false;
            }
            if (recipient == null)
            {
                diagnostic = "recipient player was not resolved";
                return false;
            }
            if (recipient.IsLocalPlayer)
            {
                diagnostic = "recipient resolved as the local player";
                return false;
            }
            if (recipient.Owner == null)
            {
                diagnostic = "recipient has no FishNet owner connection";
                return false;
            }
            if (string.IsNullOrEmpty(name))
            {
                diagnostic = "network value name is empty";
                return false;
            }

            try
            {
                source.RpcWriter___Target_ReceiveValue_3895153758(
                    recipient.Owner, name, value ?? string.Empty);
                diagnostic = "queued for client " + recipient.Owner.ClientId;
                return true;
            }
            catch (Exception ex)
            {
                diagnostic = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        internal static void BroadcastToApprovedClients(Player source, string name, float value)
        {
            BroadcastToApprovedClients(source, name,
                value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        internal static void BroadcastToApprovedClients(Player source, string name, string value)
        {
            var players = Player.PlayerList;
            if (source == null || players == null)
                return;

            for (int i = 0; i < players.Count; i++)
            {
                Player recipient = players[i];
                if (recipient == null || recipient.IsLocalPlayer ||
                    !SessionAuthorityService.Instance.IsClientApproved(recipient))
                {
                    continue;
                }

                SendToClient(source, recipient, name, value);
            }
        }

        internal static Player FindRemotePlayer(int clientId)
        {
            var players = Player.PlayerList;
            if (players == null)
                return null;

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null || player.IsLocalPlayer)
                    continue;
                try
                {
                    if (player.Owner != null && player.Owner.ClientId == clientId)
                        return player;
                }
                catch { }
            }
            return null;
        }
    }
}
