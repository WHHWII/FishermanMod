using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using Vector3 = UnityEngine.Vector3;

namespace FishermanMod.Characters.Survivors.Fisherman.Components
{
    public class HookedObjectProjectile : NetworkBehaviour
    {
        public Vector3 StartPos { set { lastPos = value; startpos = value; } get { return startpos; } }
        Vector3 startpos;
        public Vector3 endPos;
        public float maxDistance;
        Vector3 lastPos;
        float traveled = 0;
        public ThrownObjectProjectileController proj;
        float dur = 1;
        float timestamp;
        public bool checkDistance;
        public float debug;
        void CheckReachedEnd()
        {
            if (!NetworkServer.active) return;
            if (Vector3.Distance(transform.position, endPos) <= 5 || traveled >= maxDistance)
            {
                proj.ranCleanup = true;
                proj.OnEarlyExit.Invoke();
                UnityEngine.Object.Destroy(base.gameObject);

            }
        }

        public void Start()
        {
            Log.Debug($"s:{startpos} e:{endPos} proj vel: {proj.projectileRigidbody.velocity.normalized}");

        }

        public void CheckDeadPassenger()
        {
            proj.CheckForDeadPassenger(new RoR2.BlastAttack(), new RoR2.BlastAttack.Result());
        }

        void FixedUpdate()
        {
            Log.Debug($" proj vel: {proj.projectileRigidbody.velocity.normalized}");

            if (Time.time >= timestamp)
            {

                if (checkDistance)
                {
                    traveled += Vector3.Distance(transform.position, lastPos);
                    lastPos = transform.position;

                    timestamp = Time.time + dur;
                    CheckReachedEnd();
                }
                CheckDeadPassenger();
            }
        }

    }
}
