
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SimpleMover : UdonSharpBehaviour
{
    [SerializeField] private Animator animator;
    private VRCPlayerApi _localPlayer;
    void Start()
    {
        _localPlayer = Networking.LocalPlayer;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (player == _localPlayer)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerEnter");
            Debug.Log("Player Enter");
        }
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (player == _localPlayer)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerExit");
            Debug.Log("Player Exit");
        }
    }

    public void PlayerEnter()
    {
        animator.SetBool("isPlayerIn", true);
    }

    public void PlayerExit()
    {
        animator.SetBool("isPlayerIn", false);
    }
}
