using System.Collections.Generic;
using UnityEngine;
using RoR2;
using RoR2.Projectile;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using System.Collections;
using UnityEngine.UIElements;
using Rewired.Utils;
using static UnityEngine.ParticleSystem.PlaybackState;
using R2API;
using UnityEngine.Networking;
using Newtonsoft.Json.Utilities;

namespace FishermanMod.Survivors.Fisherman.Components
{
    //TODO fix chest launching terrain issues on certain maps
    //TODO fix launch force/trajectory for all objects
    //TODO allow grab of logbooks
    //TODO allow grab of pickups such as ghor gold or bandolier drops
    //TODO allow fishing out of bounds
    //TODO grab allies (non-player)
    //TODO fix crashing when recalling hook in certain conditions. (out of bounds on some maps, some entities)
    //TODO fix sticking hook to midflight manowar causes it to fly away
    public class FishHookController : MonoBehaviour
    {
        public Rigidbody rb;
        public ProjectileStickOnImpact stickComponent;
        public ProjectileController controller;
        public ProjectileDamage projectileDamage;
        public CapsuleCollider hookCollider;
        public ProjectileOverlapAttack projOverlap;
        public ProjectileSimple projSimple;
        //public GameObject enemyTaunter;
        //public HurtBox hookHurtBox;
        //public CharacterBody hookBody;

        FishermanSkillObjectTracker skillObjectTracker;

        bool isFlying = false;
        float distanceToOwner;
        float autoTriggerDistance = 600;
        float homeToBodyDistance = 50;
        float homingForce = 0.01f;
        float homingDeceleration = 0.33f;
        float returnForceBase = 1;
        Transform ownerTransform;
        float timeFlying = 0;
        float minTimeBeforeReturning = .5f;
        float maxFlyTime = 2;

        //float maxTauntTime = 5f;
        //bool canPollTauntedEnemiesForRelease = true;

        HashSet<GameObject> objectsHooked = new HashSet<GameObject>();
        //HashSet<GameObject> enemiesTaunted = new HashSet<GameObject>();
       // Dictionary<RoR2.CharacterAI.BaseAI, float> tauntedAITimers = new Dictionary<RoR2.CharacterAI.BaseAI, float>();

        void Start()
        {
            //grappleOwnerRef.enabled = false;
            ownerTransform = controller.owner.transform;
            skillObjectTracker = ownerTransform.GetComponent<FishermanSkillObjectTracker>();
            skillObjectTracker.deployedHooks.Add(this);
            hookCollider.enabled = true;
            stickComponent.stickEvent.AddListener(OnStickEvent);
            projectileDamage.force = 0;
        }
        void OnStickEvent()
        {
            //remove motion and collision in order to prevent enemy sliding
            hookCollider.enabled = false;
            rb.velocity = Vector3.zero;
            rb.drag = 0;
            rb.angularDrag = 0;
            rb.mass = 0;
            rb.useGravity = false;
            projSimple.SetForwardSpeed(0);
            //NetworkServer.Spawn(UnityEngine.Object.Instantiate(FishermanAssets.hookScannerPrefab, transform.position, Quaternion.identity));
        }

        void FixedUpdate()
        {
            if (controller.owner == null) Destroy(gameObject);
            distanceToOwner = Vector3.Distance(transform.position, ownerTransform.position);
            if (!isFlying && distanceToOwner > autoTriggerDistance) 
            {
                FlyBack();
            }
            if (isFlying)
            {
                timeFlying += Time.fixedDeltaTime;
                // if hook is near player or has been flying for a long time, engange homing to force the hook to quickly return
                if((distanceToOwner <= homeToBodyDistance && timeFlying >= minTimeBeforeReturning) || timeFlying >= maxFlyTime)
                {
                    rb.MovePosition(Vector3.Lerp(rb.position, ownerTransform.position, homingForce * distanceToOwner));
                    if (rb.velocity.magnitude > 1)
                    {
                        rb.velocity *= homingDeceleration;
                    }
                    else
                    {
                        //TODO refund stock if nothing grabbed.
                        Destroy(gameObject);
                    }
                }
                
            }
        }
        public void FlyBack()
        {
            isFlying = true; //aka is being recalled

            //reset rigid body for motion (previously frozen on impact)
            rb.velocity = Vector3.zero;
            rb.drag = 0;
            rb.angularDrag = 0.05f;
            rb.mass = 100;
            rb.useGravity = true;
            stickComponent.enabled = false;
            rb.isKinematic = false;

            //ability specific interactions (OOB crash was occuring before these were added )
            //if (skillObjectTracker.deployedBombs)
            //{
            //    ThrowHookBomb();
            //}
            //if (FishermanSurvivor.deployedPlatform && FishermanSurvivor.deployedPlatform.wasStuckByHook)
            //{
            //    ThrowPlatform();
            //}

            //apply return force to hook, causing it to arc up into the air
            projSimple.desiredForwardSpeed = 0;
            rb.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, transform.position, !stickComponent.stuck), ForceMode.VelocityChange);

