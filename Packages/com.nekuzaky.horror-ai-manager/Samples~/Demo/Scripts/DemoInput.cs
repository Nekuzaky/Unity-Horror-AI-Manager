using UnityEngine;
#if HORROR_AI_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Nekuzaky.HorrorAI.Demo
{
    public static class DemoInput
    {
#if HORROR_AI_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        #region Input System

        private const float MouseScale = 0.1f;

        public static bool Available => Keyboard.current != null;

        public static bool ClickPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        public static bool EscapePressed => Keyboard.current.escapeKey.wasPressedThisFrame;

        public static bool FlashlightPressed => Keyboard.current.fKey.wasPressedThisFrame;

        public static bool CrouchPressed => Keyboard.current.cKey.wasPressedThisFrame;

        public static bool RunHeld => Keyboard.current.leftShiftKey.isPressed;

        public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() * MouseScale : Vector2.zero;

        public static Vector2 Move
        {
            get
            {
                var keyboard = Keyboard.current;
                var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        #endregion
#elif ENABLE_LEGACY_INPUT_MANAGER
        #region Input Manager

        public static bool Available => true;

        public static bool ClickPressed => Input.GetMouseButtonDown(0);

        public static bool EscapePressed => Input.GetKeyDown(KeyCode.Escape);

        public static bool FlashlightPressed => Input.GetKeyDown(KeyCode.F);

        public static bool CrouchPressed => Input.GetKeyDown(KeyCode.C);

        public static bool RunHeld => Input.GetKey(KeyCode.LeftShift);

        public static Vector2 Look => new(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        public static Vector2 Move => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        #endregion
#else
        #region No Input

        public static bool Available => false;

        public static bool ClickPressed => false;

        public static bool EscapePressed => false;

        public static bool FlashlightPressed => false;

        public static bool CrouchPressed => false;

        public static bool RunHeld => false;

        public static Vector2 Look => Vector2.zero;

        public static Vector2 Move => Vector2.zero;

        #endregion
#endif
    }
}
