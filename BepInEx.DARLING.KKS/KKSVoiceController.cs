using KKAPI.MainGame;
using UnityEngine;

namespace BepInEx.DARLING.KK
{
    public partial class VoiceController : GameCustomFunctionController
    {
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            _OnStartH(proc, hFlag);
        }

        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            _OnEndH(proc, hFlag);
        }
    }
}