            //enable projectile overlap. (OOB crash was occuring before this was added )
            projOverlap.enabled = true;
            projectileDamage.damage = FishermanSurvivor.instance.bodyInfo.damage * FishermanStaticValues.castDamageCoefficient;
            projOverlap.damageCoefficient = FishermanStaticValues.castDamageCoefficient;
        }
        bool CanThrow(GameObject gameObject)
        {
            if (!isFlying) return false;
            if (objectsHooked.Contains(gameObject)) return false;

            objectsHooked.Add(gameObject);
            return true;
        }
        void OnCollisionEnter(UnityEngine.Collision collision)
        {
            if (collision.gameObject.GetComponent<MapZone>()) Log.Debug("[Cast Hook] Hit bounds box: col enter");
            //collision.rigidbody.velocity = Vector3.zero;
            Log.Debug($"[Cast Hook]Collision Enter {collision.gameObject.name}");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                ThrowHookBomb(hookBomb);
            }
        }
        void OnCollisionExit(UnityEngine.Collision collision)
        {
            Log.Debug($"[Cast Hook] Collision Exit {collision.gameObject.name}");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                ThrowHookBomb(hookBomb);
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.GetComponent<MapZone>()) Log.Debug("[Cast Hook] Hit bounds box: trig enter");
            Log.Debug($"[Cast Hook] Trigger Enter {collider.gameObject.name}");

            //DrawAggro(collider);
            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.GetComponent<MapZone>()) Log.Debug("[Cast Hook] Hit bounds box: trig exit");
            Log.Debug($"[Cast Hook] Trigger Exit {collider.gameObject.name}");
            if (!CanThrow(collider.gameObject)) return;
            ThrowHookBomb(collider.transform.parent?.GetComponent<HookBombController>());
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }

        bool ThrowItem(Collider collider)
        {
            
            if (collider.gameObject.name == "GenericPickup(Clone)")
            {
                Log.Debug($"[Cast Hook] Item Hit, {collider.gameObject.name}");
                Rigidbody itemBody = collider.gameObject.GetComponent<Rigidbody>();
                itemBody.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position,itemBody.position,false), ForceMode.VelocityChange);
                return true;
            }
            return false;

        } 
        void ThrowInteractable(Collider collider)
        {

            EntityLocator eLoc = collider.gameObject.GetComponent<EntityLocator>();
            if (eLoc != null)
            {
                bool isGrabbable = FishermanSurvivor.CheckIfInteractableIsGrabable(eLoc.entity.name);
             
                //Log.Debug($"{eLoc.entity.name}: Can be grabbed?: {isGrabbable}");
                if (!isGrabbable) return;
                InteractableStopOnImpact stopper = eLoc.entity.AddComponent<InteractableStopOnImpact>();
                stopper.rb = eLoc.entity.AddComponent<Rigidbody>();
                stopper.rb.mass = 300;
                
                stopper.collider = eLoc.entity.AddComponent<SphereCollider>();
                stopper.collider.radius = 0.5f;
                //interactableBody.useGravity = false;
                stopper.rb.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, stopper.rb.position, false), ForceMode.VelocityChange);
                
            }

        }
        void ThrowHookBomb(HookBombController bomb)
        {
            if (bomb == null) return;
            Log.Debug("[Cast Hook] Trying to throw bomb");
            bomb.stickComponent.Detach() ;
            bomb.DisableAllColliders();
            bomb.stickComponent.enabled = false;
            bomb.body.isKinematic = false;
            bomb.body.drag = 0;
            bomb.body.angularDrag = 0.05f;
            bomb.body.mass = 120;
            bomb.body.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, bomb.transform.position, !bomb.stickComponent.stuck), ForceMode.VelocityChange);
            bomb.wasStuckByHook = false;
            StartCoroutine(bomb.ResetStickyComponent());
        }
        void ThrowPlatform()
        {
            //FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(ownerTransform.gameObject,
            //    ownerTransform.gameObject,
            //    0,
            //    ownerTransform.position,
            //    //FishermanSurvivor.deployedPlatform.characterBody.mainHurtBox);
        }
        void OnDestroy()
        {
            //FishermanSurvivor.SetDeployedHook(null);
        }

        //void DrawAggro(Collider collider)
        //{
        //    if (CanTaunt(collider.gameObject))
        //    {
        //        //Log.Debug($"-Can Taunt {collider.gameObject.name}");
        //        TeamComponent team = collider.GetComponent<TeamComponent>();
        //        if (team != null)
        //        {
        //           // if (hookHurtBox == null) return;
        //            //Log.Debug($"{collider.gameObject.name}'s Team =  {team.teamIndex}");
        //            if (team.teamIndex != controller.owner.GetComponent<TeamComponent>().teamIndex)
        //            {
        //                CharacterBody body = collider.gameObject.GetComponent<CharacterBody>();
        //                body.AddTimedBuff(FishermanBuffs.hookTauntDebuff, maxTauntTime);
        //                body.healthComponent.dontShowHealthbar = false;
        //                //body.healt
        //                //Log.Debug($"{collider.gameObject.name} is on a different team than hook owner");
        //                //RoR2.UI.CombatHealthBarViewer. need to see about making this force show health bars
        //                //that or make an effect similiar to death mark
        //                foreach (RoR2.CharacterAI.BaseAI ai in body.master.aiComponents)
        //                {
        //                    if (ai != null)
        //                    {
        //                        //Log.Debug($"{collider.gameObject.name} Ai located, setting target");
        //                        //RoR2.CharacterAI.BaseAI.Target newTarget = new RoR2.CharacterAI.BaseAI.Target(controller.owner.GetComponent<CharacterBody>().master.GetComponent<RoR2.CharacterAI.BaseAI>());
        //                        //newTarget.gameObject = gameObject;
        //                        //newTarget._gameObject = gameObject;
        //                        //newTarget.characterBody = 
        //                        //ai.currentEnemy.Reset();
        //                        ai.currentEnemy.gameObject = gameObject;
        //                        ai.currentEnemy.bestHurtBox = null;
        //                        ai.enemyAttention = ai.enemyAttentionDuration;
        //                        ai.targetRefreshTimer = 1;
        //                        ai.BeginSkillDriver(ai.EvaluateSkillDrivers());
        //                        tauntedAITimers.Add(ai, Time.time); 
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //void ClearAgrro()
        //{
        //    //TODO properly clear debuff
        //    if (tauntedAITimers.Count <= 0) return;
        //    List<RoR2.CharacterAI.BaseAI> aisToRemove = new List<RoR2.CharacterAI.BaseAI>();
        //    foreach (var aiTimer in tauntedAITimers)
        //    {
        //        if (Time.time - aiTimer.Value > maxTauntTime)
        //        {
        //            if(aiTimer.Key == null)
        //            {
        //                aisToRemove.Add(aiTimer.Key);
        //                continue;
        //            }
        //            //Log.Debug($"Releasing: {aiTimer.Key.gameObject.name}");
        //            aiTimer.Key.currentEnemy.gameObject = controller.owner.gameObject;
        //            aiTimer.Key.currentEnemy.bestHurtBox = controller.owner.GetComponent<CharacterBody>().mainHurtBox;
        //            aiTimer.Key.BeginSkillDriver(aiTimer.Key.EvaluateSkillDrivers());
        //            //aiTimer.Key.gameObject.GetComponent<CharacterBody>().RemoveBuff(FishermanBuffs.hookTauntDebuff);
        //            aisToRemove.Add(aiTimer.Key);
        //        }
        //        else
        //        {
        //            //aiTimer.Key.gameObject.GetComponent<CharacterBody>().AddTimedBuff(FishermanBuffs.hookTauntDebuff, maxTauntTime);
        //        }
        //    }
        //    foreach (var ai in aisToRemove)
        //    {
        //        tauntedAITimers.Remove(ai);
        //    }
        //    canPollTauntedEnemiesForRelease = false;
        //}
    }

}
