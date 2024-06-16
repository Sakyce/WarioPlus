using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using WarioPlus.Characters;
using WarioPlus.Characters.Basic;
using WarioPlus.Entities;
using WarioPlus.Items;
using static BepInEx.BepInDependency;

namespace WarioPlus
{
    [BepInPlugin("sakyce.baldiplus.warioplus", "Five Nights at Wario's", PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", DependencyFlags.HardDependency)]
    public class WarioPlus : BaseUnityPlugin
    {

        public static WarioPlus Instance { get; private set; }
        internal bool areAssetsLoaded = false;
        internal static AssetManager AssetManager { get; private set; }

        public List<SceneObject> mainNights = new List<SceneObject>();

        internal AudioSource menuMusicSource;

        internal void Awake()
        {
            Instance = this;
            AssetManager = new AssetManager();
            new Harmony("sakyce.baldiplus.teacherapi").PatchAllConditionals();

            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadAssets(), false);
            //LoadingEvents.RegisterOnAssetsLoaded(Info, () => StartNight(WarioPlus.Instance.mainNights[0]), true);
            LoadingEvents.RegisterOnAssetsLoaded(Info, delegate
            {
#if DEBUG
                StartNight(mainNights[2]);
#endif
            }, true);

#if DEBUG
            SceneManager.LoadScene("Warnings");
#endif
        }

        private void EditNight(SceneObject scene, int nightNo, NightType nighttype)
        {
            WeightedTexture2D[] SingleTexture2D(Texture2D tex) 
                => new WeightedTexture2D[] { new WeightedTexture2D() { selection = tex, weight = 100 } };
            
            var manager = scene.GetManager<WarioGameManager>();
               
            manager.nightNo = $"Night {nightNo} ";
            manager.nightLabel = nightNo switch
            {
                5 => "The Cellar",
                _ => "The Schoolhouse",
            };
            if (scene.levelObject is WarioLevelObject ld)
            {
                if (nightNo == 3)
                {
                    ld.warioApparitionEnabled = true;
                    ld.maxSpecialBuilders = 15;
                    ld.maxPrePlotSpecialHalls = 30;
                    ld.maxPostPlotSpecialHalls = 30;
                }
                if (nightNo == 5)
                {
                    ld.classCeilingTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarCeiling"));
                    ld.hallCeilingTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarCeiling"));
                    ld.facultyCeilingTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarCeiling"));
                   
                    ld.classFloorTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarFloor"));
                    ld.hallFloorTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarFloor"));
                    ld.facultyFloorTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarFloor"));
                    
                    ld.classWallTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarWall"));
                    ld.hallWallTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarWall"));
                    ld.facultyWallTexs = SingleTexture2D(AssetManager.Get<Texture2D>("CellarWall"));

                    ld.levelintromusic = AssetManager.Get<SoundObject>("CellarIntro");

                    manager.darkTheme = true;
                }
                ld.potentialItems = ld.potentialItems
                    .AddItem(AssetManager.Get<ItemObject>("Pill").ToWeighted(100))
                    .AddItem(AssetManager.Get<ItemObject>("PocketWatch").ToWeighted(10000))
                    .ToArray();

                ld.lightMode = LightMode.Greatest;
                ld.standardDarkLevel = Color.white;
                ld.standardLightStrength = Mathf.RoundToInt(ld.standardLightStrength * 0.5f);
                ld.windowChance = 1f;
                ld.potentialNPCs = new List<WeightedNPC>();
                ld.potentialBaldis = new WeightedNPC[] { };
            }
        }
        private IEnumerator<object> LoadAssets()
        {
            var i = 0;
            int Incr() => i++;

            yield return Incr();
            yield return "Loading Assets";
            WarioAssets.Load();

            

            yield return Incr();

            var menuMusicObj = new GameObject();
            menuMusicObj.transform.parent = transform;
            var audiosource = menuMusicObj.AddComponent<AudioSource>();
            audiosource.clip = AssetManager.Get<SoundObject>("MainMenu").soundClip;
            menuMusicSource = audiosource;

            yield return Incr();
            yield return "Registering stuff";

            // Entities
            {
                var grandfatherClock = CreateEntityLikeAlarmClock<GrandFatherClock>(new EntityBuilder().SetName("GrandfatherClock"));
                grandfatherClock.spriteRenderer.sprite = WarioAssets.grandfatherClock;
                grandfatherClock.gameObject.ConvertToPrefab(true);
                AssetManager.Add<GrandFatherClock>("GrandfatherClock", grandfatherClock);
            }

            // Characters
            {
                // omg is that a reference to the old game from tingting
                AssetManager.Add(
                    "MarioPostComputer",
                    WarioNPC.Build(new NPCBuilder<MarioPostComputer>(Info)
                        .SetName("Mario")
                        .SetEnum("Mario")
                        .SetPoster(ObjectCreators.CreateCharacterPoster(WarioAssets.characterPosters["mario"], "Mario", "One of the workers of Wario's Fast Food Factory. Keep an eye on him!"))
                        .AddLooker()
                        .AddTrigger()
                        .SetAirborne()
                    )
                );
                AssetManager.Add(
                    "WarioApparition",
                    WarioNPC.Build(new NPCBuilder<WarioApparition>(Info)
                        .SetName("WarioApparition")
                        .SetEnum("WarioApparition")
                        .SetPoster(ObjectCreators.CreateCharacterPoster(WarioAssets.characterPosters["viruswario"], "Wario", "I WILL FOLLOW YOU WHEREEVER YOU GO. YOU CANNOT OUTRUN M E"))
                        .SetAirborne()
                    )
                );
            }

            // Items 
            {
                AssetManager.Add("Pill",
                    new ItemBuilder(Info)
                    .SetEnum("PillLongsight")
                    .SetNameAndDescription("Pill", "Might make some ghosts thinks your dead. Although, it comes with a short term myopia side effect.")
                    .SetItemComponent<ITM_Pill>()
                    .SetSprites(AssetManager.Get<Sprite>("PillSmall"), AssetManager.Get<Sprite>("PillLarge"))
                    .SetGeneratorCost(40)
                    .SetMeta(ItemFlags.None, new string[] { "Medicine" })
                    .Build());
                AssetManager.Add("PocketWatch",
                    new ItemBuilder(Info)
                    .SetEnum("PocketWatch")
                    .SetNameAndDescription("PocketWatch", "Slows down the time.")
                    .SetItemComponent<ITM_PocketWatch>()
                    .SetSprites(AssetManager.Get<Sprite>("PocketWatch"), AssetManager.Get<Sprite>("PocketWatch"))
                    .SetGeneratorCost(40)
                    .SetMeta(ItemFlags.None, new string[] {})
                    .Build());
                AssetManager.Add("CalmingMusicBox",
                    new ItemBuilder(Info)
                    .SetEnum("CalmingMusicBox")
                    .SetNameAndDescription("Calming Music Box", "Use it in the cafeteria.")
                    .SetItemComponent<ITM_CalmingMusicBox>()
                    .SetSprites(AssetManager.Get<Sprite>("CalmingMusicBox"), AssetManager.Get<Sprite>("CalmingMusicBox"))
                    .SetMeta(ItemFlags.None, new string[] { "Lore", "Important" })
                    .Build());
            }

            yield return Incr();
            yield return "Overriding SceneObjects";

            void CreateNightScene(int nightNo)
            {
                var scene = new SceneObjectBuilder("F" + Mathf.Clamp(nightNo, 1, 3))
                    .SetTitle("N" + nightNo)
                    .SetManager<WarioGameManager>()
                    .Build();
                EditNight(scene, nightNo, NightType.Main);
                mainNights.Add(scene);
            }
            // for loops ?
            CreateNightScene(1);
            CreateNightScene(2);
            CreateNightScene(3);
            CreateNightScene(4);
            CreateNightScene(5);
            SceneObjectBuilder.LinkScenes(mainNights);
            areAssetsLoaded = true;
            yield break;
        }

        public void BreachCharacter(string characterName)
        {
            CoreGameManager.Instance.GetHud(0).ShowEventText($"Uh oh, seems that {characterName} breached!", 2f);
        }

        private C CreateEntityLikeAlarmClock<C>(EntityBuilder entitybuilder) where C : CustomEntity
        {
            var entity = entitybuilder.Build();
            var ent = entity.gameObject.AddComponent<C>();

            var clock = Resources.FindObjectsOfTypeAll<ITM_AlarmClock>().First();
            var rendererbase = Instantiate(clock.gameObject.transform.GetChild(0), ent.transform);

            ent.audMan = entity.gameObject.AddComponent<PropagatedAudioManager>();
            ent.spriteRenderer = rendererbase.GetChild(0).GetComponent<SpriteRenderer>();
            ent.entity = entity;
            ent.gameObject.ConvertToPrefab(false);
            return ent;
        }

        internal static void StartNight(SceneObject night)
        {
            var gameloader = Resources.FindObjectsOfTypeAll<GameLoader>().First();
            gameloader.gameObject.SetActive(true);
            gameloader.CheckSeed();
            gameloader.Initialize(2);
            gameloader.SetMode((int)Mode.Main);
            gameloader.LoadLevel(night);
        }
    }
    [HarmonyPatch(typeof(GameLoader), "LoadLevel")]
    class StopMenuMusicOnLevelLoad
    {
        static void Postfix() => WarioPlus.Instance.menuMusicSource.Stop();
    }

    [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.PlayMidi))]
    class NoMidiInTheHallsPatch
    {
        static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(WarningScreen), "Start")]
    class QuickTestPatch
    {
        static bool Prefix()
        {
            GlobalStateManager.Instance.skipNameEntry = true;
            SceneManager.LoadScene("MainMenu");
            return false;
        }
    }
    [HarmonyPatch(typeof(GlobalCam), nameof(GlobalCam.Transition))]
    class SwipeTransitionForEveryone
    {
        static bool Prefix(ref UiTransition type, ref float duration)
        {
            type = UiTransition.SwipeRight;
            duration = 0.4f;
            return true;
        }
    }
    public class WarioLevelObject : CustomLevelObject
    {
        public bool warioApparitionEnabled = false;
        public SoundObject levelintromusic = null;
    }

    public enum NightType
    {
        Main,
        Virus
    }
}



