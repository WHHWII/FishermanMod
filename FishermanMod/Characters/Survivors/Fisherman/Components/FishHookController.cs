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
    // NETWORKING: the hook is a projectile, so the server simulates it and clients only see its synced transform.
    // Everything that changes game state (catching/throwing objects, recall physics, damage, destroying the hook) is server-only,
    // otherwise every client would run its own copy of the catch logic off slightly different positions.
    // Clients still run the cosmetic parts (fishing line, recall flash) and the owner applies hitstop to their own character.
    public class FishHookController : NetworkBehaviour
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

        void Start()
        {
            //grappleOwnerRef.enabled = false;
            // NETWORKING: controller.owner is synced, but can still be missing on a client if the owner died the same frame
            if (!controller.owner)
            {
                if (NetworkServer.active) Destroy(gameObject);
                enabled = false;
                return;
            }
            ownerTransform = controller.owner.transform;
            objTracker = ownerTransform.GetComponent<SkillObjectTracker>();
            objTracker.deployedHooks.Add(this);

            // NETWORKING: the hook indicator is HUD clutter for anyone but the Fisherman who threw it.
            // Moved here from Awake because the owner is only known once ProjectileController has synced it.
            if (Util.HasEffectiveAuthority(controller.owner))
            {
                GameObject hi = Instantiate(FishermanAssets.hookIndicator, transform);
                hi.GetComponent<PositionIndicator>().targetTransform = transform;
                hi.transform.position = Vector3.zero;
            }

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
            if (!objTracker || !objTracker.fishingPoleTip) return;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, Vector3.Lerp(transform.position, objTracker.fishingPoleTip.position, 0.5f) + (Vector3.up * 0.1f));
            lineRenderer.SetPosition(2, objTracker.fishingPoleTip.position);
        }

        void FixedUpdate()
        {
            // NETWORKING: hitstop is applied on the owner's machine (see RpcApplyHitStop), so tick it wherever it is active
            if (inHitPause) UpdateHitStop();

            // NETWORKING: everything below moves/destroys the projectile, which only the server may do.
            // Clients get the result through the projectile's network transform, and Destroy on the server despawns it everywhere.
            if (!NetworkServer.active) return;

            if (controller.owner == null)
            {
                Destroy(gameObject);
                return;
            }
            distanceToOwner = Vector3.Distance(transform.position, ownerTransform.position);
            if (!isFlying && distanceToOwner > autoTriggerDistance)
            {
                Recall();
            }
            if (isFlying)
            {
                transform.LookAt(transform.position - (ownerTransform.position - transform.position));
                //transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime);
                timeFlying += Time.fixedDeltaTime;
                // if hook is near player or has been flying for a long time, engange homing to force the hook to quickly return
                if ((distanceToOwner <= homeToBodyDistance && timeFlying >= minTimeBeforeReturning) || timeFlying >= maxFlyTime)
                {
                    Vector3 vel = (ownerTransform.position - transform.position).normalized * rb.mass * Mathf.Max(homingForce - distanceToOwner, 1) * timeFlying;
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
                if (distanceToOwner <= 3)
                {
                    projSimple.lifetime = 0.0001f;

                }
            }
        }

        // NETWORKING: entry point for recalling, server only. Starts the physics side here and tells clients to play the cosmetic side.
        [Server]
        public void Recall()
        {
            if (isFlying) return;
            StartCoroutine(FlyBack());
            RpcOnRecalled();
        }

        [ClientRpc]
        void RpcOnRecalled()
        {
            // the host already ran FlyBack (which includes the line flash) through Recall()
            if (NetworkServer.active) return;
            isFlying = true;
            StartCoroutine(RecallLineFlash());
        }

        IEnumerator RecallLineFlash()
        {
            float startWidth = lineRenderer.startWidth;
            float endWidth = lineRenderer.endWidth;

            lineRenderer.startWidth *= 10f;
            lineRenderer.endWidth *= 10f;
            for (int i = 0; i < 5; i++) yield return new WaitForEndOfFrame();
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
        }

        // server only, start it through Recall() so clients get the cosmetic side too
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

            foreach (HookBombController bomb in bombsToThrow)
            {
                if (bomb) StartCoroutine(ThrowHookBomb(bomb));

            }

            //apply return force to hook 
            projSimple.desiredForwardSpeed = 0;
            Vector3 vel = (ownerTransform.position - transform.position).normalized * rb.mass;
            rb.AddForce(vel, ForceMode.VelocityChange);


            // Enable Hitboxes
            projOverlap.enabled = true;
            projectileDamage.damage = objTracker.characterBody.damage * FishermanStaticValues.castDamageCoefficient;
            projOverlap.damageCoefficient = 1;

            yield return RecallLineFlash();
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
            // NETWORKING: catching things is server-only, clients would otherwise throw the same object a second time
            if (!NetworkServer.active) return;
            //if (collision.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] Hit bounds box: col enter");
            Log.Debug($"[HOOK] Collision Enter {collision.gameObject.name} C<==");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if (!isFlying && hookBomb)
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
            // NETWORKING: catching things is server-only, clients would otherwise throw the same object a second time
            if (!NetworkServer.active) return;
            Log.Debug($"[HOOK] Collision Exit {collision.gameObject.name} C==>");
            HookBombController hookBomb = collision.gameObject.GetComponent<HookBombController>();
            if (hookBomb && CanThrow(hookBomb.gameObject))
            {
                StartCoroutine(ThrowHookBomb(hookBomb));
            }

        }

        void OnTriggerEnter(Collider collider)
        {
            // NETWORKING: catching things is server-only, clients would otherwise throw the same object a second time
            if (!NetworkServer.active) return;
            //if (collider.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] Hit bounds box: trig enter");
            Log.Debug($"[HOOK] Trigger Enter {collider.gameObject.name} T<==");

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
            if (collider.gameObject.name.Contains("Shanty")) //todo the uhh not the dread stringy
            {
                ThrowOwner(collider.gameObject);
            }
            else
            {
                Log.Debug("ally enter attempt");
                TryThrowAlly(collider);
            }
        }
        void OnTriggerExit(Collider collider)
        {
            // NETWORKING: catching things is server-only, clients would otherwise throw the same object a second time
            if (!NetworkServer.active) return;
            //if (collider.gameObject.GetComponent<MapZone>()) //Log.Debug("[HOOK] exit bounds box: trig exit");
            //Log.Debug($"[HOOK] Trigger Exit {collider.gameObject.name} T==>");
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
            if (collider.gameObject.name.Contains("Shanty")) //todo the uhh not the dread stringy
            {
                ThrowOwner(collider.gameObject);
            }


        }

        bool TryThrowAlly(Collider collider)
        {
            if (collider.gameObject != objTracker.gameObject && collider.gameObject.TryGetComponent(out CharacterBody cb))
            {
                Log.Debug($"Hook found hurtbox: {cb.gameObject.name} ");
                Log.Debug($"Hb.Team: {cb.teamComponent.teamIndex}");

                if (cb.teamComponent.teamIndex == TeamIndex.Player)
                {
                    Log.Debug($"attempting to throw ally");


                    bool hasMotor = cb.characterMotor;
                    bool isGrounded = hasMotor && cb.characterMotor.isGrounded;
                    bool isTurret = cb.gameObject.name.Contains("Turret");
                    bool flyer = (hasMotor && !isGrounded) && !isTurret;
                    
                    Vector3 force = FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, cb.transform.position, flyer);



                    objTracker.StartCoroutine(FishermanSurvivor.FireHookedObjectProjectile(cb.gameObject, objTracker.gameObject, FishermanSurvivor.GetBodyMass(cb), force, flyer));
                    return true;
                }

            }
            return false;
        }

        bool ThrowItem(Collider collider)
        {

            if (collider.gameObject.name == "GenericPickup(Clone)")
            {
                ////Log.Debug($"[HOOK] Item Hit, {collider.gameObject.name}");
                Rigidbody itemBody = collider.gameObject.GetComponent<Rigidbody>();
                Vector3 force = FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, itemBody.position, false);
                //itemBody.AddForce(force, ForceMode.VelocityChange);
                float dmg = objTracker.characterBody.baseDamage * FishermanStaticValues.thrownInteractableCoef;

                objTracker.StartCoroutine(FishermanSurvivor.FireHookedObjectProjectile(collider.gameObject, objTracker.gameObject, dmg, force, false));

                onHookEvent.Invoke(itemBody.gameObject);
                return true;
            }
            return false;

        }
        void ThrowInteractable(Collider collider)
        {

            EntityLocator eLoc = collider.gameObject.GetComponent<EntityLocator>();

            if (eLoc != null && eLoc.entity.TryGetComponent(out SpecialObjectAttributes sobat) && sobat.grabbable)
            {
                //bool isGrabbable = FishermanSurvivor.CheckIfInteractableIsGrabable(eLoc.entity.name);

                //////Log.Debug($"{eLoc.entity.name}: Can be grabbed?: {isGrabbable}");
                //if (!isGrabbable) return;
                //InteractableStopOnImpact stopper = eLoc.entity.AddComponent<InteractableStopOnImpact>();
                //stopper.rb = eLoc.entity.AddComponent<Rigidbody>();
                //stopper.rb.mass = 300;

                //stopper.collider = eLoc.entity.AddComponent<SphereCollider>();
                //stopper.collider.radius = 0.5f;
                ////interactableBody.useGravity = false;
                //stopper.rb.AddForce(FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, stopper.rb.position, false), ForceMode.VelocityChange);
                Vector3 force = FishermanSurvivor.GetHookThrowVelocity(ownerTransform.position, eLoc.entity.transform.position, false);
                float dmg = objTracker.characterBody.baseDamage * FishermanStaticValues.thrownInteractableCoef;
                objTracker.StartCoroutine(FishermanSurvivor.FireHookedObjectProjectile(eLoc.entity, objTracker.gameObject, dmg, force, false));
                onHookEvent.Invoke(eLoc.entity);

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
        void OnDestroy()
        {
            //Log.Debug("[HOOK] HOOK DESTROYED ------------------------------------------------------------------------------------------------------------");
            // keeps the owner's list (and the hook viewer UI on every client) from holding destroyed hooks
            if (objTracker) objTracker.deployedHooks.Remove(this);

            //FishermanSurvivor.SetDeployedHook(null);
        }

        void SpawnRadar(GameObject obj)
        {
            // NETWORKING: the effect is transmitted to every client, so only the server spawns it.
            // OnStickEvent also fires on clients (ProjectileStickOnImpact syncs its stuck state), which would double it up.
            if (!NetworkServer.active) return;
            EffectData effectData = new EffectData();
            effectData.origin = obj.transform.position;
            EffectManager.SpawnEffect(FishermanAssets.hookScannerPrefab, effectData, true);
        }



        #region hitstop
        bool inHitPause;
        float hitStopDuration = 0.05f;
        Vector3 storedVelocity;
        HitStopCachedState hitStopCachedState;
        float hitPauseTimer;
        string playbackRateParam = "SecondaryCast.playbackRate";

        // NETWORKING: hits are found on the server, but hitstop freezes the owner's own movement and animator,
        // so it has to run where the owner's body is simulated (their client, or the server for the host/AI).
        void ApplyHitStop(GameObject gameObject)
        {
            if (!NetworkServer.active || !objTracker) return;
            if (objTracker.characterBody.hasEffectiveAuthority)
            {
                ApplyHitStopLocal();
            }
            else
            {
                RpcApplyHitStop();
            }
        }

        [ClientRpc]
        void RpcApplyHitStop()
        {
            if (NetworkServer.active || !objTracker || !objTracker.characterBody.hasEffectiveAuthority) return;
            ApplyHitStopLocal();
        }

        // FishermanStaticValues.hitStopMod is read locally on purpose: it's set by the hook-fail jolt (SkillObjectTracker.ApplyHookFeedback),
        // which also runs on the owner's machine, so the server's copy (which belongs to the host) never leaks into a remote player's hitstop.
        void ApplyHitStopLocal()
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

            if (inHitPause)
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
        #endregion hitstop
        void ThrowOwner(GameObject targetYanker)
        {
            if (Vector3.Distance(ownerTransform.position, targetYanker.transform.position) < 10)
            {
                return;
            }
            bool isFlyer = false; //objTracker.characterMotor.isGrounded && (collision.gameObject.transform.position.y < ownerTransform.position.y);
            Vector3 force = FishermanSurvivor.GetHookThrowVelocity(targetYanker.transform.position, objTracker.characterBody.footPosition, isFlyer);
            Log.Debug($"endpos: {targetYanker.transform.position} startpos: {ownerTransform.position} force: {force}");

            objTracker.StartCoroutine(FishermanSurvivor.FireHookedObjectProjectile(ownerTransform.gameObject, targetYanker, objTracker.characterBody.rigidbody.mass, force, isFlyer, ownerTransform.gameObject));
            /*

            Vector3 ownerPos = ownerTransform.position;
            Vector3 destination = target.transform.position;
            Vector3 directionToTarget = (destination - ownerPos).normalized;
            Vector3 targetGroundPos = destination;
            targetGroundPos.y = ownerPos.y;
            Vector3 directionToGroundTarget = (targetGroundPos - ownerPos);

            float angle = Vector3.Angle(directionToTarget, directionToGroundTarget);
            destination.y += angle * 0.01f + 0.25f;


            Log.Debug("AngleToTarget = " + angle);
            float timeToTarget = Mathf.Clamp(angle * 0.1f, 2, 3);
            Vector3 throwVelocity = FishermanSurvivor.GetHookThrowVelocity(destination, ownerPos, isFlyer, timeToTarget, 5 + timeToTarget);
            if (objTracker.characterMotor)
            {
                if (objTracker.characterMotor.isGrounded) objTracker.characterMotor.Motor.ForceUnground();
                if (!isFlyer) objTracker.characterMotor.disableAirControlUntilCollision = true;
                objTracker.characterMotor.velocity = Vector3.zero;
                objTracker.characterMotor.velocity = throwVelocity * 1.1f;
            }
            objTracker.StartCoroutine(ThrowOnDelay());
            IEnumerator ThrowOnDelay()
            {
                Log.Debug("Enter self throw corout");
                yield return new WaitForEndOfFrame();
                yield return new WaitForFixedUpdate();
                Log.Debug(" self throw corout");
                Log.Debug($"init vel: {throwVelocity.normalized}");

            }
            */
        }
    }

}
