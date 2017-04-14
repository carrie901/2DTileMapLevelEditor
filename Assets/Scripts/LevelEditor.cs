﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic; // Lists
//You must include these namespaces
//to use BinaryFormatter
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelEditor : MonoBehaviour {

	public static LevelEditor instance = null;

	const int EMPTY = -1;

	public int WIDTH = 16;
	public int HEIGHT = 14;
	public int LAYERS = 10;

	private int[, ,] level;
	private Transform[, ,] gameObjects;

	public List<Transform> tiles;

	private int selectedTile = 0;
	private int selectedLayer = 0;

	private bool toggleGrid = true;
	private bool deleteMode = false;

	GameObject tileLevelParent;

	//The object we are currently looking to spawn
	Transform toCreate;

	string levelName = "Temp";

	public Texture2D deleteTexture;

	void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if(instance != this){
			Destroy(gameObject);
		}
		DontDestroyOnLoad(gameObject);
	}

	public void Start()
	{
		// Get the tileLevelParent object so we can make it our newly created objects' parent
		tileLevelParent = GameObject.Find("TileLevel");
		if (tileLevelParent == null) {
			tileLevelParent = new GameObject ("TileLevel");
		}
		level = createEmptyLevel ();
		gameObjects = new Transform[WIDTH, HEIGHT, LAYERS];
		//BuildLevel();
		enabled = true;

		toCreate = tiles[0];
	}

	private void toggleDeleteCursor(){
		if (deleteMode) {
			Cursor.SetCursor (ScaleTexture (deleteTexture, 16, 16), Vector2.zero, CursorMode.Auto);
		} else {
			Cursor.SetCursor (null, Vector2.zero, CursorMode.Auto);
		}
	}


	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result = new Texture2D (targetWidth, targetHeight, source.format, false);
//		float incX = (1.0f / (float)targetWidth);
//		float incY = (1.0f / (float)targetHeight);
		for (int i = 0; i < result.height; ++i) {
			for (int j = 0; j < result.width; ++j) {
				Color newColor = source.GetPixelBilinear ((float)j / (float)result.width, (float)i / (float)result.height);
				result.SetPixel (j, i, newColor);
			}
		}
		result.Apply ();
		return result;
	}

	int[, ,] createEmptyLevel(){
		int[,,] level = new int[WIDTH, HEIGHT, LAYERS];
		for (int xPos = 0; xPos < WIDTH; xPos++) {
			for (int yPos = 0; yPos < HEIGHT; yPos++) {
				for (int zPos = 0; zPos < LAYERS; zPos++) {
					level [xPos, yPos, zPos] = EMPTY;
				}
			}
		}
		return level;
	}

	void clearLevel() {
		for (int x = 0; x < WIDTH; x++){
			for (int y = 0; y < HEIGHT; y++) {
				for (int z = 0; z < LAYERS; z++) {
					level [x, y, z] = EMPTY;
				}
			}
		}
	}

	private bool validPosition(int xPos, int yPos, int zPos){
		if (xPos < 0 || xPos >= WIDTH || yPos < 0 || yPos >= HEIGHT || zPos < 0 || zPos >= LAYERS) {
			return false;
		}
		else{
			return true;
		}
	}

	private bool emptyLayer(int layer){
		bool result = true;
		for (int x = 0; x < WIDTH; x++){
			for(int y = 0; y < HEIGHT; y++){
				if(level[x, y, layer] != -1){
					result = false;
				}
			}
		}
		return result;
	}
			

