using MTM101BaldAPI.Components;
using System;
using System.Collections;
using UnityEngine;

namespace WarioPlus.Characters.Basic
{
    // Lazy to use state machines
    internal class WarioApparition : WarioNPC
    {
        PlayerManager target;
        MovementModifier moveMod = new MovementModifier(Vector3.zero, 0f);

        bool attacking = false;
        public bool noHopesLeft = false;
        public float bonusSpeed = 0f;

        private IEnumerator Dialog(string text)
        {
            ec.audMan.PlaySingle(WarioPlus.AssetManager.Get<SoundObject>("click"));
            CoreGameManager.Instance.GetHud(0).ShowEventText(text, 1.9f);
            yield return new WaitForSecondsRealtime(2);
        }

        private IEnumerator YouWantFun(PlayerManager player, float speed)
        {
            ec.audMan.PlaySingle(WarioAssets.ambients["smallscare"]);
            player.plm.am.moveMods.Add(moveMod);
            yield return new WaitForSecondsRealtime(0.2f);
            float time = 1f;
            while (time > 0f)
            {
                Vector3 vector = Vector3.RotateTowards(player.transform.forward, (transform.position - player.transform.position).normalized, Time.deltaTime * 2f * 3.1415927f * speed, 0f);
                Debug.DrawRay(player.transform.position, vector, Color.yellow);
                player.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
                time -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1);
#if RELEASE
            yield return Dialog("You thought you were gonna escape that easily ?");
            yield return Dialog("How about a game ?");
            yield return Dialog("Escape to the elevator and don't lose your YTPs to win!");
            yield return Dialog("And if you lose...");
#endif
            yield return Dialog("Your soul will turn into HER property!");
            player.plm.am.moveMods.Remove(moveMod);
            yield return new WaitForSecondsRealtime(3);
            attacking = true;
            BaseGameManager.Instance.AllNotebooks();
            SetLethal(true);
            yield break;
        }

        protected override void CaughtPlayer(PlayerManager player)
        {
            base.CaughtPlayer(player);
        }

        public override void VirtualUpdate()
        {
            base.VirtualUpdate();
            if (attacking)
            {
                behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 10, ec.Players[0].transform.position));
                if (noHopesLeft)
                {
                    navigator.SetSpeed(70f);
                }
                else
                {
                    navigator.SetSpeed(18f + bonusSpeed);
                    //bonusSpeed += Time.deltaTime * ec.NpcTimeScale * 0.25f;
                    //bonusSpeed = Mathf.Clamp(bonusSpeed, 0f, 10f);
                }
                if (!audMan.AnyAudioIsPlaying)
                {
                    audMan.QueueRandomAudio(WarioPlus.AssetManager.Get<SoundObject[]>("WarioLaughs"));
                }
            }
            else
            {
                navigator.SetSpeed(0);
            }
            navigator.Entity.SetHeight(6.5f + (float)Math.Sin(Time.timeSinceLevelLoad * 4) / 2);
        }
        public override void Initialize()
        {
            base.Initialize();
            animator.animations.Add("default", new CustomAnimation<Sprite>(WarioPlus.AssetManager.Get<Sprite[]>("WarioApparition"), 1f));
            animator.SetDefaultAnimation("default", 1f);

            target = ec.players[0];
            target.plm.entity.AddForce(new Force((target.transform.position - transform.position).normalized, 10f, -5f));
            AddLoseSound(WarioAssets.warioApparitionThemes[2]);
            SetLethal(false);
            StartCoroutine(YouWantFun(target, 0.5f));

            navigationStateMachine.ChangeState(new NavigationState_DoNothing(this, 5));
            behaviorStateMachine.ChangeState(new NpcState(this));
        }
    }
}
