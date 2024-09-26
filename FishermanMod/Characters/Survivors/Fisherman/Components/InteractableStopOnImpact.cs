
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FishermanMod.Characters.Survivors.Fisherman.Components
{
    internal class InteractableStopOnImpact : MonoBehaviour
    {
        public Rigidbody rb;
        public SphereCollider collider;
        bool canStop = false;
        void Start()
        {
            StartCoroutine("SetStop");
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        public void OnCollisionEnter(Collision collision) 
        {
            if (!canStop) return;
            //Log.Debug($"Thrown Interactable colldided with {collision.gameObject.name}");
            if (collision.gameObject && collision.gameObject.layer == LayerIndex.world.intVal)
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                UnityEngine.Object.Destroy(rb);
                UnityEngine.Object.Destroy(collider);
                UnityEngine.Object.Destroy(this);
            }
                
        }
        IEnumerator SetStop()
        {
            yield return new WaitForSeconds(0.3f);
            canStop = true;
        }
    }
}
