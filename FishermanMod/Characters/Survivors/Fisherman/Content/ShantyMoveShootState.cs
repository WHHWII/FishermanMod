using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using EntityStates.AI;
using RoR2.UI;
using System;
using System.Linq;
using UnityEngine;
using TMPro;
using FishermanMod.Characters.Survivors.Fisherman.Components;
using FishermanMod.Survivors.Fisherman;
using static R2API.SoundAPI.Music.CustomMusicTrackDef;
using UnityEngine.UIElements;

namespace FishermanMod.Characters.Survivors.Fisherman.Content
{
    public class ShantyMoveShootState : BaseAIState
    {

        public  Vector3 commandTarget;
        public  Vector3 startPosition;
        public bool followingCommand;
        public bool leashing;

        public GameObject commanderObj;
        public FishermanSkillObjectTracker objTracker;

        private float strafeDirection;

        private const float strafeDuration = 0.25f;

        private float strafeTimer;

        private float activeSoundTimer;

        private float aiUpdateTimer;

        private const float minUpdateInterval = 1f / 6f;

        private const float maxUpdateInterval = 0.2f;

        private AISkillDriver dominantSkillDriver;

        protected bool currentSkillMeetsActivationConditions;

        protected SkillSlot currentSkillSlot = SkillSlot.None;

        protected Vector3 myBodyFootPosition;

        private float lastPathUpdate;

        private float fallbackNodeStartAge;

        private readonly float fallbackNodeDuration = 4f;


