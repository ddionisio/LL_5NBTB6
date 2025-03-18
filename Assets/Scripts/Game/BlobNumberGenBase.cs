using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlobNumberGenBase : MonoBehaviour {
	public int quotientResultCount = 1;

	public abstract BlobSpawnInfo[] GenerateSpawnInfos(int round);
}
