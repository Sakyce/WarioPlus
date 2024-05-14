using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using WarioPlus.Characters;
using WarioPlus.Characters.Basic;
using WarioPlus.Effects;
using WarioPlus.Entities;

namespace WarioPlus
{
    internal class CustomNotebookText
    {
        private string customSubtitle = "";
        private string nightName = "Unknown Night";
        private int hour = 11;
        private string hourSuffix = "PM";
        private bool replaceHourText;
        private bool hidden;

        public string NightName { get => nightName; }

        public CustomNotebookText()
        {
            customSubtitle = "";
            nightName = "";
        }
        public void SetHidden(bool hidden)
        {
            this.hidden = hidden;
            UpdateText();
        }
        public void SetNight(string nightName)
        {
            this.nightName = nightName;
            UpdateText();
        }
        public void SetReplaceHourText(bool replaceHourText)
        {
            this.replaceHourText = replaceHourText;
            UpdateText();
        }
        public void SetSubtitle(string customSubtitle)
        {
            this.customSubtitle = customSubtitle;
            UpdateText();
        }
        public void SetHour(int hour)
        {
            this.hour = hour;
            UpdateText();
        }
        public void SetHour(int hour, string suffix)
        {
            this.hour = hour;
            hourSuffix = suffix;
            UpdateText();
        }
        public void UpdateText()
        {
            if (CoreGameManager.Instance == null) return;
            CoreGameManager.Instance.GetHud(0).textBox[0].enableWordWrapping = false;
            CoreGameManager.Instance.GetHud(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);
            if (hidden)
            {
                CoreGameManager.Instance.GetHud(0).UpdateText(0, "");
                return;
            }
            if (replaceHourText)
            {
                CoreGameManager.Instance.GetHud(0).UpdateText(0, $"{nightName} - {customSubtitle}");
            }
            else
            {
                CoreGameManager.Instance.GetHud(0).UpdateText(0, $"{nightName} - {hour} {hourSuffix}\n{customSubtitle}");
            }
        }
    }
    internal class WarioGameManager : BaseGameManager
    {
        public bool ReplaceHourText = false;
        public bool darkTheme = false;
        public List<MonoBehaviorNPC> behaviorNPCs = new List<MonoBehaviorNPC>();

        public string nightLabel = "";
        public string nightNo = "";
        private int currentHour = 11;
        private bool isWarioApparitionActive = false;
        private int closedElevators = 0;

        public static event Action HourChanged;
        public CustomNotebookText flavorText = new CustomNotebookText();

        public WarioGameManager()
        {
            beginPlayImmediately = true;
            destroyOnLoad = true;
            spawnNpcsOnInit = false;
            spawnImmediately = false;
        }

