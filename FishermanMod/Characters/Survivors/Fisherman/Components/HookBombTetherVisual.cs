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
        GameObject lineContainer;
        public HookBombController hookBomb = FishermanSurvivor.deployedHookBomb;

        void Start ()
        {
            lineContainer = Instantiate(new GameObject("Fisherman HookBomb TetherVisual"), transform);
            lineRenderer = lineContainer.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.material = FishermanAssets.chainMat;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            
        }
        void Update ()
        {
            if (hookBomb == null)
            {
                Destroy(lineContainer);
                //Destroy(lineRenderer);
                Destroy(this);
                return;
            }
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hookBomb.transform.position);
        }
        

    }
}
