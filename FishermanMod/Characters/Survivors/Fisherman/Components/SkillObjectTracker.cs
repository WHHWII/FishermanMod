using FishermanMod.Characters.Survivors.Fisherman.Content;
using FishermanMod.Modules;
using FishermanMod.Survivors.Fisherman;
using FishermanMod.Survivors.Fisherman.Components;
using On.RoR2.Skills;
using RoR2;
using RoR2.CharacterAI;
using RoR2.HudOverlay;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.SoundAPI.Music.CustomMusicTrackDef;

namespace FishermanMod.Characters.Survivors.Fisherman.Components
{
    // NETWORKING: this lives on the Fisherman body, which the owning player's client has authority over.
    // That makes it the natural place for [Command]s (client -> server requests like "recall my hooks" or "summon my platform")
    // and [ClientRpc]s (server -> clients feedback like hook jolts and screen shake).
    // Requires the UNet weaver to run on the built dll, see NETWORKING.md.
    //
    // Convention used below, mirroring vanilla (e.g. CharacterBody.AddTimedBuffAuthority):
    //   XAuthority()  - call this from authority code (skill states). Runs X directly if we are the server, otherwise sends CmdX.
    //   CmdX()        - runs on the server.
    //   X()           - the actual server-side logic.
    public class SkillObjectTracker : NetworkBehaviour
    {
        public List<FishHookController> deployedHooks;
        public List<HookBombController> deployedBombs;
        public List<BombletController> deployedBomblets;
        public List<PlatformMinionController> deployedPlatforms;
        public GameObject platformPosTargetIndicator;
        public GameObject platformAimTargetIndicator;
        public RoR2.CharacterBody characterBody;
        public RoR2.CharacterMotor characterMotor;
        public Animator animator;
        public const string COMMAND_SKILL_DRIVER_NAME = "FollowCommand";
        public const string LEASH_SKILL_DRIVER_NAME = "LeashLeader";
        public const string STOP_SKILL_DRIVER_NAME = "StandStill";
        public Transform fishingPoleTip;
        OverlayController objectViewerOverlay;

        // NETWORKING: set on the server by CmdSetPendingHookBombStocks right before the authority fires a hook bomb.
        // The client only knows how many stocks it spent, and the bomb itself is spawned on the server, so the count is parked here
        // and picked up by HookBombController.Start on the server. Commands and ProjectileManager's fire message share
        // channel 0 (reliable, ordered), so this always arrives before the bomb is spawned.
        [NonSerialized]
        public int pendingHookBombStocks = 1;

        public void Awake()
        {
            // grabbed in Awake rather than Start so RPCs that arrive early still have a body to work with
            characterBody = GetComponent<RoR2.CharacterBody>();
            characterMotor = GetComponent<CharacterMotor>();
        }

        public void Start()
        {
            animator = characterBody.modelLocator.modelTransform.GetComponent<Animator>();
            fishingPoleTip = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("PoleEnd");

            objectViewerOverlay = HudOverlayManager.AddOverlay(this.gameObject, new OverlayCreationParams
            {
                prefab = FishermanAssets.objectViewerOverlay,
                childLocatorEntry = "ScopeContainer"
            });
        }

        public void ProccessPlatfromDeath()
        {
            if (platformAimTargetIndicator) platformAimTargetIndicator.SetActive(false);
            // NETWORKING: skill stocks and overrides are only meaningful on the machine that has authority over this body
            // (the owning client, or the server for AI Fishermen). This runs everywhere the platform gets destroyed, so gate it.
            if (characterBody && characterBody.hasEffectiveAuthority)
            {
                characterBody.skillLocator.utility.UnsetSkillOverride(gameObject, FishermanSurvivor.utilityDirectPlatform, RoR2.GenericSkill.SkillOverridePriority.Upgrade);
                characterBody.skillLocator.utility.DeductStock(1);
            }
            deployedPlatforms.Clear();
        }

        #region networked requests

