using UnityEngine;

namespace Scott.Barley.Utils
{
    public class Scotts_Utils : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GameObject gameSpeedControllerObject = new GameObject("Mng_GameSpeedController");
            gameSpeedControllerObject.AddComponent<GameSpeedController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public class GameSpeedController : MonoBehaviour
        {
            public float timeScaleIncrement = 0.1f;
            public float minTimeScale = 0.1f;
            public float maxTimeScale = 3.0f;

            void Update()
            {
                // Detect '+' key (either equals key without shift or keypad plus)
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    IncreaseGameSpeed();
                }

                // Detect '-' key (either minus key or keypad minus)
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    DecreaseGameSpeed();
                }
            }

            void IncreaseGameSpeed()
            {
                Time.timeScale = Mathf.Clamp(Time.timeScale + timeScaleIncrement, minTimeScale, maxTimeScale);
                Debug.Log("Game Speed Increased: " + Time.timeScale);
            }

            void DecreaseGameSpeed()
            {
                Time.timeScale = Mathf.Clamp(Time.timeScale - timeScaleIncrement, minTimeScale, maxTimeScale);
                Debug.Log("Game Speed Decreased: " + Time.timeScale);
            }
        }
    }
}

