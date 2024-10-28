using BepInEx.Configuration;
using EntityStates;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Modules;
using FishermanMod.Modules.Characters;
using FishermanMod.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman.SkillStates;
using HG;
using R2API.Networking;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEngine.GridBrushBase;

namespace FishermanMod.Survivors.Fisherman
{
    public class FishermanSurvivor : SurvivorBase<FishermanSurvivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string assetBundleName => "fishermanbundleprime"; //if you do not change this, you are giving permission to deprecate the mod

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string bodyName => "FishermanBody"; //if you do not change this, you get the point by now

        //name of the ai master for vengeance and goobo. must be unique
        public override string masterName => "FishermanMaster"; //if you do not

        //the names of the prefabs you set up in unity that we will use to build your character
        public override string modelPrefabName => "mdlFisherman";
        public override string displayPrefabName => "FishermanDisplay";

        public const string FISHERMAN_PREFIX = FishermanPlugin.DEVELOPER_PREFIX + "_FISHERMAN_";
        
        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => FISHERMAN_PREFIX;

        public static SkillDef secondaryRecallFishHook;
        public static SkillDef secondaryFireFishHook;

        public static SkillDef specialRecallHookBomb;
        public static SkillDef specialThrowHookBomb;

        public static SkillDef specialDrinkFlask;
        public static SkillDef specialThrowFlask;

        public static SkillDef primaryShantyCannon;
        public static SkillDef utilitySummonPlatform;
        public static SkillDef utilityDirectPlatform;

        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = FISHERMAN_PREFIX + "NAME",
            subtitleNameToken = FISHERMAN_PREFIX + "SUBTITLE",

            characterPortrait = assetBundle.LoadAsset<Texture>("texWIPFishermanIcon"),
            bodyColor = new Color32(198, 184, 2, 255),
            sortPosition = 100,

            crosshair = ModAssetManager.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 160,
            healthRegen = 2.5f,
            armor = 20f,
            damage = 14,
            damageGrowth = 2.8f,
            healthGrowth = 48,
            regenGrowth = 0.5f,
            

