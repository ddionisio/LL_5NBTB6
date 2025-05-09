using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class PlayController : GameModeController<PlayController> {
	[Header("Controls")]
	public BoardController boardControl;
	public BlobConnectController connectControl;

	[Header("Spawn Control")]
	public BlobNumberGenBase[] numberGens;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string playMusic;

	[Header("Signal Invoke")]
	public M8.Signal signalInvokeStart;
	public M8.Signal signalInvokeNewRound;
	public M8.Signal signalInvokeAttack;
	public M8.Signal signalInvokeOpRefresh; //when an equation (op) from currentNumberGen is updated
	public M8.Signal signalInvokeAttackValueRefresh; //attack value updated

	[Header("Signal Listen")]
	public SignalBlob signalListenBlobClick;
	public SignalBlob signalListenBlobDragBegin;
	public SignalBlob signalListenBlobDragEnd;

	public SignalBlobActionResult signalListenOperation;
	public SignalBlobActionResult signalListenSplit;

	public int hitpoints { get { return mCurHitpoints; } }
	public int hitpointsMax { get { return mMaxHitpoints; } }
	public float hitpointsScale {
		get {
			var fHP = (float)hitpoints;
			var fHPMax = (float)mMaxHitpoints;

			return Mathf.Clamp01(fHP / fHPMax);
		}
	}

	public int attackValue { get { return mAttackValue; } }
	public float attackScale { //[0, 1] = attackValue/attackCapacity
		get {
			var fAttackVal = (float)mAttackValue;
			var fAttackCapacity = (float)GameData.instance.playAttackCapacity;

			return Mathf.Clamp01(fAttackVal / fAttackCapacity);
		}
	}
		
	public int roundsIndex { get { return mAttackScales.Count; } }

	public BlobNumberGenBase currentNumberGen { get { return numberGens[mNumberGenInd]; } }

	private int mAttackValue;
	private int mNumberGenInd; //this is level state in GameData
	private int mCurHitpoints;
	private int mMaxHitpoints;

	private bool mIsAttackComplete;

	private M8.CacheList<float> mAttackScales = new M8.CacheList<float>(8); //current count is the rounds counter

	protected override void OnInstanceDeinit() {
		if(connectControl) {
			connectControl.groupAddedCallback -= OnGroupAdded;
		}

		if(signalListenBlobClick) signalListenBlobClick.callback -= OnSignalBlobClick;
		if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback -= OnSignalBlobDragBegin;
		if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback -= OnSignalBlobDragEnd;

		if(signalListenOperation) signalListenOperation.callback -= OnSignalOperationResult;
		if(signalListenSplit) signalListenSplit.callback -= OnSignalSplitResult;

		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		var gameDat = GameData.instance;

		mNumberGenInd = gameDat.playStateIndex;

		//initialize blob pools
		boardControl.InitBlobPool();

		mMaxHitpoints = 0;

		for(int i = 0; i < numberGens.Length; i++) {
			if(numberGens[i]) {
				numberGens[i].InitBlobPoolTypes(boardControl.blobPool);

				mMaxHitpoints += numberGens[i].rounds;
			}
		}

		ResetStates(true);
		RefreshCurrentHitpoints();

		connectControl.groupAddedCallback += OnGroupAdded;

		if(signalListenBlobClick) signalListenBlobClick.callback += OnSignalBlobClick;
		if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback += OnSignalBlobDragBegin;
		if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback += OnSignalBlobDragEnd;

		if(signalListenOperation) signalListenOperation.callback += OnSignalOperationResult;
		if(signalListenSplit) signalListenSplit.callback += OnSignalSplitResult;
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		var gameDat = GameData.instance;
				
		//board enter
		yield return boardControl.PlayReady();

		//signal start (for hud, etc.)
		if(signalInvokeStart)
			signalInvokeStart.Invoke();

		while(mNumberGenInd < numberGens.Length) {
			var numGen = numberGens[mNumberGenInd];
			if(!numGen) { //fail-safe
				mNumberGenInd++;
				continue;
			}

			//signal update (for hud, etc.)
			if(signalInvokeNewRound)
				signalInvokeNewRound.Invoke();

			//spawn blobs
			var spawnInfos = numGen.GenerateSpawnInfos();

			boardControl.Spawn(spawnInfos);

			while(!mIsAttackComplete) {
				yield return null;
			}

			//success clear the blobs
			var blobActives = boardControl.blobActives;
			for(int i = 0; i < blobActives.Count; i++) {
				var blob = blobActives[i];
				if(blob.state == Blob.State.Solved)
					blob.state = Blob.State.Correct;
				else if(blob.state != Blob.State.None)
					blob.state = Blob.State.Despawning;

				boardControl.RemoveFromActive(blob);
			}

			//wait a bit
			//yield return new WaitForSeconds(1f);

			//determine attack
			var nAttack = attackScale;

			mAttackScales.Add(nAttack);

			//progress?
			if(nAttack >= gameDat.playAttackFullThreshold || mAttackScales.Count == numGen.rounds) {
				//save
				gameDat.SaveCurrentLevelPlayState(mNumberGenInd, mAttackScales.ToArray(), numGen.rounds);

				mNumberGenInd++;

				ResetStates(true);
			}
			else
				ResetStates(false);

			RefreshCurrentHitpoints();

			if(signalInvokeAttack)
				signalInvokeAttack.Invoke();

			//do attack
			yield return boardControl.Attack();
						
			yield return null;
		}

		//board defeat
		yield return boardControl.PlayDefeat();

		//victory
	}

	IEnumerator DoOperationResult(BlobActionResult result) {
		var gameDat = GameData.instance;

		var camCtrl = CameraController.main;
		if(camCtrl)
			camCtrl.raycastTarget = false; //disable blob input

		var grp = result.group;

		Blob blobLeft = null, blobRight = null;

		if(result.blobLeft)
			blobLeft = result.blobLeft;
		else if(grp != null)
			blobLeft = grp.blobOpLeft;

		if(result.blobRight)
			blobRight = result.blobRight;
		else if(grp != null)
			blobRight = grp.blobOpRight;

		var connectOp = grp != null ? grp.connectOp : null;

		switch(result.type) {
			case BlobActionResult.Type.Success:
				//set blobs to success, move towards each other
				if(blobLeft) {
					blobLeft.state = Blob.State.Correct;

					if(blobRight) {
						var dir = (blobRight.position - blobLeft.position).normalized;
						blobLeft.AddForce(dir * gameDat.blobMergeImpuse, ForceMode2D.Impulse);
					}

					boardControl.RemoveFromActive(blobLeft);
				}

				if(blobRight) {
					blobRight.state = Blob.State.Correct;

					if(blobLeft) {
						var dir = (blobLeft.position - blobRight.position).normalized;
						blobRight.AddForce(dir * gameDat.blobMergeImpuse, ForceMode2D.Impulse);
					}

					boardControl.RemoveFromActive(blobRight);
				}

				//connector to success
				if(connectOp) {
					connectOp.state = BlobConnect.State.Correct;
				}

				//play success sfx

				//wait for blobs to release
				do {
					yield return null;
				} while((blobLeft && blobLeft.state != Blob.State.None) || (blobRight && blobRight.state != Blob.State.None));

				//refresh op equation and see if we solved any
				//if so, apply update
				var isOpSolved = false;

				var opCount = currentNumberGen.opCount;

				for(int i = 0; i < opCount; i++) {
					var op = currentNumberGen.GetOperation(i);

					if(op.equal == result.val) {						
						currentNumberGen.SetOperationSolved(i, true);
						isOpSolved = true;
						break;
					}
				}

				//let display know an operation was solved
				if(isOpSolved) {
					if(signalInvokeOpRefresh)
						signalInvokeOpRefresh.Invoke();
				}

				//spawn merged version (quotient) between the two blobs
				var mergeData = BlobData.GetMergeData(blobLeft ? blobLeft.data : null, blobRight ? blobRight.data : null);

				boardControl.Spawn(new BlobSpawnInfo(mergeData, isOpSolved ? Blob.State.Solved : Blob.State.Normal, result.val, 0));

				
				//if all are matched, attack is complete
				mIsAttackComplete = currentNumberGen.opSolvedCount == currentNumberGen.opCount;
				break;

			case BlobActionResult.Type.Fail:
				//set blobs to failure
				if(blobLeft)
					blobLeft.state = Blob.State.Error;

				if(blobRight)
					blobRight.state = Blob.State.Error;

				if(connectOp)
					connectOp.state = BlobConnect.State.Error;

				//play fail sfx

				ReduceAttackValue();

				yield return null;
				break;
		}

		if(result.group != null)
			connectControl.ClearGroup(result.group);

		if(camCtrl)
			camCtrl.raycastTarget = true;
	}

	IEnumerator DoSplitResult(BlobActionResult.Type resultType, Blob blobDividend, Blob blobDivisor, int splitValue) {
		var camCtrl = CameraController.main;
		if(camCtrl)
			camCtrl.raycastTarget = false; //disable blob input

		switch(resultType) {
			case BlobActionResult.Type.Success:
				var blobSplitDat = blobDividend.data.splitBlobData ? blobDividend.data.splitBlobData : blobDividend.data;
				var blobPos = blobDividend.position;
				var blobVal = blobDividend.number;

				//release old blob
				blobDividend.state = Blob.State.Despawning;

				boardControl.RemoveFromActive(blobDividend);

				while(blobDividend.state != Blob.State.None)
					yield return null;

				//play split sfx

				//spawn two new blobs
				var dir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));

				var radius = blobSplitDat.spawnPointCheckRadius;
								
				boardControl.Spawn(new BlobSpawnInfo(blobSplitDat, blobVal - splitValue, blobDividend.divisor, blobPos + dir * radius));
				boardControl.Spawn(new BlobSpawnInfo(blobSplitDat, splitValue, blobDividend.divisor, blobPos - dir * radius));

				//spawn a duplicate divisor
				if(blobDivisor) {
					dir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));

					boardControl.Spawn(new BlobSpawnInfo(blobDivisor.data, blobDivisor.number, 0, blobDivisor.position + (dir * blobDivisor.radius * 2f)));
				}

				//wait for spawns to finish?
				break;

			case BlobActionResult.Type.Fail:
				blobDividend.state = Blob.State.Error;

				//play fail sfx
								
				yield return null;
				break;
		}

		ReduceAttackValue();

		if(camCtrl)
			camCtrl.raycastTarget = true;
	}

	void OnSignalBlobClick(Blob blob) {
		if(!blob.canSplit)
			return;

		var blobDivisor = boardControl.GetBlobDivisor(blob.number);
		if(!blobDivisor)
			return;

		//open split modal
	}

	void OnSignalBlobDragBegin(Blob blob) {
		if(connectControl.isDragDisabled)
			return;

		var blobActives = boardControl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blobActive = blobActives[i];
			if(blobActive == blob)
				continue;

			if(blob.GetConnectOpType(blobActive) != OperatorType.None) {
				blobActive.highlightLock = true;
			}
			else {
				blobActive.inputLocked = true;
			}
		}
	}

	void OnSignalBlobDragEnd(Blob blob) {
		if(connectControl.isDragDisabled)
			return;

		var blobActives = boardControl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blobActive = blobActives[i];

			blobActive.highlightLock = false;
			blobActive.inputLocked = false;
		}
	}

	void OnSignalOperationResult(BlobActionResult result) {
		StartCoroutine(DoOperationResult(result));
	}

	void OnSignalSplitResult(BlobActionResult result) {
		//TODO: determine how split happens based on split mode
		if(!result.blobLeft)
			return;

		StartCoroutine(DoSplitResult(result.type, result.blobLeft, result.blobRight, result.val));
	}

	void OnGroupAdded(BlobConnectController.Group grp) {
		if(grp.isOpFilled) {
			//open contextual modal operator for blob merging
		}
	}

	private void ReduceAttackValue() {
		if(mAttackValue > 1)
			mAttackValue--;

		//update hud
		if(signalInvokeAttackValueRefresh)
			signalInvokeAttackValueRefresh.Invoke();
	}

	private void ResetStates(bool clearAttackScales) {
		mAttackValue = GameData.instance.playAttackCapacity;
				
		mIsAttackComplete = false;

		if(clearAttackScales)
			mAttackScales.Clear();
	}

	private void RefreshCurrentHitpoints() {
		mCurHitpoints = mMaxHitpoints;

		for(int i = 0; i < mNumberGenInd; i++) {
			if(numberGens[i])
				mCurHitpoints -= numberGens[i].rounds;
		}

		mCurHitpoints -= roundsIndex;

		if(mCurHitpoints < 0)
			mCurHitpoints = 0;
	}
}