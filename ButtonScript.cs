using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour
{
	string name;
	
	GamesManager gamesManager;
	
	void Start()
	{
		gamesManager = GameObject.Find("AppController").GetComponent<GamesManager>();
	}
	
	void Update()
	{
		name = gameObject.name.Split("_")[0];
	}
	
	public void SeekToGame()
	{
		gamesManager.SeekToGame(name);
	}
}