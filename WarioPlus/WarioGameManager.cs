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
    internal class WarioGameManager : BaseGameManager
    {
        public string nightLabel = "Unknown Night";
        public string nightName = "Custom Night";
        public bool ReplaceHourText = false;
        public bool darkTheme = false;
        public List<MonoBehaviorNPC> behaviorNPCs = new List<MonoBehaviorNPC>();

        private int currentHour = 11;
        private string customSubtitle = "";
        private bool isWarioApparitionActive = false;
        private int closedElevators = 0;

        public static event Action HourChanged;

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
            UpdateText();
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
                customSubtitle = (int)Time.time % 2 == 0 ? "ESCAPE!" : "";
                UpdateText();

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
            UpdateText();
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
            while (!elevator.ColliderGroup.HasPlayer) yield return null;
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
            ec.MakeNoise(elevator.transform.position + elevator.transform.forward * 10f, 31);
            ElevatorClosed(elevator);
            yield break;
        }
        public override void ElevatorClosed(Elevator elevator)
        {
            closedElevators += 1;
            if (!isWarioApparitionActive) return;
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
            UpdateText();
        }
        public override void CollectNotebooks(int count)
        {
            base.CollectNotebooks(count);
            UpdateText();
        }
        public override void ExitedSpawn()
        {
            base.ExitedSpawn();
            ec.audMan.PlayRandomAudio(levelObject is WarioLevelObject ld && ld.levelintromusic != null
                ? new SoundObject[] {ld.levelintromusic} 
                : WarioAssets.introSounds);
            currentHour = 12;
            UpdateText();
            MiddleScreenText.GetInstance()
                .SetText(nightLabel)
                .SetColor(Color.blue)
                .FadeInAndOut(2, 4);
            StartCoroutine(FadeDarknessLevel(Color.white, Color.black));
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

        public void UpdateText()
        {
            CoreGameManager.Instance.GetHud(0).textBox[0].enableWordWrapping = false;
            CoreGameManager.Instance.GetHud(0).gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);

            var hourSuffix = currentHour == 11 ? "PM" : "AM";
            if (ReplaceHourText)
            {
                CoreGameManager.Instance.GetHud(0).UpdateText(0, $"{nightName} - {customSubtitle}");
            }
            else
            {
                CoreGameManager.Instance.GetHud(0).UpdateText(0, $"{nightName} - {currentHour} {hourSuffix}\n{customSubtitle}");
            }
        }

        public IEnumerator Night6Am()
        {
            ec.npcs.ToArray().Do(n => n.Despawn()); // don't use ForEach as it throws InvalidOperationException
            behaviorNPCs.ToArray().Do(n => n.Despawn());
            ec.audMan.PlayRandomAudio(WarioAssets.nightEndSounds);
            ec.standardDarkLevel = Color.white;
            yield return FadeDarknessLevel(Color.black, new Color(1, 1, 0.3f));
            yield return new WaitForSecondsRealtime(8f);
            LoadNextLevel();
        }

        public IEnumerator FadeDarknessLevel(Color starting, Color target)
        {
            float i = 0;
            while (i < 1)
            {
                ec.standardDarkLevel = Color.Lerp(starting, target, i);
                Shader.SetGlobalColor("_SkyboxColor", Color.Lerp(starting, target, i));
                ec.SetAllLights(true);
                i += Time.deltaTime / 4f;
                yield return null;
            }
            yield break;
        }

        public IEnumerator NightTimer()
        {
            for (int h = 1; h <= 6; h++)
            {
                var timer = 32f;
                while (timer > 0)
                {
                    timer -= Time.deltaTime * ec.EnvironmentTimeScale;
                    yield return null;
                }
                currentHour = h;
                HourChanged.Invoke();
                UpdateText();
            }
            if (levelObject is WarioLevelObject ld && ld.warioApparitionEnabled) StartWarioApparition();
            else yield return Night6Am();
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
