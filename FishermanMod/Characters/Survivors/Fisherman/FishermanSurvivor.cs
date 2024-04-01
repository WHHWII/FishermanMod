using BepInEx.Configuration;
using EntityStates;
using FishermanMod.Modules;
using FishermanMod.Modules.Characters;
using FishermanMod.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman.SkillStates;
using HG;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using static UnityEngine.GridBrushBase;

namespace FishermanMod.Survivors.Fisherman
{
    public class FishermanSurvivor : SurvivorBase<FishermanSurvivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string assetBundleName => "fishermanassetbundle"; //if you do not change this, you are giving permission to deprecate the mod
        public override string assetBundleName2 => "fishermanextras assetbundle";

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string bodyName => "FishermanBody"; //if you do not change this, you get the point by now

        //name of the ai master for vengeance and goobo. must be unique
        public override string masterName => "FishermanMaster"; //if you do not

        //the names of the prefabs you set up in unity that we will use to build your character
        public override string modelPrefabName => "mdlHenry";
        public override string displayPrefabName => "HenryDisplay";

        public const string FISHERMAN_PREFIX = FishermanPlugin.DEVELOPER_PREFIX + "_FISHERMAN_";
        
        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => FISHERMAN_PREFIX;

        public static SkillDef secondaryRecallFishHook;
        public static SkillDef secondaryFireFishHook;
        public static SkillDef specialRecallHookBomb;
        public static SkillDef specialThrowHookBomb;
        public static SkillDef specialDrinkFlask;
        public static SkillDef specialThrowFlask;

        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = FISHERMAN_PREFIX + "NAME",
            subtitleNameToken = FISHERMAN_PREFIX + "SUBTITLE",

            characterPortrait = assetBundle.LoadAsset<Texture>("texHenryIcon"),
            bodyColor = new Color32(198, 184, 2, 255),
            sortPosition = 100,

            crosshair = Assets.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 110f,
            healthRegen = 1.5f,
            armor = 0f,
          
