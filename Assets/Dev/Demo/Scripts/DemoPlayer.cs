using UnityEngine;

namespace Nekuzaky.HorrorAI.Demo
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoPlayer : MonoBehaviour
    {
        #region Public

        public Transform head;
        public HorrorTarget target;
        public HorrorLightSource flashlight;
        public float walkSpeed = 2.6f;
        public float runSpeed = 5f;
        public float crouchSpeed = 1.3f;
        public float mouseSensitivity = 2.2f;
        public float stepDistance = 1.6f;
        public float walkLoudness = 4f;
        public float runLoudness = 10f;
        public float crouchLoudness = 1.2f;

        public bool Running { get; private set; }

        public int Caught { get; private set; }

        public void Respawn()
        {
            Caught++;
            _controller.enabled = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _controller.enabled = true;
            _pitch = 0f;
        }

        #endregion


        #region Private and Protected

        private CharacterController _controller;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _pitch;
        private float _walked;
        private float _verticalSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private bool _warned;

        private void Update()
        {
            if (!DemoInput.Available)
            {
                if (!_warned)
                    Debug.LogWarning("[Horror AI demo] No input backend is active: enable the Input System package or the Input Manager in the Player settings.");
                _warned = true;
                return;
            }
            if (DemoInput.ClickPressed)
                Cursor.lockState = CursorLockMode.Locked;
            if (DemoInput.EscapePressed)
                Cursor.lockState = CursorLockMode.None;
            if (DemoInput.FlashlightPressed && flashlight != null)
                flashlight.Toggle();
            if (DemoInput.CrouchPressed && target != null)
                target.Crouching = !target.Crouching;
            Look();
            Move();
        }

        private void Look()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;
            var look = DemoInput.Look;
            transform.Rotate(0f, look.x * mouseSensitivity, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, -80f, 80f);
            if (head != null)
                head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move()
        {
            var crouching = target != null && target.Crouching;
            Running = !crouching && DemoInput.RunHeld;
            var speed = crouching ? crouchSpeed : Running ? runSpeed : walkSpeed;
            var axes = DemoInput.Move;
            var input = new Vector3(axes.x, 0f, axes.y);
            var move = transform.TransformDirection(Vector3.ClampMagnitude(input, 1f)) * speed;
            _verticalSpeed = _controller.isGrounded ? -1f : _verticalSpeed - 9.81f * Time.deltaTime;
            move.y = _verticalSpeed;
            var before = transform.position;
            _controller.Move(move * Time.deltaTime);
            if (head != null)
                head.localPosition = new Vector3(0f, Mathf.Lerp(head.localPosition.y, crouching ? 0.9f : 1.6f, Time.deltaTime * 10f), 0f);
            var moved = transform.position - before;
            moved.y = 0f;
            _walked += moved.magnitude;
            if (_walked < stepDistance)
                return;
            _walked = 0f;
            HorrorAI.EmitNoise(transform.position, crouching ? crouchLoudness : Running ? runLoudness : walkLoudness, gameObject);
        }

        #endregion
    }
}
