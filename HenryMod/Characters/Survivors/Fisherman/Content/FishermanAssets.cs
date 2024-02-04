﻿using RoR2;
using UnityEngine;
using FishermanMod.Modules;
using System;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using UnityEngine.PlayerLoop;
using FishermanMod.Survivors.Fisherman.Components;

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

        private static AssetBundle _assetBundle;

        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            swordHitSoundEvent = Content.CreateAndAddNetworkSoundEventDef("HenrySwordHit");

            CreateEffects();

            CreateProjectiles();
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
            

            ProjectileStickOnImpact stickOnImpact = hookProjectilePrefab.GetComponent <ProjectileStickOnImpact>();
            stickOnImpact.ignoreCharacters = false;
            stickOnImpact.alignNormals = false;
          
            ProjectileSingleTargetImpact pstImpact = hookProjectilePrefab.GetComponent<ProjectileSingleTargetImpact>();

            ProjectileController pc = hookProjectilePrefab.GetComponent<ProjectileController>();

            FishHookController fishHook = hookProjectilePrefab.AddComponent<FishHookController>();
            fishHook.rb = rb;
            fishHook.stickComponent = stickOnImpact;
            fishHook.controller = pc;

            GameObject ghostPrefab = pc.ghostPrefab;
            ProjectileGhostController gpc = ghostPrefab.GetComponent<ProjectileGhostController>();
            UnityEngine.Object.Destroy(ghostPrefab.GetComponentInChildren<ObjectScaleCurve>()); // this removes the visual disapearing
            //UnityEngine.Object.Destroy(hookProjectilePrefab.GetComponent<ProjectileController>().ghostPrefab.GetComponent<WinchControl>());
            //WinchControl winch = ghostPrefab.GetComponent<WinchControl>();
            //winch.enabled = false;
            //UnityEngine.Object.Destroy(ghostPrefab.GetComponent<VFXAttributes>());
            //VFXAttributes vfx = ghostPrefab.GetComponent<VFXAttributes>();
            //vfx.enabled = false;
            //UnityEngine.Object.Destroy(ghostPrefab.GetComponent<ProjectileGhostController>());
            //ProjectileGhostCluster pgc = ghostPrefab.GetComponent<ProjectileGhostCluster>();

            // winch.tailTransform = 
            // winch.tailTransform
            //stickOnImpact.stickEvent.RemoveAllListeners();
        }
        #endregion projectiles
    }

        
}
