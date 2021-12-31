using BepInEx.Unity;
using HarmonyLib;
using Illusion.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

namespace BepInEx.DARLING.KK
{
    public partial class VoiceController
    {
        private KeywordRecognizer recognizer;
        private HFlag hFlag;
        private HSprite sprite;
        private List<HActionBase> lstProc;

        private IEnumerable<GameObject> AnimationNames {
            get
            {
                var animations = Traverse
                    .Create(sprite)
                    .Field<List<HSceneProc.AnimationListInfo>[]>("lstUseAnimInfo")
                    .Value
                    .SelectMany(a => a)
                    .ToList();
                var fakeParent = new GameObject();
                fakeParent.AddComponent<VerticalLayoutGroup>();
                fakeParent.AddComponent<ContentSizeFitter>();
                sprite.LoadMotionList(animations, fakeParent);
                return fakeParent.GetComponentsInChildren<HSprite.AnimationInfoComponent>()
                    .Select(comp => comp.gameObject);
            }
        }

        private HActionBase CurrentProc => lstProc[(int)hFlag.mode];

        private Dictionary<string, Action> commands;

        private void Orgasm()
        {
            sprite.OnFemaleGaugeLock(false);
            sprite.OnMaleGaugeLock(false);
            hFlag.FemaleGaugeUp(100f, _force: true);
            hFlag.MaleGaugeUp(100f);
        }

        private void OnDestroy()
        {
            recognizer.Dispose();
        }

        private void StartListening()
        {
            recognizer?.Stop();
            recognizer?.Dispose();
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                commands = new Dictionary<string, Action>
                {
                    { "脱いで", Undress },
                    { "入れて", Insert },
                    { "入れるぞ", Insert },
                    { "もっと早く", () => ChangeSpeed(+0.2f) },
                    { "もっとゆっくり", () => ChangeSpeed(-0.2f) },
                    { "もっと強く", () => ChangeStrength(hard: true) },
                    { "もっと優しく", () => ChangeStrength(hard: false) },
                    { "ストップ", Stop },
                    { "待って", Stop },
                    { "行っちゃう", Orgasm },
                    { "もうダメ", Orgasm }
                };
            }
            else
            {
                commands = new Dictionary<string, Action>
                {
                    { "undress", Undress },
                    { "insert", Insert },
                    { "put it in", Insert },
                    { "faster", () => ChangeSpeed(+0.2f) },
                    { "slower", () => ChangeSpeed(-0.2f) },
                    { "stronger", () => ChangeStrength(hard: true) },
                    { "harder", () => ChangeStrength(hard: true) },
                    { "weaker", () => ChangeStrength(hard: false) },
                    { "softer", () => ChangeStrength(hard: false) },
                    { "stop", Stop },
                    { "wait", Stop },
                    { "I'm coming", Orgasm }
                };
            }
            foreach (var animName in AnimationNames.Select(
                btn => btn.GetComponentInChildren<TextMeshProUGUI>().text))
            {
                commands[animName.ToLower()] = () => SelectPose(animName);
            }
            foreach (var entry in DARLINGPlugin.GetCustomCommands())
            {
                commands[entry.Key] = () => SelectPose(entry.Value);
            }
            recognizer = new KeywordRecognizer(commands.Keys.ToArray());
            recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
            recognizer.Start();
            DARLINGPlugin.Logger.LogDebug("At your service.");
        }

        private void _OnStartH(MonoBehaviour proc, HFlag hFlag)
        {
            this.hFlag = hFlag;
            sprite = Traverse.Create(proc).Field<HSprite>("sprite").Value;
            if (sprite == null)
            {
                sprite = Traverse.Create(proc).Field<HSprite[]>("sprites").Value[1];
            }
            lstProc = Traverse.Create(proc).Field<List<HActionBase>>("lstProc").Value;
            StartListening();
        }

        private void _OnEndH(MonoBehaviour proc, HFlag hFlag)
        {
            recognizer.Stop();
            recognizer.Dispose();
            commands.Clear();
        }

        private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs e)
        {
            DARLINGPlugin.Logger.LogInfo($"Voice command: {e.text}");
            commands[e.text].Invoke();
        }

        private void Insert()
        {
            if (hFlag.mode != HFlag.EMode.sonyu)
            {
                return;
            }
            var menu = sprite.sonyu.categoryActionButton.lstButton
                .Where(button => button.isActiveAndEnabled && button.interactable);
            sprite.enabled = true;
            // koikatsu actions check for left click mouse up
            InputSimulator.MouseButtonUp(0);
            sprite.OnInsertClick();
            InputSimulator.UnsetMouseButton(0);
            sprite.OnFemaleGaugeLock(true);
            sprite.OnMaleGaugeLock(true);
        }

        private void SelectPose(params string[] knownNames)
        {
            foreach (var button in AnimationNames)
            {
                var text = button.GetComponentInChildren<TextMeshProUGUI>().text;
                if (knownNames.Any(name => text.ToLower() == name.ToLower()))
                {
                    button.GetComponent<Toggle>().isOn = false;
                    sprite.OnChangePlaySelect(button);
                    return;
                }
            }
            Utils.Sound.Play(SystemSE.cancel);
        }

        private void ChangeSpeed(float delta)
        {
            SetAutoSpeed(true);
            if (hFlag.mode == HFlag.EMode.houshi
                && hFlag.nowAnimStateName == "Idle")
            {
                CurrentProc.MotionChange(1);
            }
            hFlag.speedCalc = Mathf.Clamp(hFlag.speedCalc + delta, 0, 1);
            sprite.OnFemaleGaugeLock(true);
            sprite.OnMaleGaugeLock(true);
        }

        private void ChangeStrength(bool hard)
        {
            bool softToHard = hard && hFlag.nowAnimStateName.Contains("WLoop");
            bool hardToSoft = !hard && hFlag.nowAnimStateName.Contains("SLoop");
            if (softToHard || hardToSoft)
            {
                hFlag.click = HFlag.ClickKind.motionchange;
            }
        }

        private void Stop()
        {
            SetAutoSpeed(false);
            if (hFlag.mode == HFlag.EMode.houshi)
            {
                CurrentProc.MotionChange(0);
            }
            hFlag.speedCalc = 0f;
        }

        private void Undress()
        {
            InputSimulator.MouseButtonUp(0);
            sprite.OnClickAllCloth(3);
            InputSimulator.UnsetMouseButton(0);
        }

        private void SetAutoSpeed(bool auto)
        {
            if (hFlag.mode == HFlag.EMode.sonyu)
            {
                if (auto != ((HSonyu)CurrentProc).isAuto)
                {
                    hFlag.click = HFlag.ClickKind.modeChange;
                }
            }
        }
    }
}