            jumpCount = 1,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
                new CustomRendererInfo
                {
                    childName = "FishingPole",
                    material = assetBundle.LoadMaterial("Fisherman_Diffuse_BaseColor"),
                },
                new CustomRendererInfo
                {
                    childName = "TackleBox",
                    material = assetBundle.LoadMaterial("Fisherman_Diffuse_BaseColor"),
                },
                new CustomRendererInfo
                {
                    childName = "MainMesh",
                    material = assetBundle.LoadMaterial("Fisherman_Diffuse_BaseColor"),
                },
                new CustomRendererInfo
                {
                    childName = "Drink",
                    material = assetBundle.LoadMaterial("FishermanBottle_BaseColor"),
                }

        };

        public override UnlockableDef characterUnlockableDef => FishermanUnlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new FishermanItemDisplays();

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }

        public override GameObject bodyPrefab { get; protected set; }
        public override CharacterBody prefabCharacterBody { get; protected set; }
        public override GameObject characterModelObject { get; protected set; }
        public override CharacterModel prefabCharacterModel { get; protected set; }
        public override GameObject displayPrefab { get; protected set; }

        public override void Initialize()
        {
            //uncomment if you have multiple characters
            //ConfigEntry<bool> characterEnabled = Config.CharacterEnableConfig("Survivors", "Henry");

            //if (!characterEnabled.Value)
            //    return;

            base.Initialize();

        }

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            FishermanUnlockables.Init();

            base.InitializeCharacter();
            DamageTypes.RegisterDamageTypes(); // TODO probably re organize this somehow
            FishermanConfig.Init();
            FishermanStates.Init();
            FishermanTokens.Init();

            FishermanAssets.Init(assetBundle);
            FishermanBuffs.Init(assetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            InitializeSkins();
            InitializeCharacterMaster();

            AdditionalBodySetup();

            AddHooks();
        }

        private void AdditionalBodySetup()
        {
            AddHitboxes();
            bodyPrefab.AddComponent<HenryWeaponComponent>();
            var drinkmdl = characterModelObject.GetComponent<ChildLocator>().FindChild("Drink");
            drinkmdl.gameObject.SetActive(false);
            bodyPrefab.AddComponent<SkillObjectTracker>();
            //bodyPrefab.AddComponent<HuntressTrackerComopnent>();
            //anything else here
        }

        public void AddHitboxes()
        {
            ChildLocator childLocator = characterModelObject.GetComponent<ChildLocator>();

            //example of how to create a hitbox
            Transform swipeHitBoxTransform = childLocator.FindChild("SwipeHitbox");
            Transform stabHitBoxTransform = childLocator.FindChild("StabHitbox");
            Transform uppercutHitboxTransform = childLocator.FindChild("UppercutHitbox");
            
            Prefabs.SetupHitBoxGroup(characterModelObject, "SwipeGroup", swipeHitBoxTransform);
            Prefabs.SetupHitBoxGroup(characterModelObject, "StabGroup", stabHitBoxTransform);
            Prefabs.SetupHitBoxGroup(characterModelObject, "UppercutGroup", uppercutHitboxTransform);
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(bodyPrefab);

            //if you set up a custom main characterstate, set it up here
                //don't forget to register custom entitystates in your HenryStates.cs
            //the main "body" state machine has some special properties
            Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon2");
            Prefabs.AddEntityStateMachine(bodyPrefab, "FishookRecall");
            Prefabs.AddEntityStateMachine(bodyPrefab, "NervesThrow");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(bodyPrefab);
            //add our own
            Skills.CreateSkillFamilies(bodyPrefab);
            AddPassiveSkill();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtiitySkills();
            AddSpecialSkills();
        }


        private void AddPassiveSkill()
        {
            SkillLocator skillLocator = bodyPrefab.GetComponent<SkillLocator>();
            skillLocator.passiveSkill.enabled = true;
            skillLocator.passiveSkill.skillNameToken = FISHERMAN_PREFIX + "PASSIVE_HOOK_EFFECT_NAME";
            skillLocator.passiveSkill.skillDescriptionToken = FISHERMAN_PREFIX + "PASSIVE_HOOK_EFFECT_DESCRIPTION";
            skillLocator.passiveSkill.icon = ModAssetManager.loadedBundles["fishermanbundleprime"].LoadAsset<Sprite>("Hook Icon");
        }
        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            //the primary skill is created using a constructor for a typical primary
            //it is also a SteppedSkillDef. Custom Skilldefs are very useful for custom behaviors related to casting a skill. see ror2's different skilldefs for reference
            SteppedSkillDef primaryFishingPoleMelee = Skills.CreateSkillDef<SteppedSkillDef>(new SkillDefInfo
                (
                    "Debone",
                    FISHERMAN_PREFIX + "PRIMARY_SLASH_NAME",
                    FISHERMAN_PREFIX + "PRIMARY_SLASH_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("Melee Attack Icon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.PrimaryAttack)),
                    "Weapon",
                    false
                ));
            //custom Skilldefs can have additional fields that you can set manually
            primaryFishingPoleMelee.stepCount = 3;
            primaryFishingPoleMelee.stepGraceDuration = 0.5f;
            primaryFishingPoleMelee.canceledFromSprinting = true;
            primaryFishingPoleMelee.cancelSprintingOnActivation = true;

            Skills.AddPrimarySkills(bodyPrefab, primaryFishingPoleMelee);



           
            
        }

        //private void CreateShantySpawnCard()
        //{
        //    CharacterSpawnCard card = ScriptableObject.CreateInstance<CharacterSpawnCard>();
        //    card.name = "cscFishermanShanty";
        //    card.prefab = FishermanAssets.movingPlatformBodyPrefab;
        //    card.sendOverNetwork = true;
        //    card.hullSize = HullClassification.Human;
        //    card.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Air;
        //    card.requiredFlags = RoR2.Navigation.NodeFlags.None;
        //    card.forbiddenFlags = RoR2.Navigation.NodeFlags.NoCharacterSpawn;
        //    card.eliteRules = SpawnCard.EliteRules.Default;
        //    card.occupyPosition = false;

        //    GhoulMinion.ghoulSpawnCard = card;
        //}

        private void AddSecondarySkills()
        {
            
            
            //here is a basic skill def with all fields accounted for
            FishermanSurvivor.secondaryFireFishHook = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "CastFishHook",
                skillNameToken = FISHERMAN_PREFIX + "SECONDARY_GUN_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SECONDARY_GUN_DESCRIPTION",
                keywordTokens = new string[] { FISHERMAN_PREFIX + "KEYWORD_NONLETHAL", FISHERMAN_PREFIX + "KEYWORD_TREASURE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("Hook Icon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.AimHook)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 4f, // change
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = false,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = true,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,
                

            });

            FishermanSurvivor.secondaryRecallFishHook = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RecallFishHook",
                skillNameToken = FISHERMAN_PREFIX + "SECONDARY_GUN_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SECONDARY_GUN_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE"  },
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.RecallHook)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 0.5f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = true,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            InitValidInteractableGrabs();
            Skills.AddSecondarySkills(bodyPrefab, secondaryFireFishHook);
        }

        private void AddUtiitySkills()
        {
            //here's a skilldef of a typical movement skill. some fields are omitted and will just have default values
            //FishermanSurvivor.utilitySummonPlatform = Skills.CreateSkillDef(new SkillDefInfo
            //{
            //    skillName = "HenryRoll",
            //    skillNameToken = FISHERMAN_PREFIX + "UTILITY_PLATFORM_NAME",
            //    skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_PLATFORM_DESCRIPTION",
            //    skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

            //    activationState = new EntityStates.SerializableEntityStateType(typeof(Roll)),
            //    activationStateMachineName = "Weapon2",
            //    interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

            //    baseMaxStock = 1,
            //    baseRechargeInterval = 4f,

            //    isCombatSkill = false,
            //    mustKeyPress = false,
            //    forceSprintDuringState = true,
            //    cancelSprintingOnActivation = false,
            //});

            FishermanSurvivor.utilitySummonPlatform = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "F135 Mobile Shanty Platfrom",
                skillNameToken = FISHERMAN_PREFIX + "UTILITY_PLATFORM_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_PLATFORM_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("Shanty Icon"),
                keywordTokens = new string[] { FISHERMAN_PREFIX + "KEYWORD_LINKED", FISHERMAN_PREFIX + "KEYWORD_DIRECT" },

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.SummonPlatform)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 12f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = false,
                dontAllowPastMaxStocks = true,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = false,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,


            });
            FishermanSurvivor.utilityDirectPlatform = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "Command Boat",
                skillNameToken = FISHERMAN_PREFIX + "UTILITY_DIRECT_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_DIRECT_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.DirectPlatform)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 0.5f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = true,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,


            });

            var whaleMissle = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "Strange Freind",
                skillNameToken = FISHERMAN_PREFIX + "UTILITY_WHALE_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_WHALE_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("Shanty Icon"),
                keywordTokens = new string[] { FISHERMAN_PREFIX + "KEYWORD_UNFINISHED", FISHERMAN_PREFIX + "KEYWORD_SMACK" },

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.SummonWhale)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 12f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = false,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,


            });
            
            Skills.AddUtilitySkills(bodyPrefab, utilitySummonPlatform);
            //Skills.AddUtilitySkills(bodyPrefab, whaleMissle);


        }

        private void AddSpecialSkills()
        {
            //a basic skill
            specialThrowHookBomb = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryBomb",
                skillNameToken = FISHERMAN_PREFIX + "SPECIAL_BOMB_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SPECIAL_BOMB_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("Jelly Bomb Icon"),
                keywordTokens = new string[] { FISHERMAN_PREFIX + "KEYWORD_TETHER" },
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.ThrowBomb)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "Weapon2", interruptPriority = EntityStates.InterruptPriority.Skill,


                baseRechargeInterval = 10, // change
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = false,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = true,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,
            });
            specialRecallHookBomb = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "RecallFishHook",
                skillNameToken = FISHERMAN_PREFIX + "SECONDARY_GUN_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SECONDARY_GUN_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.RecallHookBomb)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 0.5f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = true,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });


            specialDrinkFlask = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "SteadyTheNerves",
                skillNameToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_STUNNING", FISHERMAN_PREFIX + "KEYWORD_DAUNTLESS" },
                skillIcon = assetBundle.LoadAsset<Sprite>("Jelly Bomb Icon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.NervesDrinkState)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "NervesThrow",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = 10f,

                isCombatSkill = false,
                mustKeyPress = true,
            });
            specialThrowFlask = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DrownTheSorrows",
                skillNameToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.NervesThrowState)),
                activationStateMachineName = "NervesThrow",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseRechargeInterval = 0.5f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = true,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });



            Skills.AddSpecialSkills(bodyPrefab, specialThrowHookBomb, specialDrinkFlask);



        }

        #endregion
        #region skins
        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();
            ChildLocator childLocator = prefabCharacterModel.GetComponent<ChildLocator>();

            CharacterModel.RendererInfo[] defaultRendererinfos = prefabCharacterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            //this creates a SkinDef with all default fields
            SkinDef defaultSkin = Skins.CreateSkinDef("DEFAULT_SKIN",
                assetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);

            //these are your Mesh Replacements. The order here is based on your CustomRendererInfos from earlier
                //pass in meshes as they are named in your assetbundle
            //currently not needed as with only 1 skin they will simply take the default meshes
                //uncomment this when you have another skin
            //defaultSkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshHenrySword",
            //    "meshHenryGun",
            //    "meshHenry");

            //add new skindef to our list of skindefs. this is what we'll be passing to the SkinController
            skins.Add(defaultSkin);
            #endregion

            //uncomment this when you have a mastery skin
            #region MasterySkin
            
            ////creating a new skindef as we did before
            //SkinDef masterySkin = Modules.Skins.CreateSkinDef(HENRY_PREFIX + "MASTERY_SKIN_NAME",
            //    assetBundle.LoadAsset<Sprite>("texMasteryAchievement"),
            //    defaultRendererinfos,
            //    prefabCharacterModel.gameObject,
            //    HenryUnlockables.masterySkinUnlockableDef);

            ////adding the mesh replacements as above. 
            ////if you don't want to replace the mesh (for example, you only want to replace the material), pass in null so the order is preserved
            //masterySkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshHenrySwordAlt",
            //    null,//no gun mesh replacement. use same gun mesh
            //    "meshHenryAlt");

            ////masterySkin has a new set of RendererInfos (based on default rendererinfos)
            ////you can simply access the RendererInfos' materials and set them to the new materials for your skin.
            //masterySkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");
            //masterySkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");
            //masterySkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("matHenryAlt");

            ////here's a barebones example of using gameobjectactivations that could probably be streamlined or rewritten entirely, truthfully, but it works
            //masterySkin.gameObjectActivations = new SkinDef.GameObjectActivation[]
            //{
            //    new SkinDef.GameObjectActivation
            //    {
            //        gameObject = childLocator.FindChildGameObject("GunModel"),
            //        shouldActivate = false,
            //    }
            //};
            ////simply find an object on your child locator you want to activate/deactivate and set if you want to activate/deacitvate it with this skin

            //skins.Add(masterySkin);
            
            #endregion

            skinController.skins = skins.ToArray();
        }
        #endregion skins

        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            //Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            FishermanAI.Init(bodyPrefab, masterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            On.RoR2.GenericSkill.SetBonusStockFromBody += GenericSkill_SetBonusStockFromBody;
            //On.RoR2.CharacterBody.addbuff

        }

        private void GenericSkill_SetBonusStockFromBody(On.RoR2.GenericSkill.orig_SetBonusStockFromBody orig, GenericSkill self, int newBonusStockFromBody)
        {
            orig(self, newBonusStockFromBody);
            if(self.skillDef == FishermanSurvivor.utilityDirectPlatform || self.skillDef == FishermanSurvivor.utilitySummonPlatform)
            {
                var objTracker = self.characterBody.GetComponent<SkillObjectTracker>();
                objTracker?.ModifyPlayformStock(newBonusStockFromBody);
            }

        }

        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            //if (self && self.GetBody() && self.GetBody().isPlayerControlled) // this causes bugs with other characters. fix or rework later.
            //{
            //    MinionOwnership.MinionGroup minionGroup = MinionOwnership.MinionGroup.FindGroup(body.master.netId);
            //    if (minionGroup != null)
            //    {
            //        var members = minionGroup.members;
            //        foreach (var member in members)
            //        {
            //            CharacterMaster master = member?.GetComponent<CharacterMaster>();
            //            if (master.name.Contains("ShantyMaster") && body.skillLocator.utility != FishermanSurvivor.utilityDirectPlatform)
            //            {
            //                body.skillLocator.utility.SetSkillOverride(this, FishermanSurvivor.utilityDirectPlatform, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            //                body.skillLocator.utility.DeductStock(1); // may change this to deduct all stocks if all hooks are fired at once.
            //                break;
            //            }
            //        }
            //    }
            //}
        }
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self && self.body)
            {
                //ripped right from spacetime skein in tinkers satchel.
                if (self.body.HasBuff(FishermanBuffs.steadyNervesBuff))
                {
                    int buffstacks = self.body.GetBuffCount(FishermanBuffs.steadyNervesBuff);
                    //resist knockback
                    var forceMultiplier = Mathf.Max(0, 100 - buffstacks * 20);
                    if (damageInfo.canRejectForce)
                        damageInfo.force *= forceMultiplier * 0.01f;

                    //reduce incoming damage
                    damageInfo.damage = Mathf.Max(1, damageInfo.damage - buffstacks);

                    //change freeze to slow
                    if ((damageInfo.damageType & DamageType.Freeze2s) != DamageType.Generic)
                    {
                        damageInfo.damageType = damageInfo.damageType | DamageType.SlowOnHit;
                        damageInfo.damageType = damageInfo.damageType & ~DamageType.Freeze2s;
                    }
                }
            }
            orig(self, damageInfo);
        }


        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
            
            if (sender.HasBuff(FishermanBuffs.armorBuff)) // your buff
            {
                args.moveSpeedReductionMultAdd -= args.moveSpeedReductionMultAdd;
            }

            if (sender.HasBuff(FishermanBuffs.hookTetherDebuff))
            {
                args.moveSpeedReductionMultAdd += 0.3f;
            }
            if (sender.HasBuff(FishermanBuffs.steadyNervesBuff))
            {

                int buffcount = sender.GetBuffCount(FishermanBuffs.steadyNervesBuff);
                for (int i = 0; i < buffcount; i++)
                {
                    args.damageMultAdd += FishermanStaticValues.bottleDamageBuff;
                    args.armorAdd += 2;
                    args.regenMultAdd += .1f;
                    if (args.attackSpeedReductionMultAdd > -0.2f)
                        args.attackSpeedReductionMultAdd -= (Mathf.Max(0,args.attackSpeedReductionMultAdd) * buffcount * 0.2f) + 0.1f;
                    if (args.moveSpeedReductionMultAdd > -0.2f)
                    {
                        args.moveSpeedReductionMultAdd -= (Mathf.Max(0, args.moveSpeedReductionMultAdd) * buffcount * 0.2f) + 0.1f;
                        //sender.acceleration = Mathf.Max(sender.baseAcceleration / 2wf, sender.baseAcceleration / (Mathf.Max(0, args.moveSpeedReductionMultAdd) * buffcount * 0.2f) + 1f);
                    }
                    if (sender.cursePenalty > 1f && args.baseCurseAdd > -.8f)
                        args.baseCurseAdd -= .1f;
                    
                }
            }
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            

            int buffcount = self.GetBuffCount(FishermanBuffs.steadyNervesBuff);
            //float temp = self.baseAcceleration * Mathf.Max(self.baseAcceleration * 0.33f, 1f / (buffcount * 0.2f + 1f));
            //temp = self.baseAcceleration - temp;
            //self.acceleration -= self.acceleration > self.baseAcceleration * .3f ? temp : 0;
            self.acceleration -= self.acceleration > 10 ? self.baseAcceleration * Mathf.Min(25, buffcount) * 0.05f : 0;


        }


        public static int ApplyFishermanPassiveFishHookEffect(GameObject attacker, GameObject inflictor, float hookFailDamage, Vector3 targetPos, HurtBox enemyHurtBox)
        {
            //TODO Re-work hook Arc
            //TODO Prevent super mega multihit from behemoth.
            if (!attacker || !inflictor || !enemyHurtBox) return -1;
            #region calculuateThrowingArc 
            float maxMass = FishermanStaticValues.hookMaxMass;
            CharacterBody body = enemyHurtBox.healthComponent.body;
            Vector3 enemyPosition = enemyHurtBox.transform.position;
            Rigidbody bodyRB = enemyHurtBox.healthComponent.GetComponent<Rigidbody>();
            float bodyMass = (bodyRB ? bodyRB.mass : maxMass+1); // no rigid body = too heavy to hook 
            bool isHookImmune = body.HasBuff(FishermanBuffs.hookImmunityBuff);

            if (bodyMass < maxMass && isHookImmune) return -1; // stop early if target is unhookable and unbleedable
            //flying vermin seems to be the only flyer in the game that doesnt use a VectorPID to fly.
            bool isFlyer = body.isFlying || (body.characterMotor && (body.characterMotor.isFlying || !body.characterMotor.isGrounded));//body.gameObject.GetComponent<VectorPID>() != null  || body.name == "FlyingVerminBody(Clone)"? true: false;
            
            float dist = Vector3.Distance(enemyPosition, targetPos);

            
            //Vector2 distanceVector2D = new Vector2(distanceVector.x, distanceVector.z);
            //float horizontalDist = distanceVector2D.magnitude;
            //Vector2 horizontalDir = distanceVector2D / horizontalDist;
            //float timeToTarget = 1;
            //float travelRate = horizontalDist / timeToTarget;

            //float ySpeed =  Trajectory.CalculateInitialYSpeed(timeToTarget, distanceVector.y);
            //force = new Vector3(distanceVector.x * travelRate, ySpeed, distanceVector.z * travelRate) * bodyMass;


            //Vector3 halfDistVec = distanceVector * 0.5f;
            //Vector3 centerAdjEPos = halfDistVec + enemyPosition;
            //Vector3 hookTarget = centerAdjEPos;
            //if (!isFlyer)
            //{
            //    hookTarget.y += dist * 0.5f;
            //}
            //Vector3 newDistanceVector = (hookTarget - enemyPosition);
            ////float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 262, 1.1f) - 55, 1, 5); //this one is really good
            //float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 312, 1.1f) - 57.2f, 1, 5);
            //// if (isFlyer) { bonusPower += 0.1f; }
            //force = newDistanceVector * bodyMass * bonusPower;


            Vector3 throwVelocity = FishermanSurvivor.GetHookThrowVelocity(targetPos,enemyPosition, isFlyer);
            //Log.Debug($"[HOOK][Effect] owner {targetPos} Enemy position {enemyPosition}");
            //Log.Debug($"[HOOK][Effect] throwvel {throwVelocity}");

            #endregion 

            DamageInfo damageInfo = new DamageInfo
            {
                attacker = attacker,
                inflictor = inflictor,
                //force = force,
                position = enemyHurtBox.transform.position,
            };

            //Log.Debug($"\nHookInfo: " +
            //    $"\n\tName: {body.name}" +
            //    $"\n\tIsFlyer: {isFlyer}" +
            //    $"\n\ttargetMass: {bodyMass}" +
            //    $"\n\tdist: {dist}" 
            //    //$"\n\tdistanceVector: {distanceVector}" 
            //    //$"\n\ttimetotarget: {timeToTarget}"
            //    //$"\n\tnewDistanceVector: {newDistanceVector}" +
            //    //$"\n\bonusPower: {bonusPower}" +
            //    //$"\n\t>Final Force: {force}"
            //);

            if (bodyMass > maxMass)
            {
                //Log.Info($"Attacker: {attacker.name} Inflictor {inflictor.name}");
                //play hook fail sound effect
                //show hook hook fail decal on enemy
                //damageInfo.force = force * 0.1f;
                throwVelocity *= 0.1f;
                damageInfo.force = throwVelocity;
                damageInfo.procCoefficient = 0;
                damageInfo.procChainMask = new ProcChainMask();
                damageInfo.procChainMask.AddProc(ProcType.Behemoth);
                damageInfo.procChainMask.RemoveProc(ProcType.BleedOnHit);
                damageInfo.damageType = DamageType.BleedOnHit;
                enemyHurtBox.healthComponent.TakeDamageForce(damageInfo); // apply weak pull no damage to prevent double hit
                damageInfo.damage = hookFailDamage;  //add damage for bleed calcution

                enemyHurtBox.healthComponent.TakeDamageForce(damageInfo);

                enemyHurtBox.healthComponent.ApplyDot(attacker, DotController.DotIndex.Bleed, 3, FishermanStaticValues.hookBleedCoefficient);
                //Log.Debug($"Mass too large, hook failed. New force: {damageInfo.force} HookfailDamage: {hookFailDamage}");
                return 0;
            }
            else if (!isHookImmune)
            {
                //play hook success sound effect
                //show hook success decal on enemy
                //Log.Debug("Hook Succeeded");
                body.AddTimedBuff(FishermanBuffs.hookImmunityBuff, 0.3f);
                if (body.characterMotor)
                {
                    if (body.characterMotor.isGrounded) body.characterMotor.Motor.ForceUnground();
                    if (!isFlyer) body.characterMotor.disableAirControlUntilCollision = true;
                    body.characterMotor.velocity = Vector3.zero;
                    body.characterMotor.velocity = throwVelocity;
                }
                else
                {
                    body.rigidbody.AddForce(throwVelocity, ForceMode.VelocityChange);
                }


                return 1;
            }
            return -1;


        }

        static HashSet<String> GrabableInteractablesWhitelist = new HashSet<String>();
        static HashSet<String> GrabableInteractablesBlacklist = new HashSet<String>();
        static void InitValidInteractableGrabs()
        {
            GrabableInteractablesWhitelist.UnionWith(new[] {
                "Turret1Broken",
                "mdlNewtStatue", // this doesnt work

            });
            GrabableInteractablesBlacklist.UnionWith(new[] {
                "MegaDroneBroken",
                //"Chest2",
                "LunarChest",
                "GoldChest",
                //"CategoryChest2Damage",
                //"CategoryChest2Utility",
                //"CategoryChest2Healing",


            });

        }
        public static bool CheckIfInteractableIsGrabable(string name)
        {
            bool isGrabable = false;
            name = name.Replace("(Clone)", "");
            isGrabable = GrabableInteractablesWhitelist.Contains(name) ||
                name.Contains("Drone") ||
                name.Contains("Chest") ||
                name.Contains("Barrel");

            if (isGrabable) {
                isGrabable = !GrabableInteractablesBlacklist.Contains(name);
            }
            return isGrabable;
        }


        public static Vector3 GetHookThrowVelocity(Vector3 targetPos, Vector3 startPos, bool isFlyer)
        {
            Vector3 distanceVector = (targetPos - startPos);
            Vector2 xzDistanceVec = new Vector2(distanceVector.x, distanceVector.z); // 
            float distanceToTarget = xzDistanceVec.magnitude;
            float timeToTarget = Mathf.Min(distanceToTarget * 0.05f, 2);

            Vector2 normailzedDistvec = xzDistanceVec / distanceToTarget;
            float y = isFlyer ? distanceVector.y : Mathf.Max(Trajectory.CalculateInitialYSpeed(timeToTarget, distanceVector.y),6);
            float travelRate = distanceToTarget / timeToTarget;
            Vector3 direction = new Vector3(normailzedDistvec.x * travelRate, y, normailzedDistvec.y * travelRate);
            return direction;
        }
    }
}