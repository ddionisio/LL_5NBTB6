using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {
	public const string blobSpawnPoolGroup = "blobs";
	public const int blobActiveCapacity = 128;

	[Header("Board Data")]
	public int hitpointMax = 5;
	public int attackMax = 4;

	[Header("Board Telemetry")]
	public Vector2 originOffset;
	public float radius;

	[Header("Board Animation")]
	public M8.Animator.Animate animator;

	[M8.Animator.TakeSelector]
	public int takeReady = -1;
	[M8.Animator.TakeSelector]
	public int takeHurt = -1;
	[M8.Animator.TakeSelector]
	public int takeDefeat = -1;

	[Header("Blob")]
	public M8.ColorPalette blobPalette;

	public Vector2 position {
		get { return ((Vector2)transform.position) + originOffset; }
	}

	public int hitpoints { get { return mHitpointCurrent; } }

	public int attack { get { return mAttackCurrent; } }

	public M8.PoolController blobPool { get; private set; }

	public int blobSpawnQueueCount { get { return mBlobSpawnQueue.Count; } }

	public Queue<BlobSpawnInfo> blobSpawnQueue { get { return mBlobSpawnQueue; } }

	public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

	public bool isBlobSpawning { get { return mBlobSpawnRout != null; } }

	public int blobActiveCount { get { return mBlobActives.Count; } }

	private int mHitpointCurrent;
	private int mAttackCurrent;

	private WaitForSeconds mBlobSpawnWait;

	private int[] mBlobPaletteIndices;
	private int mBlobPaletteCurrentInd;

	private Queue<BlobSpawnInfo> mBlobSpawnQueue = new Queue<BlobSpawnInfo>(blobActiveCapacity);
	private Coroutine mBlobSpawnRout;

	private M8.GenericParams mBlobSpawnParms = new M8.GenericParams();
	
	private M8.CacheList<Blob> mBlobActives = new M8.CacheList<Blob>(blobActiveCapacity);

	private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

	private Collider2D[] mColliderCache = new Collider2D[128];
	
	//Board Interface
	
	public IEnumerator PlayReady() {
		if(takeReady != -1)
			yield return animator.PlayWait(takeReady);
		else
			yield return null;

		//TODO: callback ready (board hud initialize, animate enter)
	}

	public IEnumerator PlayDefeat() {
		if(takeDefeat != -1)
			yield return animator.PlayWait(takeDefeat);
		else
			yield return null;
	}

	public IEnumerator Attack() {

		//decrement hp based on attack

		//board hud animation

		//hurt

		//reset attack

		yield return null;
	}

	//Blob Interface

	public void InitBlobPool() {
		blobPool = M8.PoolController.GetPool(blobSpawnPoolGroup);
		if(!blobPool) {
			blobPool = M8.PoolController.CreatePool(blobSpawnPoolGroup);
			blobPool.gameObject.DontDestroyOnLoad();
		}
	}

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

	public void Spawn(BlobSpawnInfo spawnInfo) {
		mBlobSpawnQueue.Enqueue(spawnInfo);
		if(mBlobSpawnRout == null)
			mBlobSpawnRout = StartCoroutine(DoSpawnQueue());
	}

	public void Spawn(BlobSpawnInfo[] spawnInfos) {
		for(int i = 0; i < spawnInfos.Length; i++)
			mBlobSpawnQueue.Enqueue(spawnInfos[i]);

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
		mBlobSpawnWait = new WaitForSeconds(GameData.instance.blobSpawnDelay);

		mHitpointCurrent = hitpointMax;
		mAttackCurrent = attackMax;
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
			var templateName = blobDat.templateName;

			mBlobSpawnParms[JellySpriteSpawnController.parmPosition] = spawnPt;
			mBlobSpawnParms[Blob.parmData] = blobDat;
			mBlobSpawnParms[Blob.parmNumber] = spawnInfo.number;
						
			string blobName;

			if(string.IsNullOrEmpty(spawnInfo.nameOverride)) {
				mBlobNameCache.Clear();
				mBlobNameCache.Append(templateName);
				mBlobNameCache.Append(' ');
				mBlobNameCache.Append(spawnInfo.number);

				blobName = mBlobNameCache.ToString();
			}
			else
				blobName = spawnInfo.nameOverride;

			var blob = blobPool.Spawn<Blob>(templateName, blobName, null, mBlobSpawnParms);

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
		return position + (Random.insideUnitCircle * (radius - blobRadius));
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(position, radius);
	}
}
