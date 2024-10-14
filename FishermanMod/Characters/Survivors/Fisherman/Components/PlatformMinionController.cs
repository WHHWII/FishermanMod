
using EntityStates.AI;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Characters.Survivors.Fisherman.Content;
using R2API.Networking;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements.UIR;
using UnityEngine.XR.WSA;

namespace FishermanMod.Survivors.Fisherman.Components
{
    public class PlatformMinionController : MonoBehaviour
    {
        public bool debug = false;

        float commandPointOffset = 3;
        public bool wasStuckByHook;
        public CharacterBody characterBody;
        public LineRenderer aimVisual;
        public Transform cannonEnd;
        public EntityStateMachine stateMachine;
        public SkillObjectTracker objTracker;
        public BaseAIState aiState;
        public Vector3 direction;
        public CharacterMaster ownerMaster;
        public TeamIndex team;
        public BaseAI baseAi;
        public float commandAge;
        public float commandAgeLimit = 30f;


        LineRenderer lineRenderer;
        GameObject stupidzone;


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
            ownerMaster = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
            objTracker = ownerMaster?.GetBodyObject().GetComponent<SkillObjectTracker>();
            objTracker?.deployedPlatforms.Add(this);
            characterBody.AddBuff(RoR2.RoR2Content.Buffs.Immune);
            //for (int i = 0; i < baseAi.skillDrivers.Length; i++)
            //{
            //    baseAi.skillDrivers[i].ignoreNodeGraph = true;
            //}
            //var cl = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>();
            //cannonEnd = cl.FindChild("CannonEnd");
            //aimVisual = cannonEnd?.GetComponent<LineRenderer>();
            //stateMachine = characterBody.master.GetComponent<RoR2.EntityStateMachine>();
            if (debug)
            {
                stupidzone = new GameObject("Platform Debug Line");
                lineRenderer = stupidzone.AddComponent<LineRenderer>();
                lineRenderer.material = FishermanAssets.chainMat;
            }

            if (!objTracker.platformAimTargetIndicator) objTracker.platformAimTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
            if (!objTracker.platformPosTargetIndicator)
            {
                objTracker.platformPosTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
            }
            SetHover();

        }
        public void Update()
        {
            if (debug)
            {
                lineRenderer.SetPosition(0, baseAi.localNavigator.currentSnapshot.chestPosition);
                Vector3 temp = ownerMaster.GetBody().transform.position;
                baseAi.customTarget?.GetBullseyePosition(out temp);
                lineRenderer.SetPosition(1, baseAi.localNavigator.targetPosition);
            }

            if (!objTracker) return;
            if (baseAi.currentEnemy?.characterBody && baseAi.currentEnemy?.gameObject != objTracker?.gameObject)
            {
                if (!objTracker.platformAimTargetIndicator) objTracker.platformAimTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                if (!objTracker.platformAimTargetIndicator.activeSelf) objTracker.platformAimTargetIndicator.SetActive(true);
                objTracker.platformAimTargetIndicator.transform.position = baseAi.currentEnemy.characterBody.footPosition + (Vector3.up * 0.5f);
            }
            else
            {
                objTracker?.platformAimTargetIndicator?.SetActive(false);
            }


            //if (baseAi.currentEnemy == null && (bool)objTracker?.platformAimTargetIndicator.activeSelf)
            //{
            //    objTracker.platformAimTargetIndicator.SetActive(false);
            //}
            //else
            //{
            //    objTracker.platformAimTargetIndicator.SetActive(false);
            //}
            //if (objTracker?.platformAimTargetIndicator) objTracker.platformAimTargetIndicator.transform.position = baseAi.currentEnemy.lastKnownBullseyePosition ?? transform.position;

            //string logString = (baseAi.localNavigator.wasObstructedLastUpdate ? " ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::: OBSTRUCTED" : "");
            //ctr++;
            //Log.Debug($" ---------------------------- PLATFORM NAV UPDATE {ctr} ------------------------\n" + 
            //          $" WasObstructed: { baseAi.localNavigator.wasObstructedLastUpdate}{logString} \n" +
            //          $" Fustration: {baseAi.localNavigator.walkFrustration} \n" +
            //          $" ---------------------------- {ctr} --------------------------------------------\n"

            //);
            //if (baseAi.localNavigator.wasObstructedLastUpdate)
            //{
            //    RoR2.Projectile.ProjectileManager.instance.FireProjectile(FishermanAssets.whaleMisslePrefab, transform.position, transform.rotation, ownerMaster.GetBody().gameObject, 1, 1, false);
            //}
            //Log.Debug(baseAi.localNavigator.targetPosition)

        }

        void FixedUpdate()
        {
            if (!objTracker) return;
            if (baseAi.customTarget.gameObject) commandAge += Time.fixedDeltaTime;

            bool closeToLeader = Vector3.Distance(transform.position, objTracker.transform.position) <= 65;
            bool closeToTarget = baseAi.customTarget.gameObject && Vector3.Distance(transform.position, baseAi.customTarget.gameObject.transform.position) <= 3;
            if (baseAi.customTarget.gameObject && closeToTarget && !closeToLeader || commandAge >= commandAgeLimit)
            {
                baseAi.customTarget.gameObject = null;
                baseAi.customTarget.lastKnownBullseyePosition = null;
                objTracker.platformPosTargetIndicator.gameObject.SetActive(false);
                commandAge = 0;
            }
            if (!baseAi.customTarget.gameObject && closeToLeader)
            {
                SetHover();
            }



        }
        void SetHover()
        {
            RaycastHit hit;
            Vector3 hoverPos = objTracker.transform.position;
            Ray hoverRay = new Ray(objTracker.transform.position, Vector3.up);
            if (Physics.Raycast(hoverRay, out hit, 30))
            {
                hoverPos = hit.point + hit.normal * commandPointOffset;
            }
            else
            {
                hoverPos = hoverRay.GetPoint(30);
            }

            if (objTracker.platformPosTargetIndicator)
            {
                objTracker.platformPosTargetIndicator.transform.position = hoverPos;
                baseAi.customTarget.gameObject = objTracker.platformPosTargetIndicator.gameObject;
                baseAi.customTarget.lastKnownBullseyePosition = hoverPos;
                objTracker.platformPosTargetIndicator.gameObject.SetActive(true);
            }
        }
    }
}