            jumpCount = 1,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
                new CustomRendererInfo
                {
                    childName = "SwordModel",
                    material = assetBundle.LoadMaterial("matHenry"),
                },
                new CustomRendererInfo
                {
                    childName = "GunModel",
                },
                new CustomRendererInfo
                {
                    childName = "Model",
                }
        };

        public override UnlockableDef characterUnlockableDef => FishermanUnlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new FishermanItemDisplays();

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }
        public override AssetBundle assetBundleExtras { get; protected set; }

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
            //bodyPrefab.AddComponent<HuntressTrackerComopnent>();
            //anything else here
        }

        public void AddHitboxes()
        {
            ChildLocator childLocator = characterModelObject.GetComponent<ChildLocator>();

            //example of how to create a hitbox
            Transform swipeHitBoxTransform = childLocator.FindChild("SwipeHitbox");
            Transform stabHitBoxTransform = childLocator.FindChild("StabHitbox");
            
            Prefabs.SetupHitBoxGroup(characterModelObject, "SwipeGroup", swipeHitBoxTransform);
            Prefabs.SetupHitBoxGroup(characterModelObject, "StabGroup", stabHitBoxTransform);
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
            skillLocator.passiveSkill.icon = Assets.loadedBundles["fishermanassetbundle"].LoadAsset<Sprite>("Hook Icon");
        }
        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            //the primary skill is created using a constructor for a typical primary
            //it is also a SteppedSkillDef. Custom Skilldefs are very useful for custom behaviors related to casting a skill. see ror2's different skilldefs for reference
            SteppedSkillDef primarySkillDef1 = Skills.CreateSkillDef<SteppedSkillDef>(new SkillDefInfo
                (
                    "Debone",
                    FISHERMAN_PREFIX + "PRIMARY_SLASH_NAME",
                    FISHERMAN_PREFIX + "PRIMARY_SLASH_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("Melee Attack Icon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.SlashCombo)),
                    "Weapon",
                    true
                ));
            //custom Skilldefs can have additional fields that you can set manually
            primarySkillDef1.stepCount = 3;
            primarySkillDef1.stepGraceDuration = 0.5f;

            Skills.AddPrimarySkills(bodyPrefab, primarySkillDef1);
        }

        private void AddSecondarySkills()
        {
            
            
            //here is a basic skill def with all fields accounted for
            FishermanSurvivor.secondaryFireFishHook = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "CastFishHook",
                skillNameToken = FISHERMAN_PREFIX + "SECONDARY_GUN_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SECONDARY_GUN_DESCRIPTION",
                //keywordTokens = new string[] { "KEYWORD_AGILE" },
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
                fullRestockOnAssign = true,
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
                keywordTokens = new string[] { "KEYWORD_AGILE" },
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
            SkillDef utilitySkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "HenryRoll",
                skillNameToken = FISHERMAN_PREFIX + "UTILITY_ROLL_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_ROLL_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(Roll)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseMaxStock = 1,
                baseRechargeInterval = 4f,

                isCombatSkill = false,
                mustKeyPress = false,
                forceSprintDuringState = true,
                cancelSprintingOnActivation = false,
            });

            SkillDef utilitySummonPlatform = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "F135 Mobile Shanty Platfrom",
                skillNameToken = FISHERMAN_PREFIX + "UTILITY_ROLL_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "UTILITY_ROLL_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("Shanty Icon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.SummonPlatform)),
                activationStateMachineName = "Body",
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
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,


            });
            

            Skills.AddUtilitySkills(bodyPrefab, utilitySummonPlatform);
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

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.ThrowBomb)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "Weapon2", interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = 10f,

                isCombatSkill = true,
                mustKeyPress = true,
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
                keywordTokens = new string[] { "KEYWORD_STUNNING" },
                skillIcon = assetBundle.LoadAsset<Sprite>("Jelly Bomb Icon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.NervesDrinkState)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = 3f,

                isCombatSkill = true,
                mustKeyPress = true,
            });
            specialThrowFlask = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "DrownTheSorrows",
                skillNameToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_NAME",
                skillDescriptionToken = FISHERMAN_PREFIX + "SPECIAL_DRINK_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.NervesThrowState)),
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



            Skills.AddSpecialSkills(bodyPrefab, specialThrowHookBomb, specialDrinkFlask);



        }
        #endregion skills
        
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
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self && self.body)
            {
                //ripped right from spacetime skein in tinkers satchel.
                if (self.body.HasBuff(FishermanBuffs.SteadyNervesBuff))
                {
                    int buffstacks = self.body.GetBuffCount(FishermanBuffs.SteadyNervesBuff);
                    //resist knockback
                    var forceMultiplier = Mathf.Max(0, 100 - buffstacks * 20);
                    if (damageInfo.canRejectForce)
                        damageInfo.force *= forceMultiplier * 0.01f;

                    //reduce incoming damage
                    damageInfo.damage = Mathf.Max(1, damageInfo.damage - buffstacks);

                    //change freeze to slow
                    if (damageInfo.damageType == DamageType.Freeze2s)
                    {
                        damageInfo.damageType = DamageType.SlowOnHit;
                    }
                }
            }
            orig(self, damageInfo);
        }


        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.HasBuff(FishermanBuffs.armorBuff))
            {
                args.armorAdd += 300;
            }

            if (sender.HasBuff(FishermanBuffs.hookTetherDebuff))
            {
                args.moveSpeedReductionMultAdd += 0.3f;
            }
            if (sender.HasBuff(FishermanBuffs.SteadyNervesBuff))
            {
                int buffcount = sender.GetBuffCount(FishermanBuffs.SteadyNervesBuff);
                for (int i = 0; i < buffcount; i++)
                {
                    args.damageMultAdd += FishermanStaticValues.bottleDamageBuff;
                    args.armorAdd += 2;
                    args.regenMultAdd += .1f;
                    if (args.attackSpeedReductionMultAdd > -0.2f)
                        args.attackSpeedReductionMultAdd -= (Mathf.Max(0,args.attackSpeedReductionMultAdd) * buffcount * 0.2f) + 0.1f;
                    if (args.moveSpeedReductionMultAdd > -0.2f)
                        args.moveSpeedReductionMultAdd -= (Mathf.Max(0, args.moveSpeedReductionMultAdd) * buffcount * 0.2f) + 0.1f;
                }
            }
        }


        public static void ApplyFishermanPassiveFishHookEffect(GameObject attacker, GameObject inflictor, float hookFailDamage, Vector3 targetPos, HurtBox enemyHurtBox)
        {
            float maxMass = 700;
            CharacterBody body = enemyHurtBox.healthComponent.body;
            Vector3 enemyPosition = enemyHurtBox.transform.position;
            Rigidbody bodyRB = enemyHurtBox.healthComponent.GetComponent<Rigidbody>();
            float bodyMass = (bodyRB ? bodyRB.mass : maxMass+1); // no rigid body = too heavy to hook 
            bool isHookImmune = body.HasBuff(FishermanBuffs.hookImmunityBuff);

            if (bodyMass < maxMass && isHookImmune) return; // stop early if target is unhookable and unbleedable
            Vector3 force;
            //flying vermin seems to be the only flyer in the game that doesnt use a VectorPID to fly.
            bool isFlyer = body.gameObject.GetComponent<VectorPID>() != null  || body.name == "FlyingVerminBody(Clone)"? true: false;
            
            float dist = Vector3.Distance(enemyPosition, targetPos);

            Vector3 distanceVector = (targetPos - enemyPosition);
            Vector3 halfDistVec = distanceVector * 0.5f;
            Vector3 centerAdjEPos = halfDistVec + enemyPosition;
            Vector3 hookTarget = centerAdjEPos;
            if (!isFlyer)
            {
                hookTarget.y += dist * 0.5f;
            }
            Vector3 newDistanceVector = (hookTarget - enemyPosition);
            //float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 262, 1.1f) - 55, 1, 5); //this one is really good
            float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 312, 1.1f) - 57.2f, 1, 5);
            // if (isFlyer) { bonusPower += 0.1f; }
            force = newDistanceVector * bodyMass * bonusPower;



            //Log.Debug($"\nHookInfo: " +
            //    $"\n\tName: {body.name}" +
            //    $"\n\tIsFlyer: {isFlyer}" +
            //    $"\n\ttargetMass: {bodyMass}" +
            //    $"\n\tdist: {dist}" +
            //    $"\n\tdistanceVector: {distanceVector}" +
            //    $"\n\tnewDistanceVector: {newDistanceVector}" +
            //    $"\n\bonusPower: {bonusPower}" +
            //    $"\n\t>Final Force: {force}");




            DamageInfo damageInfo = new DamageInfo
            {
                attacker = attacker,
                inflictor = inflictor,
                force = force,
                position = enemyHurtBox.transform.position,
            };



            if(bodyMass > maxMass)
            {
                //play hook fail sound effect
                //show hook hook fail decal on enemy
                damageInfo.force = force * 0.1f;
                damageInfo.procCoefficient = 1;
                damageInfo.procChainMask = default(ProcChainMask);
                damageInfo.damageType = DamageType.BleedOnHit;
                enemyHurtBox.healthComponent.TakeDamageForce(damageInfo); // apply weak pull no damage to prevent double hit
                damageInfo.damage = hookFailDamage;  //add damage for bleed calcution
                GlobalEventManager.instance.OnHitEnemy(damageInfo, body.gameObject);
                GlobalEventManager.instance.OnHitAll(damageInfo, body.gameObject);
                Log.Debug($"Mass too large, hook failed. New force: { damageInfo.force}"); 

            }
            else if(!isHookImmune)
            {
                //play hook success sound effect
                //show hook success decal on enemy
                //Log.Debug("Hook Succeeded");
                body.AddTimedBuff(FishermanBuffs.hookImmunityBuff, 0.3f);
                //enemyHurtBox.healthComponent.TakeDamage(damageInfo);
                enemyHurtBox.healthComponent.TakeDamageForce(damageInfo);
            }
            else
            {
                //Log.Debug("Enemy has hookImmunity, hook failed");
            }
            #region Your Mother



            /*



Vector3 flyPointDir = targetPos;
flyPointDir.x = -distanceVector.y;
flyPointDir.y = distanceVector.x;
flyPointDir = flyPointDir.normalized;
Vector3 flyPoint = -flyPointDir * dist + centerAdjEPos;
Vector3 flyDirection = (flyPoint - enemyPosition).normalized;
force = flyDirection * bodyMass * dist;


Log.Debug(
    $"\n" +
    $"bodyMass......:\t{bodyMass}\n" +
    $"targetPos.....:\t{targetPos}\n" +
    $"enemyPosition.:\t{enemyPosition}\n" +
    $"dist..........:\t{dist}\n" +
    $"halfdist......:\t{halfdist}\n" +
    $"distanceVector:\t{distanceVector}\n" +
    $"halfDistVec...:\t{halfDistVec}\n" +
    $"direction.....:\t{direction}\n" +
    $"flyPointDir...:\t{flyPointDir}\n" +
    $"centerAdjEPos.:\t{centerAdjEPos}\n" +
    $"flyPoint......:\t{flyPoint}\n" +
    $"flyDirection..:\t{flyDirection}\n" +
    $"force.........:\t{force}\n"
//$"rotation......:\t{rotation}\n" +
//$"rotatedVector.:\t{rotatedVector}\n"
);
*/
            /*
            Vector3 diffs = hookTarget - enemyPosition;
            Vector3 y0 = enemyPosition;
            Vector3 y1 = hookTarget;
            Vector3 axis = Vector3.Cross(direction, Vector3.up);

            float angle = Vector3.SignedAngle(direction, Vector3.zero, axis);
            angle += 45;
            Mathf.Clamp(angle, -90, 90);
            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            direction = rotation * direction;
            */

            /*
            hookTarget.y += Mathf.Abs(dist* 1.1f);
            
            Log.Debug($"Distanace vector: {distanceVector}");
            Log.Debug($"Gravity vector: {Physics.gravity}");
            
            float baseForce = targetMass * 1.2f;
            float cDist = dist * targetMass * 0.001f;
            cDist = Mathf.Clamp(cDist, 10, 50);

            


            //force bonus log 
            float logbase = 1.1f;
            float verticalOffset = targetMass * 0.1f;
            float horizontalOffset = 100;
            float forceDistanceBonus = 0;
            string debugForceIntervals = "Force Bonuses at i:\n";
            for (int i = 0; i < dist; i++)
            {
                float possibleBonus = Mathf.Log(-i + horizontalOffset, logbase) + verticalOffset;
                possibleBonus = possibleBonus >= 1 ? possibleBonus : 1;
                forceDistanceBonus += possibleBonus;
                debugForceIntervals += $"{i}: {possibleBonus}\t ";
            }
            Log.Debug(debugForceIntervals);
            Log.Debug($"\nTotalForceBonus: {forceDistanceBonus} ");
            //-

            float distAdjForce = baseForce * cDist + forceDistanceBonus;


            Vector3 force = distAdjForce * direction;
            if(distanceVector.y > 30)
            {
                force.y *= Physics.gravity.y * -0.03f;
            }
            */


            //Log.Debug($"\nHookInfo: " +
            //    $"\n\tName: {body.name}" +
            //    $"\n\ttargetMass: {targetMass}" +
            //    $"\n\tdist: {dist}" +
            //    $"\n\tbaseForce: {baseForce}" +
            //    $"\n\tClampeddist: {cDist}" +
            //    $"\n\tdistAdjForce: {distAdjForce}" +
            //    $"\n\t>Final Force: {force}");
            //Vector3 force = Vector3.oneVector * 1000;
            	#endregion
        }

        //should probably make this use a list
        //also should probably not rely on static members as it may break in MP (apperently this should be fine)
        public static FishHookController deployedHook;
        public static HookBombController deployedHookBomb;
        public static void SetDeployedHook(FishHookController fishHookInstance)
        {
            deployedHook = fishHookInstance;
        }
        public static void SetDeployedHookBomb(HookBombController bombInstance)
        {
            deployedHookBomb = bombInstance;
        }
        static HashSet<String> GrabableInteractablesWhitelist = new HashSet<String>();
        static HashSet<String> GrabableInteractablesBlacklist = new HashSet<String>();
        static void InitValidInteractableGrabs()
        {
            GrabableInteractablesWhitelist.UnionWith(new[] {
                "Turret1Broken",

            });
            GrabableInteractablesBlacklist.UnionWith(new[] {
                "MegaDroneBroken",
                "Chest2",
                "LunarChest",
                "GoldChest",
                "CategoryChest2Damage",
                "CategoryChest2Utility",
                "CategoryChest2Healing",


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
    }
}