using System;
using UnityEngine;

namespace WarioPlus.Characters.Basic
{
    internal class MarioPostComputer : WarioNPC
    {
        float heightoffset = 0;

        public override void VirtualUpdate()
        {
            base.VirtualUpdate();
            navigator.Entity.SetHeight(6.5f + heightoffset);
            navigator.SetSpeed(19f);
            heightoffset = (float)Math.Sin(Time.timeSinceLevelLoad * 4);
        }
        public override void Initialize()
        {
            base.Initialize();
            spriteRenderer[0].sprite = WarioPlus.AssetManager.Get<Sprite>("mario");
            AddLoseSound(WarioAssets.losesounds["fsaw"]);
            SetLethal(true);
            behaviorStateMachine.ChangeState(new NpcState(this));
            behaviorStateMachine.ChangeNavigationState(new NavigationState_TargetPlayer(this, 63, ec.Players[0].transform.position));
        }
    }
}
