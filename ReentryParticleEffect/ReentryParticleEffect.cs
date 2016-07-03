using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ReentryParticleEffect
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ReentryParticleEffect : MonoBehaviour
    {
        public Vector3 velocity;
        public int MaxParticles = 3000;
        public int MaxEmissionRate = 400;
        public float EffectMultiplier = 0.9f;

        private void Start()
        {
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        public class ReentryEffect
        {
            public ReentryEffect(ParticleSystem trail, ParticleSystem sparks)
            {
                Trail = trail;
                Sparks = sparks;
            }
            public ParticleSystem Trail;
            public ParticleSystem Sparks;
        }

        public ReentryEffect GetEffect()
        {
            GameObject effect = (GameObject)GameObject.Instantiate(Resources.Load("Effects/fx_reentryTrail"));
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            ReentryEffect reentryFx = new ReentryEffect(particleSystems[0], particleSystems[1]);
            reentryFx.Trail.playbackSpeed = 5;
            reentryFx.Sparks.playbackSpeed = 5;
            return reentryFx;
        }

        public Dictionary<Guid, ReentryEffect> VesselDict = new Dictionary<Guid, ReentryEffect>();

        private void FixedUpdate()
        {
            float effectStrength = AeroFX.FxScalar * AeroFX.state * EffectMultiplier;
            List<Vessel> vessels = FlightGlobals.Vessels;
            for (int i = vessels.Count - 1; i >= 0; --i)
            {
                Vessel vessel = vessels[i];
                ReentryEffect effects  = null;
                if (VesselDict.ContainsKey(vessel.id))
                    effects = VesselDict[vessel.id];
                else
                {
                    if (vessel.loaded)
                    {
                        effects = GetEffect();
                        VesselDict.Add(vessel.id, effects);
                    }
                    else
                        continue;
                }

                if (!vessel.loaded)
                {
                    if (effects != null)
                    {
                        Destroy(effects.Sparks);
                        Destroy(effects.Trail);
                    }
                    effects = null;
                    continue;
                }

                if (effects == null || effects.Trail == null || effects.Sparks == null)
                    continue;

                if (AeroFX != null)
                {
                    // FxScalar: Strength of the effects.
                    // state: 0 = condensation, 1 = reentry.
                    if (effectStrength > 0)
                    {
                        // Ensure the particles don't lag a frame behind.
                        effects.Trail.transform.position = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;
                        effects.Trail.enableEmission = true;
                        effects.Sparks.transform.position = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;
                        effects.Sparks.enableEmission = true;

                        velocity = AeroFX.velocity * (float)AeroFX.airSpeed;

                        effects.Trail.startSpeed = velocity.magnitude;
                        effects.Trail.transform.forward = -velocity.normalized;
                        effects.Trail.maxParticles = (int)(MaxParticles * effectStrength);
                        effects.Trail.emissionRate = (int)(MaxEmissionRate * effectStrength);

                        // startSpeed controls the emission cone angle. Greater than ~1 is too wide.
                        //reentryTrailSparks.startSpeed = velocity.magnitude;
                        effects.Sparks.transform.forward = -velocity.normalized;
                        effects.Sparks.maxParticles = (int)(MaxParticles * effectStrength);
                        effects.Sparks.emissionRate = (int)(MaxEmissionRate * effectStrength);
                    }
                    else
                    {
                        effects.Trail.enableEmission = false;
                        effects.Sparks.enableEmission = false;
                    }
                }
                else
                {
                    effects.Trail.enableEmission = false;
                    effects.Sparks.enableEmission = false;
                }
            }
        }

        public void OnVesselDestroy(Vessel vessel)
        {
            if (VesselDict.ContainsKey(vessel.id))
            {
                ReentryEffect effects = VesselDict[vessel.id];
                if (effects != null)
                {
                    Destroy(effects.Trail);
                    Destroy(effects.Sparks);
                }
                VesselDict.Remove(vessel.id);
            }
        }

        private AerodynamicsFX _aeroFX;
        AerodynamicsFX AeroFX
        {
            get
            {
                if (_aeroFX == null)
                {
                    GameObject fxLogicObject = GameObject.Find("FXLogic");
                    if (fxLogicObject != null)
                        _aeroFX = fxLogicObject.GetComponent<AerodynamicsFX>();
                }
                return _aeroFX;
            }
        }
    }
}