        // NETWORKING: hooks are server-simulated projectiles, so recalling them (physics, hit detection, throwing what they caught) has to happen on the server.
        public void RecallHooksAuthority()
        {
            if (NetworkServer.active)
            {
                RecallAllHooks();
            }
            else
            {
                CmdRecallHooks();
                // purely local bookkeeping so the owner's hook viewer UI behaves the same as on the host
                deployedHooks.Clear();
            }
        }

        [Command]
        void CmdRecallHooks()
        {
            RecallAllHooks();
        }

        [Server]
        public void RecallAllHooks()
        {
            foreach (FishHookController hook in deployedHooks)
            {
                if (hook) hook.Recall();
            }

            deployedHooks.Clear();
        }

        // NETWORKING: detonating hook bombs reels in tethered enemies, removes buffs and changes the explosion, all of which are server-only.
        public void HookAllBombsAuthority()
        {
            if (NetworkServer.active)
            {
                HookAllBombs();
            }
            else
            {
                CmdHookAllBombs();
                deployedBombs.Clear();
            }
        }

        [Command]
        void CmdHookAllBombs()
        {
            HookAllBombs();
        }

        [Server]
        public void HookAllBombs()
        {
            foreach (HookBombController bomb in deployedBombs)
            {
                if (bomb) bomb.HookAllTethers();
            }
            deployedBombs.Clear();
        }

        public void SetPendingHookBombStocksAuthority(int stocks)
        {
            if (NetworkServer.active)
            {
                pendingHookBombStocks = stocks;
            }
            else
            {
                CmdSetPendingHookBombStocks(stocks);
            }
        }

        [Command]
        void CmdSetPendingHookBombStocks(int stocks)
        {
            // clamp so a modified client can't ask for an absurd number of bomblets
            pendingHookBombStocks = Mathf.Clamp(stocks, 1, Mathf.Max(1, characterBody.skillLocator.special.maxStock + 1));
        }

        // NETWORKING: MasterSummon.Perform only works on the server, so the placement is chosen by the authority and the summon is requested here.
        public void SummonPlatformAuthority(Vector3 position, Quaternion rotation)
        {
            if (NetworkServer.active)
            {
                SummonPlatform(position, rotation);
            }
            else
            {
                CmdSummonPlatform(position, rotation);
            }
        }

        [Command]
        void CmdSummonPlatform(Vector3 position, Quaternion rotation)
        {
            SummonPlatform(position, rotation);
        }

        [Server]
        void SummonPlatform(Vector3 position, Quaternion rotation)
        {
            // re-check on the server, the client's view of deployedPlatforms can lag behind
            if (HasLivePlatforms()) return;

            MasterSummon masterSummon = new MasterSummon();
            masterSummon.useAmbientLevel = true;
            masterSummon.masterPrefab = FishermanAssets.shantyMasterPrefab;
            masterSummon.ignoreTeamMemberLimit = false;
            masterSummon.teamIndexOverride = TeamIndex.Player;
            masterSummon.summonerBodyObject = gameObject;
            masterSummon.position = position;
            masterSummon.rotation = rotation;
            masterSummon.Perform();
        }

        // NETWORKING: the platform's AI (BaseAI) only runs on the server, so movement/target commands are sent there.
        // The authority does the aiming raycasts itself (it has the real aim direction) and only sends the result.
        public void DirectPlatformsAuthority(Vector3 targetPosition, GameObject enemy)
        {
            if (NetworkServer.active)
            {
                DirectPlatforms(targetPosition, enemy);
            }
            else
            {
                CmdDirectPlatforms(targetPosition, enemy);
            }
        }

        [Command]
        void CmdDirectPlatforms(Vector3 targetPosition, GameObject enemy)
        {
            DirectPlatforms(targetPosition, enemy);
        }

        [Server]
        void DirectPlatforms(Vector3 targetPosition, GameObject enemy)
        {
            GetOrCreatePlatformPosTarget().transform.position = targetPosition;
            if (enemy)
            {
                SetAllPlatformEnemy(enemy);
            }
            DirectAllPlatforms();
        }

        public void DestroyAllPlatformsAuthority()
        {
            if (NetworkServer.active)
            {
                DestroyAllPlatforms();
            }
            else
            {
                CmdDestroyAllPlatforms();
            }
        }

