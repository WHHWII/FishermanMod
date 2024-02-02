using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
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
            maxDistance = 1000f;
            rayRadius = _goodState.rayRadius;
            arcVisualizerPrefab = _goodState.arcVisualizerPrefab;
            projectilePrefab = _goodState.projectilePrefab;
            endpointVisualizerPrefab = _goodState.endpointVisualizerPrefab;
            endpointVisualizerRadiusScale = _goodState.endpointVisualizerRadiusScale;
            setFuse = _goodState.setFuse;
            damageCoefficient = _goodState.damageCoefficient;
            baseMinimumDuration = _goodState.baseMinimumDuration;
            base.OnEnter();



            
            
        }
        public override void FireProjectile() { }
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
    }
}
