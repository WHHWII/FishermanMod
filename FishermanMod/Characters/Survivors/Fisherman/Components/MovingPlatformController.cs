﻿
using FishermanMod.Characters.Survivors.Fisherman.Components;
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
        public bool wasStuckByHook;
        public CharacterBody characterBody;
        public Vector3 direction;
        public TeamIndex team;
        BaseAI baseAi;
        private Dictionary<Transform, Transform> passengersOriginalParents = new Dictionary<Transform, Transform>();
        Rigidbody rb;

        void Start()
        {
            
            //StartCoroutine(DestroyPlatform());
            rb = GetComponent<Rigidbody>();
            //AISkillDriver driver;
            //driver.requireEquipmentReady
            //gameObject.layer = 11;
            //something somehting euler angles cross productud vector3 up 
            baseAi = GetComponent<CharacterBody>().master.GetComponent<RoR2.CharacterAI.BaseAI>();
            CharacterMaster owner = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
            owner?.GetBodyObject().GetComponent<FishermanSkillObjectTracker>()?.deployedPlatforms.Add(this);
            for (int i = 0; i < baseAi.skillDrivers.Length; i++)
            {
                baseAi.skillDrivers[i].ignoreNodeGraph = true;
            }
        }
        
    }
}