        [Command]
        void CmdDestroyAllPlatforms()
        {
            DestroyAllPlatforms();
        }

        #endregion networked requests

        #region networked feedback

        // NETWORKING: hook hits are detected on the server, but the jolt (setting our own velocity) has to happen where the body is simulated,
        // which for players is their own client. Screen shake is local to each camera, so every client creates its own.
        [Server]
        public void HookFeedback(bool failed, Vector3 joltVelocity)
        {
            if (!NetworkClient.active)
            {
                // dedicated server: no RPC loopback, apply the authority side directly (only matters for AI Fishermen)
                ApplyHookFeedback(failed, joltVelocity);
            }
            RpcHookFeedback(failed, joltVelocity);
        }

        [ClientRpc]
        void RpcHookFeedback(bool failed, Vector3 joltVelocity)
        {
            ApplyHookFeedback(failed, joltVelocity);
        }

        void ApplyHookFeedback(bool failed, Vector3 joltVelocity)
        {
            if (NetworkClient.active)
            {
                Wave wave = new Wave
                {
                    amplitude = failed ? 5f : 1f,
                    frequency = 10f,
                    cycleOffset = 0.2f
                };
                ShakeEmitter.CreateSimpleShakeEmitter(transform.position, wave, 0.1f, 10, true);
            }

            if (failed && joltVelocity != Vector3.zero && characterBody.hasEffectiveAuthority && characterMotor)
            {
                if (characterMotor.isGrounded) characterMotor.Motor.ForceUnground();
                characterMotor.velocity = joltVelocity;
                FishermanStaticValues.hitStopMod = 2;
            }
        }

        #endregion networked feedback

        public bool HasLivePlatforms()
        {
            deployedPlatforms.RemoveAll(platform => !platform || !platform.characterBody || !platform.characterBody.healthComponent.alive);
            return deployedPlatforms.Count > 0;
        }

        // NETWORKING: on the server this object is the platform AI's customTarget.
        // When the owner is the local player (host or singleplayer) it doubles as the visible blueprint indicator.
        // For remote owners the server uses an invisible object instead, so the host doesn't see other players' blueprints;
        // the remote owner's own indicator is driven by PlatformMinionController's SyncVars.
        public GameObject GetOrCreatePlatformPosTarget()
        {
            if (!platformPosTargetIndicator)
            {
                platformPosTargetIndicator = characterBody.hasEffectiveAuthority
                    ? UnityEngine.Object.Instantiate(FishermanAssets.shantyBlueprintPrefab)
                    : new GameObject("FishermanPlatformCommandTarget");
            }
            return platformPosTargetIndicator;
        }

        public GameObject GetOrCreatePlatformAimIndicator()
        {
            if (!platformAimTargetIndicator)
            {
                platformAimTargetIndicator = UnityEngine.Object.Instantiate(FishermanAssets.shantyBlueprintPrefab);
            }
            return platformAimTargetIndicator;
        }

