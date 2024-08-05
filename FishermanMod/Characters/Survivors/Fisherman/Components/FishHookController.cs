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
            FishermanSurvivor.SetDeployedHook(this);
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
                if((distanceToOwner <= homeToBodyDistance && timeFlying >= minTimeBeforeReturning) || timeFlying >= maxFlyTime)
                {
                    // rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, 0.1);
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

            //if(tauntedAITimers.Count > 0 && canPollTauntedEnemiesForRelease)
            //{
            //    ClearAgrro();
            //    StartCoroutine(TauntReleasePollTimer());
            //}
        }
        //IEnumerator TauntReleasePollTimer()
        //{
        //    yield return new WaitForSeconds(maxTauntTime);
        //    canPollTauntedEnemiesForRelease = true;
        //}
        public void FlyBack()
        {
            //ClearAgrro();
            //enemyTaunter.SetActive(false);
            //Log.Debug("Flyback engaged");
            //collider.enabled = false;
            projectileDamage.damage = FishermanSurvivor.instance.bodyInfo.damage * FishermanStaticValues.castDamageCoefficient;

            rb.velocity = Vector3.zero;

            rb.drag = 0;
            rb.angularDrag = 0.05f;
            rb.mass = 100;
            rb.useGravity = true;

            isFlying = true;
            stickComponent.enabled = false;
            rb.isKinematic = false;
            if (FishermanSurvivor.deployedHookBomb && FishermanSurvivor.deployedHookBomb.wasStuckByHook)
            {
                ThrowHookBomb();
            }
            if (FishermanSurvivor.deployedPlatform && FishermanSurvivor.deployedPlatform.wasStuckByHook)
            {
                
            }
            rb.AddForce(CalculateReturnForce(returnForceBase, rb), ForceMode.Impulse);
            projOverlap.enabled = true;
            projOverlap.damageCoefficient = FishermanStaticValues.castDamageCoefficient;
            //projOverlap.ResetOverlapAttack();
        }

        private Vector3 CalculateReturnForce(float baseForce, Rigidbody rbody, bool dampPower = false)
        {
            //TODO: Add This function to fishermansurvivor and standardized what i can
            //TODO: Create custom bonus power curve for items instead of power damper.
            float powerDamper = 0.70f;
            Vector3 targetPos = ownerTransform.position;
            float bodyMass = (rbody ? rbody.mass : 1f);
            //targetPos.y += distanceToOwner * 0.45f;
            //Vector3 direction = (targetPos - transform.position).normalized;
            //float massAdjForce = baseForce + bodyMass;
            //return massAdjForce * distanceToOwner * direction;
            Vector3 startPosition = transform.position;
            float dist = Vector3.Distance(startPosition, targetPos);

            Vector3 distanceVector = (targetPos - startPosition);
            Vector3 halfDistVec = distanceVector * 0.5f;
            Vector3 centerAdjEPos = halfDistVec + startPosition;
            Vector3 hookTarget = centerAdjEPos;
            hookTarget.y += dist * 0.5f;
            Vector3 newDistanceVector = (hookTarget - startPosition);
            //float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 262, 1.1f) - 55, 1, 5); //this one is really good
            float bonusPower = Mathf.Clamp(Mathf.Log(-dist + 312, 1.1f) - 57.2f, 1, 5);
            // if (isFlyer) { bonusPower += 0.1f; }
            Vector3 force = newDistanceVector * bodyMass * bonusPower ;
            if (dampPower)
            {
                force *= powerDamper;
            }
            return force;
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
            if (collision.gameObject.GetComponent<MapZone>()) Log.Debug("Hit bounds box: col enter");
            //collision.rigidbody.velocity = Vector3.zero;
            Log.Debug($"Collision Enter {collision.gameObject.name}");

            if ((FishermanSurvivor.deployedHookBomb) && collision.gameObject == FishermanSurvivor.deployedHookBomb.gameObject)
            {
                Log.Debug($"[hookcontroller] enter bomb col");
                FishermanSurvivor.deployedHookBomb.wasStuckByHook = true;
            }
            if ((FishermanSurvivor.deployedPlatform) && collision.gameObject == FishermanSurvivor.deployedPlatform.gameObject)
            {
                Log.Debug($"[hookcontroller] enter platform col");
                FishermanSurvivor.deployedPlatform.wasStuckByHook = true;
            }
        }
        void OnTriggerExit(Collider collider)
        {
            if (collider.gameObject.GetComponent<MapZone>()) Log.Debug("Hit bounds box: trig exit");
            //Log.Debug($"Trigger Exit {collider.gameObject.name}");
            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void OnTriggerEnter(Collider collider)
        {
            if (collider.gameObject.GetComponent<MapZone>()) Log.Debug("Hit bounds box: trig enter");
            //Log.Debug($"Trigger Enter {collider.gameObject.name}");

            //DrawAggro(collider);
            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        bool ThrowItem(Collider collider)
        {
            
            if (collider.gameObject.name == "GenericPickup(Clone)")
            {
                Log.Debug($"Item Hit, {collider.gameObject.name}");
                Rigidbody itemBody = collider.gameObject.GetComponent<Rigidbody>();
                itemBody.AddForce(CalculateReturnForce(returnForceBase, itemBody), ForceMode.Impulse);
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
                stopper.rb.AddForce(CalculateReturnForce(returnForceBase, stopper.rb), ForceMode.Impulse);
                
            }

        }
        void ThrowHookBomb()
        {
            Log.Debug("[HookController] Trying to throw bomb");
            FishermanSurvivor.deployedHookBomb.stickComponent.Detach() ;
            FishermanSurvivor.deployedHookBomb.DisableAllColliders();
            FishermanSurvivor.deployedHook.stickComponent.enabled = false;
            FishermanSurvivor.deployedHookBomb.body.isKinematic = false;
            FishermanSurvivor.deployedHookBomb.body.drag = 0;
            FishermanSurvivor.deployedHookBomb.body.angularDrag = 0.05f;
            FishermanSurvivor.deployedHookBomb.body.mass = 120;
            FishermanSurvivor.deployedHookBomb.body.AddForce(CalculateReturnForce(returnForceBase, FishermanSurvivor.deployedHookBomb.body), ForceMode.Impulse);
            FishermanSurvivor.deployedHookBomb.wasStuckByHook = false;
            StartCoroutine(FishermanSurvivor.deployedHookBomb.ResetStickyComponent());
        }
        void ThrowPlatform()
        {
            FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(ownerTransform.gameObject,
                ownerTransform.gameObject,
                0,
                ownerTransform.position,
                FishermanSurvivor.deployedPlatform.characterBody.mainHurtBox);
        }
        void OnDestroy()
        {
            FishermanSurvivor.SetDeployedHook(null);
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
