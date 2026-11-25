using UnityEngine;

namespace BoxBox.Presentation.Vehicles
{
    [RequireComponent(typeof(VehiclePhysicsController))]
    public sealed class VehicleInputAdapter : MonoBehaviour
    {
        private VehiclePhysicsController _controller;

        [Header("Controls Info")]
        [SerializeField] private bool enableInput = true;

        private void Awake()
        {
            _controller = GetComponent<VehiclePhysicsController>();
        }

        private void Update()
        {
            if (!enableInput || _controller == null)
            {
                _controller?.SetInputs(0f, 0f, false);
                return;
            }

            float throttle = Input.GetAxisRaw("Vertical");
            float steer = Input.GetAxisRaw("Horizontal");
            bool brake = Input.GetKey(KeyCode.Space);
            bool handbrake = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            // Suporte direto por KeyCode (previne problemas com eixos no Linux/OpenGL)
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                throttle = 1f;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                throttle = -1f;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                steer = 1f;
            else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                steer = -1f;

            bool fastSteer = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _controller.SetInputs(throttle, steer, handbrake, fastSteer, brake);
        }

        public void SetInputEnabled(bool isEnabled)
        {
            enableInput = isEnabled;
        }
    }
}
