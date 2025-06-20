﻿
using EntityStates.AI;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Characters.Survivors.Fisherman.Content;
using R2API.Networking;
using RoR2;
using RoR2.CharacterAI;
using RoR2.HudOverlay;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements.UIR;
using UnityEngine.XR.WSA;
using static UnityEngine.UI.GridLayoutGroup;

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
        public Rigidbody standableRB;

        Vector3 lastNavTarget = Vector3.zero;

        private OverlayController overlayController;
        private GameObject overlayInstance;
        bool hasRiskUI;


        public LineRenderer lineRenderer;
        GameObject stupidzone;


        GameObject forcedEnemy;
        float forcedEnemyTime = 12f;
        float forcedEnemyTimeStamp;

        public static HashSet<GameObject> allDeployedPlatforms = new HashSet<GameObject>();



        Rigidbody rb;

        void Start()
        {
            allDeployedPlatforms.Add(gameObject);
            //StartCoroutine(DestroyPlatform());
            rb = GetComponent<Rigidbody>();
            //AISkillDriver driver;
            //driver.requireEquipmentReady
            //gameObject.layer = 11;
            //something somehting euler angles cross productud vector3 up 
            baseAi = GetComponent<CharacterBody>().master.GetComponent<RoR2.CharacterAI.BaseAI>();
            ownerMaster = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
            objTracker = ownerMaster?.GetBodyObject().GetComponent<SkillObjectTracker>();
            objTracker?.RegisterPlatform(this);
            //characterBody.AddBuff(RoR2.RoR2Content.Buffs.Immune);
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
                var lr = stupidzone.AddComponent<LineRenderer>();
                lr.material = lineRenderer.material;
                lr.colorGradient = lineRenderer.colorGradient;
                lr.widthCurve = lineRenderer.widthCurve;
                Destroy(lineRenderer);
                lineRenderer = lr;
            }

            if (!objTracker.platformAimTargetIndicator) objTracker.platformAimTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
            if (!objTracker.platformPosTargetIndicator)
            {
                objTracker.platformPosTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
            }
            SetHover();
            var ownerHUD = HUD.readOnlyInstanceList.Where(el => el.targetBodyObject == objTracker.gameObject);
            foreach (HUD hud in ownerHUD)
            {
                CreateOwnerOverlay(hud);
            }

        }
        float timeStamp;
        float flashdur = 0.5f;
        public void LateUpdate()
        {
            //aim indicator
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
                baseAi.ForceAcquireNearestEnemyIfNoCurrentEnemy();
            }
        }
        public void Update()
        {
            //direaction indicator
            if (debug)
            {
                lineRenderer.SetPosition(0, baseAi.localNavigator.currentSnapshot.chestPosition);
                Vector3 temp = ownerMaster.GetBody().transform.position;
                baseAi.customTarget?.GetBullseyePosition(out temp);
                lineRenderer.SetPosition(1, baseAi.localNavigator.targetPosition);

                if(lastNavTarget != baseAi.localNavigator.targetPosition)
                {
                    timeStamp = Time.time + flashdur;
                    lastNavTarget = baseAi.localNavigator.targetPosition;
                    lineRenderer.enabled = true;
                }
            }
            if (Time.time >= timeStamp)
            {
                lineRenderer.enabled = false;
            }


            if(forcedEnemy && Time.time < forcedEnemyTimeStamp)
            {
                if(baseAi.currentEnemy.gameObject != forcedEnemy)
                {
                    Log.Debug("PMC | UPDATE | FORCE ENEMY | Setting Enemy");
                    baseAi.currentEnemy.gameObject = forcedEnemy;
                    baseAi.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = baseAi.customTarget,
                        aimTarget = baseAi.currentEnemy,
                        dominantSkillDriver = baseAi.GetComponent<AISkillDriver>(),
                        separationSqrMagnitude = Vector3.Distance(baseAi.body.footPosition, baseAi.customTarget.gameObject.transform.position)
                    });
                }
            }

        }

        public void SetForcedEnemy(GameObject enemy)
        {
            forcedEnemy = enemy;
            forcedEnemyTimeStamp = Time.time + forcedEnemyTime;
        }

        void FixedUpdate()
        {
            if (!objTracker) return;
            if (baseAi.customTarget.gameObject) commandAge += Time.fixedDeltaTime;

            float distToLeader = Vector3.Distance(transform.position, objTracker.transform.position);
            bool closeToLeader = distToLeader <= 65 && distToLeader > 10;
            bool closeToTarget = baseAi.customTarget.gameObject && Vector3.Distance(transform.position, baseAi.customTarget.gameObject.transform.position) <= 5;
            if (baseAi.customTarget.gameObject && closeToTarget && !closeToLeader || commandAge >= commandAgeLimit)
            {
                baseAi.customTarget.gameObject = null;
                baseAi.customTarget.lastKnownBullseyePosition = null;
                objTracker.platformPosTargetIndicator.gameObject.SetActive(false);
                commandAge = 0;
            }
            if (!baseAi.customTarget.gameObject && closeToLeader && commandAge >= 5f)
            {
                SetHover();
            }

            //if (baseAi.stateMachine.state.GetType() == typeof(EntityStates.AI.Walker.LookBusy))
            //{
            //    Log.Debug("Hey Lazy ass. Set lookbusy to combat");
            //    baseAi.stateMachine.SetNextState(new EntityStates.AI.Walker.Combat());
            //}
            //standableRB.interpolation = RigidbodyInterpolation.Interpolate;


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

        void OnDestroy()
        {
            if (overlayController != null)
            {
                overlayController.onInstanceAdded -= OverlayController_onInstanceAdded;
                HudOverlayManager.RemoveOverlay(overlayController);
            }
            allDeployedPlatforms.Remove(gameObject);
            objTracker.ProccessPlatfromDeath();
        }


        #region UI

        void CreateOwnerOverlay(HUD hud)
        {
           
            GameObject skill4root = Array.Find<SkillIcon>(hud.skillIcons, icon => icon.name == "Skill3Root").gameObject;

            ChildLocator childLocator = hud.GetComponent<ChildLocator>();
            ChildLocator.NameTransformPair[] newArray = new ChildLocator.NameTransformPair[childLocator.transformPairs.Length + 1];
            childLocator.transformPairs.CopyTo(newArray, 0);
            newArray[newArray.Length - 1] = new ChildLocator.NameTransformPair
            {
                name = skill4root.transform.parent.name,
                transform = skill4root.transform.parent
            };
            childLocator.transformPairs = newArray;

            OverlayCreationParams overlayCreationParams = new OverlayCreationParams()
            {
                prefab = skill4root,
                childLocatorEntry = skill4root.transform.parent.name
            };

            overlayController = HudOverlayManager.AddOverlay(objTracker.gameObject, overlayCreationParams);
            overlayController.onInstanceAdded += OverlayController_onInstanceAdded;
        }

        private void OverlayController_onInstanceAdded(OverlayController overlayController, GameObject instance)
        {
            overlayInstance = instance;

            if (overlayController.creationParams.childLocatorEntry == "SkillIconContainer")
            {
                hasRiskUI = true;
                instance.transform.Find("BottomContainer").Find("SkillBackgroundPanel").gameObject.SetActive(false);
                instance.GetComponent<RectTransform>().anchoredPosition += new Vector2(80f, 0f);
            }
            else
            {
                instance.transform.Find("SkillBackgroundPanel").gameObject.SetActive(false);
                instance.GetComponent<RectTransform>().anchoredPosition += new Vector2(0f, 130f);
            }

            instance.name = "ShantyPrimaryRoot";

            instance.GetComponent<SkillIcon>().targetSkill = characterBody.skillLocator.primary;
        }

        #endregion UI
    }
}
