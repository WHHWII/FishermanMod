﻿using EntityStates;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class SummonPlatform : BaseState
    {
        //stolen straight from engi
        private struct PlacementInfo
        {
            public bool ok;

            public Vector3 position;

            public Quaternion rotation;
        }

        [SerializeField]
        public GameObject wristDisplayPrefab;

        [SerializeField]
        public string placeSoundString;

        [SerializeField]
        public GameObject blueprintPrefab = FishermanAssets.movingPlatformBlueprintPrefab;

        [SerializeField]
        public GameObject platformPrefab = FishermanAssets.movingPlatformPrefab;

        private const float placementMaxUp = 1f;

        private const float placementMaxDown = 3f;

        private const float placementForwardDistance = 2f;

        private const float entryDelay = 0.1f;

        private const float exitDelay = 0.25f;

        private const float turretRadius = 0.5f;

        private const float turretHeight = 1.82f;

        private const float turretCenter = 0f;

        private const float turretModelYOffset = -0.75f;

        private GameObject wristDisplayObject;

        private BlueprintController blueprints;

        private float exitCountdown;

        private bool exitPending;

        private float entryCountdown;

        private PlacementInfo currentPlacementInfo;

        public override void OnEnter()
        {
            base.OnEnter();
            if (base.isAuthority)
            {
                MovingPlatformController mpc = UnityEngine.Object.Instantiate(platformPrefab, gameObject.transform.position,gameObject.transform.rotation).GetComponent<MovingPlatformController>(); ;
                Vector3 direction = (inputBank.moveVector == Vector3.zero ? characterDirection.forward : inputBank.moveVector).normalized;
                direction.y = 0f;
                mpc.direction = direction;
                mpc.team = teamComponent.teamIndex; 
                outer.SetNextStateToMain();
            }
        }
        public override void OnExit()
        {
            base.OnExit();

        }

        public override void Update()
        {
            base.Update();

        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
