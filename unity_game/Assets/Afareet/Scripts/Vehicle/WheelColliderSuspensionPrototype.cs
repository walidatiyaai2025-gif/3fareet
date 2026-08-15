using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afareet.Vehicle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WheelColliderSuspensionPrototype : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] wheels = Array.Empty<WheelCollider>();
        [SerializeField, Min(0.1f)] private float naturalFrequencyHz = 5f;
        [SerializeField, Range(0.1f, 2f)] private float dampingRatio = 0.82f;
        [SerializeField, Min(0.01f)] private float suspensionDistance = 0.22f;
        [SerializeField, Range(0f, 1f)] private float targetPosition = 0.45f;
        [SerializeField, Min(0f)] private float forceAppPointDistance = 0.1f;
        [SerializeField, Min(0f)] private float wheelDampingRate = 0.25f;

        public int WheelCount => wheels?.Length ?? 0;

        public void ConfigurePrototype(
            WheelCollider[] prototypeWheels,
            float frequencyHz,
            float ratio,
            float distance,
            float target,
            float forcePointDistance,
            float dampingRate)
        {
            wheels = prototypeWheels ?? Array.Empty<WheelCollider>();
            naturalFrequencyHz = frequencyHz;
            dampingRatio = ratio;
            suspensionDistance = distance;
            targetPosition = target;
            forceAppPointDistance = forcePointDistance;
            wheelDampingRate = dampingRate;
        }

        public bool IsValid(out string error)
        {
            var body = GetComponent<Rigidbody>();
            if (body == null || body.mass <= 0f)
                return Fail("A positive-mass Rigidbody is required.", out error);

            var activeWheels = ResolveWheels();
            if (activeWheels.Length == 0)
                return Fail("At least one WheelCollider is required.", out error);

            var unique = new HashSet<WheelCollider>();
            foreach (var wheel in activeWheels)
            {
                if (wheel == null)
                    return Fail("WheelCollider references cannot contain null entries.", out error);
                if (!unique.Add(wheel))
                    return Fail("Each WheelCollider may only be supplied once.", out error);
            }

            if (naturalFrequencyHz <= 0f)
                return Fail("Natural frequency must be positive.", out error);
            if (dampingRatio <= 0f || dampingRatio > 2f)
                return Fail("Damping ratio must be within (0, 2].", out error);
            if (suspensionDistance <= 0f)
                return Fail("Suspension distance must be positive.", out error);
            if (targetPosition < 0f || targetPosition > 1f)
                return Fail("Target position must be within [0, 1].", out error);
            if (forceAppPointDistance < 0f || wheelDampingRate < 0f)
                return Fail("Force point distance and wheel damping rate cannot be negative.", out error);

            error = string.Empty;
            return true;
        }

        public WheelSuspensionCoefficients ApplyPrototypeTuning()
        {
            if (!IsValid(out var error))
                throw new InvalidOperationException(error);

            var body = GetComponent<Rigidbody>();
            var activeWheels = ResolveWheels();
            wheels = activeWheels;

            var coefficients = WheelSuspensionMath.Calculate(
                body.mass,
                activeWheels.Length,
                naturalFrequencyHz,
                dampingRatio);

            foreach (var wheel in activeWheels)
            {
                var spring = wheel.suspensionSpring;
                spring.spring = coefficients.SpringRate;
                spring.damper = coefficients.DamperRate;
                spring.targetPosition = targetPosition;
                wheel.suspensionSpring = spring;
                wheel.suspensionDistance = suspensionDistance;
                wheel.forceAppPointDistance = forceAppPointDistance;
                wheel.wheelDampingRate = wheelDampingRate;
            }

            return coefficients;
        }

        private WheelCollider[] ResolveWheels()
        {
            if (wheels != null && wheels.Length > 0)
                return wheels;
            return GetComponentsInChildren<WheelCollider>(true);
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
