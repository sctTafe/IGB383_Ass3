using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

    public Text playersText;
    public Slider playersSlider;
    public Text scoreText;
    public Slider scoreSlider;
    public Toggle botsOnly;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        playersText.text = "Players: " + playersSlider.value;
        scoreText.text = "Score: " + scoreSlider.value;
    }

    public void StartGame() {

        PlayerPrefs.SetInt("Players", (int)playersSlider.value);
        PlayerPrefs.SetInt("Score", (int)scoreSlider.value);
        if (botsOnly.isOn)
            PlayerPrefs.SetInt("botsOnly", 1);
        else
            PlayerPrefs.SetInt("botsOnly", 0);

        SceneManager.LoadScene("Mech Game");
    }
}
