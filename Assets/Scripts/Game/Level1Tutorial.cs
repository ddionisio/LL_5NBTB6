using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class Level1Tutorial : MonoBehaviour {
	public enum State {
		None,

		DragInstruct,		
		Operation,
		OperationInstruct,
		SplitInstruct,
		Split,
		SplitOpInstruct,
		SplitResult,
		SplitSuccess,

		Finish
	}

    public DragToGuide dragGuide;
	public M8.TransAttachTo splitPointerGuide;
	public GameObject bossHPPointerGuide;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogInstructBlobDrag;
	public ModalDialogFlowIncremental dialogInstructOp;
	public ModalDialogFlowIncremental dialogInstructAttackSuccess;
	public ModalDialogFlowIncremental dialogInstructBossHealth;
	public ModalDialogFlowIncremental dialogInstructSplit;
	public ModalDialogFlowIncremental dialogInstructSplitOp;
	public ModalDialogFlowIncremental dialogInstructSplitSuccess;

	[Header("Signal Listen")]
    public M8.Signal signalListenRoundStart;
	public M8.Signal signalListenOpProceed;
	public M8.Signal signalListenSplitProceed;
	public SignalBlobActionResult signalListenSplit;

	private State mWaitState = State.None;

	void OnDestroy() {
		if(signalListenRoundStart) signalListenRoundStart.callback -= OnSignalRoundStart;
		if(signalListenOpProceed) signalListenOpProceed.callback -= OnSignalOpProceed;
		if(signalListenSplitProceed) signalListenSplitProceed.callback -= OnSignalSplitProceed;
		if(signalListenSplit) signalListenSplit.callback -= OnSignalSplitResult;
	}

	void Awake() {
		splitPointerGuide.gameObject.SetActive(false);
		bossHPPointerGuide.SetActive(false);

		if(signalListenRoundStart) signalListenRoundStart.callback += OnSignalRoundStart;
		if(signalListenOpProceed) signalListenOpProceed.callback += OnSignalOpProceed;
		if(signalListenSplitProceed) signalListenSplitProceed.callback += OnSignalSplitProceed;
		if(signalListenSplit) signalListenSplit.callback += OnSignalSplitResult;
	}

	void OnSignalRoundStart() {
		if(mWaitState == State.Finish) return;

		var playCtrl = PlayController.instance;

		if(playCtrl.stateIndex == 0) {
			//drag instruction
			if(playCtrl.roundsIndex == 0 && mWaitState == State.None) {
				mWaitState = State.DragInstruct;
				StartCoroutine(DoBlobDragInstruct());
			}
		}
		else if(playCtrl.stateIndex == 1) {
			if(playCtrl.roundsIndex == 0 && mWaitState == State.None) {
				mWaitState = State.SplitInstruct;
				StartCoroutine(DoSplitInstruct());
			}
		}
	}

	void OnSignalOpProceed() {
		if(mWaitState == State.Operation) {
			mWaitState = State.OperationInstruct;
			StartCoroutine(DoOpInstruct());
		}
	}

	void OnSignalSplitProceed() {
		if(mWaitState == State.Split) {
			mWaitState = State.SplitOpInstruct;
			StartCoroutine(DoSplitOperationInstruct());
		}
	}

	void OnSignalSplitResult(BlobActionResult result) {
		if(mWaitState == State.SplitResult) {
			if(result.type == BlobActionResult.Type.Success) {
				mWaitState = State.SplitSuccess;
				StartCoroutine(DoSplitSuccess());
			}
		}
	}

	IEnumerator DoBlobDragInstruct() {
		yield return dialogIntro.Play();

		var boardCtrl = PlayController.instance.boardControl;

		//wait for blobs to spawn
		do {
			yield return null;
		} while(boardCtrl.isBlobSpawning);

		//grab the two blobs
		Blob blobDividend = null, blobDivisor = null;

		var blobActives = boardCtrl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blob = blobActives[i];
			if(blob.data.type == BlobData.Type.Dividend)
				blobDividend = blob;
			else if(blob.data.type == BlobData.Type.Divisor)
				blobDivisor = blob;
		}

		//show drag between dividend and divisor blob
		if(blobDividend && blobDivisor)
			dragGuide.Follow(blobDividend.transform, blobDivisor.transform);

		//show instruction
		yield return dialogInstructBlobDrag.Play();

		mWaitState = State.Operation;
	}

	IEnumerator DoOpInstruct() {
		dragGuide.Hide();

		//wait for modals to open
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);

		//show instructions
		yield return dialogInstructOp.Play();

		//wait for next round to show the rest of tutorial
		mWaitState = State.None;
	}

	IEnumerator DoSplitInstruct() {
		Blob.dragDisabled = true;

		var playCtrl = PlayController.instance;

		playCtrl.roundPause = true;

		var boardCtrl = playCtrl.boardControl;

		//talk about success
		yield return dialogInstructAttackSuccess.Play();

		//talk about boss health
		bossHPPointerGuide.SetActive(true);

		yield return dialogInstructBossHealth.Play();

		bossHPPointerGuide.SetActive(false);

		playCtrl.roundPause = false;

		//wait for blobs to spawn
		do {
			yield return null;
		} while(boardCtrl.isBlobSpawning);

		//talk about blob splitting

		//grab dividend blob
		Blob blobDividend = null;

		var blobActives = boardCtrl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blob = blobActives[i];
			if(blob.data.type == BlobData.Type.Dividend) {
				blobDividend = blob;
				break;
			}
		}

		if(blobDividend) {
			splitPointerGuide.gameObject.SetActive(true);
			splitPointerGuide.target = blobDividend.transform;
		}

		yield return dialogInstructSplit.Play();
				
		mWaitState = State.Split;
	}

	IEnumerator DoSplitOperationInstruct() {
		Blob.dragDisabled = false;

		splitPointerGuide.gameObject.SetActive(false);
		splitPointerGuide.target = null;

		//talk about click on digits to split
		yield return dialogInstructSplitOp.Play();

		mWaitState = State.SplitResult;
	}

	IEnumerator DoSplitSuccess() {
		//wait for modals to be gone
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);

		yield return dialogInstructSplitSuccess.Play();

		mWaitState = State.Finish;
	}
}
