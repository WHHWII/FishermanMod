using FishermanMod.Characters.Survivors.Fisherman.Components;
using IL.RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.UI.GridLayoutGroup;
using Random = UnityEngine.Random;


namespace FishermanMod.Survivors.Fisherman.Components
{
    public class BombletController : MonoBehaviour
    {
        
        public ProjectileImpactExplosion pie;
        public ProjectileController controller;
        public ProjectileSimple ps;
        public RoR2.AntiGravityForce agf;
        SkillObjectTracker tracker;
        
        
        void Start ()
        {
            var owner = controller.owner;
            if (!owner) return;
            tracker = owner.GetComponent<SkillObjectTracker>();
            tracker.deployedBomblets.Add(this);

            // NETWORKING: these values drive movement, which clients may simulate locally between network updates.
            // UnityEngine.Random would roll differently on every machine, so the rolls are seeded from the bomblet's netId,
            // which is the same everywhere. No extra network traffic needed.
            System.Random rng = new System.Random((int)GetComponent<NetworkIdentity>().netId.Value);
            pie.lifetime = FishermanStaticValues.bombletBaseLifetime + (tracker.deployedBomblets.Count * 0.3f);
            ps.lifetime = pie.lifetime * 10f;
            ps.desiredForwardSpeed += (tracker.deployedBomblets.Count * Mathf.Lerp(0.2f, 0.5f, (float)rng.NextDouble()));
            agf.antiGravityCoefficient -= (tracker.deployedBomblets.Count * Mathf.Lerp(0f, 0.1f, (float)rng.NextDouble()));
        }

    }
}
