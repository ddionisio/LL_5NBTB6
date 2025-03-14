using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
	[Header("Blob Config")]
	public BlobData blobDividend;
	public BlobData blobDividendSplit;
	public BlobData blobDivisor;
	public BlobData blobQuotient;
	public BlobData blobQuotientMerged;

	[Header("Blob Spawn Config")]
	public LayerMask blobSpawnCheckMask; //ensure spot is fine to spawn
	public float blobSpawnDelay = 0.3f;
	public float blobSpawnClearoutForce = 5f;
	public float blobSpawnClearoutDelay = 3f;

	public void InitBlobSpawnTypes(M8.PoolController blobPool) {
		if(blobDividend) blobDividend.InitPool(blobPool);
		if(blobDividendSplit) blobDividendSplit.InitPool(blobPool);
		if(blobDivisor) blobDivisor.InitPool(blobPool);
		if(blobQuotient) blobQuotient.InitPool(blobPool);
		if(blobQuotientMerged) blobQuotientMerged.InitPool(blobPool);
	}
}
