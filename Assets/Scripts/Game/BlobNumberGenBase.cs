using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlobNumberGenBase : MonoBehaviour {
	public int quotientResultCount = 1;

	public abstract void InitBlobPoolTypes(M8.PoolController blobPool);

	public abstract BlobSpawnInfo[] GenerateSpawnInfos(int round);

	public abstract bool CanSplit(Blob blob, int divisor);

	public abstract void Split(Blob blob, int divisor);
}