        public override void OnEnter()
        {
            base.OnEnter();
            base.OnEnter();
            activeSoundTimer = UnityEngine.Random.Range(3f, 8f);
            //if ((bool)base.ai)
            //{
            //    lastPathUpdate = base.ai.broadNavigationAgent.output.lastPathUpdate;
            //    base.ai.broadNavigationAgent.InvalidatePath();
            //}
            //fallbackNodeStartAge = float.NegativeInfinity;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!base.ai || !base.body)
            {
                return;
            }
            aiUpdateTimer -= Time.fixedDeltaTime;
            strafeTimer -= Time.fixedDeltaTime;
            UpdateFootPosition();
            if (aiUpdateTimer <= 0f)
            {
                aiUpdateTimer = BaseAIState.cvAIUpdateInterval.value;
                UpdateAI(BaseAIState.cvAIUpdateInterval.value);
                if (!dominantSkillDriver)
                {
                    outer.SetNextState(new EntityStates.AI.Walker.LookBusy());
                }
            }
            UpdateBark();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        protected void UpdateFootPosition()
        {
            myBodyFootPosition = base.body.footPosition;
            BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
            broadNavigationAgent.currentPosition = myBodyFootPosition;
        }

        protected void UpdateAI(float deltaTime)
        {
            BaseAI.SkillDriverEvaluation skillDriverEvaluation = base.ai.skillDriverEvaluation;
            dominantSkillDriver = skillDriverEvaluation.dominantSkillDriver;
            currentSkillSlot = SkillSlot.None;
            currentSkillMeetsActivationConditions = false;
            bodyInputs.moveVector = Vector3.zero;
            AISkillDriver.MovementType movementType = AISkillDriver.MovementType.Stop;
            float moveinputScale = 1f;
            bool requiresTargetLos = false;
            bool requiresAimLos = false;
            bool requiresAimConfirmation = false;
            
            if (!base.body || !base.bodyInputBank)
            {
                return;
            }
            if ((bool)dominantSkillDriver)
            {
                movementType = dominantSkillDriver.movementType;
                currentSkillSlot = dominantSkillDriver.skillSlot;
                requiresTargetLos = dominantSkillDriver.activationRequiresTargetLoS;
                requiresAimLos = dominantSkillDriver.activationRequiresAimTargetLoS;
                requiresAimConfirmation = dominantSkillDriver.activationRequiresAimConfirmation;
                moveinputScale = dominantSkillDriver.moveInputScale;
            }
            Vector3 position = base.bodyTransform.position;
            _ = base.bodyInputBank.aimOrigin;
            BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
            BroadNavigationSystem.Agent broadNavigationAgentLeash = base.ai.broadNavigationAgent;

            BroadNavigationSystem.AgentOutput navigationOutput = broadNavigationAgent.output;
            
            BaseAI.Target target = skillDriverEvaluation.target;
            if ((bool)target?.gameObject)
            {
                target.GetBullseyePosition(out var enemyPosition);
                Vector3 vector = enemyPosition;
                //if (fallbackNodeStartAge + fallbackNodeDuration < base.fixedAge)
                //{
                //    base.ai.SetGoalPosition(target);
                //}
                Vector3 targetPosition = position;



                /////
                /*todo
                 * i want the platform to use the node graph if he is far enough away from you
                 * need to get the position of the target whether that be the command or leader and calculate the node graph step
                 * Need a secondary navigation agent so that he may keep his enemy target. He should always be able to shoot independant of movement.
                 */
                

                ////

                //move to target - my code
                targetPosition = commandTarget;
                float distToCommander = commanderObj ? Vector3.Distance(position, commanderObj.transform.position) : 0;
                if (commanderObj && distToCommander > 60 && !followingCommand)
                {
                    /*
                    leashing = true;
                    RaycastHit hit;
                    Vector3 offset = Vector3.down * 8;
                    Ray upRay = new Ray(commanderObj.transform.position, Vector3.up);
                    if(Physics.Raycast(upRay,out hit, 40))
                    {
                        targetPosition = hit.point + offset;
                    }
                    else
                    {
                        targetPosition = commanderObj.transform.position + (Vector3.up * 40) + (Vector3.one * UnityEngine.Random.Range(-3, -3));

                    }
                    */

                    Vector3 commanderPosition;
                    ai.leader.GetBullseyePosition(out commanderPosition);
                    Vector3 leashLocation = commanderObj.transform.position + (Vector3.up * 40) /*+ (Vector3.one * UnityEngine.Random.Range(-3, -3))*/;
                    broadNavigationAgentLeash.goalPosition = leashLocation;



                    for (int i = 0; i < base.ai.skillDrivers.Length; i++)
                    {
                        base.ai.skillDrivers[i].ignoreNodeGraph = false;
                        base.ai.desiredSpawnNodeGraphType = MapNodeGroup.GraphType.Air;
                    }
                    Vector3 vector2 = ((!dominantSkillDriver || !dominantSkillDriver.ignoreNodeGraph) ? (broadNavigationAgentLeash.output.nextPosition ?? myBodyFootPosition) : ((!base.body.isFlying) ? vector : leashLocation));
                    targetPosition = vector2 + (position - myBodyFootPosition);

                    objTracker.platformPosTargetIndicator.SetActive(false);
                }
                else
                {
                    float distToTarg = Vector3.Distance(position, targetPosition);
                    if (distToTarg < 5)
                    {
                        followingCommand = false;
                        targetPosition = position;
                    }
                    if (distToTarg <= body.moveSpeed)
                    {
                        targetPosition = Vector3.MoveTowards(targetPosition, position, 0.5f * deltaTime);
                        //body.baseMoveSpeed = Mathf.Lerp(body.baseMoveSpeed, 0, 0.5f * deltaTime);
                        //moveinputScale = Mathf.Lerp(moveinputScale, 0.1f, 0.5f * deltaTime);
                    }
                }
                

                //else
                //{
                //    body.baseMoveSpeed = 8; // fix hardcoding later
                //}
                //if(distToCommander > 10 && distToTarg > 10)  body.isSprinting = true;




                base.ai.localNavigator.targetPosition = targetPosition;
                base.ai.localNavigator.allowWalkOffCliff = true;
                base.ai.localNavigator.Update(deltaTime);
                //if (base.ai.localNavigator.wasObstructedLastUpdate)
                //{
                //    strafeDirection *= -1f;
                //}
                bodyInputs.moveVector = base.ai.localNavigator.moveVector;
                bodyInputs.moveVector *= moveinputScale;
                if (!requiresAimConfirmation || base.ai.hasAimConfirmation)
                {
                    bool flag4 = true;
                    if (skillDriverEvaluation.target == skillDriverEvaluation.aimTarget && requiresTargetLos && requiresAimLos)
                    {
                        requiresAimLos = false;
                    }
                    if (flag4 && requiresTargetLos)
                    {
                        flag4 = skillDriverEvaluation.target.TestLOSNow();
                    }
                    if (flag4 && requiresAimLos)
                    {
                        flag4 = skillDriverEvaluation.aimTarget.TestLOSNow();
                    }
                    if (flag4)
                    {
                        currentSkillMeetsActivationConditions = true;
                    }
                }
            }
            //if (output.lastPathUpdate > lastPathUpdate && !output.targetReachable && fallbackNodeStartAge + fallbackNodeDuration < base.fixedAge)
            //{
            //    broadNavigationAgent.goalPosition = PickRandomNearbyReachablePosition();
            //    broadNavigationAgent.InvalidatePath();
            //}
            lastPathUpdate = navigationOutput.lastPathUpdate;
        }

        public override BaseAI.BodyInputs GenerateBodyInputs(in BaseAI.BodyInputs previousBodyInputs)
        {
            bool pressSkill = false;
            bool pressSkill2 = false;
            bool pressSkill3 = false;
            bool pressSkill4 = false;
            if ((bool)base.bodyInputBank)
            {
                AISkillDriver.ButtonPressType buttonPressType = AISkillDriver.ButtonPressType.Abstain;
                if ((bool)dominantSkillDriver)
                {
                    buttonPressType = dominantSkillDriver.buttonPressType;
                }
                bool flag = false;
                switch (currentSkillSlot)
                {
                    case SkillSlot.Primary:
                        flag = previousBodyInputs.pressSkill1;
                        break;
                    case SkillSlot.Secondary:
                        flag = previousBodyInputs.pressSkill2;
                        break;
                    case SkillSlot.Utility:
                        flag = previousBodyInputs.pressSkill3;
                        break;
                    case SkillSlot.Special:
                        flag = previousBodyInputs.pressSkill4;
                        break;
                }
                bool flag2 = currentSkillMeetsActivationConditions;
                switch (buttonPressType)
                {
                    case AISkillDriver.ButtonPressType.Abstain:
                        flag2 = false;
                        break;
                    case AISkillDriver.ButtonPressType.TapContinuous:
                        flag2 = flag2 && !flag;
                        break;
                }
                switch (currentSkillSlot)
                {
                    case SkillSlot.Primary:
                        pressSkill = flag2;
                        break;
                    case SkillSlot.Secondary:
                        pressSkill2 = flag2;
                        break;
                    case SkillSlot.Utility:
                        pressSkill3 = flag2;
                        break;
                    case SkillSlot.Special:
                        pressSkill4 = flag2;
                        break;
                }
            }
            bodyInputs.pressSkill1 = pressSkill;
            bodyInputs.pressSkill2 = pressSkill2;
            bodyInputs.pressSkill3 = pressSkill3;
            bodyInputs.pressSkill4 = pressSkill4;
            bodyInputs.pressSprint = false;
            bodyInputs.pressActivateEquipment = false;
            bodyInputs.desiredAimDirection = Vector3.zero;
            if ((bool)dominantSkillDriver)
            {
                bodyInputs.pressSprint = dominantSkillDriver.shouldSprint;
                bodyInputs.pressActivateEquipment = dominantSkillDriver.shouldFireEquipment && !previousBodyInputs.pressActivateEquipment;
                AISkillDriver.AimType aimType = dominantSkillDriver.aimType;
                BaseAI.Target aimTarget = base.ai.skillDriverEvaluation.aimTarget;
                if (aimType == AISkillDriver.AimType.MoveDirection)
                {
                    AimInDirection(ref bodyInputs, bodyInputs.moveVector);
                }
                if (aimTarget != null)
                {
                    AimAt(ref bodyInputs, aimTarget);
                }

                //target visualization
                if (aimTarget?.characterBody && aimTarget?.gameObject != objTracker?.gameObject)
                {
                    if(!objTracker.platformAimTargetIndicator) objTracker.platformAimTargetIndicator = UnityEngine.GameObject.Instantiate(FishermanAssets.shantyBlueprintPrefab);
                    if (!objTracker.platformAimTargetIndicator.activeSelf) objTracker.platformAimTargetIndicator.SetActive(true);
                    objTracker.platformAimTargetIndicator.transform.position = aimTarget.characterBody.footPosition;
                }
                else
                {
                    objTracker?.platformAimTargetIndicator?.SetActive(false);
                }
                
            }
            ModifyInputsForJumpIfNeccessary(ref bodyInputs);
            return bodyInputs;
        }

        protected void UpdateBark()
        {
            activeSoundTimer -= Time.fixedDeltaTime;
            if (activeSoundTimer <= 0f)
            {
                activeSoundTimer = UnityEngine.Random.Range(3f, 8f);
                base.body.CallRpcBark();
            }
        }
    }
}
