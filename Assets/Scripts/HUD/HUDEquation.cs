using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class HUDEquation : MonoBehaviour {
	[System.Serializable]
	public class MainOperationData {
		public GameObject rootGO;

		[Header("Equation")]
		public M8.Animator.Animate equationOperandLeftAnim; //play take index 0 when highlight
		public TMP_Text equationOperandLeftText;

		public M8.Animator.Animate equationOperandRightAnim; //play take index 0 when highlight
		public TMP_Text equationOperandRightText;

		public GameObject equationOpGO;
		public TMP_Text equationOpText;

		public M8.Animator.Animate equationAnsAnim; //play take index 0 when highlight
		public TMP_Text equationAnsText;
				
		public GameObject equationEqGO;

		[Header("Animation")]
		public M8.Animator.Animate animator;
		public string takeEnter;
		public string takeExit;
		public string takeCorrect;

		public bool active {
			get { return rootGO ? rootGO.activeSelf : false; }
			set { if(rootGO) rootGO.SetActive(value); }
		}

		public bool isPlaying { get { return animator ? animator.isPlaying : false; } }

		public void Clear() {
			if(equationOperandLeftText) equationOperandLeftText.text = "";
			if(equationOperandRightText) equationOperandRightText.text = "";
			if(equationAnsText) equationAnsText.text = "";

			if(equationOperandLeftAnim) equationOperandLeftAnim.Stop();
			if(equationOperandRightAnim) equationOperandLeftAnim.Stop();
			if(equationAnsAnim) equationOperandLeftAnim.Stop();

			if(equationOpGO) equationOpGO.SetActive(false);
			if(equationEqGO) equationEqGO.SetActive(false);
		}

		public void Apply(Operation op, bool isSolved) {
			if(equationOperandLeftText) equationOperandLeftText.text = op.operand1.ToString();
			if(equationOperandRightText) equationOperandRightText.text = op.operand2.ToString();

			if(equationOpGO) equationOpGO.SetActive(true);
			if(equationOpText) equationOpText.text = op.opText;

			if(equationEqGO) equationEqGO.SetActive(true);

			if(isSolved) {
				if(equationAnsText) equationAnsText.text = op.equal.ToString();

				if(equationOperandLeftAnim) equationOperandLeftAnim.Stop();
				if(equationOperandRightAnim) equationOperandRightAnim.Stop();
				if(equationAnsAnim) equationAnsAnim.Stop();
			}
			else {
				if(equationAnsText) equationAnsText.text = "";

				if(equationOperandLeftAnim) equationOperandLeftAnim.Stop();
				if(equationOperandRightAnim) equationOperandRightAnim.Stop();
				if(equationAnsAnim) equationAnsAnim.Play(0);
			}
		}

		public void Show() {
			if(!string.IsNullOrEmpty(takeEnter))
				animator.Play(takeEnter);
		}

		public void Hide() {
			if(!string.IsNullOrEmpty(takeExit))
				animator.Play(takeExit);
		}

		public void Correct() {
			if(!string.IsNullOrEmpty(takeCorrect))
				animator.Play(takeCorrect);
		}
	}

	[System.Serializable]
	public class OperationResultData {
		public GameObject rootGO;
		public TMP_Text text;
		
		public M8.Animator.Animate animator;
		public string takeEnter;
		public string takeExit;

		public bool active { get { return rootGO ? rootGO.activeSelf : false; } set { if(rootGO) rootGO.SetActive(value); } }
		public bool isCorrect { get; set; }

		private System.Text.StringBuilder mStr;
		private WaitForSeconds mShowWait;

		public void Init(float showDelay) {
			mStr = new System.Text.StringBuilder(20);
			mShowWait = new WaitForSeconds(showDelay);
		}

		public IEnumerator Play(int operand1, OperatorType op, int operand2, int eq) {
			active = true;

			mStr.Clear();

			mStr.Append(operand1);
			mStr.Append(' ');
			mStr.Append(Operation.GetOperatorTypeChar(op));
			mStr.Append(' ');
			mStr.Append(operand2);
			mStr.Append(' ');

			if(isCorrect)
				mStr.Append('=');
			else
				mStr.Append("\u2260");

			mStr.Append(' ');
			mStr.Append(eq);

			if(text) text.text = mStr.ToString();

			if(!string.IsNullOrEmpty(takeEnter))
				yield return animator.PlayWait(takeEnter);

			yield return mShowWait;

			if(!string.IsNullOrEmpty(takeExit))
				yield return animator.PlayWait(takeExit);

			active = false;
		}
	}

	[Header("Main Operations")]
	public MainOperationData[] mainOps;

	[Header("Result")]
	public OperationResultData correctResult;
	public OperationResultData errorResult;
	public float resultShowDelay = 1.5f;

	[Header("Signal Listen")]	
	public M8.Signal signalListenPlayStart;
	public M8.Signal signalListenPlayNewRound;
	public M8.SignalInteger signalListenPlayOpSuccess;
	public M8.Signal signalListenPlayEnd;

	public SignalBlobActionResult signalListenOperation;

	private Coroutine mMainOpRout;
	private Coroutine mOpResultRout;

	void OnDisable() {
		if(mMainOpRout != null) {
			StopCoroutine(mMainOpRout);
			mMainOpRout = null;
		}

		if(mOpResultRout != null) {
			StopCoroutine(mOpResultRout);
			mOpResultRout = null;
		}
	}

	void OnDestroy() {
		if(signalListenPlayStart) signalListenPlayStart.callback -= OnPlayBegin;
		if(signalListenPlayNewRound) signalListenPlayNewRound.callback -= OnPlayNewRound;
		if(signalListenPlayOpSuccess) signalListenPlayOpSuccess.callback -= OnPlayMainOpSuccess;
		if(signalListenPlayEnd) signalListenPlayEnd.callback -= OnPlayEnd;

		if(signalListenOperation) signalListenOperation.callback -= OnPlayOpResult;
	}

	void Awake() {
		for(int i = 0; i < mainOps.Length; i++) {
			mainOps[i].active = false;
			mainOps[i].Clear();
		}

		correctResult.Init(resultShowDelay);
		correctResult.isCorrect = true;
		correctResult.active = false;

		errorResult.Init(resultShowDelay);
		errorResult.isCorrect = false;
		errorResult.active = false;

		if(signalListenPlayStart) signalListenPlayStart.callback += OnPlayBegin;
		if(signalListenPlayNewRound) signalListenPlayNewRound.callback += OnPlayNewRound;
		if(signalListenPlayOpSuccess) signalListenPlayOpSuccess.callback += OnPlayMainOpSuccess;
		if(signalListenPlayEnd) signalListenPlayEnd.callback += OnPlayEnd;

		if(signalListenOperation) signalListenOperation.callback += OnPlayOpResult;
	}

	void OnPlayBegin() {

	}

	void OnPlayNewRound() {
		ClearResults();

		if(mMainOpRout != null)
			StopCoroutine(mMainOpRout);

		mMainOpRout = StartCoroutine(DoNewRound());
	}

	void OnPlayMainOpSuccess(int index) {
		if(index >= 0 && index < mainOps.Length) {
			var mainOp = mainOps[index];

			var numGen = PlayController.instance.currentNumberGen;

			mainOp.Apply(numGen.GetOperation(index), true);

			mainOp.Correct();
		}
	}

	void OnPlayOpResult(BlobActionResult result) {
		//ignore if cancel
		if(result.type == BlobActionResult.Type.None || result.type == BlobActionResult.Type.Cancel)
			return;

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
				
		var operand1 = blobLeft ? blobLeft.number : 0;
		var operand2 = blobRight ? blobRight.number : 0;
		var op = connectOp ? connectOp.op : OperatorType.None;
		var eq = result.newValue;
		var isCorrect = result.type == BlobActionResult.Type.Success;

		if(mOpResultRout != null)
			StopCoroutine(mOpResultRout);

		mOpResultRout = StartCoroutine(DoOpResult(operand1, op, operand2, eq, isCorrect));
	}

	void OnPlayEnd() {
		ClearResults();

		if(mMainOpRout != null)
			StopCoroutine(mMainOpRout);

		mMainOpRout = StartCoroutine(DoHide());
	}

	IEnumerator DoOpResult(int operand1, OperatorType op, int operand2, int eq, bool isCorrect) {
		//wait for modals to close
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);

		OperationResultData resultDat;

		if(isCorrect) {
			resultDat = correctResult;
			errorResult.active = false;
		}
		else {
			resultDat = errorResult;
			correctResult.active = false;
		}

		yield return resultDat.Play(operand1, op, operand2, eq);

		mOpResultRout = null;
	}

	IEnumerator DoNewRound() {
		yield return null;

		//wait for blobs to spawn
		var playCtrl = PlayController.instance;
		while(playCtrl.boardControl.blobActiveCount < 2 || playCtrl.boardControl.isBlobSpawning)
			yield return null;

		//apply and activate main ops
		//TODO: reposition properly based on number of actives
		var numGen = playCtrl.currentNumberGen;

		var opActiveCount = Mathf.Min(mainOps.Length, numGen.opCount);

		for(int i = 0; i < opActiveCount; i++) {
			var mainOp = mainOps[i];

			mainOp.active = true;
			mainOp.Apply(numGen.GetOperation(i), false);

			mainOp.Show();
		}

		//disable leftovers
		for(int i = opActiveCount; i < mainOps.Length; i++)
			mainOps[i].active = false;

		var doneCount = 0;

		while(doneCount < mainOps.Length) {
			yield return null;

			doneCount = 0;
			for(int i = 0; i < mainOps.Length; i++) {
				if(!mainOps[i].isPlaying)
					doneCount++;
			}
		}

		mMainOpRout = null;
	}

	IEnumerator DoHide() {
		for(int i = 0; i < mainOps.Length; i++) {
			if(mainOps[i].active)
				mainOps[i].Hide();
		}

		var doneCount = 0;

		while(doneCount < mainOps.Length) {
			yield return null;

			doneCount = 0;
			for(int i = 0; i < mainOps.Length; i++) {
				if(!mainOps[i].isPlaying)
					doneCount++;
			}
		}

		for(int i = 0; i < mainOps.Length; i++)
			mainOps[i].active = false;

		mMainOpRout = null;
	}

	private void ClearResults() {
		correctResult.active = false;
		errorResult.active = false;

		if(mOpResultRout != null) {
			StopCoroutine(mOpResultRout);
			mOpResultRout = null;
		}
	}
}
