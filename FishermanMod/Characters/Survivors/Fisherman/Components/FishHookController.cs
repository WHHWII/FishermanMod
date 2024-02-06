using System;
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

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class FishHookController : MonoBehaviour
    {
        public Rigidbody rb;
        public ProjectileStickOnImpact stickComponent;
        public ProjectileController controller;
        public ProjectileDamage projectileDamage;
        public CapsuleCollider collider;

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

        HashSet<Collider> collidersHooked = new HashSet<Collider>();
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
                        Destroy(gameObject);
                    }
                }
                
            }
        }

        public void FlyBack()
        {
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
        
        void OnCollisionExit(UnityEngine.Collision collision)
        {
            ThrowMob(collision);
        }
        void OnCollisionEnter(UnityEngine.Collision collision)
        {
            //stickComponent.TrySticking(collision.collider, Vector3.zero);
            ThrowMob(collision);
        }
        void OnTriggerExit(Collider collider)
        {
            ThrowItem(collider);
        }
        void OnTriggerEnter(Collider collider)
        {
            ThrowItem(collider);
        }
        void ThrowMob(UnityEngine.Collision collision)
        {
            if (!isFlying) return;
            if (collidersHooked.Contains(collider)) return;
            //Log.Debug("Hit something");
            Log.Debug($" Hook impacted {collision.gameObject.name}");
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
                FishermanSurvivor.ApplyFishermanPassiveFishHookEffect(ownerTransform.gameObject, ownerTransform.gameObject, projectileDamage.damage, ownerTransform.position, target);
                collidersHooked.Add(collider);
            }
        }
        void ThrowItem(Collider collider)
        {
            if (!isFlying) return;
            
            Log.Debug(collider.gameObject.name);
            if (collider.gameObject.name == "GenericPickup(Clone)")
            {
                if (collidersHooked.Contains(collider)) return;
                Rigidbody itemBody = collider.gameObject.GetComponent<Rigidbody>();
                itemBody.AddForce(CalculateReturnForce(returnForceBase, itemBody,true), ForceMode.Impulse);
                collidersHooked.Add(collider);
            }

        }
    }
}
