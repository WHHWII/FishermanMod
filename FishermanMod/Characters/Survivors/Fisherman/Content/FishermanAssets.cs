﻿using RoR2;
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
using UnityEngine.ParticleSystemJobs;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static RoR2.LocalNavigator;
using Newtonsoft.Json.Utilities;
using static UnityEngine.UIElements.UIR.BestFitAllocator;
using IL.RoR2.UI;
using UnityEngine.UI;
using KinematicCharacterController;

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordStabEffect;
        public static GameObject swordHitImpactEffect;
        public static GameObject uppercutEffect;

        public static GameObject bombExplosionEffect;
        public static GameObject bottleImpactEffect;
        public static GameObject shantyShotGhost;

        public static GameObject objectViewerOverlay;
        public static GameObject hookVisualizer;
        public static GameObject hookIndicator;
        public static GameObject bombIndicator;
        // networked hit sounds
        public static NetworkSoundEventDef swordHitSoundEvent;
        public static NetworkSoundEventDef hookSuccessSoundEvent;
        public static NetworkSoundEventDef hookFailSoundEvent;



        //projectiles
        public static GameObject bottleProjectilePrefab;
        public static GameObject hookProjectilePrefab;
        public static GameObject shantyCannonShotPrefab;
        public static GameObject hookBombProjectilePrefab;
        public static GameObject hookScannerPrefab;
        public static GameObject floatingBombletPrefab;
        public static GameObject floatingBombletPrefab2;

        public static GameObject whaleMisslePrefab; // depricated

        //minion from dispicable me
        public static GameObject shantyBlueprintPrefab;
        public static GameObject shantyBodyPrefab;
        public static GameObject shantyMasterPrefab;

        public static GameObject whaleBlueprintPrefab;
        public static GameObject whaleBodyPrefab;
        public static GameObject whaleMasterPrefab;

        //materials
        public static Material chainMat;
        public static Material hookGreenMat;

        private static AssetBundle _assetBundle;
        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            //sots
            swordHitSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");
            hookSuccessSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");
            hookFailSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");

            CreateEffects();

            CreateMaterials();

            CreateProjectiles();

            CreateMinions();


        }

        #region effects


        private static void CreateEffects()
        {
            CreateBombExplosionEffect();
            CreateBottleImpactEffect();
            CreateHookScannerEffect();
            swordSwingEffect = _assetBundle.LoadEffect("HenrySwordSwingEffect", true);
            swordStabEffect = _assetBundle.LoadEffect("HenryBazookaMuzzleFlash", true);
            //swordStabEffect.GetComponent<ParticleSystemRenderer>().sharedMaterial = Addressables.LoadAssetAsync<UnityEngine.Material>("RoR2/Base/Commando/matCommandoFMJRing.mat").WaitForCompletion();

            uppercutEffect = _assetBundle.LoadEffect("FishermanUppercutEffect", true);

            swordHitImpactEffect = _assetBundle.LoadEffect("ImpactHenrySlash");

            CreateHookTrackerOverlay();
        }

        private static void CreateHookTrackerOverlay()
        {
            // hook visual overlay
            objectViewerOverlay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerScopeLightOverlay.prefab").WaitForCompletion().InstantiateClone("FishermanObjectOverlay", false);
            RoR2.UI.SniperTargetViewer viewer = objectViewerOverlay.GetComponentInChildren<RoR2.UI.SniperTargetViewer>();
            objectViewerOverlay.transform.Find("ScopeOverlay").gameObject.SetActive(false);

            hookVisualizer = viewer.visualizerPrefab.InstantiateClone("FishermanObjectViewer", false);
            hookVisualizer.transform.Find("Scaler/Outer").gameObject.SetActive(false);

            Image headshotImage = hookVisualizer.transform.Find("Scaler/Rectangle").GetComponent<Image>();
            headshotImage.color = Color.green;
            headshotImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Captain/texCaptainCrosshairInner.png").WaitForCompletion();

            var visualizer = viewer.gameObject.AddComponent<FishermanObjectViewer>();
            visualizer.visualizerPrefab = hookVisualizer;
            MonoBehaviour.Destroy(viewer);
            Debug.Log(visualizer);


            //hookIndicator
            hookIndicator = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/PoiPositionIndicator.prefab").WaitForCompletion().InstantiateClone("FishermanHookIndicator", false);
            Transform inframe = hookIndicator.GetComponent<PositionIndicator>().insideViewObject.transform;
            inframe.Rotate(0, 0, 180);
            inframe.localScale = new Vector3(-inframe.localScale.x, inframe.localScale.y, inframe.localScale.z);

            //bombIndicator
            bombIndicator = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/BossPositionIndicator.prefab").WaitForCompletion().InstantiateClone("FishermanHookIndicator", false);
            SpriteRenderer inframeSprite = bombIndicator.GetComponent<PositionIndicator>().insideViewObject.GetComponentInChildren<SpriteRenderer>();
            inframeSprite.color = Color.green;
            SpriteRenderer outframeSprite = bombIndicator.GetComponent<PositionIndicator>().outsideViewObject.GetComponentInChildren<SpriteRenderer>();
            outframeSprite.color = Color.green;
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
            CreateBottleProjectile();
            Content.AddProjectilePrefab(bottleProjectilePrefab);
            CreateHookProjectile();
            Content.AddProjectilePrefab(hookProjectilePrefab);
            CreateJellyfishProjectile();
            Content.AddProjectilePrefab(hookBombProjectilePrefab);
            CreateMovePlatformProjectileAttack();
            Content.AddProjectilePrefab(shantyCannonShotPrefab);
            //CreateHookScanner();
            //Content.AddProjectilePrefab(hookScannerPrefab);
            CreateWhaleMissleProjectile();
            Content.AddProjectilePrefab(whaleMisslePrefab);
            CreateBombletProjectile();
            Content.AddProjectilePrefab(floatingBombletPrefab);
        }

        private static void CreateBottleProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            bottleProjectilePrefab = ModAssetManager.CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(bottleProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion bombImpactExplosion = bottleProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            
            bombImpactExplosion.blastRadius = 4f;
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
            projectileDamage.damageType.AddModdedDamageType(DamageTypes.FishermanGrantSteady);


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
            hookProjectilePrefab.layer = LayerIndex.projectile.intVal;

            Rigidbody rb = hookProjectilePrefab.GetComponent<Rigidbody>();

            ProjectileSimple ps = hookProjectilePrefab.GetComponent<ProjectileSimple>();
            
            ProjectileStickOnImpact stickOnImpact = hookProjectilePrefab.GetComponent<ProjectileStickOnImpact>();

            ProjectileController pc = hookProjectilePrefab.GetComponent<ProjectileController>();

            CapsuleCollider collider = hookProjectilePrefab.GetComponent<CapsuleCollider>();



            ProjectileOverlapAttack piss = hookProjectilePrefab.GetComponent<ProjectileOverlapAttack>();

            ProjectileDamage projectileDamage = ps.GetComponent<ProjectileDamage>();
            DamageTypeCombo hookDmg = new DamageTypeCombo
            {
                damageType = DamageType.NonLethal,
                damageTypeExtended = DamageTypeExtended.Generic,
                damageSource = DamageSource.Secondary,
            };
            hookDmg.AddModdedDamageType(DamageTypes.FishermanHookPassive);
            projectileDamage.damageType = hookDmg;
            //hookDmg.

        
            FishHookController fishHook = hookProjectilePrefab.AddComponent<FishHookController>();
            fishHook.rb = rb;
            fishHook.stickComponent = stickOnImpact;
            fishHook.controller = pc;
            fishHook.projectileDamage = projectileDamage;
            fishHook.hookCollider = collider;
            fishHook.projOverlap = piss;
            fishHook.projSimple = ps;
            fishHook.lineRenderer = hookProjectilePrefab.GetComponent<LineRenderer>();


            //item grabber
            GameObject ItemInteractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ItemInteractor.transform.parent = hookProjectilePrefab.transform;
            UnityEngine.Object.Destroy(ItemInteractor.GetComponent<MeshRenderer>());
            UnityEngine.Object.Destroy(ItemInteractor.GetComponent<MeshFilter>());
            ItemInteractor.GetComponent<SphereCollider>().isTrigger = true;
            ItemInteractor.transform.localPosition = Vector3.zero;
            ItemInteractor.transform.localScale = Vector3.one * 6;
            ItemInteractor.layer = 15;
        }

        private static void CreateHookScannerEffect()
        {
            hookScannerPrefab = _assetBundle.LoadEffect("FishermanHookScanner");
            _assetBundle.LoadEffect("Nothing");

            //hookScannerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Scanner/ChestScanner.prefab").WaitForCompletion();
            //ChestRevealer radarPing = hookScannerPrefab.GetComponent<ChestRevealer>();
            //radarPing.radius = 3.2f;
            //radarPing.pulseEffectScale /= 156.25f;
        }
       
        private static void CreateMovePlatformProjectileAttack()
        {
           
            shantyCannonShotPrefab = ModAssetManager.CloneProjectilePrefab("MageFireboltBasic", "FishermanShantyCannonShot");
            //UnityEngine.Object.Destroy(shantyCannonShotPrefab.GetComponent<ProjectileOverlapAttack>());
            var projectileController = shantyCannonShotPrefab.GetComponent<ProjectileController>();
            projectileController.ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/FMJRampingGhost.prefab").WaitForCompletion().InstantiateClone("ShantyCannon");
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
            hookBombProjectilePrefab = ModAssetManager.CloneProjectilePrefab("LoaderPylon", "FishermanJellyfish");

            #region general setup
            UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<BeginRapidlyActivatingAndDeactivating>());

            var antiGrav = hookBombProjectilePrefab.GetComponent<AntiGravityForce>();
            antiGrav.antiGravityCoefficient = 0.3f;

            var damageTypeComp = hookBombProjectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            damageTypeComp.Add(DamageTypes.FishermanTether);

            var startEvent = hookBombProjectilePrefab.GetComponent<RoR2.EntityLogic.DelayedEvent>();
            startEvent.CallDelayed(0.5f);

            hookBombProjectilePrefab.layer = LayerIndex.projectile.intVal;
            //hookBombProjectilePrefab.transform.Find("")

            var stick = hookBombProjectilePrefab.AddComponent<ProjectileStickOnImpact>();


            var controller = hookBombProjectilePrefab.GetComponent<ProjectileController>();
            var simpleProj = hookBombProjectilePrefab.GetComponent<ProjectileSimple>();
            simpleProj.lifetime = 99999;
            var pDamageComp = hookBombProjectilePrefab.GetComponent<ProjectileDamage>();
            #endregion general

            #region  beam controler
            var beamController = hookBombProjectilePrefab.GetComponent<ProjectileProximityBeamController>();
            beamController.damageCoefficient = 0.01f;
            beamController.previousTargets = new System.Collections.Generic.List<HealthComponent>();
            beamController.procCoefficient = 1;
            beamController.listClearTimer = 99999;
            beamController.listClearInterval = 99999;
            beamController.attackInterval = 0.1f;
            beamController.attackFireCount = 1;
            #endregion


            #region Explosion
            var bomb = hookBombProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            bomb.blastRadius = 15;
            bomb.blastDamageCoefficient = 1;
            bomb.blastProcCoefficient = 1;
            bomb.lifetime = 99999;
            bomb.destroyOnEnemy = false;
            bomb.impactOnWorld = false;
            bomb.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
            bomb.transformSpace = ProjectileImpactExplosion.TransformSpace.Local;
            //bomblet children
            bomb.fireChildren = true;
            bomb.childrenProjectilePrefab = floatingBombletPrefab;
            bomb.childrenCount = 1;
            bomb.childrenDamageCoefficient = FishermanStaticValues.hookbombDamageCoefficient;
            bomb.explodeOnLifeTimeExpiration = false;
            #endregion Explosion

            //// TODO use triggers to allow hook to throw hook bomb after it sticks.
            //GameObject hookInteractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //hookInteractor.transform.parent = hookBombProjectilePrefab.transform;
            //UnityEngine.Object.Destroy(hookInteractor.GetComponent<MeshRenderer>());
            //UnityEngine.Object.Destroy(hookInteractor.GetComponent<MeshFilter>());
            //hookInteractor.GetComponent<SphereCollider>().isTrigger = true;
            //hookInteractor.transform.localPosition = Vector3.zero;
            //hookInteractor.transform.localScale = Vector3.one * 6;
            //hookInteractor.layer = LayerIndex.pickups.intVal;

            //keeps track of all relevant components for later reference
            #region custom component
            var hookBomb = hookBombProjectilePrefab.AddComponent<HookBombController>();
            hookBomb.beamController = beamController;
            hookBomb.controller = controller;
            hookBomb.damageComponent = pDamageComp;
            hookBomb.explosionComponent = bomb;
            hookBomb.stickComponent = stick;
            hookBomb.antiGrav = hookBombProjectilePrefab.GetComponent<AntiGravityForce>();
            hookBomb.moddedDamageComp = damageTypeComp;
            hookBomb.bombColliders = hookBombProjectilePrefab.GetComponentsInChildren<SphereCollider>();
            #endregion custom Component

            //foreach(var col in hookBomb.bombColliders)
            //{
            //    if (col.gameObject.name.Contains("Grapple")){
            //        col.gameObject.layer = LayerIndex.projectile.intVal;
            //    }
            //}
            //awakeComponent.action.AddListener(hookBomb.ClearHooks);


            UnityEngine.GameObject.Destroy(hookBombProjectilePrefab.transform.Find("FakeActorCollider"));

        }

        private static void CreateBombletProjectile()
        {
            {
                floatingBombletPrefab = _assetBundle.LoadAsset<GameObject>("FishermanFloatingBombProjectile");
                var pc = floatingBombletPrefab.GetComponent<ProjectileController>();
                //pc.ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoGrenadeGhost.prefab").WaitForCompletion();
                floatingBombletPrefab.layer = LayerIndex.projectile.intVal;
                var pie = floatingBombletPrefab.GetComponent<ProjectileImpactExplosion>();
                pie.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
                pie.lifetime = 1.5f;
                pie.lifetimeRandomOffset = 0.1f;
                pie.explodeOnLifeTimeExpiration = false;
                var blc = floatingBombletPrefab.AddComponent<BombletController>();
                blc.pie = pie;
                blc.controller = pc;
                blc.ps = floatingBombletPrefab.GetComponent<ProjectileSimple>();
                blc.agf = floatingBombletPrefab.GetComponent<RoR2.AntiGravityForce>();
            }
            ////yes this is fucking stupid;
            //{
            //    floatingBombletPrefab2 = _assetBundle.LoadAsset<GameObject>("FishermanFloatingBombProjectile");
            //    var pc = floatingBombletPrefab2.GetComponent<ProjectileController>();
            //    pc.ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoGrenadeGhost.prefab").WaitForCompletion();
            //    floatingBombletPrefab2.layer = LayerIndex.projectile.intVal;
            //    var pie = floatingBombletPrefab2.GetComponent<ProjectileImpactExplosion>();
            //    pie.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
            //    pie.lifetime = 1.5f;


            //    pie.childrenProjectilePrefab = floatingBombletPrefab;
            //}

        }
        private static void CreateWhaleMissleProjectile()
        {
            whaleMisslePrefab = _assetBundle.LoadAndAddProjectilePrefab("FishermanWhaleMissle");
            var mdthc = whaleMisslePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            mdthc.Add(DamageTypes.FishermanWhaleFog);
        }
        #endregion projectiles

        #region Materials
        private static void CreateMaterials()
        {
            chainMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/Gravekeeper/matGravekeeperHookChain.mat").WaitForCompletion();
            hookGreenMat = _assetBundle.LoadMaterial("HookGreen");
        }
        #endregion

        #region Minions
        private static void CreateMinions()
        {
            CreateMovingPlatform();
            Content.AddCharacterBodyPrefab(shantyBodyPrefab);
            Content.AddMasterPrefab(shantyMasterPrefab);

            CreateWhale();
            Content.AddCharacterBodyPrefab(whaleBodyPrefab);
            //Prefabs.SetupCharacterModel(whaleBodyPrefab);
            Content.AddMasterPrefab(whaleMasterPrefab);
        }
        private static void CreateMovingPlatform()
        {
            //blueprint
            shantyBlueprintPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainSupplyDropBlueprint.prefab").WaitForCompletion(), "ShantyBlueprint");
            shantyBlueprintPrefab.AddComponent<NetworkIdentity>();
            shantyBlueprintPrefab.transform.localScale = new Vector3(0.2f, 0.05f, 0.2f);
            AkEvent[] soundEmitters = shantyBlueprintPrefab.GetComponents<AkEvent>();
            foreach (var soundEmitter in soundEmitters)
            {
                UnityEngine.Object.Destroy(soundEmitter);
            }

            //body
            shantyBodyPrefab = _assetBundle.LoadAsset<GameObject>("ShantyMinionBody") ;//PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/EmergencyDroneBody.prefab").WaitForCompletion(), "ShantyBody");
            var mpc = shantyBodyPrefab.AddComponent<PlatformMinionController>();
            mpc.characterBody = shantyBodyPrefab.GetComponent<CharacterBody>();
            mpc.standableRB = shantyBodyPrefab.GetComponentInChildren<PhysicsMover>().transform.GetComponent<Rigidbody>();
            mpc.lineRenderer = shantyBodyPrefab.GetComponent<LineRenderer>();

            //master
            shantyMasterPrefab = _assetBundle.LoadAsset<GameObject>("ShantyMinionMaster");//PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/MegaDroneMaster.prefab").WaitForCompletion(), "ShantyMaster");//
            CharacterMaster master = shantyMasterPrefab.GetComponent<CharacterMaster>();
            master.spawnOnStart = false;
            master.bodyPrefab = shantyBodyPrefab;


            IL.RoR2.LocalNavigator.Update += ShantyMinion_LocalNavigator_ObstructionBypass;

           

     


            //InitializeMinionSkins();
            #region ShantyMinionSkills
            FishermanSurvivor.primaryShantyCannon = Skills.CreateReloadSkillDef(new SkillDefInfo
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

                baseRechargeInterval = 6f,
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
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
                


            });
            RoR2.Skills.SkillFamily.Variant shantyShotVariant = new RoR2.Skills.SkillFamily.Variant();
            shantyShotVariant.skillDef = FishermanSurvivor.primaryShantyCannon;
            shantyBodyPrefab.GetComponent<GenericSkill>().skillFamily.variants[0] = shantyShotVariant; //FishermanSurvivor.primaryShantyCannon;
            
            var shantyAI = FishermanAssets.shantyMasterPrefab.GetComponent<RoR2.CharacterAI.BaseAI>();
            #endregion ShantyMinionSkills
            PrefabAPI.RegisterNetworkPrefab(shantyBodyPrefab);
        }

        private static void CreateWhale()
        {
            whaleBlueprintPrefab = shantyBlueprintPrefab; // temp

            whaleBodyPrefab = _assetBundle.LoadAsset<GameObject>("WhaleMinionBody");
            whaleBodyPrefab.layer = LayerIndex.entityPrecise.intVal;

            WhaleMinionController wmc = whaleBodyPrefab.AddComponent<WhaleMinionController>();
            wmc.characterBody = wmc.GetComponent<CharacterBody>();

            whaleMasterPrefab = _assetBundle.LoadAsset<GameObject>("WhaleMinionMaster");
            CharacterMaster master = whaleMasterPrefab.GetComponent<CharacterMaster>();
            master.spawnOnStart = false;
            master.bodyPrefab = whaleBodyPrefab;
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

        /// <summary>
        /// This IL hook prevents the shanty minion from ever being "obstructed" in order to prevent it from moving randomly, as the platform obstructs itself
        /// </summary>
        /// <param name="il"></param>
        static void ShantyMinion_LocalNavigator_ObstructionBypass(ILContext il)
        {
            //When an AI is forwardObstructed, IE looking straight into a wall, it will move randomly to attempt to get itself unstuck.
            //we can make sure that our platform is never considered to be forwardObstructed by adding an additional check to the line inside LocalNavigator.Update() that determines if an entity is obstructed
            //Locate the line we want to modify by Matching this line's IL instructions : 
            /*	 
                // if (raycastResults.forwardObstructed)                          
                IL_00f4: ldarg.0
                IL_00f5: ldflda valuetype RoR2.LocalNavigator/RaycastResults RoR2.LocalNavigator::raycastResults
                IL_00fa: ldfld bool RoR2.LocalNavigator/RaycastResults::forwardObstructed
                IL_00ff: brfalse IL_0188
            */
            ILCursor c = new ILCursor(il);
            ILLabel label = null;
            if (
            c.TryGotoNext(MoveType.After,                                                       //i means instruction, like the under-the-hood individual peices of any one line of code
                i => i.MatchLdarg(0),                                                           //this
                i => i.MatchLdflda<LocalNavigator>(nameof(LocalNavigator.raycastResults)),      //this.raycastResults
                i => i.MatchLdfld<RaycastResults>(nameof(RaycastResults.forwardObstructed)),    //this.raycastResults.forwardObstructed
                i => i.MatchBrfalse(out label)                                                  // == true else skip if body
            ))
            {

                c.Emit(OpCodes.Ldarg_0);                                                        // get a ref to this localnavigator instance (this)
                c.Emit<LocalNavigator>(OpCodes.Ldfld, nameof(LocalNavigator.bodyComponents));   // use ref to get the body field becauese we need to check if this local navigator corresponds to fishermans platform
                c.EmitDelegate<Func<BodyComponents, bool>>((body) =>                            // black magic (add additional conditions to the if statement)
                {
                    //TODO optimize this by finding a better way to determine if the body is the platform. This check is run on every single ai every frame so it matters.
                    return !PlatformMinionController.allDeployedPlatforms.Contains(body.body.gameObject); // check if the body contains the platform's unique component, and return false if it does to stop the navigator from being obstructed
                });
                c.Emit(OpCodes.Brfalse, label);                                                 // pop this value to change it to the result of the delegate
                Log.Debug("Platform Obstruction Prevention IL hook Succeeded");
            }
            else
            {
                Log.Debug("Platform Obstruction Prevention IL hook Failed");
            }




        }
    }




}
