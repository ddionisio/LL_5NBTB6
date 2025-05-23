using LoLExt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level3Tutorial : MonoBehaviour {
	public enum State {
		None,

		SplitInstruct,
		Split,
		SplitOpInstruct,
		SplitResult,
		SplitSuccess,

		Finish
	}

	public M8.TransAttachTo splitPointerGuide;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogInstructSplit;
	public ModalDialogFlowIncremental dialogInstructSplitOp;
	public ModalDialogFlowIncremental dialogInstructSplitOpNext;
	public ModalDialogFlowIncremental dialogInstructSplitSuccess;

	[Header("Signal Listen")]
	public M8.Signal signalListenRoundStart;
	public M8.Signal signalListenSplitProceed;
	public SignalBlobActionResult signalListenSplit;

	private State mWaitState = State.None;

	void OnDestroy() {
		if(signalListenRoundStart) signalListenRoundStart.callback -= OnSignalRoundStart;
		if(signalListenSplitProceed) signalListenSplitProceed.callback -= OnSignalSplitProceed;
		if(signalListenSplit) signalListenSplit.callback -= OnSignalSplitResult;
	}

	void Awake() {
		splitPointerGuide.gameObject.SetActive(false);

		if(signalListenRoundStart) signalListenRoundStart.callback += OnSignalRoundStart;
		if(signalListenSplitProceed) signalListenSplitProceed.callback += OnSignalSplitProceed;
		if(signalListenSplit) signalListenSplit.callback += OnSignalSplitResult;
	}

	void OnSignalRoundStart() {
		if(mWaitState == State.Finish) return;

		var playCtrl = PlayController.instance;

		if(playCtrl.stateIndex == 0) {
			if(playCtrl.roundsIndex == 0 && mWaitState == State.None) {
				mWaitState = State.SplitInstruct;
				StartCoroutine(DoSplitInstruct());
			}
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

	IEnumerator DoSplitInstruct() {
		Blob.dragDisabled = true;

		var boardCtrl = PlayController.instance.boardControl;

		//wait for blobs to spawn
		do {
			yield return null;
		} while(boardCtrl.isBlobSpawning);

		yield return dialogIntro.Play();

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

		//talk about the process
		yield return dialogInstructSplitOp.Play();

		//wait for modals to open (just in case)
		do { yield return null; } while(M8.ModalManager.main.isBusy);

		//grab the splitter modal
		var modalSplitter = M8.ModalManager.main.GetBehaviour<ModalNumberSplitterPartialQuotient>(GameData.instance.modalBlobSplitPartialQuotient);
		if(modalSplitter) {
			//wait for user to select output (assume values were put in)
			while(modalSplitter.curSelectType != ModalNumberSplitterPartialQuotient.SelectType.Output)
				yield return null;

			yield return dialogInstructSplitOpNext.Play();
		}

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