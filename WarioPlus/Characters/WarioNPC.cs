using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using System.Collections.Generic;
using UnityEngine;

namespace WarioPlus.Characters
{
    internal class MonoBehaviorNPC : MonoBehaviour
    {
        public virtual void Despawn()
        {

        }
    }
    internal class WarioNPC : NPC
    {
        public CustomSpriteAnimator animator;
        public PropagatedAudioManager audMan;
        private bool isLethal = false;
        private List<WeightedSelection<SoundObject>> loseSounds = new List<WeightedSelection<SoundObject>>();
        protected void SetLethal(bool lethal)
        {
            isLethal = lethal;
        }
        protected void AddLoseSound(SoundObject snd)
        {
            loseSounds.Add(new WeightedSelection<SoundObject> { selection = snd, weight = 100 });
        }

        public override void VirtualOnTriggerStay(Collider other)
        {
            base.VirtualOnTriggerStay(other);
            if (isLethal)
            {
                if (other.CompareTag("Player"))
                {
                    looker.Raycast(other.transform, UnityEngine.Vector3.Magnitude(transform.position - other.transform.position), out bool canSee);
                    if (canSee)
                    {
                        PlayerManager component = other.GetComponent<PlayerManager>();
                        if (!component.invincible)
                        {
                            CaughtPlayer(component);
                        }
                    }
                }
            }
        }

        protected virtual void CaughtPlayer(PlayerManager player)
        {
            Time.timeScale = 0f;
            MusicManager.Instance.StopMidi();
            CoreGameManager.Instance.disablePause = true;
            CoreGameManager.Instance.GetCamera(0).UpdateTargets(transform, 0);
            CoreGameManager.Instance.GetCamera(0).offestPos = (player.transform.position - transform.position).normalized * 2f + UnityEngine.Vector3.up;
            CoreGameManager.Instance.GetCamera(0).SetControllable(false);
            CoreGameManager.Instance.GetCamera(0).matchTargetRotation = false;
            CoreGameManager.Instance.audMan.volumeModifier = 0.6f;
            CoreGameManager.Instance.audMan.PlaySingle(WeightedSelection<SoundObject>.RandomSelection(loseSounds.ToArray()));
            CoreGameManager.Instance.StartCoroutine(CoreGameManager.Instance.EndSequence());
            InputManager.Instance.Rumble(1f, 2f);
        }

        internal static E Build<E>(NPCBuilder<E> builder) where E : WarioNPC
        {
            var newnpc = builder.Build();
            newnpc.audMan = newnpc.GetComponent<PropagatedAudioManager>();

            CustomSpriteAnimator animator = newnpc.gameObject.AddComponent<CustomSpriteAnimator>();
            animator.spriteRenderer = newnpc.spriteRenderer[0];
            newnpc.animator = animator;

            return newnpc;
        }
    }
}
