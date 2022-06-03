
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerManager : UdonSharpBehaviour
{
    private const short maxPlayer = 8; // constでないとクラス変数定義の時に使えない
    [SerializeField] private Text playerDisplay;
    [SerializeField] private Transform[] locomotionSystem;
    [UdonSynced (UdonSyncMode.None), FieldChangeCallback(nameof(ListData))] private ushort[] statusList = new ushort[maxPlayer * 2];
    private VRCPlayerApi localPlayer;
    private  VRCPlayerApi[] playersList;

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        DebugLog("OnPlayerJoined");
        if (player == Networking.LocalPlayer)
        {
            DebugLog("Set LocalPlayer");
            localPlayer = player;
            // DebugLog("Send Network Event to Owner");
            // SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "ReloadPlayerList");
        }
        if (Networking.IsOwner(localPlayer, this.gameObject))
        {
            DebugLog("ReloadPlayerList");
            ReloadPlayerList();
            DisplayInfo();
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        DebugLog("OnPlayerLeft");
        if (Networking.IsOwner(localPlayer, this.gameObject))
        {
            DebugLog("ReloadPlayerList");
            ReloadPlayerList();
        }
    }

    // public override void OnPreSerialization()
    // {
    //     DebugLog("OnPreSerialization");
    //     DisplayInfo();
    // }

    public override void OnDeserialization()
    {
        DebugLog("OnDeserialization");
        DisplayInfo();
    }

    // 頭が回らない深夜には理解できない処理
    // 空いているStationを検索する処理
    public void ReloadPlayerList()
    {
        DebugLog("ReloadPlayerList");
        VRCPlayerApi[] playerList = new VRCPlayerApi[maxPlayer*2];
        VRCPlayerApi.GetPlayers(playerList);

        for (int i=0; i<maxPlayer*2; i++)
        {
            if (statusList[i] == 0) continue;

            bool isPlayerInRoom = false;
            foreach (VRCPlayerApi player in playerList)
            {
                if (player == null) continue;
                if (statusList[i] == player.playerId)
                {
                    isPlayerInRoom = true;
                    break;
                }
            }
            if (!isPlayerInRoom) statusList[i] = 0;
        }

        foreach (VRCPlayerApi player in playerList)
        {
            if (player == null) continue;
            DebugLog($"PlayerID:{player.playerId}");
            bool isPlayerInRoom = false;
            int freeSystemNum = -1;
            for (int i=0; i<maxPlayer*2; i++)
            {
                DebugLog($"statusList[{i}]:{statusList[i]} = {(ushort)player.playerId} : {statusList[i] == (ushort)player.playerId}");
                if (statusList[i] == (ushort)player.playerId) isPlayerInRoom = true;
                if (statusList[i] == 0 & freeSystemNum == -1) freeSystemNum = i;
            }
            if (!isPlayerInRoom)
            {
                statusList[freeSystemNum] = (ushort)player.playerId;
                Networking.SetOwner(player, locomotionSystem[freeSystemNum].Find("Player").gameObject);
            }
        }

        DebugLog("RequestSerialization");
        RequestSerialization();
    }

    public void Enter()
    {
        if (localPlayer == null)
        {
            localPlayer = Networking.LocalPlayer;
        }
        DebugLog($"id:{localPlayer.playerId}");
        for (int i=0; i<maxPlayer*2; i++)
        {
            // 空いているStationに座る
            if (statusList[i] == localPlayer.playerId)
            {
                DebugLog($"System Number:{i}");
                if (locomotionSystem[i] == null) continue;
                UdonBehaviour udon = (UdonBehaviour)locomotionSystem[i].GetComponent(typeof(UdonBehaviour));
                udon.enabled = true;
                udon.SendCustomEvent("Enter");
                return;
            }
        }
    }

    public void Exit()
    {
        if (localPlayer == null)
        {
            localPlayer = Networking.LocalPlayer;
        }
        DebugLog($"id:{localPlayer.playerId}");
        for (int i=0; i<maxPlayer*2; i++)
        {
            if (statusList[i] == localPlayer.playerId)
            {
                DebugLog($"System Number:{i}");
                if (locomotionSystem[i] == null) continue;
                UdonBehaviour udon = (UdonBehaviour)locomotionSystem[i].GetComponent(typeof(UdonBehaviour));
                udon.SendCustomEvent("Exit");
                return;
            }
        }
    }

    public void DisplayInfo()
    {
        string t = "";
        string list = "";
        for (int i=0; i<maxPlayer*2; i++)
        {
            list += $"{statusList[i]}[{i}], ";
            if (statusList[i] != 0)
            {
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(statusList[i]);
                if (player != null)
                {
                    t += $"Index:{i:D2} PlayerID:{statusList[i]:D2} Name:{player.displayName.PadRight(10)} Owner:{Networking.GetOwner(locomotionSystem[i].Find("Player").gameObject).displayName.PadRight(10)}\n";
                }
            }
            else
            {
                    t += $"Index:{i:D2} PlayerID:{statusList[i]:D2} Name:{"".PadRight(10)} Owner:{Networking.GetOwner(locomotionSystem[i].Find("Player").gameObject).displayName.PadRight(10)}\n";
            }
        }
        if (playerDisplay != null) playerDisplay.text = t;
        DebugLog(list);
    }

    public void DebugLog(string message)
    {
        Debug.Log("[<color=lime>PlayerManager</color>] " + message);
    }

    // そう書けって書いてあった。デリゲートはよくわかってない
    public ushort[] ListData
    {
        get => statusList;
        set
        {
            statusList = value;
        }
    }
}
