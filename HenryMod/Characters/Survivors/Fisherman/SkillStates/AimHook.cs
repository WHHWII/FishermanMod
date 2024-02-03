using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
using IL.RoR2;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.SkillStates
{
    public class AimHook : AimThrowableBase
    {
       // private static EntityStates.Toolbot.AimStunDrone _goodState;
        public override void OnEnter()
        {
            EntityStates.Toolbot.AimStunDrone _goodState = new EntityStates.Toolbot.AimStunDrone();
           // _goodState.transform.localPosition += new Vector3(10, 0, 0);
            maxDistance = 700f;
            rayRadius = 1;
            arcVisualizerPrefab = _goodState.arcVisualizerPrefab;
            projectilePrefab = FishermanAssets.hookProjectilePrefab;
            endpointVisualizerPrefab = _goodState.endpointVisualizerPrefab;
            endpointVisualizerRadiusScale = 1f;
            setFuse = _goodState.setFuse;
            damageCoefficient = _goodState.damageCoefficient;
            baseMinimumDuration = _goodState.baseMinimumDuration;
            useGravity = true;
            base.OnEnter();
            
           


            
            
        }
        public override void OnExit() 
        { 
            base.OnExit(); 
        }
        public override void FixedUpdate()
        {

            base.FixedUpdate();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        public override void UpdateTrajectoryInfo(out TrajectoryInfo dest)
        {
            base.UpdateTrajectoryInfo(out dest);

        }
    }
}
