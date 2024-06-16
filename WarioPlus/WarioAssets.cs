using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WarioPlus
{

    public static class WarioAssets
    {
        public static SoundObject[] introSounds;
        public static SoundObject[] nightEndSounds;
        public static SoundObject[] warioApparitionThemes;

        public static SoundObject grandfatherClockSound;
        public static Sprite grandfatherClock;

        public static SoundObject marioMusicBox;
        public static Material[] marioComputer;

        public static Dictionary<string, Texture2D> characterPosters;
        public static Dictionary<string, SoundObject> ambients;
        public static Dictionary<string, SoundObject> losesounds;

        public static Sprite menuSprite;

        private static Material QuickMaterial(string baseMatName, Texture tex)
        {
            var mat = Object.Instantiate(Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name.Contains(baseMatName)).First());
            mat.name = "CustomMat";
            mat.mainTexture = tex;
            return mat;
        }
        public static AudioClip Clip(params string[] paths) => AssetLoader.AudioClipFromMod(WarioPlus.Instance, paths);
        public static SoundObject NoSubtitle(AudioClip clip, SoundType type = SoundType.Effect) => ObjectCreators.CreateSoundObject(clip, "", type, Color.red, 0);
        public static Texture2D Texture(params string[] paths) => AssetLoader.TextureFromMod(WarioPlus.Instance, paths);

        private static Dictionary<string, Texture2D> LoadTextures2D(params string[] paths)
        {
            return Directory
                .GetFiles(Path.Combine(AssetLoader.GetModPath(WarioPlus.Instance), Path.Combine(paths)))
                .ToDictionary(
                    x => Path.GetFileNameWithoutExtension(x),
                    x => AssetLoader.TextureFromFile(x)
                );
        }
        private static Dictionary<string, SoundObject> LoadSoundObjectsNoSubtitle(params string[] paths)
        {
            return Directory
                .GetFiles(Path.Combine(AssetLoader.GetModPath(WarioPlus.Instance), Path.Combine(paths)))
                .ToDictionary(
                    x => Path.GetFileNameWithoutExtension(x),
                    x => NoSubtitle(Clip(x))
                );
        }

        public static void Load()
        {
            grandfatherClockSound = ObjectCreators.CreateSoundObject(Clip("audio", "clockbell.ogg"), "*Ding Dong!*", SoundType.Effect, Color.white);
            grandfatherClock = Texture("entity", "grandfatherclock.png").ToSprite(26f);

            characterPosters = LoadTextures2D("characters", "posters");

            ambients = LoadSoundObjectsNoSubtitle("audio", "ambient");
            losesounds = LoadSoundObjectsNoSubtitle("audio", "losesounds");

            WarioPlus.AssetManager.Add("click", NoSubtitle(Clip("audio", "click.ogg")));


            var warioapparitionsprites = Utils.TexturesFromMod(WarioPlus.Instance, "characters/basic/apparition_{0}.png", (0, 9)).ToSprites(35f);
            WarioPlus.AssetManager.Add("WarioApparition", warioapparitionsprites.AddRangeToArray(warioapparitionsprites.Reverse().ToArray()));
            WarioPlus.AssetManager.Add("WarioLaughs", LoadSoundObjectsNoSubtitle("audio", "characters", "warioapparition").Values.ToArray());

            WarioPlus.AssetManager.Add("MainMenu", Texture("menu.png").ToSprite(1f));
            WarioPlus.AssetManager.Add("MainMenu", NoSubtitle(Clip("musics", "menu.ogg")));

            // Builds
            WarioPlus.AssetManager.Add("CellarFloor", Texture("building", "DarkWoodFloor.png"));
            WarioPlus.AssetManager.Add("CellarWall", Texture("building", "InvertedWall.png"));
            WarioPlus.AssetManager.Add("CellarCeiling", Texture("building", "DarkWoodCeiling.png"));

            // Mario
            marioMusicBox = ObjectCreators.CreateSoundObject(Clip("musics", "mario.ogg"), "*Music box*", SoundType.Effect, Color.white);
            WarioPlus.AssetManager.Add("mario", Texture("characters", "basic", "mario.png").ToSprite(35f));
            marioComputer = new Material[]
            {
                QuickMaterial("MyComputer", Texture("entity", "Mario0.png")),
                QuickMaterial("MyComputer", Texture("entity", "Mario1.png")),
                QuickMaterial("MyComputer", Texture("entity", "Mario1.png")),
                QuickMaterial("MyComputer", Texture("entity", "Mario1.png")),
                QuickMaterial("MyComputer", Texture("entity", "Mario1.png")),
            };

            // Various sounds
            WarioPlus.AssetManager.Add("CellarIntro", NoSubtitle(Clip("musics", "cellarintro.ogg")));

            // Items
            WarioPlus.AssetManager.Add("PillLarge", Texture("items", "longsightpill_item.png").ToSprite(64f));
            WarioPlus.AssetManager.Add("PillSmall", Texture("items", "longsightpill_hud.png").ToSprite(1f));
            WarioPlus.AssetManager.Add("PocketWatch", Texture("items", "clock_item.png").ToSprite(64f));
            WarioPlus.AssetManager.Add("CalmingMusicBox", Texture("items", "musicbox_item.png").ToSprite(64f));
            WarioPlus.AssetManager.Add("CalmingMusicBox", NoSubtitle(Clip("audio", "characters", "richard", "calmingmusicbox.ogg")));

            warioApparitionThemes = new SoundObject[]
            {
                NoSubtitle(Clip("musics", "warioapparition1.ogg"), SoundType.Music),
                NoSubtitle(Clip("musics", "warioapparition2.ogg"), SoundType.Music),
                NoSubtitle(Clip("musics", "warioapparition3.ogg"), SoundType.Music),
            };
            introSounds = LoadSoundObjectsNoSubtitle("musics", "intros").Values.ToArray();
            nightEndSounds = new SoundObject[] {
                NoSubtitle(Clip("musics", "6am.ogg")),
            };
        }

    }
}
