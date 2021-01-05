using ModuleWheels;
using UnityEngine;

namespace SteeringTweaker
{
    /// <summary>
    /// When applied to a part that has a ModuleWheelSteering, allows setting a steering limiter to reduce the
    /// range of steering motion of the part.
    /// </summary>
    public class ModuleWheelSteeringTweak : PartModule
    {
        private ModuleWheelSteering steeringModule = null;
        private FloatCurve originalSteeringCurve = null;
        private FloatCurve scaledSteeringCurve = null;

        /// <summary>
        /// Sets the steering limiter for the part.  When set to less than 100%, limits the steering range of the part.
        /// </summary>
        [KSPField(guiName = "#SteeringTweaker_steeringLimiter", guiActive = false, guiActiveEditor = true, isPersistant = true, guiUnits ="%"),
         UI_FloatRange(scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All, controlEnabled = true, minValue = 1F, maxValue = 100F, stepIncrement = 1F)]
        public float steeringLimiter = 100f;

        /// <summary>
        /// Called when the module is starting up.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight) return;

            steeringModule = FindModule<ModuleWheelSteering>(part);
            if (steeringModule == null)
            {
                Logging.Warn("No ModuleWheelSteering found for " + part.GetTitle());
                return;
            }
            originalSteeringCurve = steeringModule.steeringCurve;
            UpdateSteering();
        }

        /// <summary>
        /// Updates the steering curve on the part, based on the current setting of steeringLimiter.
        /// </summary>
        private void UpdateSteering()
        {
            scaledSteeringCurve = GetScaledCurve(originalSteeringCurve, steeringLimiter);
            if (scaledSteeringCurve == null) return;
            float steeringRange;
            scaledSteeringCurve.FindMinMaxValue(out var _, out steeringRange);
            steeringRange = Mathf.Max(steeringRange, 0.01f);
            steeringModule.steeringCurve = scaledSteeringCurve;
            steeringModule.steeringRange = steeringRange;
            steeringModule.steeringResponse *= steeringLimiter * 0.01f;
        }

        /// <summary>
        /// Try to find a ModuleWheelSteering on the current part and return it.
        /// Returns null if none found.
        /// </summary>
        /// <returns></returns>
        private static T FindModule<T>(Part part) where T : PartModule
        {
            if (part == null) return null;
            for (int i = 0; i < part.Modules.Count; ++i)
            {
                T candidate = part.Modules[i] as T;
                if (candidate != null) return candidate;
            }
            return null;
        }

        /// <summary>
        /// Given a float curve and a percentage scale, return a new curve whose values
        /// and slopes are the specified percentage of the original.
        /// </summary>
        /// <param name="originalCurve"></param>
        /// <param name="scalePercent"></param>
        /// <returns></returns>
        private static FloatCurve GetScaledCurve(FloatCurve originalCurve, float scalePercent)
        {
            if (originalCurve == null) return null;
            if (originalCurve.Curve == null) return null;
            Keyframe[] keys = originalCurve.Curve.keys;
            if (keys == null) return null;
            float scale = scalePercent / 100f;
            FloatCurve newCurve = new FloatCurve();
            for (int i = 0; i < keys.Length; ++i)
            {
                Keyframe key = keys[i];
                newCurve.Add(key.time, key.value * scale);
            }
            return newCurve;
        }
    }
}
