using IL.RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FishermanMod.Survivors.Fisherman.Components
{
    /// <summary>
    ///this component is added to enemies to show that they are attached to a hook bomb. 
    ///It is added by the FishermanTetherDamage type, and the hook for that damage type assigns the line termination object to the inflictor
    /// </summary>
    internal class HookBombTetherVisual : MonoBehaviour
    {
        LineRenderer lineRenderer;
        GameObject lineContainer;
        public GameObject lineTerminationObject;

        //setup appearance for tether
        void Start ()
        {
            lineContainer = Instantiate(new GameObject("Fisherman HookBomb TetherVisual"), transform);
            lineRenderer = lineContainer.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = FishermanAssets.chainMat;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            Log.Debug($"Tether Terminator: {lineTerminationObject.name}");
            
        }
        //update the end points of the line renderer each frame to be at the enemy parent and at the inflictor
        //if the inflictor no longer exists, it destroys itself.
        void Update ()
        {
            if (lineTerminationObject == null)
            {
                Destroy(lineContainer);
                //Destroy(lineRenderer);
                RoR2.CharacterBody body = GetComponent<RoR2.CharacterBody>();
                //even though this is just a visual, and the debuff should be removed already by other components, this is here just in case this damage type is used anywhere else
                if (body.HasBuff(FishermanBuffs.hookTetherDebuff))
                {
                    body.RemoveBuff(FishermanBuffs.hookTetherDebuff);
                }
                Destroy(this);
                return;
            }
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, lineTerminationObject.transform.position);
        }
        

    }
}
