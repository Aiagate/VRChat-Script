
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ShimaeAsset.Player.FakePlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FakePlayer : UdonSharpBehaviour
    {
        [SerializeField] Transform worldOrigin;
        [SerializeField] Transform playerObject;
        private Transform fakePlayer;

        void Start()
        {
            fakePlayer = this.transform;
        }

        void Update()
        {
            fakePlayer.position = worldOrigin.position + (worldOrigin.rotation * playerObject.position);
            fakePlayer.rotation = worldOrigin.rotation * playerObject.rotation;
        }
    }
}