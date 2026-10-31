using FishermanMod.Characters.Survivors.Fisherman.Components;
using HG;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishermanMod.Survivors.Fisherman.Components
{
    [RequireComponent(typeof(PointViewer))]
    internal class FishermanObjectViewer : MonoBehaviour
    {
        SkillObjectTracker objectTracker;

        float maxViewRange = 9000;
        float minViewRange = 25;

        public GameObject visualizerPrefab;

        public PointViewer pointViewer;

        public HUD hud;

        public Dictionary<UnityObjectWrapperKey<GameObject>, GameObject> objectToVisualizer = new Dictionary<UnityObjectWrapperKey<GameObject>, GameObject>();

        public List<GameObject> displayedTargets = new List<GameObject>();

        public List<GameObject> previousDisplayedTargets = new List<GameObject>();

        public void Awake()
        {
            pointViewer = GetComponent<PointViewer>();
            OnTransformParentChanged();
            Log.Debug("Hook viewer awaked : " + transform.name);
        }

        public void OnTransformParentChanged()
        {
            hud = GetComponentInParent<HUD>();
        }

        public void OnDisable()
        {
            SetDisplayedTargets(Array.Empty<GameObject>());
            objectToVisualizer.Clear();
        }

        public void Update()
        {
            var list = CollectionPool<GameObject, List<GameObject>>.RentCollection();
            if (hud && hud.targetBodyObject)
            {
                // NETWORKING: on clients the lists can briefly hold objects the server already destroyed, hence the null checks below.
                // Also re-fetch when spectating someone else so we don't keep showing the previous body's objects.
                if (!objectTracker || objectTracker.gameObject != hud.targetBodyObject) objectTracker = hud.targetBodyObject.GetComponent<SkillObjectTracker>();
            }
            if (objectTracker && hud && hud.targetBodyObject)
            {
                foreach (var hookObject in objectTracker.deployedHooks)
                {
                    if (!hookObject) continue;
                    var dist = Vector3.Distance(hookObject.transform.position, hud.targetBodyObject.transform.position);
                    if (dist < maxViewRange && dist > minViewRange)
                    {
                        list.Add(hookObject.gameObject);
                    }
                }
                foreach (var hookObject in objectTracker.deployedBombs)
                {
                    if (!hookObject) continue;
                    var dist = Vector3.Distance(hookObject.transform.position, hud.targetBodyObject.transform.position);
                    if (dist < maxViewRange && dist > minViewRange)
                    {
                        list.Add(hookObject.gameObject);
                    }
                }
                foreach (var hookObject in objectTracker.deployedPlatforms)
                {
                    if (!hookObject) continue;
                    var dist = Vector3.Distance(hookObject.transform.position, hud.targetBodyObject.transform.position);
                    if (dist < maxViewRange && dist > minViewRange)
                    {
                        list.Add(hookObject.gameObject);
                    }
                }
            }

            SetDisplayedTargets(list);
            list = CollectionPool<GameObject, List<GameObject>>.ReturnCollection(list);
        }

        public void OnTargetDiscovered(GameObject targetObject)
        {
            if (!objectToVisualizer.ContainsKey(targetObject))
            {
                objectToVisualizer.Add(targetObject, pointViewer.AddElement(new PointViewer.AddElementRequest
                {
                    elementPrefab = visualizerPrefab,
                    target = targetObject.transform,
                    targetWorldVerticalOffset = 0f,
                    targetWorldRadius = HurtBox.sniperTargetRadius,
                    scaleWithDistance = true
                })) ;
            }
            else
            {
                Debug.LogWarning($"Already discovered hurtbox: {targetObject}");
            }
        }

        public void OnTargetLost(GameObject targetObject)
        {
            previousDisplayedTargets.Remove(targetObject);
            if (objectToVisualizer.Remove(targetObject, out var value))
            {
                pointViewer.RemoveElement(value);
            }
        }

        public void SetDisplayedTargets(IReadOnlyList<GameObject> newDisplayedTargets)
        {
            Util.Swap(ref displayedTargets, ref previousDisplayedTargets);
            displayedTargets.Clear();
            ListUtils.AddRange(displayedTargets, newDisplayedTargets);
            var list = CollectionPool<GameObject, List<GameObject>>.RentCollection();
            var list2 = CollectionPool<GameObject, List<GameObject>>.RentCollection();
            ListUtils.FindExclusiveEntriesByReference(displayedTargets, previousDisplayedTargets, list, list2);
            foreach (var item in list2)
            {
                OnTargetLost(item);
            }

            foreach (var item2 in list)
            {
                OnTargetDiscovered(item2);
            }

            list2 = CollectionPool<GameObject, List<GameObject>>.ReturnCollection(list2);
            list = CollectionPool<GameObject, List<GameObject>>.ReturnCollection(list);
        }
    }
}
