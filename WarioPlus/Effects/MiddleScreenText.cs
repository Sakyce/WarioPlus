using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WarioPlus.Effects
{
    internal class MiddleScreenText : MonoBehaviour
    {
        private TextMeshProUGUI textbox;

        public static MiddleScreenText GetInstance()
        {
            var obj = CoreGameManager.Instance.GetHud(0).transform.Find("ModdedMiddleScreenText");
            if (obj != null)
                return obj.GetComponent<MiddleScreenText>();
            else
                return Create();
        }
        public static MiddleScreenText Create() {
            var parent = CoreGameManager.Instance.GetHud(0).transform;
            var copy = Instantiate(parent.Find("Notebook Text"));
            copy.name = "ModdedMiddleScreenText";

            var comp = copy.gameObject.AddComponent<MiddleScreenText>();
            comp.textbox = copy.GetComponent<TextMeshProUGUI>();
            comp.textbox.horizontalAlignment = HorizontalAlignmentOptions.Center;
            comp.textbox.SetText("FUCK");

            var trans = copy.GetComponent<RectTransform>();
            trans.anchorMin = Vector2.one * 0.5f;
            trans.anchorMax = Vector2.one * 0.5f;
            trans.pivot = Vector2.one * 0.5f;
            trans.SetParent(parent, false);
            return comp;
        }
        public MiddleScreenText SetText(string text)
        {
            textbox.SetText(text);
            return this;
        }
        public MiddleScreenText SetColor(Color color)
        {
            textbox.color = color;
            return this;
        }

        public void FadeInAndOut(float duration, float holdDuration)
        {
            StartCoroutine(FadeInAndOutTimer(duration, holdDuration));
        }
        private IEnumerator FadeInAndOutTimer(float duration, float holdDuration)
        {
            textbox.color = textbox.color.AlphaMultiplied(0);
            yield return FadeTimer(new Color(textbox.color.r, textbox.color.g, textbox.color.b, 1), duration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return FadeTimer(textbox.color.AlphaMultiplied(0), duration);
            yield break;
        }

        public void Fade(Color target, float duration) => StartCoroutine(FadeTimer(target, duration));
        private IEnumerator FadeTimer(Color target, float duration)
        {
            var delay = 0f;
            var startingColor = textbox.color;
            while (delay < duration)
            {
                delay += Time.deltaTime;
                textbox.color = Color.Lerp(startingColor, target, delay/duration);
                yield return null;
            }
            textbox.color = target;
            yield break;
        }
    }
}
