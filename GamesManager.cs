using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Diagnostics;
using UDebug = UnityEngine.Debug;
using System.Threading;

public class GamesManager : MonoBehaviour
{
	//Dict format
	//{NAME : [cartridgeObject, buttonObject]}
	SortedDictionary<string, GameObject[]> games = new SortedDictionary<string, GameObject[]>();
	
	
	public GameObject CartridgePrefab;
	public GameObject ButtonPrefab;
	
	public Transform CartridgeParent;
	public Transform ButtonParent;
	
	bool moving = false;
	
	int selectedGame = 0;
	
	List<string> launchPaths = new List<string>();
	
	public Scrollbar scrollbar;
	
	bool ready = false;
	
	//AUDIO STUFF
	public AudioSource source;
	public AudioClip[] clips;
	
	void Start()
	{
		StartCoroutine(UpdateGames());
		QualitySettings.vSyncCount = 1;
	}
	
	
	
	IEnumerator MoveObject(GameObject obj, Vector3 targetPosition, float duration)
	{
		Vector3 startPosition = obj.transform.position;
		float startTime = Time.time;

		while (Time.time - startTime < duration)
		{
			float t = (Time.time - startTime) / duration;
			t = Mathf.SmoothStep(0, 1, t);
			obj.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
			yield return null;
		}

		obj.transform.position = targetPosition;
	}
	
	void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			int width = Screen.width;
			int height = width * 9 / 16;
			Screen.SetResolution(width, height, false);
		}
	}
	
	public void SeekToGame(string name)
	{
		if(!moving)
		{
			moving = true;
			//Get difference between x-positions of selected and wanted cartridges and then move the parent object by that difference.
			GameObject[] gameObjects1 = games[name];
			GameObject[] gameObjects2 = games[games.Keys.Cast<string>().ElementAt(selectedGame)];
			
			List<string> keys = games.Keys.ToList();
			selectedGame = keys.IndexOf(name);
			
			float diff = gameObjects1[1].transform.position.x - gameObjects2[1].transform.position.x;
			//UDebug.Log(diff);
			float timeToSwitch = 0.5f;
			StartCoroutine(MoveObject(CartridgeParent.gameObject, CartridgeParent.position + new Vector3(-diff,0,0), timeToSwitch));
			Invoke("StopMoving", timeToSwitch);
		}
	}
	
	void StopMoving()
	{
		moving = false;
	}
	
	public void Play()
	{
		StartCoroutine(LaunchGame());
	}
	
	void PlaySound(int index)
	{
		source.PlayOneShot(clips[index], 0.2f);
	}
	
	string GetFirstLine(string path)
	{
		using (StreamReader reader = new StreamReader(path))
		{
			return reader.ReadLine();
		}
	}
	
	IEnumerator LaunchGame()
	{
		PlaySound(0);
		GameObject currentCartridge = games[games.Keys.Cast<string>().ElementAt(selectedGame)][1];
		StartCoroutine(MoveObject(currentCartridge, currentCartridge.transform.position - new Vector3(0,4,0), 1f));
		yield return new WaitForSeconds(0.8f);
		PlaySound(1);
		yield return new WaitForSeconds(0.2f);
		Process process = Process.Start(launchPaths[selectedGame]);
		Thread.Sleep(5000);
		// Find the game's process
		
		var list = launchPaths[selectedGame].Split(@"\");
		list[list.Length-1] = null;
		
		Process[] processes = Process.GetProcessesByName(GetFirstLine(string.Join(@"\", list)+"processName.txt"));
		UDebug.Log(GetFirstLine(string.Join(@"\", list)+"processName.txt"));
		if (processes.Length > 0)
		{
			Process gameProcess = processes[0];

			// Wait for the game to exit
			gameProcess.WaitForExit();
		}
		process.WaitForExit();
		StartCoroutine(MoveObject(currentCartridge, currentCartridge.transform.position + new Vector3(0,4,0), 1f));
	}
	
	
	IEnumerator LoadImage(string filePath, System.Action<Sprite> callback)
	{
		string[] extensions = new string[] { ".png", ".jpg", ".jpeg" };
		foreach (string ext in extensions)
		{
			string url = "file://" + filePath + ext;
			WWW www = new WWW(url);
			yield return www;
			if (string.IsNullOrEmpty(www.error))
			{
				Texture2D texture = www.texture;
				Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
				callback(sprite);
				yield break;
			}
		}
		UDebug.LogError("Error loading image: File not found");
	}

	IEnumerator UpdateGames()
	{
		//Paths to all game dirs
		string[] gameDirs = Directory.GetDirectories("C:\\Users\\Coldy\\Desktop\\Games");
		
		games = new SortedDictionary<string, GameObject[]>();
		launchPaths = new List<string>();
		
		int i = 0;
		bool done = true;
		foreach (string dir in gameDirs)
		{
			done = false;
			string[] tempList = dir.Split(@"\");
			
			string name = tempList[tempList.Length - 1];
			
			Sprite gameImg;
			

			StartCoroutine(LoadImage(dir + @"\IMG", (sprite) => {
				gameImg = sprite;
				
				//Create a button for the game
				GameObject button = Instantiate(ButtonPrefab, ButtonParent);
				button.transform.localPosition = new Vector3(0, i*-0.7f, 0);
				button.name = name + "_" + button.name.Split("_")[1];
				button.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = name;
				
				//Create a Cartridge for the game
				GameObject cartridge = Instantiate(CartridgePrefab, CartridgeParent);
				cartridge.transform.localPosition = new Vector3(i*10, 0, 0);
				cartridge.name = name + "_" + cartridge.name.Split("_")[1];
				cartridge.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = gameImg;
				
				//Temp array that gets added to the Dict
				GameObject[] objs = new GameObject[2];
				objs[0] = button;
				objs[1] = cartridge;
				
				//Add to Dict
				games.Add(name, objs);
				
				//Add to List
				launchPaths.Add(dir + @"\app.lnk");
				UDebug.Log("GAME DONE");
				done = true;
				i++;
			}));
			yield return new WaitUntil(() => done);
		}
		
		UDebug.Log("Games Dictionary updated.");
		scrollbar.GetComponent<Scrollbar>().size = 13f/games.Keys.ToList().Count;
		ready = true;
	}
	
	void Update()
	{
		if(ready)
		{
			ButtonParent.localPosition = new Vector3(-732,420+(games.Keys.ToList().Count-13)*70f*scrollbar.GetComponent<Scrollbar>().value,0);
		}
	}
}