
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class MovingPlatformController : MonoBehaviour
    {
        public float duration = 30;
        public float speed = 5;
        public Vector3 direction;
        public TeamIndex team;
        BaseAI baseAi;
        private Dictionary<Transform, Transform> passengersOriginalParents = new Dictionary<Transform, Transform>();

        Rigidbody rb;

        void Start()
        {
            FishermanSurvivor.SetDeployedPlatform(this);
            //StartCoroutine(DestroyPlatform());
            rb = GetComponent<Rigidbody>();
            //AISkillDriver driver;
            //driver.requireEquipmentReady
            //gameObject.layer = 11;
            //something somehting euler angles cross productud vector3 up 
            baseAi = GetComponent<CharacterBody>().master.GetComponent<RoR2.CharacterAI.BaseAI>();
            for (int i = 0; i < baseAi.skillDrivers.Length; i++)
            {
                baseAi.skillDrivers[i].ignoreNodeGraph = true;
            }
        }

        void FixedUpdate()
        {
            //transform.position += direction * speed * Time.fixedDeltaTime;
            //rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);
        }

        IEnumerator DestroyPlatform()
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            //Log.Debug($"Passenger Detected: {collision.gameObject.name} \n parent : {collision.gameObject.transform.parent} ");
            //TeamComponent teamComp = collision.gameObject.GetComponent<TeamComponent>();
            //if(teamComp.teamIndex == team)
            //{
               
            //    passengersOriginalParents.Add(collision.transform, collision.transform.parent);
            //    collision.gameObject.transform.parent = transform;
            //}
        }
        void OnCollisionExit(Collision collision)
        {
            //Log.Debug($"Passenger Detected: {collision.gameObject.name} \n parent : {collision.gameObject.transform.parent} ");
            //TeamComponent teamComp = collision.gameObject.GetComponent<TeamComponent>();
            //if (teamComp.teamIndex == team)
            //{
                
            //    collision.gameObject.transform.parent = passengersOriginalParents[collision.gameObject.transform];
            //    passengersOriginalParents.Remove(collision.gameObject.transform);
            //}
        }
        void OnDestroy()
        {
            FishermanSurvivor.SetDeployedPlatform(null);
        }
        
    }
}
