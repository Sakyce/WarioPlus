using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WarioPlus.Items
{
    internal class ITM_CalmingMusicBox : Item
    {
        public bool canBeUsed = true;
        public override bool Use(PlayerManager pm)
        {
            var tile = pm.ec.CellFromPosition(IntVector2.GetGridPosition(pm.transform.position));
            if (tile.room.category != RoomCategory.Special)
            {
                Singleton<CoreGameManager>.Instance
                    .GetHud(0)
                    .ShowEventText("You can only use that in the cafeteria!", 3f);
                pm.ec.audMan.PlaySingle(WarioPlus.AssetManager.Get<SoundObject>("click"));
                return false;
            }

            if (canBeUsed)
            {
                canBeUsed = false;
                if (BaseGameManager.Instance is WarioGameManager manager)
                {
                    manager.CalmingMusicBoxPlayed();
                    Destroy(this);
                    return true;
                }
            }
            return false;
        }
    }

    internal class ITM_Pill : Item
    {
        private readonly Fog fog = new Fog() { color = Color.black + new Color(0.02f,0.02f,0.05f), maxDist = 50, startDist = 30, priority = int.MaxValue - 1, strength = 0.5f};
        private readonly Fog fastFog = new Fog() { color = Color.black, maxDist = 12, startDist = 0, priority = int.MaxValue, strength = 1f};
        private readonly List<NavigationState_WanderFleeOverride> fleeStates = new List<NavigationState_WanderFleeOverride>();
        private DijkstraMap dijkstraMap;

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            pm.ec.AddFog(fog);
            pm.ec.AddFog(fastFog);
            dijkstraMap = new DijkstraMap(pm.ec, PathType.Const, new Transform[] { pm.transform });
            CoreGameManager.Instance.audMan.PlaySingle(Resources.FindObjectsOfTypeAll<ITM_ZestyBar>().First().audEat);
            foreach (NPC npc in pm.ec.Npcs)
            {
                if (npc.Navigator.enabled)
                {
                    var fleestate = new NavigationState_WanderFleeOverride(npc, 65, dijkstraMap);
                    fleeStates.Add(fleestate);
                    npc.navigationStateMachine.ChangeState(fleestate);
                }
            }
            StartCoroutine(RemoveFog());
            return true;
        }
        public IEnumerator RemoveFog()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            pm.ec.RemoveFog(fastFog);
            yield return new WaitForSecondsRealtime(5);
            foreach (NavigationState_WanderFleeOverride states in fleeStates)
            {
                states.End();
            }
            dijkstraMap.Deactivate();
            yield return new WaitForSecondsRealtime(5);
            pm.ec.RemoveFog(fog);
            Destroy(this);
            yield break;
        }
    }

    internal class ITM_PocketWatch : Item
    {
        private readonly TimeScaleModifier timescale = new TimeScaleModifier() { environmentTimeScale = 0.5f, npcTimeScale = 0.5f };
        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            pm.ec.AddTimeScale(timescale);
            
            StartCoroutine(UseTimer());
            return true;
        }
        private IEnumerator UseTimer()
        {
            yield return new WaitForSecondsRealtime(15);
            pm.ec.RemoveTimeScale(timescale);
            Destroy(this);
            yield break;
        }
    }
}
