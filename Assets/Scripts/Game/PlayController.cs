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
	public M8.Signal signalInvokeAttackFinish;
	public M8.SignalInteger signalInvokeOpSuccess; //when an equation (op) from currentNumberGen is completed
	public M8.Signal signalInvokeAttackValueRefresh; //attack value updated
	public M8.Signal signalInvokeFail;
	public M8.Signal signalInvokeEnd;
	public M8.Signal signalInvokeSplitProceed;
	public M8.Signal signalInvokeOpProceed;

	[Header("Signal Listen")]
	public SignalBlob signalListenBlobClick;
	public SignalBlob signalListenBlobDragBegin;
	public SignalBlob signalListenBlobDragEnd;

	public SignalBlobActionResult signalListenOperation;
	public SignalBlobActionResult signalListenSplit;

	[Header("Debug")]
	public bool debugOverrideNumberIndexEnable;
	public int debugOverrideNumberIndex;

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

	public int stateIndex { get { return mNumberGenInd; } }

	public int failRoundCount { get { return mFailRoundCount; } }

	public int failCount { get { return mFailCount; } }

	public bool roundPause { get; set; }

	public BlobNumberGenBase currentNumberGen { get { return numberGens[mNumberGenInd]; } }

	private int mAttackValue;
	private int mNumberGenInd; //this is level state in GameData
	private int mCurHitpoints;
	private int mMaxHitpoints;

	private int mFailRoundCount; //relative to a round
	private int mFailCount; //saved

	private bool mIsAttackComplete;

	private M8.CacheList<float> mAttackScales = new M8.CacheList<float>(8); //current count is the rounds counter

	private M8.GenericParams mModalOpParms = new M8.GenericParams();
	private M8.GenericParams mModalBlobSplitParms = new M8.GenericParams();

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
		
		//initialize board
		boardControl.Init();

		mMaxHitpoints = 0; //also initialize max hitpoints

		for(int i = 0; i < numberGens.Length; i++) {
			if(numberGens[i]) {
				numberGens[i].InitBlobPoolTypes(boardControl.blobPool);

				mMaxHitpoints += numberGens[i].rounds;
			}
		}

		//setup callbacks
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

		mNumberGenInd = gameDat.playStateIndex;

		if(Application.isEditor) {
			if(debugOverrideNumberIndexEnable)
				mNumberGenInd = debugOverrideNumberIndex;
		}

		if(mNumberGenInd >= numberGens.Length) //fail-safe if we changed the play config
			mNumberGenInd = 0;

		mFailCount = 0;

		ResetStates(true);
		RefreshCurrentHitpoints();

		//board enter
		yield return boardControl.PlayEnter();

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

			while(roundPause)
				yield return null;

			//spawn blobs
			var spawnInfos = numGen.GenerateSpawnInfos();

			boardControl.Spawn(spawnInfos);
						
			while(!mIsAttackComplete && mFailRoundCount < gameDat.playFailRoundCount) {
				yield return null;
			}

			yield return new WaitForSeconds(1f);

			if(mIsAttackComplete) {
				//success clear the blobs
				var blobActives = boardControl.blobActives;
				for(int i = 0; i < blobActives.Count; i++) {
					var blob = blobActives[i];
					if(blob.state == Blob.State.Solved)
						blob.state = Blob.State.Correct;
					else
						blob.state = Blob.State.Despawning;
				}

				//determine attack
				var nAttack = attackScale;

				mAttackScales.Add(nAttack);

				//progress?
				if(nAttack >= gameDat.playAttackFullThreshold || mAttackScales.Count == numGen.rounds) {
					//save
					mNumberGenInd = gameDat.Progress(mNumberGenInd, mAttackScales.ToArray(), mFailCount, numGen.rounds);

					ResetStates(true);
				}
				else
					ResetStates(false);

				RefreshCurrentHitpoints();

				//do attack
				yield return boardControl.Attack();
								
				if(signalInvokeAttackFinish)
					signalInvokeAttackFinish.Invoke();

				yield return new WaitForSeconds(1f);
			}
			else if(mFailRoundCount >= gameDat.playFailRoundCount) {
				//clear all blobs
				boardControl.DespawnAllBlobs();

				yield return new WaitForSeconds(1f);

				ResetStates(true);
				RefreshCurrentHitpoints();
			}

			//wait for board to be cleared
			do {
				yield return null;
			} while(boardControl.blobActiveCount > 0);
		}

		if(signalInvokeEnd)
			signalInvokeEnd.Invoke();

		//board end
		yield return boardControl.PlayEnd();

		//modal victory
		M8.ModalManager.main.Open(gameDat.modalVictory);

		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy || M8.ModalManager.main.IsInStack(gameDat.modalVictory));

		gameDat.ProgressNext();
	}

	IEnumerator DoOperationResult(BlobActionResult result) {
		var camCtrl = CameraController.main;
		if(camCtrl)
			camCtrl.raycastTarget = false; //disable blob input

		//wait for modals to close
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);

		var gameDat = GameData.instance;
				
		var grp = result.group;

		Blob blobLeft = null, blobRight = null;
				
		if(result.blobDividend)
			blobLeft = result.blobDividend;
		else if(grp != null)
			blobLeft = grp.blobOpLeft;

		if(result.blobDivisor)
			blobRight = result.blobDivisor;
		else if(grp != null)
			blobRight = grp.blobOpRight;

		var connectOp = grp != null ? grp.connectOp : null;

		switch(result.type) {
			case BlobActionResult.Type.Success:
				Vector2 posLeft = Vector2.zero, posRight = Vector2.zero;

				int divisor = blobLeft ? blobLeft.divisor : blobRight ? blobRight.divisor : 0;

				//set blobs to success, move towards each other
				if(blobLeft) {
					blobLeft.state = Blob.State.Correct;

					posLeft = blobLeft.position;

					if(blobRight) {
						var dir = (blobRight.position - blobLeft.position).normalized;
						blobLeft.AddForce(dir * gameDat.blobMergeImpuse, ForceMode2D.Impulse);
					}

					boardControl.RemoveFromActive(blobLeft);
				}

				if(blobRight) {
					blobRight.state = Blob.State.Correct;

					posRight = blobRight.position;

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
				var opInd = -1;

				var opCount = currentNumberGen.opCount;

				for(int i = 0; i < opCount; i++) {
					var op = currentNumberGen.GetOperation(i);

					if(op.equal == result.newValue) {						
						currentNumberGen.SetOperationSolved(i, true);
						isOpSolved = true;
						opInd = i;
						break;
					}
				}

				//let display know an operation was solved
				if(isOpSolved) {
					if(signalInvokeOpSuccess)
						signalInvokeOpSuccess.Invoke(opInd);
				}

				var mergeVal = result.newValue;

				//spawn merged version (quotient) between the two blobs
				var mergeData = BlobData.GetMergeData(blobLeft, blobRight, mergeVal);

				if(mergeData) {
					boardControl.Spawn(new BlobSpawnInfo(mergeData, isOpSolved ? Blob.State.Solved : Blob.State.Normal, Vector2.Lerp(posLeft, posRight, 0.5f), mergeVal, divisor));

					//wait for spawns to complete
					do {
						yield return null;
					} while(boardControl.isBlobSpawning);
				}

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

				yield return null;

				Fail();
				break;
		}

		if(result.group != null)
			connectControl.ClearGroup(result.group);

		if(camCtrl)
			camCtrl.raycastTarget = true;
	}

	IEnumerator DoSplitResult(BlobActionResult.Type resultType, Blob blobDividend, Blob blobDivisor, int leftValue, int rightValue) {
		var camCtrl = CameraController.main;
		if(camCtrl)
			camCtrl.raycastTarget = false; //disable blob input

		//wait for modals to close
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);
				
		switch(resultType) {
			case BlobActionResult.Type.Success:
				var blobPos = blobDividend.position;
								
				//release old blob
				blobDividend.state = Blob.State.Despawning;

				boardControl.RemoveFromActive(blobDividend);

				while(blobDividend.state != Blob.State.None)
					yield return null;

				//play split sfx

				//spawn two new blobs
				var blobSplitDataLeft = leftValue > 0 ? blobDividend.data.GetReduceData(leftValue) : null;
				var blobSplitDataRight = rightValue > 0 ? blobDividend.data.GetSplitData(rightValue) : null;

				var dir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));

				var splitCount = blobDividend.splitCount > 1 ? blobDividend.splitCount - 1 : 0;

				if(blobSplitDataLeft)
					boardControl.Spawn(new BlobSpawnInfo(blobSplitDataLeft, blobPos + dir * blobSplitDataLeft.spawnPointCheckRadius, leftValue, blobDividend.divisor, splitCount));

				if(blobSplitDataRight)
					boardControl.Spawn(new BlobSpawnInfo(blobSplitDataRight, blobPos - dir * blobSplitDataRight.spawnPointCheckRadius, rightValue, blobDividend.divisor, splitCount));

				//spawn a duplicate divisor
				if(blobDivisor) {
					dir = M8.MathUtil.RotateAngle(Vector2.up, Random.Range(0f, 360f));

					boardControl.Spawn(new BlobSpawnInfo(blobDivisor.data, blobDivisor.position + (dir * blobDivisor.radius * 2f), blobDivisor.number, blobDivisor.number));
				}

				ReduceAttackValue(GameData.instance.playAttackSplitReduce);

				//wait for spawns to finish?
				break;

			case BlobActionResult.Type.Fail:
				blobDividend.state = Blob.State.Error;

				yield return null;

				Fail();
				break;
		}

		if(camCtrl)
			camCtrl.raycastTarget = true;
	}

	void OnSignalBlobClick(Blob blob) {
		//Debug.Log("clicked: "+blob.name);

		if(!blob.canSplit)
			return;

		var blobDivisor = boardControl.GetBlobDivisor(blob.number);
		if(!blobDivisor)
			return;

		//open split modal
		switch(blob.data.splitMode) {
			case BlobData.SplitMode.Tenths:
				mModalBlobSplitParms[ModalNumberSplitterTenths.parmBlobDividend] = blob;
				mModalBlobSplitParms[ModalNumberSplitterTenths.parmBlobDivisor] = blobDivisor;

				M8.ModalManager.main.Open(GameData.instance.modalBlobSplitTenths, mModalBlobSplitParms);

				if(signalInvokeSplitProceed) signalInvokeSplitProceed.Invoke();
				break;

			case BlobData.SplitMode.PartialQuotient:
				mModalBlobSplitParms[ModalNumberSplitterPartialQuotient.parmBlobDividend] = blob;
				mModalBlobSplitParms[ModalNumberSplitterPartialQuotient.parmBlobDivisor] = blobDivisor;

				M8.ModalManager.main.Open(GameData.instance.modalBlobSplitPartialQuotient, mModalBlobSplitParms);

				if(signalInvokeSplitProceed) signalInvokeSplitProceed.Invoke();
				break;
		}
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
		if(result.type == BlobActionResult.Type.Cancel)
			return;

		if(!result.blobDividend)
			return;

		switch(result.blobDividend.data.splitMode) {
			case BlobData.SplitMode.Tenths:
				StartCoroutine(DoSplitResult(result.type, result.blobDividend, result.blobDivisor, result.newValue, result.splitValue));
				break;

			case BlobData.SplitMode.PartialQuotient:
				StartCoroutine(DoSplitResult(result.type, result.blobDividend, null, result.newValue, result.splitValue));
				break;
		}
	}

	void OnGroupAdded(BlobConnectController.Group grp) {
		if(grp.isOpFilled) {
			//ensure divide is in correct order
			if(!grp.isOpLeftGreaterThanRight)
				grp.SwapOps();

			//open contextual modal operator for blob merging
			mModalOpParms[ModalOperationSolver.parmBlobConnectGroup] = grp;

			M8.ModalManager.main.Open(GameData.instance.modalOpSolver, mModalOpParms);

			if(signalInvokeOpProceed) signalInvokeOpProceed.Invoke();
		}
	}

	private void ReduceAttackValue(int amt) {
		var newVal = mAttackValue - amt;
		if(newVal < 1)
			newVal = 1;

		if(mAttackValue != newVal) {
			mAttackValue = newVal;

			//update hud
			if(signalInvokeAttackValueRefresh)
				signalInvokeAttackValueRefresh.Invoke();
		}
	}

	private void ResetStates(bool clearAttackScales) {
		mAttackValue = GameData.instance.playAttackCapacity;
				
		mIsAttackComplete = false;

		if(clearAttackScales)
			mAttackScales.Clear();

		mFailRoundCount = 0;

		roundPause = false;

		Blob.dragDisabled = false;
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

	private void Fail() {
		//play fail sfx

		boardControl.PlayerFail();

		ReduceAttackValue(GameData.instance.playAttackErrorReduce);

		mFailRoundCount++;
		mFailCount++;

		if(signalInvokeFail) signalInvokeFail.Invoke();
	}
}