        [Server]
        public bool DirectAllPlatforms()
        {
            GameObject posTarget = GetOrCreatePlatformPosTarget();
            List<PlatformMinionController> toRemove = new List<PlatformMinionController>();
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                //Debug.Log("Platform instance: " + platform);
                if (platform == null || !platform.characterBody.healthComponent.alive)
                {
                    toRemove.Add(platform);
                    Debug.Log("Platform was null. Removing");
                }
                else
                {
                    CharacterMaster master = platform.GetComponent<RoR2.CharacterBody>().master;
                    BaseAI ai = master.GetComponent<BaseAI>();
                    posTarget.SetActive(true);
                    ai.customTarget.gameObject = posTarget;
                    ai.customTarget.lastKnownBullseyePosition = posTarget.transform.position;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = GetComponent<AISkillDriver>(),
                        separationSqrMagnitude = Vector3.Distance(ai.body.footPosition, ai.customTarget.gameObject.transform.position)
                    }); ;
                }

            }
            posTarget.SetActive(true); // note that the target indicators will not work when there is more than one platform.
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            if(deployedPlatforms.Count == 0)
            {
                return false;
            }
            toRemove.Clear();
            return true;
            
        }

        [Server]
        public bool SetAllPlatformEnemy(GameObject enemy)
        {
            List<PlatformMinionController> toRemove = new List<PlatformMinionController>();
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                //Debug.Log("Platform instance: " + platform);
                if (platform == null || !platform.characterBody.healthComponent.alive)
                {
                    toRemove.Add(platform);
                    Debug.Log("Platform was null. Removing");
                }
                else
                {
                    CharacterMaster master = platform.GetComponent<RoR2.CharacterBody>().master;
                    BaseAI ai = master.GetComponent<BaseAI>();
                    platform.SetForcedEnemy(enemy);

                    ai.currentEnemy.gameObject = enemy;
                    ai.BeginSkillDriver(new BaseAI.SkillDriverEvaluation
                    {
                        target = ai.customTarget,
                        aimTarget = ai.currentEnemy,
                        dominantSkillDriver = GetComponent<AISkillDriver>(),
                        separationSqrMagnitude = Vector3.Distance(ai.body.footPosition, ai.customTarget.gameObject.transform.position)
                    }); ;
                }

            }
            GetOrCreatePlatformPosTarget().SetActive(true); // note that the target indicators will not work when there is more than one platform.
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            if (deployedPlatforms.Count == 0)
            {
                return false;
            }
            toRemove.Clear();
            return true;
        }
        // NETWORKING: runs on every machine when a platform body spawns, so clients have their own list for UI and skill checks.
        // Only the server is allowed to actually kill the old platform; clients just drop it from their list.
        public void RegisterPlatform(PlatformMinionController newMinion)
        {
            deployedPlatforms.RemoveAll(platform => !platform || platform == newMinion);
            if(deployedPlatforms.Count > 0)
            {
                if (NetworkServer.active)
                {
                    DestroyAllPlatforms();
                }
                else
                {
                    deployedPlatforms.Clear();
                }
            }
            deployedPlatforms.Add(newMinion);
        }

        [Server]
        public void DestroyAllPlatforms()
        {
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                if (platform && platform.characterBody && platform.characterBody.master) platform.characterBody.master.TrueKill();
            }
            ProccessPlatfromDeath();
        }

        public bool ModifyPlayformStock(int stocks)
        {
            if (deployedPlatforms.Count <= 0) return false;
            List<PlatformMinionController> toRemove = new List<PlatformMinionController>();
            foreach (PlatformMinionController platform in deployedPlatforms)
            {
                //Debug.Log("Platform instance: " + platform);
                if (platform == null || !platform.characterBody.healthComponent.alive)
                {
                    toRemove.Add(platform);
                    Debug.Log("Platform was null. Removing");
                }
                else
                {
                    
                    platform.characterBody.skillLocator.primary.SetBonusStockFromBody(stocks);
     
                }



                

            }
            deployedPlatforms.RemoveAll((x) => toRemove.Contains(x));
            if (deployedPlatforms.Count == 0)
            {
                return false;
            }
            toRemove.Clear();
            return true;
        }

        //pretend this list is populated somwhere in your setup
        List<RoR2.Skills.SkillDef> AllSpells = new List<RoR2.Skills.SkillDef>();
        //would convert this to your custom skilldef or wrapper later
        RoR2.Skills.SkillDef[] spellSlots = new RoR2.Skills.SkillDef[4];


       
        // NETWORKING: buffs can only be removed on the server
        [Server]
        public void CleanupHookBombTethers(List<HealthComponent> previousTargets)
        {
            if(previousTargets.Count <= 0) return;
            StartCoroutine(Cleanup());
            IEnumerator Cleanup()
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForFixedUpdate();
                for (int i = 0; i < previousTargets.Count; i++)
                {
                    if (previousTargets[i]?.body && previousTargets[i].body.HasBuff(FishermanBuffs.hookTetherDebuff))
                    {
                        previousTargets[i].body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                    }
                }
            }
        }

    }
}




