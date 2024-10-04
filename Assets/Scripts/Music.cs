using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour {

    GameManager gameManager;

    //Music - Should make this a seperate class if becomes more complex
    public AudioSource music;
    public AudioClip musicClip;

    // Use this for initialization
    void Start () {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
	}
	
	// Update is called once per frame
	void Update () {
        //Change Music upon Game Start
        if (gameManager.gameStarted && music.clip != musicClip) {
            music.Stop();
            music.clip = musicClip;
            music.Play();
        }
    }
}
