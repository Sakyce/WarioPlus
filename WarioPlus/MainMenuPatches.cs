using HarmonyLib;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Reflection;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WarioPlus
{
    internal class WarioModes : MonoBehaviour
    {
        private static void SetModeText(StandardMenuButton button, string text)
        {
            var modeText = button.transform.parent.Find("ModeText").GetComponent<TMP_Text>();
            button.OnHighlight = new UnityEvent();
            button.OnHighlight.AddListener(() => modeText.SetText(text));
        }
        private static void SetModeOnPress(StandardMenuButton button, Action action)
        {
            button.OnPress = new UnityEvent();
            button.OnPress.AddListener(action.Invoke);
        }
        public void Awake()
        {
            transform.Find("BG").GetComponent<Image>().color = Color.black;
            foreach (var text in transform.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.color = Color.white;
            }
            foreach (var text in transform.GetComponentsInChildren<TextLocalizer>())
            {
                text.enabled = false;
            }
            transform.Find("MainNew").GetComponent<TMP_Text>().SetText("Main Mode");
            transform.Find("Endless").GetComponent<TMP_Text>().SetText("2018 Mode");
            transform.Find("Free").GetComponent<TMP_Text>().SetText("Showtime Mode");
            Destroy(transform.Find("FieldTrips").gameObject);
            Destroy(transform.Find("Challenge").gameObject);

            {
                var button = transform.Find("MainNew").GetComponent<StandardMenuButton>();
                SetModeText(button, "Play the main 5 nights!");
                SetModeOnPress(button, () => WarioPlus.StartNight(WarioPlus.Instance.mainNights[2]));
            }
            {
                var button = transform.Find("Endless").GetComponent<StandardMenuButton>();
                SetModeText(button, "One of your friends told you to come back. But things don't goes as expected...");
                SetModeOnPress(button, delegate
                {

                });
            }
            {
                var button = transform.Find("Free").GetComponent<StandardMenuButton>();
                SetModeText(button, "ITs SHOWTIME...");
                SetModeOnPress(button, delegate
                {

                });
            }
        }
    }
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Start))]
    internal class MainMenuPatch
    {
        static void Postfix(MainMenu __instance)
        {
            Object.Destroy(__instance.transform.Find("About").gameObject);
            Object.Destroy(__instance.transform.Find("ChangelogButton").gameObject);
            __instance.transform.Find("Version").GetComponent<TextMeshProUGUI>().color = Color.white;

            __instance.StartCoroutine(WaitForAssets());
        }

        static IEnumerator WaitForAssets()
        {
            yield return new WaitUntil(() => WarioPlus.Instance.areAssetsLoaded);
            
            Resources.FindObjectsOfTypeAll<Image>()
                .Where(x => x.sprite != null && x.sprite.texture.name == "TempMenu_Low")
                .Do(x => x.sprite = WarioPlus.AssetManager.Get<Sprite>("MainMenu"));
            WarioPlus.Instance.menuMusicSource.Play();
        }
    }
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.Quit))]
    internal class MainMenuQuitPatch
    {
        static bool Prefix(MainMenu __instance)
        {
            Application.Quit();
            return false;
        }
    }

    [HarmonyPatch(typeof(MainModeButtonController), nameof(MainModeButtonController.OnEnable))]
    internal class PickModePatch
    {
        
        static void Postfix(MainMenu __instance)
        {
            if (__instance.GetComponent<WarioModes>() == null)
            {
                __instance.gameObject.AddComponent<WarioModes>();
            }
        }
    }
}
