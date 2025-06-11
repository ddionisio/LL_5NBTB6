using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {
	public const string blobSpawnPoolGroup = "blobs";
	public const int blobActiveCapacity = 128;

	[Header("Board Telemetry")]
	public Vector2 originOffset;
	public float radius;

	[Header("Attack Info")]
	public HUDAttackValue attackHUD;
	public AttackEntity attackEntityPrefab;
	public ParticleSystem attackBoardFX;
	public Transform attackEntityRoot;
	public Vector2 attackStartOffset;
	public float attackStartRadius;
	public float attackEndRadiusOffset;
	public float attackIntervalDelay = 0.3f;
	public int attackValue = 3;

	[Header("Board Animation")]
	public M8.Animator.Animate animator;

	[M8.Animator.TakeSelector]
	public int takeEnter = -1;
	[M8.Animator.TakeSelector]
	public int takeHurt = -1;
	[M8.Animator.TakeSelector]
	public int takeEnd = -1;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxAttack;

	public bool active { get { return gameObject.activeSelf; } set { gameObject.SetActive(value); } }

	public Vector2 position {
		get { return ((Vector2)transform.position) + originOffset; }
	}

	public M8.PoolController blobPool { get; private set; }

	public int blobSpawnQueueCount { get { return mBlobSpawnQueue.Count; } }

	public Queue<BlobSpawnInfo> blobSpawnQueue { get { return mBlobSpawnQueue; } }

	public M8.CacheList<Blob> blobActives { get { return mBlobActives; } }

	public bool isBlobSpawning { get { return mBlobSpawnRout != null; } }

	public int blobActiveCount { get { return mBlobActives.Count; } }

	private WaitForSeconds mBlobSpawnWait;

	private Queue<BlobSpawnInfo> mBlobSpawnQueue = new Queue<BlobSpawnInfo>(blobActiveCapacity);
	private Coroutine mBlobSpawnRout;

	private M8.GenericParams mBlobSpawnParms = new M8.GenericParams();
	
	private M8.CacheList<Blob> mBlobActives = new M8.CacheList<Blob>(blobActiveCapacity);

	private System.Text.StringBuilder mBlobNameCache = new System.Text.StringBuilder();

	private Collider2D[] mColliderCache = new Collider2D[128];

	private AttackEntity[] mAttackEntities;
	private WaitForSeconds mAttackWaitInterval;

	public void Init() {
		//initialize all blob things
		blobPool = M8.PoolController.GetPool(blobSpawnPoolGroup);
		if(!blobPool) {
			blobPool = M8.PoolController.CreatePool(blobSpawnPoolGroup);
			blobPool.gameObject.DontDestroyOnLoad();
		}

		//initialize all attack things
		var attackCapacity = (GameData.instance.playAttackCapacity / attackValue) + 1;

		mAttackEntities = new AttackEntity[attackCapacity];
		for(int i = 0; i < attackCapacity; i++) {
			var attackEnt = Instantiate(attackEntityPrefab, attackEntityRoot);

			mAttackEntities[i] = attackEnt;

			attackEnt.active = false;
		}

		if(attackEntityRoot) attackEntityRoot.gameObject.SetActive(false);

		mAttackWaitInterval = new WaitForSeconds(attackIntervalDelay);
		mBlobSpawnWait = new WaitForSeconds(GameData.instance.blobSpawnDelay);
	}

	//Board Interface

	public void PlayerFail() {
		if(takeHurt != -1)
			animator.Play(takeHurt);
	}
	
	public IEnumerator PlayEnter() {
		if(takeEnter != -1)
			yield return animator.PlayWait(takeEnter);
		else
			yield return null;
	}

	public IEnumerator PlayEnd() {
		if(takeEnd != -1)
			yield return animator.PlayWait(takeEnd);
		else
			yield return null;
	}

	public IEnumerator Attack() {
		yield return null;

		if(attackEntityRoot) attackEntityRoot.gameObject.SetActive(true);

		var attackOrigin = (Vector2)transform.position + attackStartOffset;
		var boardOrigin = position;

		var attackCount = 0;

		for(; attackCount < mAttackEntities.Length && attackHUD.attackValue > 1; attackCount++) {
			if(!string.IsNullOrEmpty(sfxAttack))
				M8.SoundPlaylist.instance.Play(sfxAttack, false);

			var ent = mAttackEntities[attackCount];

			var start = attackOrigin + Random.insideUnitCircle * attackStartRadius;
			var end = boardOrigin + Random.insideUnitCircle * (radius - attackEndRadiusOffset);

			ent.active = true;
			ent.Move(start, end, attackBoardFX);

			attackHUD.attackValue -= attackValue;

			yield return mAttackWaitInterval;
		}

		attackHUD.attackValue = 0; //just in case

		var attackFinishCount = 0;
		while(attackFinishCount < attackCount) {
			yield return null;

			var lastCount = attackFinishCount;

			attackFinishCount = 0;
			for(int i = 0; i < attackCount; i++) {
				var ent = mAttackEntities[i];
				if(!ent.isBusy)
					attackFinishCount++;
			}
		}

		for(int i = 0; i < attackCount; i++)
			mAttackEntities[i].active = false;

		if(attackEntityRoot) attackEntityRoot.gameObject.SetActive(false);
	}

	//Blob Interface
		
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

	/// <summary>
	/// Find a blob that can divide given dividend as a whole number.
	/// </summary>
	public Blob GetBlobDivisor(int dividend) {
		if(mBlobActives == null)
			return null;

		for(int i = 0; i < mBlobActives.Count; i++) {
			var blob = mBlobActives[i];
			if(!blob || blob.data.type != BlobData.Type.Divisor)
				continue;

			var divisorVal = blob.number;

			if(dividend % divisorVal == 0)
				return blob;
		}

		return null;
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
		if(takeEnter != -1)
			animator.ResetTake(takeEnter);
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

			Vector2 spawnPt;

			if(spawnInfo.isSpawnPointOverride) {
				var pt = spawnInfo.spawnPointOverride;

				//check to see if it is 'out of bounds'
				if(Physics2D.OverlapCircle(pt, checkRadius, gameDat.blobSpawnCheckSolidMask))
					spawnPt = GenerateBlobSpawnPoint(checkRadius); //just use guaranteed point within board
				else
					spawnPt = pt;
			}
			else
				spawnPt = GenerateBlobSpawnPoint(checkRadius);

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

			//spawn
			var templateName = blobDat.templateName;

			mBlobSpawnParms[JellySpriteSpawnController.parmPosition] = spawnPt;
			mBlobSpawnParms[Blob.parmData] = blobDat;
			mBlobSpawnParms[JellySpriteSpawnController.parmColor] = blobDat.color;
			mBlobSpawnParms[Blob.parmNumber] = spawnInfo.number;
			mBlobSpawnParms[Blob.parmDivisor] = spawnInfo.divisor;
			mBlobSpawnParms[Blob.parmSplitCount] = spawnInfo.splitCount;
			mBlobSpawnParms[Blob.parmState] = spawnInfo.spawnToState != Blob.State.None ? spawnInfo.spawnToState : Blob.State.Normal;
			mBlobSpawnParms[Blob.parmLock] = spawnInfo.locked;

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

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere((Vector2)transform.position + attackStartOffset, attackStartRadius);
	}
}
