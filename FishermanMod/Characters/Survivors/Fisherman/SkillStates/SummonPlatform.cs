﻿using EntityStates;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class SummonPlatform : BaseState
    {
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
        public GameObject platformMasterPrefab = FishermanAssets.movingPlatformMasterPrefab;

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
                currentPlacementInfo = GetPlacementInfo();
                blueprints = Object.Instantiate(blueprintPrefab, currentPlacementInfo.position, currentPlacementInfo.rotation).GetComponent<BlueprintController>();
            }
            PlayAnimation("Gesture", "PrepTurret");
            entryCountdown = 0.1f;
            exitCountdown = 0.25f;
            exitPending = false;
            if (!base.modelLocator)
            {
                return;
            }
            //ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
            //if ((bool)component)
            //{
            //    Transform transform = component.FindChild("WristDisplay");
            //    if ((bool)transform)
            //    {
            //        wristDisplayObject = Object.Instantiate(wristDisplayPrefab, transform);
            //    }
            //}
        }

        private PlacementInfo GetPlacementInfo()
        {
            Ray aimRay = GetAimRay();
            Vector3 direction = aimRay.direction;
            direction.y = 0f;
            direction.Normalize();
            aimRay.direction = direction;
            PlacementInfo result = default(PlacementInfo);
            result.ok = false;
            result.rotation = Util.QuaternionSafeLookRotation(-direction);
            Ray ray = new Ray(aimRay.GetPoint(2f) + Vector3.up * 1f, Vector3.down);
            float num = 4f;
            float num2 = num;
            if (Physics.SphereCast(ray, 0.5f, out var hitInfo, num, LayerIndex.world.mask) && hitInfo.normal.y > 0.5f)
            {
                num2 = hitInfo.distance;
                result.ok = true;
            }
            Vector3 vector = (result.position = ray.GetPoint(num2 + 0.5f));
            if (result.ok)
            {
                float num3 = Mathf.Max(1.82f, 0f);
                if (Physics.CheckCapsule(result.position + Vector3.up * (num3 - 0.5f), result.position + Vector3.up * 0.5f, 0.45f, (int)LayerIndex.world.mask | (int)LayerIndex.defaultLayer.mask))
                {
                    result.ok = false;
                }
            }
            return result;
        }

        private void DestroyBlueprints()
        {
            if ((bool)blueprints)
            {
                EntityState.Destroy(blueprints.gameObject);
                blueprints = null;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            PlayAnimation("Gesture", "PlaceTurret");
            if ((bool)wristDisplayObject)
            {
                EntityState.Destroy(wristDisplayObject);
            }
            DestroyBlueprints();
        }

        public override void Update()
        {
            base.Update();
            currentPlacementInfo = GetPlacementInfo();
            if ((bool)blueprints)
            {
                blueprints.PushState(currentPlacementInfo.position, currentPlacementInfo.rotation, currentPlacementInfo.ok);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!base.isAuthority)
            {
                return;
            }
            entryCountdown -= Time.fixedDeltaTime;
            if (exitPending)
            {
                exitCountdown -= Time.fixedDeltaTime;
                if (exitCountdown <= 0f)
                {
                    outer.SetNextStateToMain();
                }
            }
            else
            {
                if (!base.inputBank || !(entryCountdown <= 0f))
                {
                    return;
                }
                if ((base.inputBank.skill1.down || base.inputBank.skill3.justPressed) && currentPlacementInfo.ok)
                {
                    if ((bool)base.characterBody)
                    {
                        //base.characterBody.SendConstructTurret(base.characterBody, currentPlacementInfo.position, currentPlacementInfo.rotation, MasterCatalog.FindMasterIndex(platformMasterPrefab));
                        MasterSummon masterSummon = new MasterSummon();
                        masterSummon.masterPrefab = platformMasterPrefab;
                        masterSummon.ignoreTeamMemberLimit = true;
                        masterSummon.teamIndexOverride = TeamIndex.Player;
                        masterSummon.summonerBodyObject = base.gameObject;
                        masterSummon.position = GetPlacementInfo().position;
                        masterSummon.rotation = Util.QuaternionSafeLookRotation(base.characterDirection.forward);
                        CharacterMaster platformMaster = masterSummon.Perform();
                        if (platformMaster)
                        {
                            platformMaster.inventory.CopyItemsFrom(base.characterBody.inventory);
                        }
                        if ((bool)base.skillLocator)
                        {
                            GenericSkill skill = base.skillLocator.GetSkill(SkillSlot.Utility);
                            if ((bool)skill)
                            {
                                skill.DeductStock(1);
                            }
                        }
                    }
                    Util.PlaySound(placeSoundString, base.gameObject);
                    DestroyBlueprints();
                    exitPending = true;
                }
                if (base.inputBank.skill2.justPressed)
                {
                    DestroyBlueprints();
                    exitPending = true;
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}




/*
using EntityStates;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

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

        private static float baseDuration = 1;

        private float earlyExitTime;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            earlyExitTime = duration * 0.3f;

            if (base.isAuthority)
            {
                Vector3 spawnPos = gameObject.transform.position;
                spawnPos.y -= 10;

                MasterSummon masterSummon = new MasterSummon();
                masterSummon.masterPrefab = FishermanAssets.movingPlatformMasterPrefab;
                masterSummon.ignoreTeamMemberLimit = true;
                masterSummon.teamIndexOverride = TeamIndex.Player;
                masterSummon.summonerBodyObject = base.gameObject;
                masterSummon.position = spawnPos;
                masterSummon.rotation = Util.QuaternionSafeLookRotation(base.characterDirection.forward);

                //MovingPlatformController mpc = drone.GetComponent<MovingPlatformController>(); ;
                //Vector3 direction = (inputBank.moveVector == Vector3.zero ? characterDirection.forward : inputBank.moveVector).normalized;
                //direction.y = 0f;
                //mpc.direction = direction;

                if (NetworkServer.active)
                {
                    masterSummon.Perform();
                }
                //mpc.team = teamComponent.teamIndex;
                
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

            if(fixedAge > exitDelay && isAuthority)
            {
                outer.SetNextStateToMain();
            }

        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return fixedAge >= earlyExitTime ? InterruptPriority.Any : InterruptPriority.PrioritySkill;
        }
    }
}
*/
