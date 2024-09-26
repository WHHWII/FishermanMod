using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
using IL.RoR2;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    //TODO: Correct overide behavior so it isnt creating a new overide each time
    public class AimHook : AimThrowableBase
    {
        float hookRangeGrowRate = 0.5f;
        float hookRangeMax = 80;
       // private static EntityStates.Toolbot.AimStunDrone _goodState;
        public override void OnEnter()
        {
            EntityStates.Toolbot.AimStunDrone _goodState = new EntityStates.Toolbot.AimStunDrone();
            maxDistance = 5;
            rayRadius = 1;
            arcVisualizerPrefab = _goodState.arcVisualizerPrefab;
            projectilePrefab = FishermanAssets.hookProjectilePrefab;
            endpointVisualizerPrefab = _goodState.endpointVisualizerPrefab;
            endpointVisualizerRadiusScale = 1f;
            setFuse = false ;
            damageCoefficient = 0;
            baseMinimumDuration = _goodState.baseMinimumDuration;
            useGravity = true;
            projectileBaseSpeed = _goodState.projectileBaseSpeed;
            //comes after modification to working AimThrowableBase
            base.OnEnter();
            if (base.isAuthority && !KeyIsDown() && !base.IsKeyDownAuthority()) { }
                PlayAnimation("Gesture, Override", "SecondaryCastStart", "SecondaryCast.playbackRate", 0.65f);


        }
        public override void OnExit() 
        { 
            base.OnExit();

            if(base.isAuthority && !KeyIsDown() && !base.IsKeyDownAuthority())
                PlayAnimation("Gesture, Override", "SecondaryCastEnd", "SecondaryCast.playbackRate", 0.65f);
            base.skillLocator.secondary.SetSkillOverride(this, FishermanSurvivor.secondaryRecallFishHook, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
            base.skillLocator.secondary.DeductStock(1);
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
            // have non centered aiming??
            // could also alter aim origin?
        }


    }
}
