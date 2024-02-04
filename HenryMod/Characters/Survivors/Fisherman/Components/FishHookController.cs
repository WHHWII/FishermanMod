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

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class FishHookController : MonoBehaviour
    {
        public Rigidbody rb;
        public ProjectileStickOnImpact stickComponent;
        public ProjectileController controller;
        EntityStateMachine weaponStateMachine;
        bool isFlying = false;
        bool isReturning = false;
        float distanceToOwner;
        float autoTriggerDistance = 600;
        float homeToBodyDistance = 50;
        float homingForce = 0.01f;
        float homingDeceleration = 0.33f;
        float returnForceBase = 50;
        Transform ownerTransform;
        float timeFlying = 0;
        float minTimeBeforeReturning = .5f;
        float maxFlyTime = 2;
        void Start()
        {
            //grappleOwnerRef.enabled = false;
            ownerTransform = controller.owner.transform;
            FishermanSurvivor.SetDeployedHook(this);
        }
        void FixedUpdate()
        {
            distanceToOwner = Vector3.Distance(transform.position, ownerTransform.position);
            if (!isFlying && distanceToOwner > autoTriggerDistance) 
            {
                FlyBack();
            }
            if (isFlying)
            {
                timeFlying += Time.fixedDeltaTime;
                if (controller != null)
                {
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
        }

        public void FlyBack()
        {
            Log.Debug("Flyback engaged");
            isFlying = true;
            stickComponent.enabled = false;
            rb.isKinematic = false;
            rb.AddForce(CalculateReturnForce(returnForceBase), ForceMode.Impulse);
        }

        public void ReturnToPlayer()
        {
            Log.Debug("Return engaged");
            isReturning = true;
        }



        private Vector3 CalculateReturnForce(float baseForce)
        {
            Vector3 target = ownerTransform.position;
            float targetMass = (rb ? rb.mass : 1f);
            target.y += distanceToOwner * 0.45f;
            Vector3 direction = (target - transform.position).normalized;
            float massAdjForce = baseForce + targetMass;
            return massAdjForce * distanceToOwner * direction;
        }
    }
}
