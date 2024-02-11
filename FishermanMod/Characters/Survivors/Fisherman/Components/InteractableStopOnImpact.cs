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
        }
        public void OnCollisionEnter(Collision collision) 
        {
            if (!canStop) return;
            //Log.Debug($"Thrown Interactable colldided with {collision.gameObject.name}");
            if (collision.gameObject.name.Contains("Terrain"))
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                UnityEngine.Object.Destroy(rb);
                UnityEngine.Object.Destroy(collider);
            }
                
        }
        IEnumerator SetStop()
        {
            yield return new WaitForSeconds(0.3f);
            canStop = true;
        }
    }
}
