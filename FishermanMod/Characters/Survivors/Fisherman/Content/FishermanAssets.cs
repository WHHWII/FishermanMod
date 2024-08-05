using RoR2;
using UnityEngine;
using FishermanMod.Modules;
using System;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using UnityEngine.PlayerLoop;
using FishermanMod.Survivors.Fisherman.Components;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using IL.RoR2.Orbs;
using R2API;
using IL.RoR2.EntityLogic;
using RoR2.CharacterAI;
using RoR2.EntityLogic;
using On.RoR2.Orbs;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.Networking;
using HarmonyLib;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;

        public static GameObject bombExplosionEffect;
        public static GameObject bottleImpactEffect;
        public static GameObject shantyShotGhost;

        // networked hit sounds
        public static NetworkSoundEventDef swordHitSoundEvent;

        //projectiles
        public static GameObject bottleProjectilePrefab;
        public static GameObject hookProjectilePrefab;
        public static GameObject shantyBlueprintPrefab;
        public static GameObject shantyBodyPrefab;
        public static GameObject shantyMasterPrefab;
        public static GameObject shantyCannonShotPrefab;
        public static GameObject hookBombProjectilePrefab;

        //materials
        public static Material chainMat;

        private static AssetBundle _assetBundle;
        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            swordHitSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");

            CreateEffects();

            CreateProjectiles();

            CreateMaterials();

            CreateMinions();


        }

        #region effects
        private static void CreateEffects()
        {
            CreateBombExplosionEffect();
            CreateBottleImpactEffect();

            swordSwingEffect = _assetBundle.LoadEffect("HenrySwordSwingEffect", true);
            swordHitImpactEffect = _assetBundle.LoadEffect("ImpactHenrySlash");
        }

        private static void CreateBombExplosionEffect()
        {
            bombExplosionEffect = _assetBundle.LoadEffect("BombExplosionEffect", "HenryBombExplosion");

            if (!bombExplosionEffect)
                return;

            ShakeEmitter shakeEmitter = bombExplosionEffect.AddComponent<ShakeEmitter>();
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.duration = 0.5f;
            shakeEmitter.radius = 200f;
            shakeEmitter.scaleShakeRadiusWithLocalScale = false;

            shakeEmitter.wave = new Wave
            {
                amplitude = 1f,
                frequency = 40f,
                cycleOffset = 0f
            };

        }

        private static void CreateBottleImpactEffect()
        {
            bottleImpactEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MageIceExplosion.prefab").WaitForCompletion(),"bottleImpact");
            UnityEngine.Object.Destroy(bottleImpactEffect.transform.Find("RuneRings").gameObject);
            UnityEngine.Object.Destroy(bottleImpactEffect.transform.Find("Point Light").gameObject);
            bottleImpactEffect.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            bottleImpactEffect.AddComponent<NetworkIdentity>();
            //TODO thunderkit import
        }
        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateBombProjectile();
            Content.AddProjectilePrefab(bottleProjectilePrefab);
            CreateHookProjectile();
            Content.AddProjectilePrefab(hookProjectilePrefab);
            CreateJellyfishProjectile();
            Content.AddProjectilePrefab(hookBombProjectilePrefab);
            CreateMovePlatformProjectileAttack();
            Content.AddProjectilePrefab(shantyCannonShotPrefab);
        }

        private static void CreateBombProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            bottleProjectilePrefab = Assets.CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(bottleProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion bombImpactExplosion = bottleProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            
            bombImpactExplosion.blastRadius = 1f;
            bombImpactExplosion.blastDamageCoefficient = 1f;
            bombImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.destroyOnWorld = true;
            bombImpactExplosion.impactOnWorld = true;
            bombImpactExplosion.impactEffect = bottleImpactEffect;
            bombImpactExplosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("HenryBombExplosion");
            bombImpactExplosion.applyDot = true;
            var ps = bottleProjectilePrefab.GetComponent<ProjectileSimple>();
            ProjectileDamage projectileDamage = ps.GetComponent<ProjectileDamage>();
            projectileDamage.damageType = DamageType.Stun1s;


            ProjectileController bombController = bottleProjectilePrefab.GetComponent<ProjectileController>();

            if (_assetBundle.LoadAsset<GameObject>("DrinkThrowGhost") != null)
            {
                bombController.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("DrinkThrowGhost");
            }
            
            bombController.startSound = "";
        }

        private static void CreateHookProjectile()
        {
            hookProjectilePrefab = _assetBundle.LoadAndAddProjectilePrefab("FishermanHookProjectile");
            //hookProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiMine.prefab").WaitForCompletion();
            //hookProjectilePrefab = Assets.CloneProjectilePrefab("FMJRamping", "FishermanHookProjectile");
            Rigidbody rb = hookProjectilePrefab.GetComponent<Rigidbody>();
            //rb.useGravity = true;
            //rb.mass = 100;
            //hookProjectilePrefab.layer = 0;
            hookProjectilePrefab.layer = LayerIndex.projectile.intVal;


            ProjectileSimple ps = hookProjectilePrefab.GetComponent<ProjectileSimple>();
            //ps.desiredForwardSpeed = 80;
            //ps.lifetime = 999999;
            
            
            

            ProjectileStickOnImpact stickOnImpact = hookProjectilePrefab.GetComponent<ProjectileStickOnImpact>();
            //stickOnImpact.ignoreCharacters = false;
            //stickOnImpact.alignNormals = false;
          
            //ProjectileSingleTargetImpact pstImpact = hookProjectilePrefab.GetComponent<ProjectileSingleTargetImpact>();
            //if(pstImpact) UnityEngine.Object.Destroy(pstImpact);
            //pstImpact.enabled = false;


            ProjectileController pc = hookProjectilePrefab.GetComponent<ProjectileController>();

            CapsuleCollider collider = hookProjectilePrefab.GetComponent<CapsuleCollider>();
            


            GameObject ItemInteractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ItemInteractor.transform.parent = hookProjectilePrefab.transform;
            UnityEngine.Object.Destroy(ItemInteractor.GetComponent<MeshRenderer>());
            UnityEngine.Object.Destroy(ItemInteractor.GetComponent<MeshFilter>());
            ItemInteractor.GetComponent<SphereCollider>().isTrigger = true;
            ItemInteractor.transform.localPosition = Vector3.zero;
            ItemInteractor.transform.localScale = Vector3.one * 6;
            ItemInteractor.layer = 15;

            ProjectileOverlapAttack piss = hookProjectilePrefab.GetComponent<ProjectileOverlapAttack>();
            //piss.onServerHit.RemoveAllListeners();
            //piss.enabled = false;
            //GameObject hbobj = new GameObject();
            //hbobj.transform.localScale = new Vector3(2, 2, 2);
            //HitBox hb = hbobj.AddComponent<HitBox>();
            //HitBoxGroup hbgroup = hookProjectilePrefab.AddComponent<HitBoxGroup>();
            //hbgroup.hitBoxes.AddItem(hb);
            //piss.damageCoefficient = FishermanStaticValues.castDamageCoefficient;


            //hbobj.transform.parent = hookProjectilePrefab.transform;

            var damageTypeComp = hookProjectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            damageTypeComp.Add(DamageTypes.FishermanHookPassive);
            ProjectileDamage projectileDamage = ps.GetComponent<ProjectileDamage>();
            //projectileDamage.damageType = DamageType.NonLethal;



            //this was used for taunt
            //TeamComponent teamComp = hookProjectilePrefab.AddComponent<TeamComponent>();
            //teamComp.teamIndex = TeamIndex.Player;
            //CharacterBody hookBody = hookProjectilePrefab.AddComponent<CharacterBody>();
            //hookBody.baseRegen = int.MaxValue;
            //hookBody.baseMaxHealth = int.MaxValue;
            //hookBody.baseArmor = int.MinValue;
            //HealthComponent healthComp = hookProjectilePrefab.AddComponent<HealthComponent>();
            //healthComp.health = int.MaxValue;
            //healthComp.dontShowHealthbar = true;



            //GameObject enemyTaunter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //enemyTaunter.transform.parent = hookProjectilePrefab.transform;
            ////UnityEngine.Object.Destroy(enemyTaunter.GetComponent<MeshRenderer>());
            ////UnityEngine.Object.Destroy(enemyTaunter.GetComponent<MeshFilter>());
            //enemyTaunter.GetComponent<SphereCollider>().isTrigger = true;
            //enemyTaunter.transform.localPosition = Vector3.zero;
            //enemyTaunter.transform.localScale = Vector3.one * 30;
            //enemyTaunter.layer = 15;


            FishHookController fishHook = hookProjectilePrefab.AddComponent<FishHookController>();
            fishHook.rb = rb;
            fishHook.stickComponent = stickOnImpact;
            fishHook.controller = pc;
            fishHook.projectileDamage = projectileDamage;
            fishHook.collider = collider;
            fishHook.projOverlap = piss;
            fishHook.projSimple = ps;


           //fishHook.stickComponent.stickEvent.AddListener(() => {
           //    Log.Info("=-----------------HOOK IMPACT EVENT CALLED");
           //    //fishHook.collider.radius = 0.1f; ;
           //    fishHook.rb.velocity = Vector3.zero;
           //    ps.enabled = false;
           //    }
           //);

            //fishHook.enemyTaunter = enemyTaunter;
            //fishHook.hookBody = hookBody;

            //fishHook.pstImpact = pstImpact;


            //if (_assetBundle.LoadAsset<GameObject>("CastHookGhost") != null)
            //{
            //    pc.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("CastHookGhost");
            //}
            //GameObject ghostPrefab = pc.ghostPrefab;
            //ProjectileGhostController gpc = ghostPrefab.GetComponent<ProjectileGhostController>();
            //UnityEngine.Object.Destroy(ghostPrefab.GetComponentInChildren<ObjectScaleCurve>()); // this removes the visual disapearing
            ////want to change this to custom line renderer effect later
            //AnimateShaderAlpha trailFadeEffect = gpc.GetComponentInChildren<AnimateShaderAlpha>();
            //trailFadeEffect.timeMax = 20;
            //TrailRenderer trailEffect = gpc.GetComponentInChildren<TrailRenderer>();
            //trailEffect.startWidth = 0.2f;
            //trailEffect.endWidth = 0.01f;

        }

       
        private static void CreateMovePlatformProjectileAttack()
        {
           
            shantyCannonShotPrefab = Assets.CloneProjectilePrefab("MageFireboltBasic", "FishermanShantyCannonShot");
            //UnityEngine.Object.Destroy(shantyCannonShotPrefab.GetComponent<ProjectileOverlapAttack>());
            var projectileController = shantyCannonShotPrefab.GetComponent<ProjectileController>();
            projectileController.ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/FMJRampingGhost.prefab").WaitForCompletion();
            var impactExplosion = shantyCannonShotPrefab.GetComponent<ProjectileImpactExplosion>();
            impactExplosion.blastRadius = 14;
            impactExplosion.blastDamageCoefficient = 1f;
            impactExplosion.blastProcCoefficient = 1f;
            impactExplosion.bonusBlastForce = new Vector3(0, 10, 0);
            impactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            impactExplosion.lifetime = 20f;
            impactExplosion.impactEffect = bombExplosionEffect;
            impactExplosion.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
            impactExplosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("HenryBombExplosion");
            impactExplosion.timerAfterImpact = false;
            impactExplosion.applyDot = true;

            Rigidbody rb = shantyCannonShotPrefab.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            var projectileSimple = shantyCannonShotPrefab.GetComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 500;

            var projectileDamage = shantyCannonShotPrefab.GetComponent<ProjectileDamage>();
            projectileDamage.damage = FishermanStaticValues.shantyCannonDamage;
            projectileDamage.damageType &= ~DamageType.IgniteOnHit;

            var damageTypeComp = shantyCannonShotPrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            damageTypeComp.Add(DamageTypes.FishermanKnockup);


            var effectTrail = projectileController.ghostPrefab.GetComponentInChildren<TrailRenderer>();
            effectTrail.time = 3f;

            var flameParticles = projectileController.ghostPrefab.transform.Find("Flames").GetComponent<ParticleSystem>();
            var flameParticle_main = flameParticles.main;
            flameParticle_main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1);



        }
        private static void CreateJellyfishProjectile()
        {
            hookBombProjectilePrefab = Assets.CloneProjectilePrefab("LoaderPylon", "FishermanJellyfish");

            //UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<AntiGravityForce>());
            //UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<BeginRapidlyActivatingAndDeactivating>());
            //var awakeComponent = hookBombProjectilePrefab.GetComponent<AwakeEvent>();
            //var functionComponent = hookBombProjectilePrefab.GetComponent<EventFunctions>();
           

            var antiGrav = hookBombProjectilePrefab.GetComponent<AntiGravityForce>();
            antiGrav.antiGravityCoefficient = 0.3f;

            var beamController = hookBombProjectilePrefab.GetComponent<ProjectileProximityBeamController>();
            beamController.damageCoefficient = 0.01f;
            beamController.previousTargets = new System.Collections.Generic.List<HealthComponent>();
            beamController.procCoefficient = 1;
            beamController.listClearTimer = 99999;
            beamController.listClearInterval = 99999;
            beamController.attackInterval = 0.1f;
            beamController.attackFireCount = 1;

            var damageTypeComp = hookBombProjectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            damageTypeComp.Add(DamageTypes.FishermanTether);

            var startEvent = hookBombProjectilePrefab.GetComponent<RoR2.EntityLogic.DelayedEvent>();
            startEvent.CallDelayed(0.5f);

            var bomb = hookBombProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            bomb.blastRadius = 25;
            bomb.blastDamageCoefficient = 1;
            bomb.blastProcCoefficient = 1;
            bomb.lifetime = 99999;
            bomb.destroyOnEnemy = false;
            bomb.impactOnWorld = false;
            bomb.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
            bomb.transformSpace = ProjectileImpactExplosion.TransformSpace.World;

            var stick = hookBombProjectilePrefab.AddComponent<ProjectileStickOnImpact>();
            

            var controller = hookBombProjectilePrefab.GetComponent<ProjectileController>();
            var simpleProj = hookBombProjectilePrefab.GetComponent<ProjectileSimple>();
            simpleProj.lifetime = 99999;
            var pDamageComp = hookBombProjectilePrefab.GetComponent<ProjectileDamage>();


            var hookBomb = hookBombProjectilePrefab.AddComponent<HookBombController>();
            hookBomb.beamController = beamController;
            hookBomb.controller = controller;
            hookBomb.damageComponent = pDamageComp;
            hookBomb.explosionComponent = bomb;
            hookBomb.stickComponent = stick;
            hookBomb.antiGrav = hookBombProjectilePrefab.GetComponent<AntiGravityForce>();
            //awakeComponent.action.AddListener(hookBomb.ClearHooks);
            
            


        }
        #endregion projectiles

        #region Materials
        private static void CreateMaterials()
        {
            chainMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/Gravekeeper/matGravekeeperHookChain.mat").WaitForCompletion();
        }
        #endregion

        #region Minions
        private static void CreateMinions()
        {
            CreateMovingPlatform();
            Content.AddCharacterBodyPrefab(shantyBodyPrefab);
            Content.AddMasterPrefab(shantyMasterPrefab);
        }
        private static void CreateMovingPlatform()
        {
            //blueprint
            shantyBlueprintPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiWalkerTurretBlueprints.prefab").WaitForCompletion(), "ShantyBlueprint");
            shantyBlueprintPrefab.AddComponent<NetworkIdentity>();


            //body
            shantyBodyPrefab = _assetBundle.LoadAsset<GameObject>("ShantyPlatformBody") ;//PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/EmergencyDroneBody.prefab").WaitForCompletion(), "ShantyBody");
            shantyBodyPrefab.gameObject.layer = LayerIndex.playerBody.intVal;
           // LayerIndex.entityPrecise
            //var hc = shantyBodyPrefab.GetComponent<HealthComponent>();
            //var deplyoable = shantyBodyPrefab.GetComponent<Deployable>(); //have to add for non default deployables. dont forget to change if you change your base
            //deplyoable.onUndeploy.AddListener(() => hc.Suicide()); //// todo: do this not this way??

            //emergency drone base version
            //shantyBodyPrefab.transform.localScale *= 4;
            shantyBodyPrefab.AddComponent<MovingPlatformController>();

            //master
            shantyMasterPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/MegaDroneMaster.prefab").WaitForCompletion(), "ShantyMaster");//_assetBundle.LoadAsset<GameObject>("ShantyPlatformMaster");//
            shantyMasterPrefab.GetComponent<CharacterMaster>().bodyPrefab = shantyBodyPrefab;



            //InitializeMinionSkins();
            #region ShantyMinionSkills
            FishermanSurvivor.primaryShantyCannon = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "FireShantyCannon",
                skillNameToken = "UTILITY_PLATFORM_NAME",
                skillDescriptionToken = "UTILITY_PLATFORM_DESCRIPTION",
                //keywordTokens = new string[] { "KEYWORD_AGILE" },
                //skillIcon = assetBundle.LoadAsset<Sprite>("Shanty Icon"),
                skillIcon = _assetBundle.LoadAsset<Sprite>("breaking bad but good"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.ShantyCannon)),
                activationStateMachineName = "Weapon",
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
            RoR2.Skills.SkillFamily.Variant shantyShotVariant = new RoR2.Skills.SkillFamily.Variant();
            shantyShotVariant.skillDef = FishermanSurvivor.primaryShantyCannon;
            shantyBodyPrefab.GetComponent<GenericSkill>().skillFamily.variants[0] = shantyShotVariant; //FishermanSurvivor.primaryShantyCannon;
            
            var shantyAI = FishermanAssets.shantyMasterPrefab.GetComponent<RoR2.CharacterAI.BaseAI>();

            AISkillDriver fireCannon = shantyMasterPrefab.AddComponent<AISkillDriver>();
            fireCannon.customName = "fireCannon";
            fireCannon.skillSlot = SkillSlot.Primary;
            fireCannon.requireSkillReady = true;
            fireCannon.minUserHealthFraction = float.NegativeInfinity;
            fireCannon.maxUserHealthFraction = float.PositiveInfinity;
            fireCannon.minTargetHealthFraction = float.NegativeInfinity;
            fireCannon.maxTargetHealthFraction = float.PositiveInfinity;
            fireCannon.minDistance = 0f;
            fireCannon.maxDistance = 200f;
            fireCannon.activationRequiresAimConfirmation = true;
            fireCannon.activationRequiresTargetLoS = true;
            fireCannon.selectionRequiresTargetLoS = true;
            fireCannon.maxTimesSelected = -1;
            fireCannon.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            fireCannon.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            fireCannon.aimType = AISkillDriver.AimType.AtMoveTarget;
            fireCannon.moveInputScale = 1f;
            fireCannon.ignoreNodeGraph = true;
            fireCannon.buttonPressType = AISkillDriver.ButtonPressType.TapContinuous;
            #endregion ShantyMinionSkills
            PrefabAPI.RegisterNetworkPrefab(shantyBodyPrefab);
        }
        #endregion
        static void InitializeMinionSkins()
        {
            GameObject model = shantyBodyPrefab.GetComponentInChildren<ModelLocator>().modelTransform.gameObject;
            CharacterModel characterModel = model.GetComponent<CharacterModel>();

            ModelSkinController skinController = model.GetComponent<ModelSkinController>();
            //ChildLocator childLocator = model.GetComponent<ChildLocator>();

           // SkinnedMeshRenderer mainRenderer = characterModel.mainSkinnedMeshRenderer;

            CharacterModel.RendererInfo[] defaultRenderers = characterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            SkinDef defaultSkin = Modules.Skins.CreateSkinDef(FishermanPlugin.DEVELOPER_PREFIX + "_FISHERMAN_BODY_DEFAULT_SKIN_NAME",
                _assetBundle.LoadAsset<Sprite>("breaking bad but good"),
                defaultRenderers,
                model);

            skins.Add(defaultSkin);
            #endregion

            skinController.skins = skins.ToArray();
        }
    }




}
