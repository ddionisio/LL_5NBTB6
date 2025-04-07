using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
	[Header("Blob Spawn Config")]
	public LayerMask blobSpawnCheckMask; //ensure spot is fine to spawn
	public float blobSpawnDelay = 0.3f;
	public float blobSpawnClearoutForce = 5f;
	public float blobSpawnClearoutDelay = 3f;
}
