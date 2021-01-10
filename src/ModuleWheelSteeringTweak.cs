using System.Collections.Generic;
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
        private const float MIN_STEERING_LIMITER = 1f;
        private const float MAX_STEERING_LIMITER = 100f;
        private const float DEFAULT_AXIS_SPEED = 20f;
        private const string STEERING_MODE_CONSTANT = "constant";
        private const string STEERING_MODE_ADAPTIVE = "adaptive";
        private const float DEFAULT_ADAPTIVE_MAX_SPEED = 50f; // m/s

        private static readonly Dictionary<string, string> steeringModeDisplayNames;

        private ModuleWheelSteering steeringModule = null;

        /// <summary>
        /// Stores whether the wheel's steering is in "constant" or "adaptive" mode.
        /// In "constant", it keeps one steering curve at all speeds, whose limiter value
        /// the user sets with a slider.  In "adaptive", the steering curve is based
        /// on current speed, which the user configures with a somewhat more complex
        /// set of controls.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string steeringMode = STEERING_MODE_CONSTANT;

        /// <summary>
        /// Sets the upper bound on the adaptive-mode speed settings in adaptiveSpeedBounds,
        /// in m/s.
        /// </summary>
        [KSPField]
        public float maxAdaptiveSpeed = DEFAULT_ADAPTIVE_MAX_SPEED;

        /// <summary>
        /// Here when the "change steering mode" button is clicked.  The guiName here is a dummy
        /// that will be replaced dynamically at run time based on the steeringMode field.
        /// </summary>
        /// <param name="actionParam"></param>
        [KSPEvent(guiName = "Change steering mode", guiActive = true, guiActiveEditor = true)]
        public void DoChangeSteeringModeEvent()
        {
            ChangeSteeringMode(steeringMode == STEERING_MODE_CONSTANT, true);
        }
        private BaseEvent ChangeSteeringModeEvent { get { return Events["DoChangeSteeringModeEvent"]; } }

        /// <summary>
        /// Action group for toggling steering mode.
        /// </summary>
        /// <param name="action"></param>
        [KSPAction(guiName = "#SteeringTweaker_toggleSteeringMode")]
        public void DoChangeSteeringModeAction(KSPActionParam action)
        {
            ChangeSteeringMode(action.type == KSPActionType.Activate, false);
        }

        /// <summary>
        /// Sets the steering limiter for the part.  When set to less than 100%, limits the steering range of the part.
        /// </summary>
        [KSPAxisField(
            guiName = "#SteeringTweaker_steeringLimiter",
            guiActive = true,
            guiActiveEditor = true,
            isPersistant = true,
            guiUnits = "%",
            guiFormat = "F0",
            axisMode = KSPAxisMode.Incremental,
            minValue = MIN_STEERING_LIMITER,
            maxValue = MAX_STEERING_LIMITER,
            incrementalSpeed = DEFAULT_AXIS_SPEED),
         UI_FloatRange(
            scene = UI_Scene.All,
            affectSymCounterparts = UI_Scene.All,
            controlEnabled = true,
            minValue = MIN_STEERING_LIMITER,
            maxValue = MAX_STEERING_LIMITER,
            stepIncrement = 1F)]
        public float steeringLimiter = 100f;

        private BaseField SteeringLimiterField { get { return Fields["steeringLimiter"]; } }

        /// <summary>
        /// Sets the min/max speed bounds for adaptive steering.
        /// </summary>
        [KSPField(
            guiName = "#SteeringTweaker_adaptiveSpeedRange",
            guiFormat = "F0",
            isPersistant = true,
            guiActive = false,
            guiActiveEditor = false),
         UI_MinMaxRange(
            minValueX = 0,
            minValueY = 0,
            maxValueX = DEFAULT_ADAPTIVE_MAX_SPEED,
            maxValueY = DEFAULT_ADAPTIVE_MAX_SPEED,
            stepIncrement = 1f,
            affectSymCounterparts = UI_Scene.All)]
        public Vector2 adaptiveSpeedRange = new Vector2(0f, DEFAULT_ADAPTIVE_MAX_SPEED);

        private BaseField AdaptiveSpeedRangeField { get { return Fields["adaptiveSpeedRange"]; } }
        private UI_MinMaxRange AdaptiveSpeedBounds { get { UI_MinMaxRange result; Fields.TryGetFieldUIControl<UI_MinMaxRange>("adaptiveSpeedRange", out result); return result; } }

        /// <summary>
        /// Sets the steering limiter for adaptive mode at the slow-speed bound.
        /// </summary>
        [KSPField(
            guiName = "#SteeringTweaker_adaptiveLimiterSlow",
            guiActive = false,
            guiActiveEditor = false,
            isPersistant = true,
            guiUnits = "%",
            guiFormat = "F0"),
         UI_FloatRange(
            scene = UI_Scene.All,
            affectSymCounterparts = UI_Scene.All,
            controlEnabled = true,
            minValue = MIN_STEERING_LIMITER,
            maxValue = MAX_STEERING_LIMITER,
            stepIncrement = 1F)]
        public float adaptiveLimiterSlow = 100f;

        private BaseField AdaptiveLimiterSlowField { get { return Fields["adaptiveLimiterSlow"]; } }

        /// <summary>
        /// Sets the steering limiter for adaptive mode at the high-speed bound.
        /// </summary>
        [KSPField(
            guiName = "#SteeringTweaker_adaptiveLimiterFast",
            guiActive = false,
            guiActiveEditor = false,
            isPersistant = true,
            guiUnits = "%",
            guiFormat = "F0"),
         UI_FloatRange(
            scene = UI_Scene.All,
            affectSymCounterparts = UI_Scene.All,
            controlEnabled = true,
            minValue = MIN_STEERING_LIMITER,
            maxValue = MAX_STEERING_LIMITER,
            stepIncrement = 1F)]
        public float adaptiveLimiterFast = 100f;

        private BaseField AdaptiveLimiterFastField { get { return Fields["adaptiveLimiterFast"]; } }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ModuleWheelSteeringTweak()
        {
            string steeringModeLabel = KSP.Localization.Localizer.GetStringByTag("#SteeringTweaker_steeringMode");
            string steeringModeConstant = KSP.Localization.Localizer.GetStringByTag("#SteeringTweaker_modeConstant");
            string steeringModeAdaptive = KSP.Localization.Localizer.GetStringByTag("#SteeringTweaker_modeAdaptive");

            steeringModeDisplayNames = new Dictionary<string, string>();
            steeringModeDisplayNames.Add(STEERING_MODE_CONSTANT, string.Format("{0}: {1}", steeringModeLabel, steeringModeConstant));
            steeringModeDisplayNames.Add(STEERING_MODE_ADAPTIVE, string.Format("{0}: {1}", steeringModeLabel, steeringModeAdaptive));
        }


        /// <summary>
        /// Called when the module is starting up.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            InitializeAdaptiveBounds();
            UpdateSteeringMode();

            if (!HighLogic.LoadedSceneIsFlight) return;

            steeringModule = FindModule<ModuleWheelSteering>(part);
            if (steeringModule == null)
            {
                Logging.Warn("No ModuleWheelSteering found for " + part.GetTitle());
                return;
            }
            UpdateSteering();
        }

        /// <summary>
        /// Called on every Unity physics frame.
        /// </summary>
        private void FixedUpdate()
        {
            // We only do stuff in the flight scene, since no steering happens in the editor.
            if (!HighLogic.LoadedSceneIsFlight) return;
            
            UpdateSteering();
        }

        /// <summary>
        /// Updates the steering curve on the part, based on the current setting of steeringLimiter.
        /// </summary>
        private void UpdateSteering()
        {
            SteeringState.Initialize(steeringModule, GetAppliedSteeringLimiter());
        }

        /// <summary>
        /// Set up the "adaptive speed bounds" UI.
        /// </summary>
        private void InitializeAdaptiveBounds()
        {
            AdaptiveSpeedBounds.maxValueX = maxAdaptiveSpeed;
            AdaptiveSpeedBounds.maxValueY = maxAdaptiveSpeed;
            if (maxAdaptiveSpeed <= 10f)
            {
                AdaptiveSpeedBounds.stepIncrement = 0.1f;
                AdaptiveSpeedRangeField.guiFormat = "F1";
                return;
            }
            if (maxAdaptiveSpeed <= 20f)
            {
                AdaptiveSpeedBounds.stepIncrement = 0.2f;
                AdaptiveSpeedRangeField.guiFormat = "F1";
                return;
            }
            if (maxAdaptiveSpeed <= 50f)
            {
                AdaptiveSpeedBounds.stepIncrement = 0.5f;
                AdaptiveSpeedRangeField.guiFormat = "F1";
                return;
            }
        }

        /// <summary>
        /// Here when the "change steering mode" button is pressed.
        /// </summary>
        private void ChangeSteeringMode(bool isAdaptiveMode, bool includeSymmetryCounterparts)
        {
            steeringMode = isAdaptiveMode ? STEERING_MODE_ADAPTIVE : STEERING_MODE_CONSTANT;
            UpdateSteeringMode();
            if (!includeSymmetryCounterparts) return;
            if (part == null) return;
            if (part.symmetryCounterparts == null) return;
            for (int i = 0; i < part.symmetryCounterparts.Count; ++i)
            {
                ModuleWheelSteeringTweak symmetricModule = FindModule<ModuleWheelSteeringTweak>(part.symmetryCounterparts[i]);
                if (symmetricModule == null) continue;
                symmetricModule.steeringMode = steeringMode;
                symmetricModule.UpdateSteeringMode();
            }
            if (HighLogic.LoadedSceneIsFlight) UpdateSteering();
        }

        /// <summary>
        /// Update the PAW based on current steering mode.
        /// </summary>
        private void UpdateSteeringMode()
        {
            string displayName;
            if (!steeringModeDisplayNames.TryGetValue(steeringMode, out displayName)) return;
            ChangeSteeringModeEvent.guiName = displayName;

            switch (steeringMode)
            {
                case STEERING_MODE_ADAPTIVE:
                    SteeringLimiterField.guiActive = SteeringLimiterField.guiActiveEditor = false;
                    AdaptiveSpeedRangeField.guiActive = AdaptiveSpeedRangeField.guiActiveEditor = true;
                    AdaptiveLimiterSlowField.guiActive = AdaptiveLimiterSlowField.guiActiveEditor = true;
                    AdaptiveLimiterFastField.guiActive = AdaptiveLimiterFastField.guiActiveEditor = true;
                    break;
                default:
                    SteeringLimiterField.guiActive = SteeringLimiterField.guiActiveEditor = true;
                    AdaptiveSpeedRangeField.guiActive = AdaptiveSpeedRangeField.guiActiveEditor = false;
                    AdaptiveLimiterSlowField.guiActive = AdaptiveLimiterSlowField.guiActiveEditor = false;
                    AdaptiveLimiterFastField.guiActive = AdaptiveLimiterFastField.guiActiveEditor = false;
                    break;
            }
        }

        /// <summary>
        /// Gets the current value of the steering limiter to use. Should only be called in flight scene.
        /// </summary>
        /// <returns></returns>
        private float GetAppliedSteeringLimiter()
        {
            if (steeringMode != STEERING_MODE_ADAPTIVE) return steeringLimiter;
            if ((part == null) || (part.vessel == null)) return steeringLimiter;
            Vessel vessel = part.vessel;
            switch (vessel.situation)
            {
                    case Vessel.Situations.PRELAUNCH:
                    case Vessel.Situations.ORBITING:
                    case Vessel.Situations.ESCAPING:
                        return adaptiveLimiterSlow;
                    default:
                        break;
            }
            if (vessel.srfSpeed <= adaptiveSpeedRange.x) return adaptiveLimiterSlow;
            if (vessel.srfSpeed >= adaptiveSpeedRange.y) return adaptiveLimiterFast;
            double fraction = (vessel.srfSpeed - adaptiveSpeedRange.x) / (adaptiveSpeedRange.y - adaptiveSpeedRange.x);
            return (float)(adaptiveLimiterSlow + fraction * (adaptiveLimiterFast - adaptiveLimiterSlow));
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
    }
}
