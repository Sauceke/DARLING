using BepInEx.Unity;
using HarmonyLib;
using Illusion.Game;
using KKAPI.MainGame;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

namespace BepInEx.DARLING.KK
{
    public class VoiceController : GameCustomFunctionController
    {
        private KeywordRecognizer recognizer;
        private BaseLoader proc;
        private HFlag hFlag;
        private HSprite sprite;
        private List<HActionBase> lstProc;
        private GameObject fakeAnimButton;

        private List<HSceneProc.AnimationListInfo>[] AnimationLists => Traverse.Create(proc)
            .Field<List<HSceneProc.AnimationListInfo>[]>("lstUseAnimInfo")
            .Value;
        private HActionBase CurrentProc => lstProc[(int)hFlag.mode];

        private Dictionary<string, Action> commands;

        public VoiceController()
        {
            commands = new Dictionary<string, Action>
            {
                { "undress", Undress },
                { "missionary", () => SelectPose("Missionary", "正常位") },
                { "cowgirl", () => SelectPose("Cowgirl", "騎乗位") },
                { "doggy", () => SelectPose("Doggy", "後背位") },
                { "blowjob", () => SelectPose("Blowjob", "フェラ") },
                { "insert", Insert },
                { "put it in", Insert },
                { "faster", () => ChangeSpeed(+0.2f) },
                { "slower", () => ChangeSpeed(-0.2f) },
                { "stronger", () => ChangeStrength(hard: true) },
                { "weaker", () => ChangeStrength(hard: false) },
                { "I'm coming", Orgasm }
            };
        }

        private void Orgasm()
        {
            sprite.OnFemaleGaugeLock(false);
            sprite.OnMaleGaugeLock(false);
            hFlag.FemaleGaugeUp(100f, _force: true);
            hFlag.MaleGaugeUp(100f);
        }

        void Start()
        {
            recognizer = new KeywordRecognizer(commands.Keys.ToArray());
            recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
        }

        void OnDestroy()
        {
            recognizer.Dispose();
        }

        protected override void OnStartH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            this.proc = proc;
            this.hFlag = hFlag;
            sprite = Traverse.Create(proc).Field<HSprite>("sprite").Value;
            if (sprite == null)
            {
                sprite = Traverse.Create(proc).Field<HSprite[]>("sprites").Value[1];
            }
            lstProc = Traverse.Create(proc).Field<List<HActionBase>>("lstProc").Value;
            fakeAnimButton = Instantiate(sprite.objMotionListNode, gameObject.transform, false);
            fakeAnimButton.AddComponent<HSprite.AnimationInfoComponent>();
            fakeAnimButton.SetActive(true);
            recognizer.Start();
        }

        protected override void OnEndH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            recognizer.Stop();
        }

        private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs e)
        {
            DARLINGPlugin.Logger.LogDebug($"Voice command: {e.text}");
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
            var selected = AnimationLists.SelectMany(a => a)
                .Where(anim => knownNames.Any(name => anim.nameAnimation.Contains(name)))
                .FirstOrDefault();
            if (selected == null)
            {
                Utils.Sound.Play(SystemSE.cancel);
                return;
            }
            fakeAnimButton.GetComponent<HSprite.AnimationInfoComponent>().info = selected;
            fakeAnimButton.GetComponent<Toggle>().isOn = false;
            sprite.OnChangePlaySelect(fakeAnimButton);
            fakeAnimButton.GetComponent<HSprite.AnimationInfoComponent>().info = null;
        }

        private void ChangeSpeed(float delta)
        {
            if (hFlag.mode == HFlag.EMode.sonyu)
            {
                if (!((HSonyu)CurrentProc).isAuto)
                {
                    hFlag.click = HFlag.ClickKind.modeChange;
                }
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

        private void Undress()
        {
            InputSimulator.MouseButtonUp(0);
            sprite.OnClickAllCloth(3);
            InputSimulator.UnsetMouseButton(0);
        }
    }
}
