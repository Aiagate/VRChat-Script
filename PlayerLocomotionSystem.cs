#define DEBUG
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace ShimaeAsset.Player.PlayerLocomotionSystem
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerLocomotionSystem : UdonSharpBehaviour
    {
        [SerializeField] private Transform emvironment;
        [SerializeField] private int slideLayer = 29;
        [SerializeField] private int mobilityLayer = 30;
        [SerializeField] private int gravityLayer = 31;
        [SerializeField] private Transform playerObject; // ObjectSyncでプレイヤーの座標を渡さないと、他人視点ではStationに座ってるので座標が取得できない
        [SerializeField] private Transform fakePlayer;
        [SerializeField] private Transform worldOrigin;
        private VRCPlayerApi localPlayer;
        private Transform prePlatform;
        private Vector3 prePlatformPosition;
        private Quaternion prePlatformRotation;

        void Start()
        {
            // Start関数内だと取得失敗した経験があるので実際にワールドとしてアップロードする場合は取得タイミングを変えたほうが吉
            this.localPlayer = Networking.LocalPlayer;
        }
        
        void FixedUpdate()
        {
            // プレイヤーの位置回転をキャッシュ
            Vector3 playerPosition = this.localPlayer.GetPosition();
            Quaternion playerRotation = this.localPlayer.GetRotation();

            // Rayを設定
            Ray ray = new Ray(playerPosition + new Vector3(0, 0.1f, 0), -Vector3.up);
            RaycastHit hit;

            // ワールドを移動するオフセット値を初期化
            Vector3 emvironmentOffsetPosition = Vector3.zero;
            Quaternion emvironmentOffsetRotation = Quaternion.identity;

            // プレイヤーの足元からRayを射出
            Physics.Raycast(ray, out hit, 1, ~(1 << 9)); // 他のプレイヤーのコライダーを取得しないようにマスクを設定 ~(1 << 9)

            if (hit.collider != null)
            {
                GameObject targetObject = hit.collider.gameObject;

                // 子オブジェクト「offset」からスライド量を取得
                // 毎回子オブジェクトを取得しに行ってるので要改善
                if (targetObject.layer == this.slideLayer)
                {
                    Transform slideOffset = hit.collider.transform.Find("offset");
                    if (slideOffset != null)
                    {
                        emvironmentOffsetPosition += (slideOffset.rotation * slideOffset.localPosition) * Time.deltaTime;
                    }
                }
                // 移動床(プラットフォーム)の差分をワールドの座標に反映
                else if (hit.transform.gameObject.layer == this.mobilityLayer)
                {
                    Transform platform = hit.collider.transform;
                    // 移動床に初めて乗った/乗り換えた際の処理
                    if (this.prePlatform == null)
                    {
                        this.prePlatform = platform;
                        this.prePlatformPosition = platform.position;
                        this.prePlatformRotation = platform.rotation;
                    }
                    else
                    {
                        emvironmentOffsetPosition += platform.position - this.prePlatformPosition;
                        emvironmentOffsetRotation *= platform.rotation * Quaternion.Inverse(this.prePlatformRotation);
                    }
                }
                // 重力方向を変える機能のテスト"未完成"
                // (座標の変換で頭がこんがらがって分からなくなった)
                else if (hit.transform.gameObject.layer == this.gravityLayer)
                {
                    Vector3 hitPosition = hit.point;
                    Quaternion hitRotationOffset = Quaternion.Inverse(Quaternion.LookRotation(hit.normal) * Quaternion.Inverse(Quaternion.LookRotation(Vector3.up)));
                    this.emvironment.position += (hitRotationOffset * (playerPosition - this.emvironment.position)) - (playerPosition - this.emvironment.position);
                    this.emvironment.rotation *= Quaternion.Euler(hitRotationOffset.eulerAngles.x, 0, hitRotationOffset.eulerAngles.z);

                    DebugLog($"{(Quaternion.LookRotation(hit.normal) * Quaternion.Inverse(Quaternion.LookRotation(Vector3.up))).eulerAngles} {this.emvironment.rotation.eulerAngles}");
                }
                // 移動床から降りた際、キャッシュした移動床を初期化
                else
                {
                    this.prePlatform = null;
                }
            }
            
            // 移動差分をworldOriginに反映(先に反映することで落下した高さを次の処理で使用できるから、特にしなくても良いことに気付いた)
            this.worldOrigin.position -= (emvironmentOffsetPosition);// + this.emvironment.rotation * chunkOffset);

            // プレイヤーが落下した時リスポーンした時っぽい事をする処理
            if (this.worldOrigin.position.y > 100)
            {
                this.emvironment.position = Vector3.zero;
                this.emvironment.rotation = Quaternion.identity;
                this.worldOrigin.position = Vector3.zero;
                this.playerObject.position = Vector3.zero;
                this.localPlayer.TeleportTo(Vector3.zero, this.localPlayer.GetRotation());
            }
            // 適当に差分を反映
            else
            {
                this.emvironment.position -= (emvironmentOffsetPosition);// + this.emvironment.rotation * chunkOffset);
                this.emvironment.rotation *= Quaternion.Inverse(emvironmentOffsetRotation);
                this.worldOrigin.rotation *= Quaternion.Inverse(emvironmentOffsetRotation);
                this.playerObject.position = Quaternion.Inverse(this.worldOrigin.rotation) * (playerPosition - worldOrigin.position);
                this.playerObject.rotation = Quaternion.Inverse(this.worldOrigin.rotation) * playerRotation;
            }
        }

        // リスポーン時の処理
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player == this.localPlayer)
            {
                this.emvironment.position = Vector3.zero;
                this.emvironment.rotation = Quaternion.identity;
                this.worldOrigin.position = Vector3.zero;
                this.playerObject.position = Vector3.zero;
                this.localPlayer.TeleportTo(Vector3.zero, this.localPlayer.GetRotation());
                Enter();
            }
        }

        public void Enter()
        {
            // 取得失敗してそうだからもう一回取得してるやつ
            this.localPlayer = Networking.LocalPlayer;

            // OwnerじゃないとObjectSyncがアタッチされたオブジェクトを移動できないので、Ownerを変更
            // ローカルのテストではこれで動いてるけどなんかヤバそうな雰囲気がする
            VRCPlayerApi owner = Networking.GetOwner(fakePlayer.gameObject);
            DebugLog($"Object Owner is {owner.displayName}");
            if (Networking.IsOwner(this.localPlayer, fakePlayer.gameObject))
                Networking.SetOwner(this.localPlayer, fakePlayer.gameObject);
            DebugLog($"Object Owner is {owner.displayName}");
            VRCStation station = (VRCStation)fakePlayer.GetComponent(typeof(VRCStation));
#if DEBUG
            if (station == null)
            {
                DebugLog("Station is null!!!");
                return;
            }
            DebugLog("Enter System");
#endif
            station.PlayerMobility = VRCStation.Mobility.Mobile;
            station.UseStation(localPlayer);

        }
        public void Exit()
        {
            VRCStation station = (VRCStation)fakePlayer.GetComponent(typeof(VRCStation));
#if DEBUG
            if (station == null)
            {
                DebugLog("Station is null!!!");
                return;
            }
            DebugLog("Exit System");
#endif
            station.ExitStation(localPlayer);
        }

        // デバックログが見やすくなる
        public void DebugLog(string message)
        {
            Debug.Log($"[<color=lime>PlayerManager</color>] {message}");
        }
    }
}