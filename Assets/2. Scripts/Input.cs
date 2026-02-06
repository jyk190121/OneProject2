using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// 네임스페이스 충돌 방지
using Key = UnityEngine.InputSystem.Key;

public static class Input
{
    private static float sensitivity = 3f;
    private static float gravity = 3f;

    private static Dictionary<string, float> axisValues = new Dictionary<string, float>
    {
        { "Horizontal", 0f },
        { "Vertical", 0f }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        GameObject updaterObj = new GameObject("AxisUpdater");
        updaterObj.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(updaterObj);
        updaterObj.AddComponent<AxisUpdater>();
    }

    private class AxisUpdater : MonoBehaviour
    {
        private void Update()
        {
            UpdateAxis("Horizontal", GetAxisRaw("Horizontal"));
            UpdateAxis("Vertical", GetAxisRaw("Vertical"));
        }

        private void UpdateAxis(string name, float target)
        {
            float current = axisValues[name];
            if (target != 0)
                current = Mathf.MoveTowards(current, target, sensitivity * Time.deltaTime);
            else
                current = Mathf.MoveTowards(current, 0, gravity * Time.deltaTime);

            axisValues[name] = current;

            GetCombinedAxis(name);
        }

        private float GetCombinedAxis(string axisName)
        {
            float val = GetAxisRaw(axisName); // 키보드 값

            // 실제 게임패드가 연결되어 있다면 왼쪽 스틱 값 추가
            if (Gamepad.current != null && Keyboard.current == null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (axisName == "Horizontal") val += stick.x;
                else if (axisName == "Vertical") val += stick.y;
            }

            return Mathf.Clamp(val, -1f, 1f);
        }
    }

    // --- 키보드 관련 함수들 ---

    //public static bool GetKeyDown(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    //public static bool GetKey(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
    //public static bool GetKeyUp(Key key) => Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;

    // --- 키보드 & 게임패드 버튼 통합 ---

    public static bool GetKeyDown(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    public static bool GetKey(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;

    // 게임패드 특정 버튼 확인 (남쪽 버튼: Xbox의 A, PS의 X)
    public static bool GetGamepadButton() => Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;

    // --- 축(Axis) 관련 ---
    public static float GetAxis(string axisName) => axisValues.ContainsKey(axisName) ? axisValues[axisName] : 0f;

    // --- 마우스 관련 함수들 (0:왼쪽, 1:오른쪽, 2:휠클릭) ---

    public static bool GetMouseButtonDown(int button) => GetMouseState(button, "down");
    public static bool GetMouseButton(int button) => GetMouseState(button, "held");
    public static bool GetMouseButtonUp(int button) => GetMouseState(button, "up");

    private static bool GetMouseState(int button, string state)
    {
        if (Mouse.current == null) return false;

        var buttonControl = button switch
        {
            0 => Mouse.current.leftButton,
            1 => Mouse.current.rightButton,
            2 => Mouse.current.middleButton,
            _ => null
        };

        if (buttonControl == null) return false;

        return state switch
        {
            "down" => buttonControl.wasPressedThisFrame,
            "held" => buttonControl.isPressed,
            "up" => buttonControl.wasReleasedThisFrame,
            _ => false
        };
    }

    // --- 축(Axis) 관련 함수들 ---

    //public static float GetAxis(string axisName) => axisValues.ContainsKey(axisName) ? axisValues[axisName] : 0f;

    public static float GetAxisRaw(string axisName)
    {
        //if (Keyboard.current == null) return 0f;
        //float val = 0f;
        //if (axisName == "Horizontal")
        //{
        //    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) val += 1f;
        //    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) val -= 1f;
        //}
        //else if (axisName == "Vertical")
        //{
        //    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) val += 1f;
        //    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) val -= 1f;
        //}
        //return val;

        float val = 0f;

        // 1. 키보드 입력 체크
        if (Keyboard.current != null && Gamepad.current == null)
        {
            if (axisName == "Horizontal")
            {
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) val += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) val -= 1f;
            }
            else if (axisName == "Vertical")
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) val += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) val -= 1f;
            }
        }

        // 2. 게임패드 입력 체크
        if (Gamepad.current != null && Keyboard.current == null)
        {
            if (axisName == "Horizontal")
            {
                // 왼쪽 아날로그 스틱
                val += Gamepad.current.leftStick.x.ReadValue();
                // D-Pad (십자키) 좌우
                if (Gamepad.current.dpad.right.isPressed) val += 1f;
                if (Gamepad.current.dpad.left.isPressed) val -= 1f;
            }
            else if (axisName == "Vertical")
            {
                // 왼쪽 아날로그 스틱
                val += Gamepad.current.leftStick.y.ReadValue();
                // D-Pad (십자키) 위아래
                if (Gamepad.current.dpad.up.isPressed) val += 1f;
                if (Gamepad.current.dpad.down.isPressed) val -= 1f;
            }
        }

        // 최종 값 제한 (-1 ~ 1 사이로 고정)
        return Mathf.Clamp(val, -1f, 1f);
    }
}