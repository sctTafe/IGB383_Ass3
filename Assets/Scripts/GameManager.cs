using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public int noOfPlayers = 2;     //Currently only between 1-4 for current map
    public int[] playerScores;
    public int[] playerDeaths;
    public bool AIOnly = false;

    public int scoreLimit = 25;
    public bool gameStarted = false;
    public bool gameOver = false;

    public GameObject[] spawnPoints;

    public GameObject playerMech;
    public GameObject currentPlayer;
    public bool respawn = false;
    public GameObject AIMech;

    private GameObject[] allActivePlayers;

    public ThirdPersonCamera mainCamera;
    public PlayerGUI GUI;

	// Use this for initialization
	void Start () {

        //Reset time
        Time.timeScale = 1;

        //Player Preferences Setup
        noOfPlayers = PlayerPrefs.GetInt("Players", 1);
        scoreLimit = PlayerPrefs.GetInt("Score", 5);
        if (PlayerPrefs.GetInt("botsOnly", 0) == 0)
            AIOnly = false;
        else
            AIOnly = true;

        //Score elements
        playerScores = new int[noOfPlayers];
        playerDeaths = new int[noOfPlayers];

        //Spawning
        spawnPoints = GameObject.FindGameObjectsWithTag("Spawn Point");
        
    }
	
	// Update is called once per frame
	void Update () {

        allActivePlayers = GameObject.FindGameObjectsWithTag("Player");

        //Check if a player has reached the score limit
        ScoreLimit();
	}

    //Check all player scores for a winner. Pause game if so.
    void ScoreLimit() {

        //Look through playerScores
        for (int i = 0; i < playerScores.Length; i++) {
            if (playerScores[i] == scoreLimit) {
                gameOver = true;
                Time.timeScale = 0;
            }
        }
    }

    //Respawn and handle current active player object and camera
    public IEnumerator RespawnPlayer() {

        yield return new WaitForSeconds(3);
        if (respawn) {
            currentPlayer = Instantiate(playerMech, spawnPoints[SafestSpawnPoint()].transform.position, spawnPoints[SafestSpawnPoint()].transform.rotation) as GameObject;
            mainCamera.CameraTarget = GameObject.FindGameObjectWithTag("Camera Target").transform;
            respawn = false;
        }
        yield break;
    }

    //Return the spawn point that is the farthest away from other players
    private int SafestSpawnPoint() {

        float bestDistance = 0;
        float combinedDistance = 0;
        int thisSpawnIndex = 0;

        //For every spawn point...
        for (int i = 0; i < spawnPoints.Length; i++) {
            //Add up all the distances from all the active players
            foreach (GameObject player in allActivePlayers) {
                if (player) {
                    combinedDistance += Vector3.Distance(player.transform.position, spawnPoints[i].transform.position);
                }
            }
            //If the combined distance is better than the best distance, make this the new best distance and record the spawn index
            if (combinedDistance > bestDistance) {
                bestDistance = combinedDistance;
                thisSpawnIndex = i;
            }
            //Reset combined distance
            combinedDistance = 0;
        }

        return thisSpawnIndex;
    }

    
    public IEnumerator RespawnBot(int ID) {
        yield return new WaitForSeconds(5);

        GameObject thisAIMech = Instantiate(AIMech, spawnPoints[SafestSpawnPoint()].transform.position, spawnPoints[SafestSpawnPoint()].transform.rotation) as GameObject;
        thisAIMech.GetComponent<MechSystems>().ID = ID;

        yield break;
    }

    //Spawn initial player(s) configuration
    public void StartGame() {
        //Botmatch
        if (AIOnly) {
            for (int i = 0; i < noOfPlayers; i++) {
                GameObject thisAIMech = Instantiate(AIMech, spawnPoints[i].transform.position, spawnPoints[i].transform.rotation) as GameObject;
                thisAIMech.GetComponent<MechSystems>().ID = i;
            }
        }
        //Player and bots (1-3)
        else {
            currentPlayer = Instantiate(playerMech, spawnPoints[0].transform.position, spawnPoints[0].transform.rotation) as GameObject;
            mainCamera.CameraTarget = GameObject.FindGameObjectWithTag("Camera Target").transform;
            respawn = false;
            for (int i = 1; i < noOfPlayers; i++) {
                GameObject thisAIMech = Instantiate(AIMech, spawnPoints[i].transform.position, spawnPoints[i].transform.rotation) as GameObject;
                thisAIMech.GetComponent<MechSystems>().ID = i;
            }
        }
        gameStarted = true;
    }
}
