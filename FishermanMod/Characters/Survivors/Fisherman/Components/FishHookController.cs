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
using UnityEngine.UI;
using UnityEngine.Assertions.Must;

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
        float minTimeBeforeReturning = 1f;
        float maxFlyTime = 2;

        HashSet<GameObject> objectsHooked = new HashSet<GameObject>();


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
            if (isFlying) return;
            //remove motion and collision in order to prevent enemy sliding
            hookCollider.enabled = false;
            rb.velocity = Vector3.zero;
            rb.drag = 0;
            rb.angularDrag = 0;
            rb.mass = 0; 
            rb.useGravity = false;
            projSimple.SetForwardSpeed(0);
            EffectData effectData = new EffectData();
            effectData.origin = transform.position;
            EffectManager.SpawnEffect(FishermanAssets.hookScannerPrefab, effectData, true);
        }

        void FixedUpdate()
        {
            if (controller.owner == null) Destroy(gameObject);
            distanceToOwner = Vector3.Distance(transform.position, ownerTransform.position);
            if (!isFlying && distanceToOwner > autoTriggerDistance) 
            {
                StartCoroutine(FlyBack());
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
        public IEnumerator FlyBack()
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

            yield return new WaitForFixedUpdate();

            //apply return force to hook, causing it to arc up into the air
            projSimple.desiredForwardSpeed = 0;
            Log.Debug($"[HOOK][FLYBACK] owner {ownerTransform.position} hook position {transform.position}");
            Vector3 vel = FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, transform.position, !stickComponent.stuck);
            Log.Debug($"[HOOK][FLYBACK] vel {vel}");
            vel.y = -vel.y;
            Log.Debug($"[HOOK][FLYBACK] altered vel {vel}");

            rb.AddForce(vel, ForceMode.VelocityChange);
            Log.Debug($"[HOOK][FLYBACK] RB vel {rb.velocity}");


            projOverlap.enabled = true;
            projectileDamage.damage =  skillObjectTracker.characterBody.damage * FishermanStaticValues.castDamageCoefficient;
            projOverlap.damageCoefficient = 1;
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

            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.GetComponent<MapZone>()) Log.Debug("[Cast Hook] exit bounds box: trig exit");
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
                //Log.Debug($"[Cast Hook] Item Hit, {collider.gameObject.name}");
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

        
    }

}
