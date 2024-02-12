using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FishermanMod.Survivors.Fisherman.Components
{
    internal class HookBombTetherVisual : MonoBehaviour
    {
        LineRenderer lineRenderer;
        public HookBombController hookBomb = FishermanSurvivor.deployedHookBomb;

        void Start ()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = FishermanAssets.chainMat;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            
        }
        void Update ()
        {
            if (hookBomb == null)
            {
                Destroy(lineRenderer);
                Destroy(this);
            }
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hookBomb.transform.position);
        }
        

    }
}
