using System.Collections.Generic;
using ModuleWheels;
using UnityEngine;

namespace SteeringTweaker
{
    /// <summary>
    /// An instance of this class stores the necessary information for updating the state of
    /// a ModuleWheelSteering to reflect a given steering limiter value.
    /// 
    /// A private static field maintains a read-only list of pre-generated values for any parts
    /// in use, in order to avoid spamming the heap by having to create lots of new FloatCurve
    /// objects at runtime.
    /// </summary>
    class SteeringState
    {
        private readonly FloatCurve steeringCurve;
        private readonly float steeringRange;
        private readonly float steeringResponse;

        /// <summary>
        /// Key = part name
        /// 
        /// Value = array of 100 SteeringState values, corresponding to steering limiter percentages 1 to 100
        /// </summary>
        private static readonly Dictionary<string, SteeringState[]> partStates = new Dictionary<string, SteeringState[]>();

        /// <summary>
        /// Given a ModuleWheelSteering, initialize it for the specified steering limiter value.
        /// </summary>
        /// <param name="steeringModule"></param>
        /// <param name="steeringLimiter"></param>
        /// <returns></returns>
        public static void Initialize(ModuleWheelSteering steeringModule, float steeringLimiter)
        {
            if ((steeringModule == null) || (steeringModule.part == null)) return;
            string partName = steeringModule.part.partInfo.name;

            // Get the set of states for all steering limiter values, for this part
            SteeringState[] states;
            if (!partStates.TryGetValue(partName, out states))
            {
                Logging.Log("Initializing steering states for " + partName);
                states = GetSteeringStatesFor(steeringModule);
                partStates.Add(partName, states);
            }

            // Work out which of them to use, based on the steering limiter
            int index;
            if (steeringLimiter <= 1f)
            {
                index = 0;
            }
            else if (steeringLimiter >= 100f)
            {
                index = 99;
            }
            else
            {
                index = (int)(steeringLimiter + 0.5f) - 1;
            }

            // Now write the state into the steering module
            states[index].Initialize(steeringModule);
        }

        /// <summary>
        /// Write the steering state's values into the steering module.
        /// </summary>
        /// <param name="steeringModule"></param>
        public void Initialize(ModuleWheelSteering steeringModule)
        {
            steeringModule.steeringCurve = steeringCurve;
            steeringModule.steeringRange = steeringRange;
            steeringModule.steeringResponse = steeringResponse;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="originalSteeringCurve">The original steering curve on which this is based.</param>
        /// <param name="originalSteeringResponse">The original steering response.</param>
        /// <param name="steeringLimiter">A percentage amount, in the range 0-100, by which to scale the original curve.</param>
        private SteeringState(FloatCurve originalSteeringCurve, float originalSteeringResponse, float steeringLimiter)
        {
            // Set up the steering curve first
            if (steeringLimiter == 100f)
            {
                steeringCurve = originalSteeringCurve;
            }
            else
            {
                Keyframe[] keys = originalSteeringCurve.Curve.keys;
                float scale = steeringLimiter / 100f;
                steeringCurve = new FloatCurve();
                for (int i = 0; i < keys.Length; ++i)
                {
                    Keyframe key = keys[i];
                    steeringCurve.Add(key.time, key.value * scale);
                }
            }
            // Set up the steering range and steering response based on the curve
            steeringCurve.FindMinMaxValue(out var _, out steeringRange);
            steeringRange = Mathf.Max(steeringRange, 0.01f);
            steeringResponse = originalSteeringResponse * steeringLimiter * 0.01f;
        }

        /// <summary>
        /// Get an array of SteeringState, corresponding to steering limiter values in the range 1 to 100.
        /// </summary>
        /// <param name="steeringModule"></param>
        /// <returns></returns>
        private static SteeringState[] GetSteeringStatesFor(ModuleWheelSteering steeringModule)
        {
            SteeringState[] states = new SteeringState[100];
            for (int steeringLimiter = 1; steeringLimiter <= 100; ++steeringLimiter)
            {
                states[steeringLimiter - 1] = new SteeringState(
                    steeringModule.steeringCurve,
                    steeringModule.steeringResponse,
                    steeringLimiter);
            }
            return states;
        }
    }
}
