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

namespace FishermanMod.Survivors.Fisherman
{
    public static class FishermanAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;

        public static GameObject bombExplosionEffect;

        // networked hit sounds
        public static NetworkSoundEventDef swordHitSoundEvent;

        //projectiles
        public static GameObject bombProjectilePrefab;
        public static GameObject hookProjectilePrefab;
        public static GameObject movingPlatformBlueprintPrefab;
        public static GameObject movingPlatformPrefab;
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
        }

        #region effects
        private static void CreateEffects()
        {
            CreateBombExplosionEffect();

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
        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateBombProjectile();
            Content.AddProjectilePrefab(bombProjectilePrefab);
            CreateHookProjectile();
            Content.AddProjectilePrefab(hookProjectilePrefab);
            CreateMovingPlatform();
            Content.AddProjectilePrefab(movingPlatformBlueprintPrefab);
            CreateJellyfishProjectile();
            Content.AddProjectilePrefab(hookBombProjectilePrefab);
        }

        private static void CreateBombProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            bombProjectilePrefab = Assets.CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(bombProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion bombImpactExplosion = bombProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            
            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.blastDamageCoefficient = 1f;
            bombImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = bombExplosionEffect;
            bombImpactExplosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("HenryBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombProjectilePrefab.GetComponent<ProjectileController>();

            if (_assetBundle.LoadAsset<GameObject>("HenryBombGhost") != null)
                bombController.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("HenryBombGhost");
            
            bombController.startSound = "";
        }

        private static void CreateHookProjectile()
        {
            //hookProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiMine.prefab").WaitForCompletion();
            hookProjectilePrefab = Assets.CloneProjectilePrefab("GravekeeperHookProjectileSimple", "FishermanHookProjectile");
            Rigidbody rb = hookProjectilePrefab.GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 100;

            ProjectileSimple ps = hookProjectilePrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = 80;
            ps.lifetime = 999999;
            
            ProjectileDamage projectileDamage = ps.GetComponent<ProjectileDamage>();
            

            ProjectileStickOnImpact stickOnImpact = hookProjectilePrefab.GetComponent <ProjectileStickOnImpact>();
            stickOnImpact.ignoreCharacters = false;
            stickOnImpact.alignNormals = false;
          
            ProjectileSingleTargetImpact pstImpact = hookProjectilePrefab.GetComponent<ProjectileSingleTargetImpact>();
            UnityEngine.Object.Destroy(pstImpact);
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



            TeamComponent teamComp = hookProjectilePrefab.AddComponent<TeamComponent>();
            teamComp.teamIndex = TeamIndex.Player;
            CharacterBody hookBody = hookProjectilePrefab.AddComponent<CharacterBody>();
            hookBody.baseRegen = int.MaxValue;
            hookBody.baseMaxHealth = int.MaxValue;
            hookBody.baseArmor = int.MinValue;
            HealthComponent healthComp = hookProjectilePrefab.AddComponent<HealthComponent>();
            healthComp.health = int.MaxValue;
            healthComp.dontShowHealthbar = true;

            

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
            //fishHook.enemyTaunter = enemyTaunter;
            fishHook.hookBody = hookBody;
            //fishHook.pstImpact = pstImpact;

            GameObject ghostPrefab = pc.ghostPrefab;
            ProjectileGhostController gpc = ghostPrefab.GetComponent<ProjectileGhostController>();
            UnityEngine.Object.Destroy(ghostPrefab.GetComponentInChildren<ObjectScaleCurve>()); // this removes the visual disapearing
            //want to change this to custom line renderer effect later
            AnimateShaderAlpha trailFadeEffect = gpc.GetComponentInChildren<AnimateShaderAlpha>();
            trailFadeEffect.timeMax = 20;
            TrailRenderer trailEffect = gpc.GetComponentInChildren<TrailRenderer>();
            trailEffect.startWidth = 0.2f;
            trailEffect.endWidth = 0.01f;

        }

        private static void CreateMovingPlatform()
        {
            movingPlatformBlueprintPrefab = _assetBundle.LoadAsset<GameObject>("MovingPlatformBlueprint");
            //var bpc = movingPlatformBlueprintPrefab.AddComponent<BlueprintController>();
            movingPlatformPrefab = _assetBundle.LoadAsset<GameObject>("MovingPlatform");
            movingPlatformPrefab.AddComponent<MovingPlatformController>();
           
        }

        private static void CreateJellyfishProjectile()
        {
            hookBombProjectilePrefab = Assets.CloneProjectilePrefab("LoaderPylon", "FishermanJellyfish");

            //UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<AntiGravityForce>());
            UnityEngine.Object.Destroy(hookBombProjectilePrefab.GetComponent<AwakeEvent>());

            var antiGrav = hookBombProjectilePrefab.GetComponent<AntiGravityForce>();
            antiGrav.antiGravityCoefficient = 0.5f;

            var beamController = hookBombProjectilePrefab.GetComponent<ProjectileProximityBeamController>();
            beamController.damageCoefficient = 0.01f;
            beamController.previousTargets = new System.Collections.Generic.List<HealthComponent>();
            beamController.procCoefficient = 1;
            beamController.listClearTimer = 99999;
            beamController.listClearInterval = 99999;
            beamController.attackInterval = 0.1f;
            beamController.attackFireCount = 1;

            var damageTypeComp = hookBombProjectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            damageTypeComp.Add(DamageTypes.TetherHook);

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
            var pDamageComp = hookBombProjectilePrefab.GetComponent<ProjectileDamage>();

            var hookBomb = hookBombProjectilePrefab.AddComponent<HookBombController>();
            hookBomb.beamController = beamController;
            hookBomb.controller = controller;
            hookBomb.damageComponent = pDamageComp;
            hookBomb.explosionComponent = bomb;

            
        }
        #endregion projectiles

        private static void CreateMaterials()
        {
            chainMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/Gravekeeper/matGravekeeperHookChain.mat").WaitForCompletion();
        }
        

    }

        
}
