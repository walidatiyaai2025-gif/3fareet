using System;
using UnityEngine.InputSystem;

namespace Afareet.Vehicle
{
    public sealed class ProductionInputMap : IDisposable
    {
        public InputActionMap Driving { get; }
        public InputAction Steer { get; }
        public InputAction Throttle { get; }
        public InputAction Brake { get; }
        public InputAction Drift { get; }
        public InputAction Nitro { get; }
        public InputAction Start { get; }
        public InputAction TouchPress { get; }

        public ProductionInputMap()
        {
            Driving = new InputActionMap("Driving");
            Steer = Driving.AddAction("Steer", InputActionType.Value);
            Steer.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            Steer.AddBinding("<Gamepad>/leftStick/x");

            Throttle = Driving.AddAction("Throttle", InputActionType.Value);
            Throttle.AddBinding("<Keyboard>/w");
            Throttle.AddBinding("<Gamepad>/rightTrigger");

            Brake = Driving.AddAction("Brake", InputActionType.Value);
            Brake.AddBinding("<Keyboard>/s");
            Brake.AddBinding("<Gamepad>/leftTrigger");

            Drift = Button("Drift", "<Keyboard>/space", "<Gamepad>/buttonWest");
            Nitro = Button("Nitro", "<Keyboard>/leftShift", "<Gamepad>/buttonSouth");
            Start = Button("Start", "<Keyboard>/enter", "<Gamepad>/start");
            TouchPress = Driving.AddAction("TouchPress", InputActionType.Button, "<Touchscreen>/primaryTouch/press");
        }

        public void Enable() => Driving.Enable();
        public void Disable() => Driving.Disable();
        public void Dispose() => Driving.Dispose();

        private InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = Driving.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard);
            action.AddBinding(gamepad);
            return action;
        }
    }
}