        public override void BeginPlay()
        {
            base.BeginPlay();
            flavorText.SetNight(nightNo);
            if (darkTheme)
            {
                CoreGameManager.Instance.GetHud(0).transform.Find("Item Text").GetComponent<TextMeshProUGUI>().color = Color.white;
                CoreGameManager.Instance.GetHud(0).transform.Find("Notebook Text").GetComponent<TextMeshProUGUI>().color = Color.white;
            }
        }
        public override void Update()
        {
            base.Update();
            if (isWarioApparitionActive)
            {
                flavorText.SetSubtitle((int)Time.time % 2 == 0 ? "ESCAPE!" : "");

                if (closedElevators > 0)
                {
                    Shader.SetGlobalInt("_ColorGlitching", 1);
                    Shader.SetGlobalInt("_SpriteColorGlitching", 1);
                    Shader.SetGlobalInt("_ColorGlitchVal", (int)(Time.time % 4096f));
                    Shader.SetGlobalInt("_SpriteColorGlitchVal", (int)(Time.time % 4096f));
                    Shader.SetGlobalFloat("_ColorGlitchPercent", 0.25f * closedElevators);
                    Shader.SetGlobalFloat("_SpriteColorGlitchPercent", 0.25f * closedElevators);
                    Shader.SetGlobalFloat("_VertexGlitchIntensity", 0.25f * closedElevators);
                    Shader.SetGlobalFloat("_TileVertexGlitchIntensity", 0.25f * closedElevators);
                }
            }
        }
        public override void Initialize()
        {
            base.Initialize();

            Shader.SetGlobalColor("_SkyboxColor", Color.black);
            Shader.SetGlobalFloat("_VertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));
            Shader.SetGlobalFloat("_TileVertexGlitchSeed", UnityEngine.Random.Range(0f, 1000f));

            foreach (var room in ec.rooms)
            {
                if (room.category.Equals(RoomCategory.Class))
                {
                    var prefab = WarioPlus.AssetManager.Get<GrandFatherClock>("GrandfatherClock");
                    Instantiate(prefab).Initialize(room);
                }
            }
        }

        public void StartWarioApparition()
        {
            isWarioApparitionActive = true;
            ReplaceHourText = true;
            ec.audMan.FlushQueue(true);
            ec.standardDarkLevel = new Color(0.2f, 0.2f, 0.2f);
            ec.npcs.ToArray().Do(n => n.Despawn()); // don't use ForEach as it throws InvalidOperationException
            behaviorNPCs.ToArray().Do(n => n.Despawn());
            ec.SpawnNPC(WarioPlus.AssetManager.Get<WarioApparition>("WarioApparition"), IntVector2.GetGridPosition(ec.Players[0].transform.position));
        }
        public override void AllNotebooks()
        {
            if (!allNotebooksFound)
            {
                allNotebooksFound = true;
                ec.audMan.FlushQueue(true);
                ec.audMan.QueueAudio(WarioAssets.warioApparitionThemes[0], true);
                ec.SetElevators(true);
                elevatorsToClose = ec.elevators.Count - 1;
                foreach (Elevator elevator in ec.elevators)
                {
                    elevator.PrepareToClose();
                    StartCoroutine(ReturnSpawnFinal(elevator));
                }
            }
        }
        protected new IEnumerator ReturnSpawnFinal(Elevator elevator)
        {
            if (!isWarioApparitionActive)
            {
                yield return base.ReturnSpawnFinal(elevator);
                yield break;
            }
            yield return new WaitUntil(() => elevator.ColliderGroup.HasPlayer);

            if (elevatorsToClose == 1)
            {
                ec.audMan.FlushQueue(true);
                ec.audMan.QueueAudio(WarioAssets.warioApparitionThemes[2], true);
                ec.audMan.SetLoop(true);
            }
            if (elevatorsToClose == 0)
            {
                ec.npcs
                    .Where(x => x.character.Equals(EnumExtensions.GetFromExtendedName<Character>("WarioApparition")))
                    .Do(x => ((WarioApparition)x).noHopesLeft = true);
            }
            elevator.Door.Shut();
            elevator.ColliderGroup.Enable(false);
            elevator.Close();
            elevatorsToClose--;
            elevatorsClosed++;
            ec.MakeNoise(elevator.transform.position + (elevator.transform.forward * 10f), 31);
            ElevatorClosed(elevator);
            yield break;
        }
        public override void ElevatorClosed(Elevator elevator)
        {
            closedElevators += 1;
            if (!isWarioApparitionActive)
            {
                return;
            }

            ec.npcs.Where(x => x.character.Equals(EnumExtensions.GetFromExtendedName<Character>("WarioApparition")))
                .Do(x => ((WarioApparition)x).bonusSpeed += 4f);

            if (closedElevators == 1)
            {
                var untitledList = new List<Cell>();
                foreach (Cell cell in ec.lights)
                {
                    if (cell.lightStrength <= 1)
                    {
                        cell.lightColor = Color.red;
                        ec.SetLight(true, cell);
                    }
                    else
                    {
                        untitledList.Add(cell);
                    }
                }
                Shader.SetGlobalColor("_SkyboxColor", Color.red);
                ec.FlickerLights(true);
                ec.audMan.QueueAudio(WarioAssets.warioApparitionThemes[1]);
                ec.audMan.SetLoop(true);
            }
            else if (closedElevators == 2)
            {
            }
            else if (closedElevators == 3)
            {
            }
        }
        public override void BeginSpoopMode()
        {
            base.BeginSpoopMode();
            StartCoroutine(NightTimer());
            var marioAdded = false;
            foreach (var room in ec.rooms)
            {
                foreach (Transform descendant in room.GetComponentsInChildren<Transform>())
                {
                    if (!marioAdded && descendant.name.Contains("MyComputer") && !descendant.name.Contains("Off"))
                    {
                        marioAdded = true;
                        var m = descendant.gameObject.AddComponent<MarioComputer>();
                        behaviorNPCs.Add(m);
                        m.Initialize(room);
                    }
                }
            }
        }
        public override void CollectNotebooks(int count)
        {
            base.CollectNotebooks(count);
        }
        public override void ExitedSpawn()
        {
            base.ExitedSpawn();

            ec.audMan.PlayRandomAudio(levelObject is WarioLevelObject ld && ld.levelintromusic != null
                ? new SoundObject[] { ld.levelintromusic }
                : WarioAssets.introSounds);

            currentHour = 12;

            MiddleScreenText.GetInstance("1")
                .SetText(nightLabel)
                .SetColor(Color.blue)
                .FadeInAndOut(2, 4);

            BeginSpoopMode();
        }
        public override void RestartLevel()
        {
            if (isWarioApparitionActive && elevatorsToClose <= 0)
            {
                LoadNextLevel();
            }
            else
            {
                base.RestartLevel();
            }
        }

        public IEnumerator Night6Am()
        {
            ec.npcs.ToArray().Do(n => n.Despawn()); // don't use ForEach as it throws InvalidOperationException
            behaviorNPCs.ToArray().Do(n => n.Despawn());
            ec.audMan.PlayRandomAudio(WarioAssets.nightEndSounds);

            ec.standardDarkLevel = Color.black;
            ec.SetAllLights(false);
            CoreGameManager.Instance.GetHud(0).transform.Find("Staminometer").gameObject.SetActive(false);
            CoreGameManager.Instance.GetHud(0).transform.Find("ItemSlots").gameObject.SetActive(false);
            CoreGameManager.Instance.GetHud(0).transform.Find("Item Text").gameObject.SetActive(false);
            CoreGameManager.Instance.GetHud(0).transform.Find("Notebook Text").gameObject.SetActive(false);
            MiddleScreenText.GetInstance("1")
                .SetText("5AM")
                .SetColor(Color.white)
                .Fade(Color.white, 2);
            yield return new WaitForSecondsRealtime(5f);
            MiddleScreenText.GetInstance("1")
                .SetText("5AM")
                .SetColor(Color.white)
                .Fade(Color.white.AlphaMultiplied(0), 2);
            MiddleScreenText.GetInstance("2")
                .SetText("6AM")
                .SetColor(Color.white.AlphaMultiplied(0))
                .Fade(Color.white, 2);
            yield return FadeDarknessLevel(Color.black, new Color(1, 0.5f, 0), 3);
            yield return new WaitForSecondsRealtime(6f);
            CoreGameManager.Instance.GetHud(0).transform.Find("Staminometer").gameObject.SetActive(true);
            CoreGameManager.Instance.GetHud(0).transform.Find("ItemSlots").gameObject.SetActive(true);
            CoreGameManager.Instance.GetHud(0).transform.Find("Item Text").gameObject.SetActive(true);
            CoreGameManager.Instance.GetHud(0).transform.Find("Notebook Text").gameObject.SetActive(true);
            MiddleScreenText.GetInstance("2").SetText("");
            MiddleScreenText.GetInstance("1").SetText("");
            LoadNextLevel();
        }

        public IEnumerator FadeDarknessLevel(Color starting, Color target, float duration)
        {
            float tick = 0;
            while (tick < duration)
            {
                ec.standardDarkLevel = Color.Lerp(starting, target, tick / duration);
                Shader.SetGlobalColor("_SkyboxColor", Color.Lerp(starting, target, tick / duration));
                foreach (Cell cell in ec.lights)
                {
                    ec.SetLight(cell.lightOn, cell);
                }
                tick += Time.deltaTime / 4f;
                yield return null;
            }
            yield break;
        }

        public IEnumerator NightTimer()
        {
            flavorText.SetHour(currentHour, "AM");
            for (int h = 1; h <= 6; h++)
            {
                var timer = 1f;
                while (timer > 0)
                {
                    timer -= Time.deltaTime * ec.EnvironmentTimeScale;
                    yield return null;
                }
                currentHour = h;
                HourChanged.Invoke();
                flavorText.SetHour(currentHour);
            }
            if (levelObject is WarioLevelObject ld && ld.warioApparitionEnabled)
            {
                StartWarioApparition();
            }
            else
            {
                yield return Night6Am();
            }
            yield break;
        }

        internal void CalmingMusicBoxPlayed()
        {
            StopAllCoroutines();
            ec.audMan.FlushQueue(true);
            ec.audMan.PlaySingle(WarioPlus.AssetManager.Get<SoundObject>("CalmingMusicBox"));
        }
    }
}
