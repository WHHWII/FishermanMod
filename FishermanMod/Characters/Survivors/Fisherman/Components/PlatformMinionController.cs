
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
    // NETWORKING: the platform is an AI minion, and BaseAI only runs on the server, so everything that steers the AI
    // (hover point, command expiry, forced enemy) is server-only here.
    // Clients still need to show the owner where the platform is headed and what it's aiming at, so the server mirrors
    // that into the SyncVars below. The owner is resolved through MinionOwnership (synced to clients) instead of
    // AIOwnership (server-only), otherwise objTracker was null on every client and Start threw.
    public class PlatformMinionController : NetworkBehaviour
    {
        public bool debug = false;

        [SyncVar]
        bool hasCommandTarget;
        [SyncVar]
        Vector3 commandTargetPosition;
        [SyncVar]
        GameObject aimTargetObject;
        bool ownerResolved;
        bool appliedHasCommandTarget;
        Vector3 appliedCommandTargetPosition;

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
            TryResolveOwner();
        }

        // NETWORKING: on clients the master and its MinionOwnership arrive through separate network messages,
        // so the owner may not be known yet in Start. Keep trying from Update until it is.
        void TryResolveOwner()
        {
            if (ownerResolved) return;
            CharacterMaster master = characterBody ? characterBody.master : null;
            if (!master) return;
            baseAi = master.GetComponent<RoR2.CharacterAI.BaseAI>();
            ownerMaster = master.minionOwnership ? master.minionOwnership.ownerMaster : null;
            if (!ownerMaster && baseAi) ownerMaster = baseAi.GetComponent<AIOwnership>()?.ownerMaster;
            GameObject ownerBodyObject = ownerMaster ? ownerMaster.GetBodyObject() : null;
            objTracker = ownerBodyObject ? ownerBodyObject.GetComponent<SkillObjectTracker>() : null;
            if (!objTracker) return;
            ownerResolved = true;
            objTracker.RegisterPlatform(this);
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

            // NETWORKING: indicators are only created where they're used. The aim indicator is a visual for the owner;
            // the position target is the AI's customTarget on the server and the owner's visual (see SkillObjectTracker.GetOrCreatePlatformPosTarget)
            if (OwnerIsLocal()) objTracker.GetOrCreatePlatformAimIndicator();
            if (NetworkServer.active || OwnerIsLocal()) objTracker.GetOrCreatePlatformPosTarget();
            if (NetworkServer.active) SetHover();
            var ownerHUD = HUD.readOnlyInstanceList.Where(el => el.targetBodyObject == objTracker.gameObject);
            foreach (HUD hud in ownerHUD)
            {
                CreateOwnerOverlay(hud);
            }

        }
        bool OwnerIsLocal()
        {
            return objTracker && objTracker.characterBody && objTracker.characterBody.hasEffectiveAuthority;
        }

        float timeStamp;
        float flashdur = 0.5f;
        public void LateUpdate()
        {
            if (!objTracker) return;

            if (NetworkServer.active && baseAi)
            {
                // NETWORKING: the AI's enemy only exists on the server, mirror it so the owner's client can draw the aim indicator
                GameObject enemy = baseAi.currentEnemy?.characterBody && baseAi.currentEnemy.gameObject != objTracker.gameObject ? baseAi.currentEnemy.gameObject : null;
                if (aimTargetObject != enemy) aimTargetObject = enemy;
                if (!enemy) baseAi.ForceAcquireNearestEnemyIfNoCurrentEnemy();
            }

            //aim indicator, owner only
            if (!OwnerIsLocal()) return;
            CharacterBody aimBody = aimTargetObject ? aimTargetObject.GetComponent<CharacterBody>() : null;
            if (aimBody)
            {
                GameObject aimIndicator = objTracker.GetOrCreatePlatformAimIndicator();
                if (!aimIndicator.activeSelf) aimIndicator.SetActive(true);
                aimIndicator.transform.position = aimBody.footPosition + (Vector3.up * 0.5f);
            }
            else if (objTracker.platformAimTargetIndicator)
            {
                objTracker.platformAimTargetIndicator.SetActive(false);
            }

            // NETWORKING: on the host the position indicator *is* the AI target, so it's already in the right place.
            // A remote owner has their own local copy that follows the synced command point.
            // Only applied when the synced values change, so DirectPlatform's immediate local placement isn't snapped back
            // to the old point while the command is still on its way to the server.
            if (!NetworkServer.active && (appliedHasCommandTarget != hasCommandTarget || appliedCommandTargetPosition != commandTargetPosition))
            {
                appliedHasCommandTarget = hasCommandTarget;
                appliedCommandTargetPosition = commandTargetPosition;
                GameObject posIndicator = objTracker.GetOrCreatePlatformPosTarget();
                posIndicator.SetActive(hasCommandTarget);
                if (hasCommandTarget) posIndicator.transform.position = commandTargetPosition;
            }
        }
        public void Update()
        {
            if (!ownerResolved) TryResolveOwner();

            //direaction indicator
            if (debug && baseAi)
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


            // NETWORKING: steering the AI, server only
            if(NetworkServer.active && forcedEnemy && Time.time < forcedEnemyTimeStamp)
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
            // NETWORKING: command expiry and re-hovering change the AI's target, server only
            if (!objTracker || !NetworkServer.active || !baseAi) return;
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

            // NETWORKING: mirror the command point for the owner's indicator (SyncVars only send when the value actually changes)
            GameObject commandTarget = baseAi.customTarget.gameObject;
            bool hasTarget = commandTarget && commandTarget.activeSelf;
            if (hasCommandTarget != hasTarget) hasCommandTarget = hasTarget;
            if (hasTarget && commandTargetPosition != commandTarget.transform.position) commandTargetPosition = commandTarget.transform.position;

            //if (baseAi.stateMachine.state.GetType() == typeof(EntityStates.AI.Walker.LookBusy))
            //{
            //    Log.Debug("Hey Lazy ass. Set lookbusy to combat");
            //    baseAi.stateMachine.SetNextState(new EntityStates.AI.Walker.Combat());
            //}
            //standableRB.interpolation = RigidbodyInterpolation.Interpolate;


        }

        [Server]
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

            GameObject posTarget = objTracker.GetOrCreatePlatformPosTarget();
            posTarget.transform.position = hoverPos;
            baseAi.customTarget.gameObject = posTarget;
            baseAi.customTarget.lastKnownBullseyePosition = hoverPos;
            posTarget.SetActive(true);
        }

        void OnDestroy()
        {
            if (overlayController != null)
            {
                overlayController.onInstanceAdded -= OverlayController_onInstanceAdded;
                HudOverlayManager.RemoveOverlay(overlayController);
            }
            allDeployedPlatforms.Remove(gameObject);
            // only if we're still the registered platform: when a new platform replaces this one, the old body is destroyed a frame later
            // and used to wipe the new platform from the list (now also on clients, which keep their own list)
            if (objTracker && objTracker.deployedPlatforms.Contains(this)) objTracker.ProccessPlatfromDeath();
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

            // NETWORKING: on a remote owner's client this shows the platform's primary as last synced, which is fine for a cooldown display
            instance.GetComponent<SkillIcon>().targetSkill = characterBody.skillLocator.primary;
        }

        #endregion UI
    }
}
