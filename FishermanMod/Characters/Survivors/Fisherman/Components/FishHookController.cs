﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using EntityStates.Toolbot;
using FishermanMod.Survivors.Fisherman;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine.XR.WSA;
using static UnityEngine.UI.GridLayoutGroup;
using static UnityEngine.ParticleSystem.PlaybackState;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using IL.RoR2.CharacterAI;
using On.RoR2.CharacterAI;
using System.Collections;

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class FishHookController : MonoBehaviour
    {
        public Rigidbody rb;
        public ProjectileStickOnImpact stickComponent;
        public ProjectileController controller;
        public ProjectileDamage projectileDamage;
        public CapsuleCollider collider;
        public GameObject enemyTaunter;
        public HurtBox hookHurtBox;
        public CharacterBody hookBody;

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

        float maxTauntTime = 5f;
        bool canPollTauntedEnemiesForRelease = true;

        HashSet<GameObject> objectsHooked = new HashSet<GameObject>();
        HashSet<GameObject> enemiesTaunted = new HashSet<GameObject>();
        Dictionary<RoR2.CharacterAI.BaseAI, float> tauntedAITimers = new Dictionary<RoR2.CharacterAI.BaseAI, float>();

        void Start()
        {
            //grappleOwnerRef.enabled = false;
            ownerTransform = controller.owner.transform;
            FishermanSurvivor.SetDeployedHook(this);
            
            
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

            if(tauntedAITimers.Count > 0 && canPollTauntedEnemiesForRelease)
            {
                ClearAgrro();
                StartCoroutine(TauntReleasePollTimer());
            }
        }
        IEnumerator TauntReleasePollTimer()
        {
            yield return new WaitForSeconds(maxTauntTime);
            canPollTauntedEnemiesForRelease = true;
        }
        public void FlyBack()
        {
            ClearAgrro();
            enemyTaunter.SetActive(false);
            //Log.Debug("Flyback engaged");
            projectileDamage.damage = FishermanSurvivor.instance.bodyInfo.damage * FishermanStaticValues.gunDamageCoefficient;
            collider.radius += 1f;
            rb.velocity = Vector3.zero;
            isFlying = true;
            stickComponent.enabled = false;
            rb.isKinematic = false;
            rb.AddForce(CalculateReturnForce(returnForceBase, rb), ForceMode.Impulse);
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
        bool CanTaunt(GameObject gameObject)
        {
            if(enemiesTaunted.Contains(gameObject)) return false;
            enemiesTaunted.Add(gameObject);
            return true;
        }
        void OnCollisionExit(UnityEngine.Collision collision)
        {
            //Log.Debug($"Collision Exit {collision.gameObject.name}");
            if (!CanThrow(collision.gameObject)) return;
            ThrowMob(collision);
        }
        void OnCollisionEnter(UnityEngine.Collision collision)
        {
            //Log.Debug($"Collision Enter {collision.gameObject.name}");
            //if (hookHurtBox == null) hookHurtBox = gameObject.AddComponent<HurtBox>();
            //stickComponent.TrySticking(collision.collider, Vector3.zero);
            if (!CanThrow(collision.gameObject)) return;
            ThrowMob(collision);
        }
        void OnTriggerExit(Collider collider)
        {
            //Log.Debug($"Trigger Exit {collider.gameObject.name}");
            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void OnTriggerEnter(Collider collider)
        {
            //Log.Debug($"Trigger Enter {collider.gameObject.name}");

            DrawAggro(collider);
            if (!CanThrow(collider.gameObject)) return;
            if (ThrowItem(collider)) return;
            ThrowInteractable(collider);
        }
        void ThrowMob(UnityEngine.Collision collision)
        {
            //Log.Debug("Hit something");
            //Log.Debug($" Hook impacted {collision.gameObject.name}");
            HurtBox target = collision.gameObject.GetComponent<HurtBox>();
            if (target != null)
            {
                
                DamageInfo FlyAttackDamage = new DamageInfo
                {
                    attacker = controller.owner,
                    inflictor = gameObject,
                    damage = projectileDamage.damage,
                    damageType = DamageType.NonLethal,
                    damageColorIndex = DamageColorIndex.Default,
                    procCoefficient = 1,
                    procChainMask = default(ProcChainMask),

                };
                target.healthComponent.TakeDamage(FlyAttackDamage);
                GlobalEventManager.instance.OnHitEnemy(FlyAttackDamage, target.gameObject);
                GlobalEventManager.instance.OnHitAll(FlyAttackDamage, target.gameObject);
                //Log.Debug("\t a mob!");
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(ownerTransform.gameObject, gameObject, projectileDamage.damage, ownerTransform.position, target);
            }
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
                stopper.rb.mass = 150;
                
                stopper.collider = eLoc.entity.AddComponent<SphereCollider>();
                stopper.collider.radius = 0.5f;
                //interactableBody.useGravity = false;
                stopper.rb.AddForce(CalculateReturnForce(returnForceBase, stopper.rb), ForceMode.Impulse);
                
            }

        }

        void DrawAggro(Collider collider)
        {
            if (CanTaunt(collider.gameObject))
            {
                //Log.Debug($"-Can Taunt {collider.gameObject.name}");
                TeamComponent team = collider.GetComponent<TeamComponent>();
                if (team != null)
                {
                   // if (hookHurtBox == null) return;
                    //Log.Debug($"{collider.gameObject.name}'s Team =  {team.teamIndex}");
                    if (team.teamIndex != controller.owner.GetComponent<TeamComponent>().teamIndex)
                    {
                        CharacterBody body = collider.gameObject.GetComponent<CharacterBody>();
                        body.AddTimedBuff(FishermanBuffs.hookTauntDebuff, maxTauntTime);
                        body.healthComponent.dontShowHealthbar = false;
                        //body.healt
                        //Log.Debug($"{collider.gameObject.name} is on a different team than hook owner");
                        //RoR2.UI.CombatHealthBarViewer. need to see about making this force show health bars
                        //that or make an effect similiar to death mark
                        foreach (RoR2.CharacterAI.BaseAI ai in body.master.aiComponents)
                        {
                            if (ai != null)
                            {
                                //Log.Debug($"{collider.gameObject.name} Ai located, setting target");
                                //RoR2.CharacterAI.BaseAI.Target newTarget = new RoR2.CharacterAI.BaseAI.Target(controller.owner.GetComponent<CharacterBody>().master.GetComponent<RoR2.CharacterAI.BaseAI>());
                                //newTarget.gameObject = gameObject;
                                //newTarget._gameObject = gameObject;
                                //newTarget.characterBody = 
                                //ai.currentEnemy.Reset();
                                ai.currentEnemy.gameObject = gameObject;
                                ai.currentEnemy.bestHurtBox = null;
                                ai.enemyAttention = ai.enemyAttentionDuration;
                                ai.targetRefreshTimer = 1;
                                ai.BeginSkillDriver(ai.EvaluateSkillDrivers());
                                tauntedAITimers.Add(ai, Time.time);
                                
                            }


                        }



                    }
                }




            }
        }

        void ClearAgrro()
        {
            //TODO properly clear debuff
            if (tauntedAITimers.Count <= 0) return;
            List<RoR2.CharacterAI.BaseAI> aisToRemove = new List<RoR2.CharacterAI.BaseAI>();
            foreach (var aiTimer in tauntedAITimers)
            {
                if (Time.time - aiTimer.Value > maxTauntTime)
                {
                    if(aiTimer.Key == null)
                    {
                        aisToRemove.Add(aiTimer.Key);
                        continue;
                    }
                    //Log.Debug($"Releasing: {aiTimer.Key.gameObject.name}");
                    aiTimer.Key.currentEnemy.gameObject = controller.owner.gameObject;
                    aiTimer.Key.currentEnemy.bestHurtBox = controller.owner.GetComponent<CharacterBody>().mainHurtBox;
                    aiTimer.Key.BeginSkillDriver(aiTimer.Key.EvaluateSkillDrivers());
                    //aiTimer.Key.gameObject.GetComponent<CharacterBody>().RemoveBuff(FishermanBuffs.hookTauntDebuff);
                    aisToRemove.Add(aiTimer.Key);
                }
                else
                {
                    //aiTimer.Key.gameObject.GetComponent<CharacterBody>().AddTimedBuff(FishermanBuffs.hookTauntDebuff, maxTauntTime);
                }
            }
            foreach (var ai in aisToRemove)
            {
                tauntedAITimers.Remove(ai);
            }
            canPollTauntedEnemiesForRelease = false;
        }
    }
}
