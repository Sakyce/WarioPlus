using UnityEngine;

namespace WarioPlus.Characters.Basic
{
    internal class MarioComputer : MonoBehaviorNPC
    {
        EnvironmentController ec;
        RoomController room;
        MeshRenderer renderer;
        PropagatedAudioManager audMan;

        float progress = 0;
        readonly float progressTime = 1f;

        int currentStage = 0;
        private MarioPostComputer mario;
        readonly int maxStages = 3;
        private bool active = true;

        public override void Despawn()
        {
            base.Despawn();
            active = false;
            audMan.FlushQueue(true);
        }

        private bool AnyPlayerInRoom()
        {
            foreach (var player in ec.Players)
            {
                if (player != null && player.currentRoom == room)
                {
                    return true;
                }
            }
            return false;
        }
        private void AdjustPerStage()
        {
            switch (currentStage)
            {
                case 0:
                    audMan.volumeMultiplier = 0;
                    renderer.material = WarioAssets.marioComputer[0];
                    break;
                case 1:
                    audMan.volumeMultiplier = 0.2f;
                    renderer.material = WarioAssets.marioComputer[1];
                    break;
                case 2:
                    audMan.volumeMultiplier = 0.5f;
                    renderer.material = WarioAssets.marioComputer[2];
                    break;
                case 3:
                    audMan.volumeMultiplier = 1f;
                    renderer.material = WarioAssets.marioComputer[3];
                    break;
            }
        }

        internal void Update()
        {
            if (!active) return;
            if (AnyPlayerInRoom())
            {
                progress -= Time.deltaTime * ec.NpcTimeScale / (progressTime / 2);
            }
            else
            {
                progress += Time.deltaTime * ec.NpcTimeScale / progressTime;
            }
            //progress = Mathf.Clamp(progress, 0, 1);

            if (progress > 1 && currentStage >= maxStages)
            {
                currentStage = 0;
                AdjustPerStage();
                ec.SpawnNPC(mario, IntVector2.GetGridPosition(ec.RealRoomMid(room)));
                Debug.Log("Spawning mario");
                enabled = false;
                return;
            }

            if (progress > 1 && currentStage < maxStages)
            {
                currentStage += 1;
                progress = 0;
                AdjustPerStage();
            }
            else if (progress < 0 && currentStage > 0)
            {
                currentStage -= 1;
                progress = 1;
                AdjustPerStage();
            }
        }
        internal void Initialize(RoomController room)
        {
            ec = room.ec;
            this.room = room;
            renderer = GetComponent<MeshRenderer>();

            audMan = GetComponent<PropagatedAudioManager>() ?? gameObject.AddComponent<PropagatedAudioManager>();
            audMan.SetLoop(true);
            audMan.maxDistance = 800;
            audMan.QueueAudio(WarioAssets.marioMusicBox, true);
            AdjustPerStage();

            mario = WarioPlus.AssetManager.Get<MarioPostComputer>("MarioPostComputer");
            ec.offices.ForEach(x => ec.BuildPosterInRoom(x, mario.poster, new System.Random(CoreGameManager.Instance.Seed())));
        }
    }
}
