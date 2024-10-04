using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerGUI : MonoBehaviour {

    public GameManager gameManager;
    private MechSystems player;

    //Game Status UI Objects
    public GameObject playerAliveUI;
    public GameObject playerDeadUI;

    public Text gameControl;
    public Text gameStatus;

    //Player Alive Elements
    public Slider healthSlider;
    public Slider energySlider;
    public Text shellsText;
    public Text missilesText;

    //Pause Menu
    public GameObject pauseUI;
    public bool gamePaused = false;
    public Slider musicSlider;
    private AudioSource music;
    public Slider mouseSlider;
    private ThirdPersonCamera mouse;

    //Game Over Screen
    public GameObject gameOverScreen;

    //Scoreboard
    public GameObject scoreBoard;
    public Text[] scores;

    
    // Use this for initialization
    void Start () {
        Cursor.visible = false;

        //Collect Music and Camera Objects
        music = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<AudioSource>();
        mouse = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<ThirdPersonCamera>();
    }
	
	// Update is called once per frame
	void Update () {

        //Honestly this code is a little messy, but not something I have much
        //time to spend on... :/

        //Game Over
        if (gameManager.gameOver) {
            gamePaused = true;
            Time.timeScale = 0;
            Cursor.visible = true;
            playerAliveUI.SetActive(false);
            playerDeadUI.SetActive(false);
            pauseUI.SetActive(false);
            gameOverScreen.SetActive(true);
            scoreBoard.SetActive(true);
        }
        else
            GameUILogic();
        

        //Update player GUI variables
        if (player) {
            healthSlider.value = player.health;
            energySlider.value = player.energy;
            shellsText.text = "Shells: " + player.shells;
            missilesText.text = "Missiles: " + player.missiles;
        }

        //Pause Menu
        if (Input.GetKeyDown("escape") && !gamePaused) {
            gamePaused = true;
            Time.timeScale = 0;
            playerAliveUI.SetActive(false);
            playerDeadUI.SetActive(false);
            pauseUI.SetActive(true);
            Cursor.visible = true;
        } else if (Input.GetKeyDown("escape") && gamePaused) {
            Resume();
        }

        //Music Volume
        music.volume = musicSlider.value;
        //Mouse/camera Sensivitivy
        mouse.cameraSpeed = (int)mouseSlider.value;

        //Display Scoreboard - Tab
        if (Input.GetKeyDown("tab")) 
            scoreBoard.SetActive(true);
        else if (Input.GetKeyUp("tab"))
            scoreBoard.SetActive(false);

        //Scoreboard numbers
        if (scoreBoard) {
            for (int i = 0; i < gameManager.playerScores.Length; i++) {
                scores[i].text = "Player " + (i + 1) + "             " + gameManager.playerScores[i] + "              " + gameManager.playerDeaths[i];
            }
        }

    }

    void GameUILogic() {
        //Game Starting
        if (!gameManager.gameStarted) {
            gameStatus.text = "Mech Deathmatch!";
            gameControl.text = "Click to Begin...";
            if (Input.GetMouseButtonDown(0)) {
                gameManager.StartGame();
                playerDeadUI.SetActive(false);
            }
        }
        //Game underway, player alive
        else if (gameManager.gameStarted && gameManager.currentPlayer && !gamePaused) {
            player = gameManager.currentPlayer.GetComponent<MechSystems>();
            playerAliveUI.SetActive(true);
            playerDeadUI.SetActive(false);
        }
        //Game underway, player dead
        else if (gameManager.gameStarted && !gameManager.currentPlayer) {
            playerAliveUI.SetActive(false);
            if (!gameManager.AIOnly && !gameManager.respawn) {
                playerDeadUI.SetActive(true);
                gameStatus.text = "You were Destroyed!";
                gameControl.text = "Click to respawn...";

                if (Input.GetMouseButtonDown(0)) {
                    gameManager.StartCoroutine(gameManager.RespawnPlayer());
                    gameManager.respawn = true;
                    gameControl.text = "Respawning...";
                }
            }
        }
    }

    public void ReturnToMain() {
        SceneManager.LoadScene("MainMenu");
    }

    public void Resume() {
        gamePaused = false;
        Time.timeScale = 1;
        if (!gameManager.currentPlayer && !gameManager.AIOnly) {
            playerAliveUI.SetActive(false);
            playerDeadUI.SetActive(true);
        }
        else if (gameManager.currentPlayer) {
            playerAliveUI.SetActive(true);
            playerDeadUI.SetActive(false);
        }
        pauseUI.SetActive(false);
        Cursor.visible = false;
    }
}