//	void BuildLevel()
//	{
//		//Go through each element inside our level variable
//		for (int yPos = 0; yPos < level.Length; yPos++)
//		{
//			for (int xPos = 0; xPos < (level[yPos]).Length; xPos++)
//			{
//				CreateBlock(level[yPos][xPos], xPos, level.Length - yPos - 1);
//			}
//		}
//	}

	public void CreateBlock(int value, int xPos, int yPos, int zPos)
	{
		Transform toCreate = null;
		// We need to know the size of our level to save later
		if (!validPosition(xPos, yPos, zPos)) {
			return;
		}
		level [xPos, yPos, zPos] = value;
		// If the value is not empty 
		if (value != EMPTY) {
			toCreate = tiles [value];
		}
		if (toCreate != null) {
			print ("Creating " + tiles [value].name + " on: (" + xPos + "," + yPos + "," + zPos + ")");
			//Create the object we want to create
			Transform newObject = Instantiate (toCreate, new Vector3 (xPos, yPos, 0), Quaternion.identity) as Transform;
			//Give the new object the same name as ours
			newObject.name = toCreate.name;
			// Set the object's parent to the DynamicObjects
			// variable so it doesn't clutter our Hierarchy
			newObject.parent = tileLevelParent.transform;

			gameObjects [xPos, yPos, zPos] = newObject;
		}
	}

	void Update()
	{
		// Left click - Create object
		if (Input.GetMouseButton (0) && GUIUtility.hotControl == 0) {
			Vector3 mousePos = Input.mousePosition;
			//Set the position in the z axis to the opposite of the
			// camera's so that the position is on the world so
			// ScreenToWorldPoint will give us valid values.
			mousePos.z = Camera.main.transform.position.z * -1;
			Vector3 pos = Camera.main.ScreenToWorldPoint (mousePos);
			// Deal with the mouse being not exactly on a block
			int posX = Mathf.FloorToInt (pos.x + .5f);
			int posY = Mathf.FloorToInt (pos.y + .5f);
			if(!validPosition(posX, posY, selectedLayer)){
				return;
			}
			if (!deleteMode) {
//				print (posX);
//				print (posY);
//				print ("Selected tile: " + selectedTile);
//				print ("Currently on position " + level [posX, posY, selectedLayer]);
				if (level [posX, posY, selectedLayer] == EMPTY) {
					CreateBlock (tiles.IndexOf (toCreate), posX, posY, selectedLayer);
				}
				//If it's the same, just keep the previous one
				else if (level [posX, posY, selectedLayer] == selectedTile) {
					//print ("Already there, yo");
				} else {
					DestroyImmediate (gameObjects [posX, posY, selectedLayer].gameObject);
					CreateBlock (tiles.IndexOf (toCreate), posX, posY, selectedLayer);
				}
			} else {
				// Right clicking - Delete object
				//if ((Input.GetMouseButton (1) || Input.GetKeyDown (KeyCode.T)) && GUIUtility.hotControl == 0) {

				// If we hit something (!= EMPTY), we want to destroy it!
				if (level [posX, posY, selectedLayer] != EMPTY) {
					DestroyImmediate (gameObjects [posX, posY, selectedLayer].gameObject);
					level [posX, posY, selectedLayer] = EMPTY;
				}
			}
		}
	}

	void OnGUI()
	{
		GUILayout.BeginArea (new Rect (Screen.width - 210, 20, 200, 800));
		Texture[] GUItiles = new Texture[tiles.Count];
		int i = 0;
		foreach (Transform item in tiles) {
			GUItiles [i] = item.gameObject.GetComponent<SpriteRenderer> ().sprite.texture;
			i++;
		}
		selectedTile = GUILayout.SelectionGrid (selectedTile, (Texture[])GUItiles, 3);
		toCreate = tiles [selectedTile];
		GUILayout.EndArea ();

		GUILayout.BeginArea (new Rect (10, 120, 100, 300));
		levelName = GUILayout.TextField (levelName);
		if (GUILayout.Button ("Save")) {
			SaveLevel ();
		}
		if (GUILayout.Button ("Load")) {
			LoadLevelFile ();

		}
		if (GUILayout.Button ("Quit")) {
			enabled = false;
		}

		GUILayout.Label ("Layer " + selectedLayer);
		if (GUILayout.Button ("+ Layer")) {
			selectedLayer = Mathf.Clamp (selectedLayer + 1, 0, 100);
		}
		if (GUILayout.Button ("- Layer")) {
			selectedLayer = Mathf.Clamp (selectedLayer - 1, 0, 100);
		}

		toggleGrid = GUILayout.Toggle (toggleGrid, "Show Grid");
		deleteMode = GUILayout.Toggle (deleteMode, "Delete Mode");
		toggleDeleteCursor ();
		GridOverlay.instance.enabled = toggleGrid;
		GUILayout.EndArea ();
	}

	void SaveLevel()
	{
		print(this.level.ToString ());
		List<string> newLevel = new List<string> ();
		for (int layer = 0; layer < LAYERS; layer++) {
			if (!emptyLayer(layer)) {
				for (int y = 0; y < HEIGHT; y++) {
					string newRow = "";
					for (int x = 0; x < WIDTH; x++) {
						newRow += + level[x, y, layer] + ",";
					}
					if (y != 0) {
						newRow += "\n";
					}
					newLevel.Add (newRow);
				}
				//if (layer != 0) {
					newLevel.Add ("\t");
				//}
			}
		}

		// Reverse the rows to make the final version rightside up
		newLevel.Reverse ();
		string levelComplete = "";
		foreach (string level in newLevel) {
			levelComplete += level;
		}
		// This is the data we're going to be saving
		print (levelComplete);
		//Save to a file
		BinaryFormatter bFormatter = new BinaryFormatter ();
		string path = EditorUtility.SaveFilePanel("Save level", "", levelName, "lvl" );
		if (path.Length != 0) {
			FileStream file = File.Create (path);
			bFormatter.Serialize (file, levelComplete);
			file.Close ();
		} else {
			print ("Failed to save level");
		}
	}

	void LoadLevelFile()
	{
		// Destroy everything inside our currently level that's created
		// dynamically
		foreach(Transform child in tileLevelParent.transform) {
			Destroy(child.gameObject);
		}
		BinaryFormatter bFormatter = new BinaryFormatter();
		string path = EditorUtility.OpenFilePanel("Open level", "", "lvl" );
		if (path.Length != 0) {
			FileStream file = File.OpenRead (path);
			// Convert the file from a byte array into a string
			string levelData = bFormatter.Deserialize (file) as string;
			// We're done working with the file so we can close it
			file.Close ();
			LoadLevelFromStringLayers (levelData);
		}else {
			print ("Failed to open level");
		}
	}

	public void LoadLevelFromStringLayers(string content){
		List <string> layers = new List <string> (content.Split('\t'));
		int layerCounter = 0;
		foreach (string layer in layers) {
			if (layer.Trim () != "") {
				print ("Loaded Layer " + layerCounter + ":");
				print (layer);
				LoadLevelFromString(layer);
			}
			layerCounter++;
		}
	}

	public void LoadLevelFromString(string content)
	{
		print ("Load content:\n" + content);
		// Split our string by the new lines (enter)
		List <string> lines = new List <string> (content.Split('\n'));
		// Place each block in order in the correct x and y position
		for(int i = 0; i < lines.Count; i++)
		{
			string[] blockIDs = lines[i].Split (',');
			for(int j = 0; j < blockIDs.Length - 1; j++)
			{
				CreateBlock(int.Parse(blockIDs[j]), j, lines.Count - i - 1, 0);
			}
		}
	}
}
