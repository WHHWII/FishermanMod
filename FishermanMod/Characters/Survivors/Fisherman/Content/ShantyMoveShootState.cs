﻿using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using EntityStates.AI;
using RoR2.UI;
using System;
using System.Linq;
using UnityEngine;
using TMPro;

namespace FishermanMod.Characters.Survivors.Fisherman.Content
{
    public class ShantyMoveShootState : BaseAIState
    {

        public Vector3 CommandTarget;

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
            if ((bool)base.ai)
            {
                lastPathUpdate = base.ai.broadNavigationAgent.output.lastPathUpdate;
                base.ai.broadNavigationAgent.InvalidatePath();
            }
            fallbackNodeStartAge = float.NegativeInfinity;
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
            float num = 1f;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            if (!base.body || !base.bodyInputBank)
            {
                return;
            }
            if ((bool)dominantSkillDriver)
            {
                movementType = dominantSkillDriver.movementType;
                currentSkillSlot = dominantSkillDriver.skillSlot;
                flag = dominantSkillDriver.activationRequiresTargetLoS;
                flag2 = dominantSkillDriver.activationRequiresAimTargetLoS;
                flag3 = dominantSkillDriver.activationRequiresAimConfirmation;
                num = dominantSkillDriver.moveInputScale;
            }
            Vector3 position = base.bodyTransform.position;
            _ = base.bodyInputBank.aimOrigin;
            BroadNavigationSystem.Agent broadNavigationAgent = base.ai.broadNavigationAgent;
            BroadNavigationSystem.AgentOutput output = broadNavigationAgent.output;
            BaseAI.Target target = skillDriverEvaluation.target;
            if ((bool)target?.gameObject)
            {
                target.GetBullseyePosition(out var position2);
                Vector3 vector = position2;
                //if (fallbackNodeStartAge + fallbackNodeDuration < base.fixedAge)
                //{
                //    base.ai.SetGoalPosition(target);
                //}
                Vector3 targetPosition = position;

                targetPosition = CommandTarget;
                if (Vector3.Distance(position, CommandTarget) < 1.5) targetPosition = position;

                base.ai.localNavigator.targetPosition = targetPosition;
                base.ai.localNavigator.allowWalkOffCliff = true;
                base.ai.localNavigator.Update(deltaTime);
                //if (base.ai.localNavigator.wasObstructedLastUpdate)
                //{
                //    strafeDirection *= -1f;
                //}
                bodyInputs.moveVector = base.ai.localNavigator.moveVector;
                bodyInputs.moveVector *= num;
                if (!flag3 || base.ai.hasAimConfirmation)
                {
                    bool flag4 = true;
                    if (skillDriverEvaluation.target == skillDriverEvaluation.aimTarget && flag && flag2)
                    {
                        flag2 = false;
                    }
                    if (flag4 && flag)
                    {
                        flag4 = skillDriverEvaluation.target.TestLOSNow();
                    }
                    if (flag4 && flag2)
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
            lastPathUpdate = output.lastPathUpdate;
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
