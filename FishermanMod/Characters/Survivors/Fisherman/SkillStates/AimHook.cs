using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
using IL.RoR2;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA;
using static RoR2.CameraTargetParams;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    public class AimHook : AimThrowableBase
    {
        float hookRangeGrowRate = 0.5f;
        float hookRangeMax = 70;
        Transform poletip;
        RoR2.CameraTargetParams.AimRequest aimRequest;

        // private static EntityStates.Toolbot.AimStunDrone _goodState;
        public override void OnEnter()
        {
            EntityStates.Toolbot.AimStunDrone _goodState = new EntityStates.Toolbot.AimStunDrone();
            maxDistance = 4;
            rayRadius = 1;
            arcVisualizerPrefab = _goodState.arcVisualizerPrefab;
            projectilePrefab = FishermanAssets.hookProjectilePrefab;
            endpointVisualizerPrefab = _goodState.endpointVisualizerPrefab;
            endpointVisualizerRadiusScale = 1f;
            setFuse = false;
            damageCoefficient = 0;
            baseMinimumDuration = _goodState.baseMinimumDuration;
            useGravity = true;
            //projectileBaseSpeed = _goodState.projectileBaseSpeed;
            projectileBaseSpeed = FishermanAssets.hookBombProjectilePrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed;
            //comes after modification to working AimThrowableBase

            poletip = GetModelChildLocator().FindChild("PoleEnd");
            base.OnEnter();
            if (base.isAuthority && !KeyIsDown() && !base.IsKeyDownAuthority()) { }
            PlayAnimation("Gesture, Override", "SecondaryCastStart", "SecondaryCast.playbackRate", 0.65f);

            if (cameraTargetParams)
            {
                aimRequest = BuildAimRequest();
            }

        }


        private RoR2.CameraTargetParams.AimRequest BuildAimRequest()
        {
            RoR2.CharacterCameraParamsData camData = cameraTargetParams.cameraParams.data;
            camData.idealLocalCameraPos.value += new Vector3(1f, -0.1f, -1f);
            CameraParamsOverrideHandle overrideHandle = cameraTargetParams.AddParamsOverride(new CameraParamsOverrideRequest
            {
                cameraParamsData = camData,
                priority = 0.1f
            }, 0.5f);
            AimRequest newAimRequest = new AimRequest(AimType.OverTheShoulder, delegate (AimRequest aimRequest)
            {
                cameraTargetParams.RemoveRequest(aimRequest);
                cameraTargetParams.RemoveParamsOverride(overrideHandle, 0.5f);
            });
            cameraTargetParams.aimRequestStack.Add(newAimRequest);
            return newAimRequest;
        }
        public override void OnExit() 
        { 
            base.OnExit();

            if(base.isAuthority && !KeyIsDown() && !base.IsKeyDownAuthority())
                PlayAnimation("Gesture, Override", "SecondaryCastEnd", "SecondaryCast.playbackRate", 0.65f);
            base.skillLocator.secondary.SetSkillOverride(gameObject, FishermanSurvivor.secondaryRecallFishHook, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            base.skillLocator.secondary.DeductStock(1);
            aimRequest.Dispose();
        }
        public override void FixedUpdate()
        {
            
            base.FixedUpdate();
            if(maxDistance < hookRangeMax)
            {
                maxDistance += hookRangeGrowRate;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        public override void UpdateTrajectoryInfo(out TrajectoryInfo dest)
        {

            base.UpdateTrajectoryInfo(out dest);
            //dest.finalRay.origin = poletip.position;
            //dest.hitPoint = arcVisualizerLineRenderer.GetPosition(arcVisualizerLineRenderer.positionCount-1);
            StartAimMode();
            // have non centered aiming??
            // could also alter aim origin?
        }

        public override void FireProjectile()
        {
            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.crit = RollCrit();
            fireProjectileInfo.owner = base.gameObject;
            fireProjectileInfo.position = currentTrajectoryInfo.finalRay.origin;
            fireProjectileInfo.projectilePrefab = projectilePrefab;
            fireProjectileInfo.rotation = Quaternion.LookRotation(currentTrajectoryInfo.finalRay.direction, Vector3.up);
            fireProjectileInfo.speedOverride = currentTrajectoryInfo.speedOverride;
            fireProjectileInfo.damage = damageCoefficient * damageStat;
            FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
            if (setFuse)
            {
                fireProjectileInfo2.fuseOverride = currentTrajectoryInfo.travelTime;
            }
            ModifyProjectile(ref fireProjectileInfo2);
            ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
        }
    }
}
