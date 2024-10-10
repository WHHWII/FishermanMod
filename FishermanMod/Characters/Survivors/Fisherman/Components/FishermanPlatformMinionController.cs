
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
    public class FishermanPlatformMinionController : MonoBehaviour
    {

        public bool wasStuckByHook;
        public CharacterBody characterBody;
        public LineRenderer aimVisual;
        public Transform cannonEnd;
        public EntityStateMachine stateMachine;
        public FishermanSkillObjectTracker objTracker;
        public BaseAIState aiState;
        public Vector3 direction;
        public CharacterMaster ownerMaster;
        public TeamIndex team;
        BaseAI baseAi;

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
            objTracker = ownerMaster?.GetBodyObject().GetComponent<FishermanSkillObjectTracker>();
            objTracker?.deployedPlatforms.Add(this);
            characterBody.AddBuff(BuffCatalog.GetBuffDef(BuffCatalog.FindBuffIndex("HiddenRejectAllDamage")));
            //for (int i = 0; i < baseAi.skillDrivers.Length; i++)
            //{
            //    baseAi.skillDrivers[i].ignoreNodeGraph = true;
            //}
            //var cl = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>();
            //cannonEnd = cl.FindChild("CannonEnd");
            //aimVisual = cannonEnd?.GetComponent<LineRenderer>();
            //stateMachine = characterBody.master.GetComponent<RoR2.EntityStateMachine>();
            stupidzone = new GameObject("Platform Debug Line");
            lineRenderer = stupidzone.AddComponent<LineRenderer>();
            lineRenderer.material = FishermanAssets.chainMat;
            if (!objTracker.platformAimTargetIndicator) objTracker.platformAimTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);

        }
        int ctr;
        public void Update()
        {
            lineRenderer.SetPosition(0, baseAi.localNavigator.currentSnapshot.chestPosition);
            Vector3 temp = ownerMaster.GetBody().transform.position;
            baseAi.customTarget?.GetBullseyePosition(out temp);
            lineRenderer.SetPosition(1, baseAi.localNavigator.targetPosition);
            if (baseAi.currentEnemy == null && (bool)objTracker?.platformAimTargetIndicator.activeSelf)
            {
                objTracker.platformAimTargetIndicator.SetActive(false);
            }
            else
            {
                objTracker.platformAimTargetIndicator.SetActive(false);
            }
            if (objTracker?.platformAimTargetIndicator) objTracker.platformAimTargetIndicator.transform.position = baseAi.currentEnemy.lastKnownBullseyePosition ?? transform.position;

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
    }
}
