using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WarioPlus
{
    internal static class Utils
    {
        [CanBeNull]
        internal static Manager GetManager<Manager>(this SceneObject scene) where Manager : BaseGameManager
        {
            try
            {
                return (Manager)scene.manager;
            }
            catch (InvalidCastException e)
            {
                return null;
            }
        }
        internal static Texture2D[] TexturesFromMod(BaseUnityPlugin mod, string pattern, (int, int) range)
        {
            var textures = new List<Texture2D>();
            for (int i = range.Item1; i <= range.Item2; i++)
            {
                textures.Add(AssetLoader.TextureFromMod(mod, String.Format(pattern, i)));
            }
            return textures.ToArray();
        }
        public static SoundObject NoSubtitles(this SoundObject snd)
        {
            snd.subtitle = false;
            return snd;
        }
        public static WeightedItemObject ToWeighted(this ItemObject itm, int weight)
        {
            return new WeightedItemObject() { selection = itm, weight = weight };
        }
        public static Sprite ToSprite(this Texture2D tex, float pixelsPerUnit)
        {
            return AssetLoader.SpriteFromTexture2D(tex, pixelsPerUnit);
        }
        public static IEnumerable<T> Print<T>(this IEnumerable<T> array, string arrayName)
        {
            Debug.Log("Array " + arrayName + " {");
            foreach (var item in array)
            {
                Debug.Log("    " + item.ToString());
            }
            Debug.Log("}");
            return array;
        }
    }

    internal class SceneObjectBuilder
    {
        private readonly SceneObject scene;
        public static void LinkScenes(List<SceneObject> scenes)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                scene.levelNo = i + 1;
                if (i < scenes.Count - 1)
                {
                    var nextScene = scenes[i + 1];
                    scene.nextLevel = nextScene;
                }
            }
        }
        public SceneObjectBuilder(string baseLevelTitle)
        {
            var baseScene = Resources.FindObjectsOfTypeAll<SceneObject>()
                .Where(x => x.levelTitle == baseLevelTitle)
                .First();

            var wariolevel = ScriptableObject.CreateInstance<WarioLevelObject>();
            typeof(LevelObject).GetFields().Do(x => x.SetValue(wariolevel, x.GetValue(baseScene.levelObject)));
            wariolevel.previousLevels = new LevelObject[] { };
            wariolevel.name = "UnnamedLevelObject";
            wariolevel.MarkAsNeverUnload();

            scene = Object.Instantiate(baseScene);
            scene.levelObject = wariolevel;
            scene.levelTitle = "CS";
            scene.name = $"Modded_Unnamed_Scene";
            scene.MarkAsNeverUnload();
        }
        public SceneObjectBuilder SetTitle(string title)
        {
            scene.levelTitle = title;
            scene.name = $"Modded_{title}_Scene";
            scene.levelObject.name = $"{title}LevelObject";
            return this;
        }
        public SceneObjectBuilder SetManager<Manager>() where Manager : BaseGameManager
        {
            var managerObj = new GameObject();
            managerObj.name = scene.name + "_Manager";
            managerObj.ConvertToPrefab(true);
            Object.DontDestroyOnLoad(managerObj);

            var manager = managerObj.AddComponent<Manager>();
            manager.ec = scene.manager.ec;

            scene.manager = manager;
            return this;
        }
        public SceneObjectBuilder Do(Action<SceneObject> action)
        {
            action.Invoke(scene);
            return this;
        }
        public SceneObject Build()
        {
            return scene;
        }
    }

    internal class CustomEntity : MonoBehaviour
    {
        internal Entity entity;
        internal AudioManager audMan;
        internal SpriteRenderer spriteRenderer;
        internal EnvironmentController ec;

        public virtual void Initialize(RoomController rc)
        {
            ec = rc.ec;
            audMan = GetComponent<PropagatedAudioManager>();
            spriteRenderer = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
            entity = GetComponent<Entity>();
            entity.transform = GetComponent<Transform>();
            entity.rendererBase = transform.GetChild(0);
            entity.Initialize(ec, transform.position);
        }
    }
}
