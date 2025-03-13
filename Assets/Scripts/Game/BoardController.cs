using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {
	public const string blobSpawnPoolGroup = "blobs";
	public const int blobActiveCapacity = 128;

	public struct BlobSpawnInfo {
		public string nameOverride; //ignored if null or empty
		public BlobData data;
		public int number;
	}

	[Header("Board")]
	public Vector2 boardOriginOffset;
	public float boardRadius;

	[Header("Blob")]
	public M8.ColorPalette blobPalette;

	public Vector2 boardPosition {
		get { return ((Vector2)transform.position) + boardOriginOffset; }
	}

	public int blobSpawnQueueCount { get { return mBlobSpawnQueue.Count; } }

	public Queue<BlobSpawnInfo> blobSpawnQueue { get { return mBlobSpawnQueue; } }

	public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

	public bool isBlobSpawning { get { return mBlobSpawnRout != null; } }

	private M8.PoolController mBlobPool;

	private WaitForSeconds mBlobSpawnWait;

	private int[] mBlobPaletteIndices;
	private int mBlobPaletteCurrentInd;

	private Queue<BlobSpawnInfo> mBlobSpawnQueue = new Queue<BlobSpawnInfo>();
	private Coroutine mBlobSpawnRout;

	private M8.GenericParams mBlobSpawnParms = new M8.GenericParams();
	
	private M8.CacheList<Blob> mBlobActives;

	private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

	private Collider2D[] mColliderCache = new Collider2D[128];

	public bool CheckAnyBlobActiveState(params Blob.State[] states) {
		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(blob) {
				for(int j = 0; j < states.Length; j++) {
					if(blob.state == states[j])
						return true;
				}
			}
		}

		return false;
	}

	public int GetBlobStateCount(params Blob.State[] states) {
		int count = 0;

		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(blob) {
				for(int j = 0; j < states.Length; j++) {
					if(blob.state == states[j]) {
						count++;
						break;
					}
				}
			}
		}

		return count;
	}

	public Blob GetBlobActiveByName(string blobName) {
		if(mBlobActives == null)
			return null;

		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(!blob)
				continue;

			if(blob.name == blobName)
				return blob;
		}

		return null;
	}

	public void DespawnAllBlobs() {
		if(mBlobActives == null)
			return;

		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(!blob)
				continue;

			if(blob.poolData)
				blob.poolData.despawnCallback -= OnBlobRelease;

			blob.state = Blob.State.Despawning;
		}

		mBlobActives.Clear();
	}

	public void SpawnStop() {
		if(mBlobSpawnRout != null) {
			StopCoroutine(mBlobSpawnRout);
			mBlobSpawnRout = null;
		}

		mBlobSpawnQueue.Clear();
	}

	public void Spawn(BlobData blobData, int number) {
		mBlobSpawnQueue.Enqueue(new BlobSpawnInfo { data = blobData, number = number });
		if(mBlobSpawnRout == null)
			mBlobSpawnRout = StartCoroutine(DoSpawnQueue());
	}

	public void Spawn(string nameOverride, BlobData blobData, int number) {
		mBlobSpawnQueue.Enqueue(new BlobSpawnInfo { nameOverride = nameOverride, data = blobData, number = number });
		if(mBlobSpawnRout == null)
			mBlobSpawnRout = StartCoroutine(DoSpawnQueue());
	}

	public void RemoveFromActive(Blob blob) {
		if(mBlobActives.Remove(blob)) {
			blob.poolData.despawnCallback -= OnBlobRelease;
		}
	}

	void OnDisable() {
		SpawnStop();
	}

	void Awake() {
		//initialize pool
		mBlobPool = M8.PoolController.GetPool(blobSpawnPoolGroup);
		if(!mBlobPool) {
			mBlobPool = M8.PoolController.CreatePool(blobSpawnPoolGroup);
			mBlobPool.gameObject.DontDestroyOnLoad();

			GameData.instance.InitBlobSpawnTypes(mBlobPool);
		}

		mBlobActives = new M8.CacheList<Blob>(blobActiveCapacity);

		mBlobSpawnWait = new WaitForSeconds(GameData.instance.blobSpawnDelay);
	}

	IEnumerator DoSpawnQueue() {
		var gameDat = GameData.instance;

		while(mBlobSpawnQueue.Count > 0) {
			while(mBlobActives.IsFull) //wait for blobs to release
				yield return null;

			yield return mBlobSpawnWait;

			var spawnInfo = mBlobSpawnQueue.Dequeue();

			var blobDat = spawnInfo.data;

			//get spawn point, and clear out other blobs within spawn area
			var checkRadius = blobDat.spawnPointCheckRadius;
			Vector2 spawnPt = GenerateBlobSpawnPoint(checkRadius);

			var curTime = 0f;
			while(curTime < gameDat.blobSpawnClearoutDelay) {
				var overlapCount = Physics2D.OverlapCircleNonAlloc(spawnPt, checkRadius, mColliderCache, gameDat.blobSpawnCheckMask);
				for(int i = 0; i < overlapCount; i++) {
					var coll = mColliderCache[i];

					Rigidbody2D overlapBody = null;

					//grab central body of blob
					var jellyRefPt = coll.GetComponent<JellySpriteReferencePoint>();
					if(jellyRefPt) {
						var jellySpr = jellyRefPt.ParentJellySprite;
						if(jellySpr)
							overlapBody = jellySpr.CentralPoint.Body2D;
					}
					else //some other object (connector)
						overlapBody = coll.GetComponent<Rigidbody2D>();

					if(overlapBody) {
						var dir = (overlapBody.position - spawnPt).normalized;
						overlapBody.AddForce(dir * gameDat.blobSpawnClearoutForce);
					}
				}

				if(overlapCount == 0)
					break;

				yield return null;

				curTime += Time.deltaTime;
			}

			//setup color
			if(blobPalette && blobPalette.count > 0) {
				Color spawnColor;

				if(mBlobPaletteIndices == null) { //init
					mBlobPaletteIndices = new int[blobPalette.count];
					for(int i = 0; i < mBlobPaletteIndices.Length; i++)
						mBlobPaletteIndices[i] = i;
					M8.ArrayUtil.Shuffle(mBlobPaletteIndices);
				}

				spawnColor = blobPalette.GetColor(mBlobPaletteIndices[mBlobPaletteCurrentInd]);

				mBlobPaletteCurrentInd++;
				if(mBlobPaletteCurrentInd == mBlobPaletteIndices.Length)
					mBlobPaletteCurrentInd = 0;

				mBlobSpawnParms[JellySpriteSpawnController.parmColor] = spawnColor;
			}

			//spawn
			var template = blobDat.template;

			mBlobSpawnParms[JellySpriteSpawnController.parmPosition] = spawnPt;
			mBlobSpawnParms[Blob.parmData] = blobDat;
			mBlobSpawnParms[Blob.parmNumber] = spawnInfo.number;
						
			string blobName;

			if(string.IsNullOrEmpty(spawnInfo.nameOverride)) {
				mBlobNameCache.Clear();
				mBlobNameCache.Append(template.name);
				mBlobNameCache.Append(' ');
				mBlobNameCache.Append(spawnInfo.number);

				blobName = mBlobNameCache.ToString();
			}
			else
				blobName = spawnInfo.nameOverride;

			var blob = mBlobPool.Spawn<Blob>(template.name, blobName, null, mBlobSpawnParms);

			blob.poolData.despawnCallback += OnBlobRelease;

			mBlobActives.Add(blob);
		}

		mBlobSpawnRout = null;
	}

	void OnBlobRelease(M8.PoolDataController pdc) {
		pdc.despawnCallback -= OnBlobRelease;

		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(blob && blob.poolData == pdc) {
				mBlobActives.RemoveAt(i);
				break;
			}
		}
	}

	private Vector2 GenerateBlobSpawnPoint(float blobRadius) {
		return boardPosition + (Random.insideUnitCircle * (boardRadius - blobRadius));
	}
}
