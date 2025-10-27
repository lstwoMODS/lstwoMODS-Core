using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Globalization;
using UniverseLib.UI.Panels;

namespace lstwoMODS_Core.UI.Components
{
    public class CustomInputField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public InputField inputField;

        private bool isDragging = false;
        private Vector2 initialMousePosition;
        private float initialValue;
        private float sensitivity;

        public float baseSensitivity = 0.05f;  // Baseline sensitivity (close to field)
        public float maxSensitivity = 5f;      // Max sensitivity (far from field)
        public float distanceScale = 300f;     // How quickly sensitivity ramps up

        private bool isIntegerMode = false;

        private void Start()
        {
            DetectMode();
        }

        private void DetectMode()
        {
            if (inputField == null) return;

            isIntegerMode = inputField.characterValidation == InputField.CharacterValidation.Integer;
        }

        private void Update()
        {
            if (!isDragging) return;
            
            var currentMousePosition = Input.mousePosition;
            var delta = (currentMousePosition.x - initialMousePosition.x) * sensitivity;

            if (float.TryParse(inputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float currentValue))
            {
                var newValue = initialValue + delta;

                if (isIntegerMode)
                {
                    newValue = Mathf.Round(newValue);
                }

                inputField.text = newValue.ToString(isIntegerMode ? "F0" : "F2", CultureInfo.InvariantCulture);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            initialMousePosition = Input.mousePosition;

            if (float.TryParse(inputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float currentValue))
            {
                initialValue = currentValue;
            }
            else
            {
                initialValue = 0f;
            }

            // Calculate distance from click point to input field center
            Vector3 inputFieldWorldPos = inputField.transform.position;
            float distance = Mathf.Abs(initialMousePosition.x - inputFieldWorldPos.x);

            // Map distance to sensitivity
            sensitivity = Mathf.Lerp(baseSensitivity, maxSensitivity, Mathf.Clamp01(distance / distanceScale));

            isDragging = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void SetInputField(InputField field)
        {
            inputField = field;
            DetectMode();
        }
    }
}
