using BepInEx.Logging;
using KKAPI.MainGame;

namespace BepInEx.DARLING.KK
{

    [BepInPlugin(GUID, "D. A. R. L. I. N. G.", "1.0.0")]
    internal class DARLINGPlugin : BaseUnityPlugin
    {
        private const string GUID = "Sauceke.DARLING";

        public static new ManualLogSource Logger { get; private set; }

        void Start()
        {
            Logger = base.Logger;
            GameAPI.RegisterExtraBehaviour<VoiceController>(GUID);
            Logger.LogDebug("At your service.");
        }
    }
}
