using KKAPI.MainGame;

namespace BepInEx.DARLING.KK
{
    public partial class VoiceController : GameCustomFunctionController
    {
        protected override void OnStartH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            _OnStartH(proc, hFlag);
        }

        protected override void OnEndH(BaseLoader proc, HFlag hFlag, bool vr)
        {
            _OnEndH(proc, hFlag);
        }
    }
}
