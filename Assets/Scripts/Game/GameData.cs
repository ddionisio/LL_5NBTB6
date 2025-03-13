using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
	[Header("Blob Config")]
	public BlobData blobDividend;
	public BlobData blobDivisor;
	public BlobData blobQuotient;

	[Header("Blob Spawn Config")]
	public int blobSpawnPoolCapacity = 16;
	public LayerMask blobSpawnCheckMask; //ensure spot is fine to spawn
	public float blobSpawnDelay = 0.3f;
	public float blobSpawnClearoutForce = 5f;
	public float blobSpawnClearoutDelay = 3f;

	public void InitBlobSpawnTypes(M8.PoolController blobPool) {
		if(blobDividend) blobDividend.InitPool(blobPool, blobSpawnPoolCapacity);
		if(blobDivisor) blobDivisor.InitPool(blobPool, blobSpawnPoolCapacity);
		if(blobQuotient) blobQuotient.InitPool(blobPool, blobSpawnPoolCapacity);
	}
}
