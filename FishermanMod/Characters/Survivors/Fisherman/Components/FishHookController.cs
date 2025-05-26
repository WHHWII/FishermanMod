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
using UnityEngine.Events;
using static EntityStates.BaseState;
using EntityStates;

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
        public LineRenderer lineRenderer;

        SkillObjectTracker objTracker;

        bool isFlying = false;
        float distanceToOwner;
        float autoTriggerDistance = 600;
        float homeToBodyDistance = 50;
        float homingForce = 5f;
        float homingDeceleration = 0.33f;
        float returnForceBase = 1;
        Transform ownerTransform;
        float timeFlying = 0;
        float minTimeBeforeReturning = 0.25f;
        float maxFlyTime = 2;

        HashSet<GameObject> objectsHooked = new HashSet<GameObject>();

        UnityEvent<GameObject> onHookEvent = new UnityEvent<GameObject>();

        List<HookBombController> bombsToThrow = new List<HookBombController>();

        void Awake()
        {
            //Log.Debug("[HOOK] New Hook Created ------------------------------------------------------------------------------------------------------------");
            GameObject hi = Instantiate(FishermanAssets.hookIndicator, transform);
            hi.GetComponent<PositionIndicator>().targetTransform = transform;
            hi.transform.position = Vector3.zero;
        }

        void Start()
        {
            //grappleOwnerRef.enabled = false;
            ownerTransform = controller.owner.transform;
            objTracker = ownerTransform.GetComponent<SkillObjectTracker>();
            objTracker.deployedHooks.Add(this);
            hookCollider.enabled = true;
            stickComponent.stickEvent.AddListener(OnStickEvent);
            projectileDamage.force = 0;
            onHookEvent.AddListener(SpawnRadar);
            onHookEvent.AddListener(ApplyHitStop);
            projOverlap.onServerHit.AddListener(() => ApplyHitStop(null));
        }
        void OnStickEvent()
        {
            if (isFlying) return;
            //Log.Debug($"[HOOK] STICK EVENT");

            //remove motion and collision in order to prevent enemy sliding
            hookCollider.enabled = false;
            rb.velocity = Vector3.zero;
            //rb.drag = 0;
            //rb.angularDrag = 0;
            //rb.mass = 0; 
            rb.useGravity = false;
            projSimple.SetForwardSpeed(0);
            SpawnRadar(gameObject);
        }
        void Update()
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, Vector3.Lerp(transform.position, objTracker.fishingPoleTip.position, 0.5f) + (Vector3.up * 0.1f));
            lineRenderer.SetPosition(2, objTracker.fishingPoleTip.position);
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
                transform.LookAt(transform.position - (ownerTransform.position - transform.position));
                //transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime);
                timeFlying += Time.fixedDeltaTime;
                // if hook is near player or has been flying for a long time, engange homing to force the hook to quickly return
                if((distanceToOwner <= homeToBodyDistance && timeFlying >= minTimeBeforeReturning) || timeFlying >= maxFlyTime)
                {
                    Vector3 vel = (ownerTransform.position - transform.position).normalized * rb.mass *  Mathf.Max(homingForce - distanceToOwner, 1) * timeFlying;
                    rb.AddForce(vel, ForceMode.VelocityChange);
                    //rb.MovePosition(Vector3.Lerp(rb.position, objTracker.fishingPoleTip.position, homingForce * distanceToOwner));
                    if (rb.velocity.magnitude > 1)
                    {
                        rb.velocity *= homingDeceleration;
                    }
                    else
                    {
                        //TODO refund stock if nothing grabbed.
                        projSimple.lifetime = 0.0001f;
                    }
                }
                if(distanceToOwner <= 3)
                {
                    projSimple.lifetime = 0.0001f;

                }
                UpdateHitStop();
            }
        }
        public IEnumerator FlyBack()
        {
            //Log.Debug("[HOOK] Flyback Start");

            isFlying = true; //aka is being recalled

            hookCollider.enabled = true;
            hookCollider.gameObject.layer = LayerIndex.noCollision.intVal;

            //reset rigid body for motion (previously frozen on impact)
            rb.velocity = Vector3.zero;
            rb.useGravity = false;
            stickComponent.Detach();
            rb.detectCollisions = true;
            stickComponent.ignoreCharacters = true;
            stickComponent.ignoreWorld = true;
            rb.isKinematic = false;

            yield return new WaitForFixedUpdate();

            foreach(HookBombController bomb in bombsToThrow)
            {
                if (bomb) StartCoroutine(ThrowHookBomb(bomb));

            }

            //apply return force to hook 
            projSimple.desiredForwardSpeed = 0;
            Vector3 vel = (ownerTransform.position - transform.position).normalized * rb.mass;
            rb.AddForce(vel, ForceMode.VelocityChange);
            

            // Enable Hitboxes
            projOverlap.enabled = true;
            projectileDamage.damage =  objTracker.characterBody.damage * FishermanStaticValues.castDamageCoefficient;
            projOverlap.damageCoefficient = 1;

            float startWidth = lineRenderer.startWidth;
            float endWidth = lineRenderer.endWidth;

            lineRenderer.startWidth *= 10f;
            lineRenderer.endWidth *= 10f;
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;

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
            //if (collision.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] Hit bounds box: col enter");
           // Log.Debug($"[HOOK] Collision Enter {collision.gameObject.name} C<==");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if(!isFlying && hookBomb)
            {
                bombsToThrow.Add(hookBomb);
            }
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                StartCoroutine(ThrowHookBomb(hookBomb));
            }
        }
        void OnCollisionExit(UnityEngine.Collision collision)
        {
           // Log.Debug($"[HOOK] Collision Exit {collision.gameObject.name} C==>");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                StartCoroutine(ThrowHookBomb(hookBomb));
            }
        }

        void OnTriggerEnter(Collider collider)
        {
            //if (collider.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] Hit bounds box: trig enter");
           // Log.Debug($"[HOOK] Trigger Enter {collider.gameObject.name} T<==");

            if (!CanThrow(collider.gameObject)) return;
            HookBombController hookBomb = collider.transform.parent?.GetComponent<HookBombController>();
            if (!isFlying && hookBomb)
            {
                bombsToThrow.Add(hookBomb);
            }
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                StartCoroutine(ThrowHookBomb(hookBomb));
            }
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void OnTriggerExit(Collider collider)
        {
            //if (collider.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] exit bounds box: trig exit");
           // Log.Debug($"[HOOK] Trigger Exit {collider.gameObject.name} T==>");
            if (!CanThrow(collider.gameObject)) return;
            HookBombController hookBomb = collider.transform.parent?.GetComponent<HookBombController>();
            if (!isFlying && hookBomb)
            {
                bombsToThrow.Add(hookBomb);
            }
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                StartCoroutine(ThrowHookBomb(hookBomb));
            }
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }

        bool ThrowItem(Collider collider)
        {
            
            if (collider.gameObject.name == "GenericPickup(Clone)")
            {
                ////Log.Debug($"[HOOK] Item Hit, {collider.gameObject.name}");
                Rigidbody itemBody = collider.gameObject.GetComponent<Rigidbody>();
                itemBody.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position,itemBody.position,false), ForceMode.VelocityChange);
                onHookEvent.Invoke(itemBody.gameObject);
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
             
                ////Log.Debug($"{eLoc.entity.name}: Can be grabbed?: {isGrabbable}");
                if (!isGrabbable) return;
                InteractableStopOnImpact stopper = eLoc.entity.AddComponent<InteractableStopOnImpact>();
                stopper.rb = eLoc.entity.AddComponent<Rigidbody>();
                stopper.rb.mass = 300;
                
                stopper.collider = eLoc.entity.AddComponent<SphereCollider>();
                stopper.collider.radius = 0.5f;
                //interactableBody.useGravity = false;
                stopper.rb.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, stopper.rb.position, false), ForceMode.VelocityChange);
                onHookEvent.Invoke(stopper.gameObject);

            }

        }
        IEnumerator ThrowHookBomb(HookBombController bomb)
        {
            if (bomb)
            {
                //Log.Debug("[HOOK] Trying to throw bomb");
                bool wasStuck = bomb.stickComponent.stuck;
                bomb.stickComponent.Detach();
                //bomb.DisableAllColliders();
                bomb.stickComponent.enabled = false;
                bomb.body.isKinematic = false;
                bomb.body.drag = 0;
                bomb.antiGrav.enabled = false;
                //bomb.body.angularDrag = 0.05f;
                //bomb.body.mass = 120;
                yield return new WaitForFixedUpdate();

                bomb.body.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, bomb.transform.position, !wasStuck), ForceMode.VelocityChange);
                bomb.wasStuckByHook = false;
                //bomb needs to hold coroutine because this hook will be destroyed before it can complete.
                bomb.StartCoroutine(bomb.ResetPhysics());
            }
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
            //Log.Debug("[HOOK] HOOK DESTROYED ------------------------------------------------------------------------------------------------------------");

            //FishermanSurvivor.SetDeployedHook(null);
        }

        void SpawnRadar(GameObject obj)
        {
            EffectData effectData = new EffectData();
            effectData.origin = obj.transform.position;
            EffectManager.SpawnEffect(FishermanAssets.hookScannerPrefab, effectData, true);
        }

        bool inHitPause;
        float hitStopDuration = 0.05f;
        Vector3 storedVelocity;
        HitStopCachedState hitStopCachedState;
        float hitPauseTimer;
        string playbackRateParam = "SecondaryCast.playbackRate";

        void ApplyHitStop(GameObject gameObject)
        {
            if (!inHitPause && FishermanStaticValues.CurHitStop > 0f)
            {
                storedVelocity = objTracker.characterMotor.velocity;
                hitStopCachedState = CreateHitStopCachedState(objTracker.characterMotor, objTracker.animator, playbackRateParam);
                hitPauseTimer = FishermanStaticValues.CurHitStop / objTracker.characterBody.attackSpeed;
                inHitPause = true;
            }
        }

        protected void UpdateHitStop()
        {
            hitPauseTimer -= Time.fixedDeltaTime;

            if (hitPauseTimer <= 0f && inHitPause)
            {
                RemoveHitstop();
            }

            if(inHitPause)
            {
                objTracker.characterMotor.velocity = Vector3.zero;
                objTracker.animator.SetFloat(playbackRateParam, 0f);
            }
        }

        private void RemoveHitstop()
        {
            ConsumeHitStopCachedState(hitStopCachedState, objTracker.characterMotor, objTracker.animator);
            inHitPause = false;
            objTracker.characterMotor.velocity = storedVelocity;
            FishermanStaticValues.hitStopMod = 1;
        }

        protected HitStopCachedState CreateHitStopCachedState(CharacterMotor characterMotor, Animator animator, string playbackRateAnimationParameter)
        {
            HitStopCachedState result = default(HitStopCachedState);
            result.characterVelocity = new Vector3(characterMotor.velocity.x, Mathf.Max(0f, characterMotor.velocity.y), characterMotor.velocity.z);
            result.playbackName = playbackRateAnimationParameter;
            result.playbackRate = animator.GetFloat(result.playbackName);
            return result;
        }

        protected void ConsumeHitStopCachedState(HitStopCachedState hitStopCachedState, CharacterMotor characterMotor, Animator animator)
        {
            characterMotor.velocity = hitStopCachedState.characterVelocity;
            animator.SetFloat(hitStopCachedState.playbackName, hitStopCachedState.playbackRate);
        }
    }

}
