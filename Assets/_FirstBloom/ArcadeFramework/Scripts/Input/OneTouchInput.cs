using UnityEngine;
using UnityEngine.EventSystems;

namespace FirstBloom.ArcadeFramework.Input
{
    public static class OneTouchInput
    {
        public static bool WasPressedThisFrame(bool includeKeyboard = true, bool ignoreUiPresses = true)
        {
            if (ignoreUiPresses && IsPointerOverUi())
            {
                return false;
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                return true;
            }

            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            if (includeKeyboard)
            {
                return UnityEngine.Input.GetKeyDown(KeyCode.Space)
                    || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)
                    || UnityEngine.Input.GetKeyDown(KeyCode.W)
                    || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0);
            }

            return false;
        }

        public static bool WasPausePressedThisFrame()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape)
                || UnityEngine.Input.GetKeyDown(KeyCode.P)
                || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7);
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
