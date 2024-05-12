namespace WarioPlus.Entities
{
    class GrandFatherClock : CustomEntity
    {
        private void HourChanged()
        {
            audMan.QueueAudio(WarioAssets.grandfatherClockSound);
        }

        internal void OnDestroy()
        {
            WarioGameManager.HourChanged -= HourChanged;
        }

        public override void Initialize(RoomController rc)
        {
            base.Initialize(rc);
            WarioGameManager.HourChanged += HourChanged;
            transform.position = rc.RandomEventSafeCellNoGarbage().CenterWorldPosition;
            entity.Initialize(ec, transform.position);
            entity.SetHeight(entity.Height + 1f);
        }
    }
}
