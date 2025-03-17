using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlobNumberGenBase : MonoBehaviour {
	public abstract BlobSpawnInfo[] GenerateSpawnInfos(int round);
}
