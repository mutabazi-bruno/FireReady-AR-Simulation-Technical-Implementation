using UnityEngine;
using UnityEngine.InputSystem;

namespace FireReady.AR
{
    /// <summary>
    /// Lets the camera be moved in the editor when no AR provider is running.
    /// WASD to walk, Q/E down/up, hold right mouse to look. Does nothing on a device.
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
        [SerializeField] ARManager arManager;
        [SerializeField] float moveSpeed = 1.4f;
        [SerializeField] float lookSpeed = 0.15f;
        [SerializeField] Vector3 startPosition = new Vector3(0f, 1.4f, -2.2f);

        float yaw;
        float pitch = 20f;
        bool initialised;

        void Update()
        {
            if (!Application.isEditor || arManager == null || !arManager.UsingFallback) return;

            if (!initialised)
            {
                initialised = true;
                transform.position = startPosition;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSpeed;
                pitch = Mathf.Clamp(pitch - delta.y * lookSpeed, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            var move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += Vector3.forward;
            if (keyboard.sKey.isPressed) move += Vector3.back;
            if (keyboard.aKey.isPressed) move += Vector3.left;
            if (keyboard.dKey.isPressed) move += Vector3.right;

            var flatForward = Quaternion.Euler(0f, yaw, 0f);
            var step = flatForward * move * (moveSpeed * Time.deltaTime);
            if (keyboard.eKey.isPressed) step.y += moveSpeed * Time.deltaTime;
            if (keyboard.qKey.isPressed) step.y -= moveSpeed * Time.deltaTime;

            transform.position += step;
        }
    }
}
