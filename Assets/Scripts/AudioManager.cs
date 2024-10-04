using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour {

	public static AudioManager audioManager;

	public AudioSource audioSource;

	void Awake () {
		audioManager = this;
	}
	
	public void PlayAudio(AudioClip soundEffect){
		audioSource.PlayOneShot (soundEffect);
	}
}
