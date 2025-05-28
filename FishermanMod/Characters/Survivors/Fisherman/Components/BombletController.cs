using FishermanMod.Characters.Survivors.Fisherman.Components;
using IL.RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
            tracker = owner.GetComponent<SkillObjectTracker>();
            tracker.deployedBomblets.Add(this);
            pie.lifetime = FishermanStaticValues.bombletBaseLifetime + (tracker.deployedBomblets.Count * 0.3f);
            ps.lifetime = pie.lifetime * 10f;
            ps.desiredForwardSpeed += (tracker.deployedBomblets.Count * Random.Range(0.2f, 0.5f));
            agf.antiGravityCoefficient -= (tracker.deployedBomblets.Count * Random.Range(0, 0.1f));
        }

    }
}
