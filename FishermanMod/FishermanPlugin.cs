using BepInEx;
using FishermanMod.Survivors.Fisherman;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

//rename this namespace
namespace FishermanMod
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class FishermanPlugin : BaseUnityPlugin
    {
        // if you do not change this, you are giving permission to deprecate the mod-
        //  please change the names to your own stuff, thanks
        //   this shouldn't even have to be said
        public const string MODUID = "com.wunt.FishermanMod";
        public const string MODNAME = "Wunt-FishermanMod";
        public const string MODVERSION = "0.3.1";

        // a prefix for name tokens to prevent conflicts- please capitalize all name tokens for convention
        public const string DEVELOPER_PREFIX = "WUNT";

        public static FishermanPlugin instance;

        void Awake()
        {
            instance = this;

            //easy to use logger
            Log.Init(Logger);

            // used when you want to properly set up language folders
            Modules.Language.Init();

            // character initialization
            new FishermanSurvivor().Initialize();

            
            // make a content pack and add it. this has to be last
            new Modules.ContentPacks().Initialize();
        }
        void Start()
        {
            
        }
    }
}
