using System;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Native input, torque, braking and steering replication remain in use.
    // Controlled pitch follows roads; roll is cosmetic and limited to six degrees.
    internal sealed class MotorcycleHandling
    {
        internal const float Radius = 0.35f;
        internal const float MaxLean = 6f;
        internal const float VisualClearance = 0.10f;
        internal const float BrakeAssist = 2.4f, HandbrakeAssist = 1.8f;
        private readonly LandVehicle _vehicle;
        private readonly Transform _visual, _rear, _front, _fork;
        private readonly WheelCollider[] _wheels;
        private readonly Quaternion _forkRotation;
        private float _lean, _forkAngle;
        internal readonly MotorcycleStability Stability;
        internal Quaternion SteeringDeltaWorld => _fork == null || _fork.parent == null ? Quaternion.identity :
            _fork.rotation * Quaternion.Inverse(_fork.parent.rotation * _forkRotation);

        internal MotorcycleHandling(LandVehicle vehicle, Transform visual, Transform rear,
            Transform front, Transform fork, WheelCollider[] wheels)
        {
            _vehicle = vehicle; _visual = visual; _rear = rear; _front = front;
            _fork = fork; _wheels = wheels;
            _forkRotation = fork != null ? fork.localRotation : Quaternion.identity;
            Stability = new MotorcycleStability(vehicle, wheels);
        }

        internal static void SizeBox(BoxCollider box, LandVehicle vehicle, Vector3 center, Vector3 size)
        {
            Vector3 scale = box.transform.lossyScale;
            if (Math.Abs(scale.x) < 0.0001f || Math.Abs(scale.y) < 0.0001f || Math.Abs(scale.z) < 0.0001f)
                throw new InvalidOperationException("Motorcycle collider has an invalid scale");
            // The sedan's interaction/bounds objects have non-unit scales. Size
            // is specified in metres, then converted to their local coordinates.
            box.center = box.transform.InverseTransformPoint(vehicle.transform.TransformPoint(center));
            box.size = new Vector3(size.x / Math.Abs(scale.x), size.y / Math.Abs(scale.y), size.z / Math.Abs(scale.z));
        }

        internal static void Configure(LandVehicle vehicle, Transform front, Transform rear, WheelCollider[] wheels)
        {
            Vector3 frontCenter = vehicle.transform.InverseTransformPoint(front.position);
            Vector3 rearCenter = vehicle.transform.InverseTransformPoint(rear.position);
            int frontCount = 0, rearCount = 0;
            foreach (WheelCollider wheel in wheels)
            {
                bool steer = false;
                foreach (WheelCollider candidate in vehicle.steerWheels) if (candidate == wheel) steer = true;
                int side = steer ? frontCount++ : rearCount++;
                Vector3 center = steer ? frontCenter : rearCenter;
                // frontWheelContact is already the native axle-height anchor.
                // Four narrow contacts retain the registered wheel/RPC layout.
                center.x = side % 2 == 0 ? -0.22f : 0.22f;
                wheel.transform.position = vehicle.transform.TransformPoint(center);
                wheel.center = Vector3.zero; wheel.radius = Radius;
                wheel.suspensionDistance = 0.16f; wheel.mass = 15f;
                JointSpring spring = wheel.suspensionSpring;
                spring.spring = 10000f; spring.damper = 1400f; spring.targetPosition = 0.5f;
                wheel.suspensionSpring = spring;
            }
            float ratio = 350f / vehicle.Rb.mass;
            vehicle.Rb.mass = 350f; vehicle.Rb.centerOfMass = new Vector3(0f, -0.15f, 0f);
            // LandVehicle.Start reads this transform after a freshly spawned
            // vehicle is converted. Give that initializer the same bike COM.
            if (vehicle.centerOfMass != null)
                vehicle.centerOfMass.position = vehicle.transform.TransformPoint(vehicle.Rb.centerOfMass);
            vehicle.Rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            vehicle.Rb.angularDrag = 1f;
            // Preserve the native forward/reverse torque curve, scaled for bike
            // mass, rather than accelerating a 350kg bike with a sedan's torque.
            vehicle.diffGearing *= ratio;
            // Retain native braking/reverse controls, with stronger bike brakes.
            // The chassis snapshot restores these original values on detach.
            vehicle.BrakeForceMultiplier *= ratio * BrakeAssist;
            vehicle.handBrakeForce *= ratio * HandbrakeAssist;
            vehicle.maxSteeringAngle = SteeringLimit(0f); vehicle.steerRate = 85f;
            vehicle.ResetMaxSteerAngle(); vehicle.downforce = 2f;
            vehicle.overrideControls = false; vehicle.throttleOverride = 0f;
            vehicle.steerOverride = 0f; vehicle.handbrakeOverride = false;
        }

        internal static float SteeringLimit(float speed)
        {
            // Metres/seconds are used by the chassis; Speed_Kmh is km/h here.
            // Keep parking turns useful but reduce abrupt highway steering.
            return Mathf.Clamp(30f / (1f + speed * speed * 0.0005f), 9f, 30f);
        }

        internal void PrepareSteering()
        {
            _vehicle.maxSteeringAngle = SteeringLimit(_vehicle.Speed_Kmh);
            _vehicle.steerRate = 85f * _vehicle.maxSteeringAngle / 30f;
            if (_vehicle.LocalPlayerIsDriver)
                _vehicle.lastFrameSteerAngle = Mathf.Clamp(_vehicle.lastFrameSteerAngle,
                    -_vehicle.maxSteeringAngle, _vehicle.maxSteeringAngle);
        }

        internal void Update()
        {
            float speed = _vehicle.Speed_Kmh;
            float rotation = speed / 3.6f / Radius * Mathf.Rad2Deg * Time.deltaTime;
            _rear.Rotate(Vector3.right, rotation, Space.Self);
            if (_front != null) _front.Rotate(Vector3.right, rotation, Space.Self);
            float steering = _vehicle.IsOccupied ? Mathf.Clamp(_vehicle.CurrentSteerAngle,
                -SteeringLimit(speed), SteeringLimit(speed)) : 0f;
            float target = -Mathf.Clamp(steering / SteeringLimit(speed), -1f, 1f) *
                Mathf.Clamp01(Math.Abs(speed) / 20f) * MaxLean;
            _lean = Mathf.MoveTowards(_lean, target, Time.deltaTime * (Math.Abs(target) < Math.Abs(_lean) ? 40f : 25f));
            _visual.localRotation = Quaternion.Euler(0f, 0f, _lean);
            _forkAngle = Mathf.MoveTowards(_forkAngle, steering, Time.deltaTime * _vehicle.steerRate);
            if (_fork != null) _fork.localRotation = _forkRotation * Quaternion.AngleAxis(_forkAngle, Vector3.up);
        }

        internal void AlignSuspension()
        {
            // Follow actual WheelCollider suspension before drawing, without
            // moving the Rigidbody/network root or adding a second controller.
            if (!_vehicle.IsPhysicallySimulated) return;
            float sum = 0f; int count = 0;
            foreach (WheelCollider wheel in _wheels)
                if (wheel != null && wheel.enabled && wheel.isGrounded)
                { wheel.GetWorldPose(out Vector3 point, out Quaternion _); sum += point.y; count++; }
            if (count != _wheels.Length) return;
            float shown = (_rear.position.y + (_front != null ? _front.position.y : _rear.position.y)) * 0.5f;
            Vector3 position = _visual.position;
            // Keep the measured mesh-radius clearance in the suspension target;
            // otherwise this correction would cancel the initial display lift.
            position.y += Mathf.Clamp(sum / count + VisualClearance - shown, -0.12f, 0.12f);
            _visual.position = position;
        }
    }
}
