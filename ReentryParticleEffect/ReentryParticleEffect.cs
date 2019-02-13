using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ReentryParticleEffect
{
    /*
     * BUGS
     * 
     * Trail too dense
     * Trail doesn't ease in at top of atmosphere
     * Trail isn't deleted on craft destroyed
     * Trail too short?
     */
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ReentryParticleEffect : MonoBehaviour
    {
        public Vector3 velocity;
        public static int MaxParticles = 3000;
        public static int MaxEmissionRate = 200;
        public static float TrailScale = 0.15f;
        // Minimum reentry strength that the effects will activate at.
        // 0 = Activate at the first sign of the flame effects.
        // 1 = Never activate, even at the strongest reentry strength.
        public float EffectThreshold = 0.4f;

        public static bool DrawGui = true;

        private void Start()
        {
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        public class ReentryEffect
        {
            public ReentryEffect(GameObject effect)
            {
                ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                Trail = particleSystems[0];
                Sparks = particleSystems[1];
                FXPrefab[] prefabs = effect.GetComponentsInChildren<FXPrefab>();
                trailPrefab = prefabs[0];
            }
            public FXPrefab trailPrefab;
            public ParticleSystem Trail;
            public ParticleSystem Sparks;

            public void Die()
            {
                Destroy(trailPrefab);
                Destroy(Trail);
                Destroy(Sparks);
            }
        }

        public ReentryEffect CreateEffect()
        {
            GameObject effect = (GameObject)GameObject.Instantiate(Resources.Load("Effects/fx_reentryTrail"));
            ReentryEffect reentryFx = new ReentryEffect(effect);
            // Set the effect speed high to animate as fast as is visible.
            var trailMain = reentryFx.Trail.main;
            reentryFx.Trail.transform.localScale = new Vector3(TrailScale, TrailScale, TrailScale);
            trailMain.scalingMode = ParticleSystemScalingMode.Local;
            trailMain.simulationSpeed = 5;

            var sparksMain = reentryFx.Sparks.main;
            sparksMain.simulationSpeed = 5;

            return reentryFx;
        }

        public static Dictionary<Guid, ReentryEffect> VesselDict = new Dictionary<Guid, ReentryEffect>();

        private void FixedUpdate()
        {
            float effectStrength = (AeroFX.FxScalar * AeroFX.state - EffectThreshold) * (1 / EffectThreshold);
            List<Vessel> vessels = FlightGlobals.Vessels;
            for (int i = vessels.Count - 1; i >= 0; --i)
            {
                Vessel vessel = vessels[i];
                ReentryEffect effects = null;
                if (VesselDict.ContainsKey(vessel.id))
                    effects = VesselDict[vessel.id];
                else
                {
                    if (vessel.loaded)
                    {
                        effects = CreateEffect();
                        VesselDict.Add(vessel.id, effects);
                    }
                    else
                        continue;
                }

                if (!vessel.loaded)
                {
                    if (effects != null)
                    {
                        effects.Die();
                    }
                    effects = null;
                    continue;
                }

                if (effects == null || effects.Trail == null || effects.Sparks == null)
                    continue;

                ParticleSystem.EmissionModule trailEmission = effects.Trail.emission;
                ParticleSystem.EmissionModule sparksEmission = effects.Sparks.emission;
                if (AeroFX != null)
                {
                    //effects.Trail.transform.localScale = new Vector3(TrailScale, TrailScale, TrailScale);
#if DEBUG
                    afx1 = AeroFX;
                    
                    effects.Trail.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

                    var main = effects.Trail.main;
                    main.scalingMode = _scalingMode;
#endif

                    // FxScalar: Strength of the effects.
                    // state: 0 = condensation, 1 = reentry.
                    if (effectStrength > 0)
                    {
                        // Ensure the particles don't lag a frame behind.
                        effects.Trail.transform.position = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;
                        trailEmission.enabled = true;
                        effects.Sparks.transform.position = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;
                        sparksEmission.enabled = true;

                        velocity = AeroFX.velocity * (float)AeroFX.airSpeed;

                        var trailMain = effects.Trail.main;
                        trailMain.startSpeed = velocity.magnitude;
                        effects.Trail.transform.forward = -velocity.normalized;
                        trailMain.maxParticles = (int)(MaxParticles * effectStrength);
                        trailEmission.rateOverTime = (int)(MaxEmissionRate * effectStrength);

                        // startSpeed controls the emission cone angle. Greater than ~1 is too wide.
                        //reentryTrailSparks.startSpeed = velocity.magnitude;
                        var sparksMain = effects.Sparks.main;
                        effects.Sparks.transform.forward = -velocity.normalized;
                        sparksMain.maxParticles = (int)(MaxParticles * effectStrength);
                        sparksEmission.rateOverTime = (int)(MaxEmissionRate * effectStrength);
                    }
                    else
                    {
                        trailEmission.enabled = false;
                        sparksEmission.enabled = false;
                    }
                }
                else
                {
                    trailEmission.enabled = false;
                    sparksEmission.enabled = false;
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
                    effects.Die();
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


        public static Color BlackBodyToRgb(float tempKelvin)
        {
            // C# implementation of Tanner Helland's approximation from here:
            // http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
            // For use with temperatures between 1000 and 40,000 K.
            float temp = tempKelvin / 100;
            // Colour values (0 to 255).
            float red = 0;
            float green = 0;
            float blue = 0;

            // Calculate red
            if (temp <= 66)
            {
                red = 255;
            }
            else
            {
                red = temp - 60f;
                red = 329.698727446f * (float)(Math.Pow(red, -0.1332047592f));
                if (red < 0)
                    red = 0;
                if (red > 255)
                    red = 255;
            }

            // Calculate green
            if (temp <= 66)
            {
                green = temp;
                green = 99.4708025861f * (float)Math.Log(green) - 161.1195681661f;
                if (green < 0)
                    green = 0;
                if (green > 255)
                    green = 255;
            }
            else
            {
                green = temp - 60;
                green = 288.1221695283f * (float)Math.Pow(green, -0.0755148492f);
                if (green < 0)
                    green = 0;
                if (green > 255)
                    green = 255;
            }

            // Calculate Blue
            if (temp <= 66)
            {
                blue = 255;
            }
            else
            {
                blue = temp - 10;
                blue = 138.5177312231f * (float)Math.Log(blue) - 305.0447927307f;
                if (blue < 0)
                    blue = 0;
                if (blue > 255)
                    blue = 255;
            }

            return new Color(red/255, green/255, blue/255, 1);
        }

#if DEBUG
        private static float effectStrength = 0;
        private static AerodynamicsFX afx1 = null;
        private static Rect windowPos = new Rect(Screen.width / 4, Screen.height / 4, 10f, 10f);

        /// <summary>
        /// GUI draw event. Called (at least once) each frame.
        /// </summary>
        public void OnGUI()
        {
            if (DrawGui)
                windowPos = GUILayout.Window(GetInstanceID(), windowPos, Gui, "Test GUI", GUILayout.Width(600), GUILayout.Height(50));
        }
        private static string _trailPlaybackText = "5";
        private static string _sparksPlaybackText = "5";
        private static ParticleSystemScalingMode _scalingMode;
        private static float scaleX;
        private static float scaleY;
        private static float scaleZ;
        private static string scaleXText = "1";
        private static string scaleYText = "1";
        private static string scaleZText = "1";


        public static void Gui(int windowID)
        {
            ReentryEffect effects = null;
            if (VesselDict.ContainsKey(FlightGlobals.ActiveVessel.id))
                effects = VesselDict[FlightGlobals.ActiveVessel.id];

            if (effects == null)
            {
                GUILayout.Label("ReentryFX is null");
                return;
            }
            if (effects.Trail == null)
            {
                GUILayout.Label("Trail is null");
            }
            if (effects.Sparks == null)
            {
                GUILayout.Label("Sparks is null");
            }
            
            GuiUtils.label("Effect Strength", effectStrength);
            GuiUtils.label("Stock effect Strength", afx1.FxScalar * afx1.state);
            
            float highestTemp = GetVesselMaxSkinTemp();
            Color blackBodyColor = BlackBodyToRgb(highestTemp);
            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.rootPart != null)
            {
                GuiUtils.label("Highest temp", highestTemp);
                GuiUtils.label("Blackbody colour", blackBodyColor);

                GuiUtils.label("temperature", FlightGlobals.ActiveVessel.rootPart.temperature);
                GuiUtils.label("skinTemperature", FlightGlobals.ActiveVessel.rootPart.skinTemperature);
                GuiUtils.label("skinUnexposedExternalTemp", FlightGlobals.ActiveVessel.rootPart.skinUnexposedExternalTemp);
                GuiUtils.label("tempExplodeChance", FlightGlobals.ActiveVessel.rootPart.tempExplodeChance);

                GuiUtils.label("MaxTemp", FlightGlobals.ActiveVessel.rootPart.maxTemp);
                GuiUtils.label("SkinMaxTemp", FlightGlobals.ActiveVessel.rootPart.skinMaxTemp);
            }

            GUILayout.Label("Max Particles");
            MaxParticles = (int)GUILayout.HorizontalSlider(MaxParticles, 0, 10000);
            GUILayout.Label("Max Emission Rate");
            MaxEmissionRate = (int)GUILayout.HorizontalSlider(MaxEmissionRate, 0, 1000);

            GUILayout.Label("Trail");
            if (effects.Trail == null)
                GUILayout.Label("Trail is null");
            else
            {
                var trailMain = effects.Trail.main;
                float trailPlaybackSpeed = trailMain.simulationSpeed;
                _trailPlaybackText = GuiUtils.editFloat("Playback speed", _trailPlaybackText, out trailPlaybackSpeed, 5);
                trailMain.simulationSpeed = trailPlaybackSpeed;

                //Color key0 = new Color(1f, 0.545f, 0.192f, 1f);
                //Color key1 = new Color(0.725f, 0.169f, 0f, 1f);
                Color key0 = effects.Trail.colorOverLifetime.color.gradient.colorKeys[0].color;
                Color key1 = effects.Trail.colorOverLifetime.color.gradient.colorKeys[1].color;

                /*Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.red, 1.0f) }, 
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                    );
                */

                key0 = GuiUtils.rgbaSlider("Colour Gradient 0", ref key0.r, ref key0.g, ref key0.b, ref key0.a, 0f, 1f);
                key1 = GuiUtils.rgbaSlider("Colour Gradient 1", ref key1.r, ref key1.g, ref key1.b, ref key1.a, 0f, 1f);

                effects.Trail.colorOverLifetime.color.gradient.colorKeys[0].color = key0;
                effects.Trail.colorOverLifetime.color.gradient.colorKeys[1].color = key1;

                //Color cMax = trailMain.startColor.colorMax;
                //Color cMin = trailMain.startColor.colorMin;
                //cMax = GuiUtils.rgbaSlider("Max Colour", ref cMax.r, ref cMax.g, ref cMax.b, ref cMax.a, 0f, 1f);
                //cMin = GuiUtils.rgbaSlider("Min Colour", ref cMin.r, ref cMin.g, ref cMin.b, ref cMin.a, 0f, 1f);
                //trailMain.startColor = new ParticleSystem.MinMaxGradient(cMin, cMax);
                //trailMain.startColor = new ParticleSystem.MinMaxGradient(blackBodyColor, BlackBodyToRgb(highestTemp * 2));
            }

            /*
            GUILayout.Label("Sparks");
            if (effects.Sparks == null)
                GUILayout.Label("Sparks is null");
            else
            {
                var sparksMain = effects.Sparks.main;
                float sparksPlaybackSpeed = sparksMain.simulationSpeed;
                _sparksPlaybackText = GuiUtils.editFloat("Playback speed", _sparksPlaybackText, out sparksPlaybackSpeed, 5);
                sparksMain.simulationSpeed = sparksPlaybackSpeed;
            }*/

            scaleXText = GuiUtils.editFloat("X Scale", scaleXText, out scaleX, 1);
            scaleYText = GuiUtils.editFloat("Y Scale", scaleYText, out scaleY, 1);
            scaleZText = GuiUtils.editFloat("Z Scale", scaleZText, out scaleZ, 1);
            GUI.DragWindow();
        }

        public static float GetVesselMaxSkinTemp()
        {
            float maxTemp = 0;

            int partCount = FlightGlobals.ActiveVessel.parts.Count;
            for (int i = 0; i < partCount; i++)
            {
                maxTemp = (float)Math.Max(FlightGlobals.ActiveVessel.parts[i].skinTemperature, maxTemp);
            }

            return maxTemp;
        }
#endif
    }

    /*[KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class AutoStartup : UnityEngine.MonoBehaviour
    {
        public static bool first = true;
        public void Start()
        {
            //only do it on the first entry to the menu
            if (first)
            {
                first = false;
                HighLogic.SaveFolder = "test";
                /*var game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible)
                    FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);* /
                CheatOptions.InfinitePropellant = true;
                CheatOptions.InfiniteElectricity = true;
                CheatOptions.IgnoreMaxTemperature = true;
            }
        }
    }*/
}
