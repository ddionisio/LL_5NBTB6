using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class ModalNumberSplitterPartialQuotient : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
	public const string parmBlobDividend = "blobDividend";
	public const string parmBlobDivisor = "blobDivisor";

	public const int quotientDigitLimit = 2;

	public enum SelectType {
		None,
		Quotient,
		Output
	}

	[Header("HP")]
	public MistakeCounterWidget mistakeCounter;

	[Header("Blobs")]
	public BlobWidgetTemplateMatch blobDividendTemplate;
	public BlobWidget blobDivisorWidget;
	public BlobWidget blobQuotientWidget;
	public M8.ColorPalette blobQuotientPalette;
	public BlobWidget blobOutputWidget;

	[Header("Connectors")]
	public M8.UI.Graphics.ColorGroup connectDivisorToOpColorGroup;
	public M8.UI.Graphics.ColorGroup connectQuotientToOpColorGroup;
	public M8.UI.Graphics.ColorGroup connectQuotientToEqColorGroup;
	public M8.UI.Graphics.ColorGroup connectOutputToEqColorGroup;

	[Header("Numbers")]
	public TMP_Text dividendNumberText;
	public TMP_Text divisorNumberText;
		
	public RectTransform outputNumberRoot;
	public TMP_Text outputNumberText;
	public GameObject outputNumberHighlightGO;

	public TMP_Text reduceNumberText;

	[Header("Quotient")]
	public RectTransform quotientNumberRoot;
	public GameObject quotientNumberHighlightGO;
	public TMP_Text[] quotientDigitTexts; //this determines the tens-limit
	public CanvasGroup quotientDigitButtonCanvasGroup;
	public CanvasGroup quotientTensButtonCanvasGroup;
	public Button quotientNextButton;
	public GameObject quotientTensButtonGO; //buttons
	public M8.Animator.Animate quotientAnimator;
	[M8.Animator.TakeSelector(animatorField= "quotientAnimator")]
	public int quotientTakeEnter = -1;
	[M8.Animator.TakeSelector(animatorField = "quotientAnimator")]
	public int quotientTakeExit = -1;

	[Header("Reduce Number Config")]
	public float reduceNumberMoveDelay = 0.3f;
	public DG.Tweening.Ease reduceMoveEase = DG.Tweening.Ease.InOutSine;
	public float reduceNumberMoveHeight;
	public float reduceNumberCountDelay = 0.3f;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxNumberJump;
	[M8.SoundPlaylist]
	public string sfxCorrect;
	[M8.SoundPlaylist]
	public string sfxError;

	[Header("Signal Invoke")]
	public M8.SignalBoolean signalInvokeInputActive;
	public M8.Signal signalInvokeError;
	public SignalBlobActionResult signalInvokeResult;

	[Header("Signal Listen")]
	public M8.SignalFloat signalListenProceed;
	public M8.SignalFloat signalListenNumberChanged;
	public M8.Signal signalListenPrev;
	public M8.Signal signalListenNext;

	public SelectType curSelectType { get { return mCurSelect; } }
	public int mistakeCount { get { return mMistakeCount; } }

	public int quotientDigitNumber { get { return mQuotientDigitNumber; } }
	public int quotientDigitCount { get { return mQuotientDigitCount; } }
	public int quotientDigitMax { get { return mQuotientDigitMax; } }

	private M8.GenericParams mNumpadParms = new M8.GenericParams();

	private int mMistakeCount;

	private DG.Tweening.EaseFunction mReduceMoveEaseFunc;

	private BlobWidget mBlobDividendWidget;

	private Blob mBlobDividend;
	private Blob mBlobDivisor;

	private SelectType mCurSelect;

	private int mDividendNumber;
	private int mDivisorNumber;	
	private int mOutputNumber;

	private int mQuotientNumber;
	private int mQuotientDigitNumber; //left-most digit value
	private int mQuotientDigitCount;
	private int mQuotientDigitMax;

	private WaitForSeconds mWaitInterval = new WaitForSeconds(0.2f);
	private WaitForSeconds mWaitOutput = new WaitForSeconds(1f);

	private System.Text.StringBuilder mOpText = new System.Text.StringBuilder();

	public void QuotientIncreaseDigit() {
		if(mQuotientDigitCount < mQuotientDigitMax) {
			mQuotientDigitCount++;

			QuotientRefreshNumber();

			QuotientRefreshDigitCountDisplay();
		}
	}

	public void QuotientDecreaseDigit() {
		if(mQuotientDigitCount > 1) {
			mQuotientDigitCount--;

			QuotientRefreshNumber();

			QuotientRefreshDigitCountDisplay();
		}
	}

	public void QuotientIncreaseNumber() {
		mQuotientDigitNumber++;
		if(mQuotientDigitNumber == 10)
			mQuotientDigitNumber = 1;

		QuotientRefreshNumber();

		QuotientRefreshDigitNumberDisplay();
	}

	public void QuotientDecreaseNumber() {
		mQuotientDigitNumber--;
		if(mQuotientDigitNumber == 0)
			mQuotientDigitNumber = 9;

		QuotientRefreshNumber();

		QuotientRefreshDigitNumberDisplay();
	}

	void M8.IModalPop.Pop() {
		mBlobDividend = null;
		mBlobDivisor = null;

		if(signalListenProceed) signalListenProceed.callback -= OnSignalProceed;
		if(signalListenNumberChanged) signalListenNumberChanged.callback -= OnSignalNumberChanged;
		if(signalListenPrev) signalListenPrev.callback -= OnSignalNext;
		if(signalListenNext) signalListenNext.callback -= OnSignalNext;
	}

	void M8.IModalActive.SetActive(bool aActive) {
		if(aActive) {
			mCurSelect = SelectType.Quotient;

			if(quotientTakeEnter != -1)
				quotientAnimator.Play(quotientTakeEnter);
		}
		else if(mCurSelect == SelectType.Quotient) {
			if(quotientTakeExit != -1)
				quotientAnimator.Play(quotientTakeExit);
		}
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		mBlobDividend = null;
		mBlobDivisor = null;

		mDividendNumber = 0;
		mDivisorNumber = 0;
		mOutputNumber = 0;

		var dividendClr = Color.white;

		if(parms != null) {
			if(parms.ContainsKey(parmBlobDividend))
				mBlobDividend = parms.GetValue<Blob>(parmBlobDividend);

			if(parms.ContainsKey(parmBlobDivisor))
				mBlobDivisor = parms.GetValue<Blob>(parmBlobDivisor);
		}

		//setup blobs
		if(mBlobDividend) {
			mDividendNumber = mBlobDividend.number;

			dividendClr = mBlobDividend.color;

			blobDividendTemplate.ApplyMatchingWidget(mBlobDividend.data);
			mBlobDividendWidget = blobDividendTemplate.blobWidget;

			if(mBlobDividendWidget) {
				mBlobDividendWidget.color = dividendClr;

				if(dividendNumberText)
					mBlobDividendWidget.SetToAnchor(dividendNumberText.rectTransform, true);
			}
		}

		if(mBlobDivisor) {
			mDivisorNumber = mBlobDivisor.number;

			if(blobDivisorWidget) {
				blobDivisorWidget.color = mBlobDivisor.color;

				if(divisorNumberText) {
					divisorNumberText.text = mDivisorNumber.ToString();

					blobDivisorWidget.SetToAnchor(divisorNumberText.rectTransform, true);
				}

				if(connectDivisorToOpColorGroup)
					connectDivisorToOpColorGroup.ApplyColor(mBlobDivisor.color);
			}
		}

		if(blobQuotientWidget) {
			var clr = blobQuotientPalette ? blobQuotientPalette.GetRandomColor() : Color.yellow;

			blobQuotientWidget.color = clr;

			if(connectQuotientToOpColorGroup) connectQuotientToOpColorGroup.ApplyColor(clr);
			if(connectQuotientToEqColorGroup) connectQuotientToEqColorGroup.ApplyColor(clr);

			if(quotientNumberRoot)
				blobQuotientWidget.SetToAnchor(quotientNumberRoot, false);
		}

		if(blobOutputWidget) {
			blobOutputWidget.color = dividendClr;

			if(connectOutputToEqColorGroup) connectOutputToEqColorGroup.ApplyColor(dividendClr);

			if(outputNumberRoot)
				blobOutputWidget.SetToAnchor(outputNumberRoot, false);
		}

		mCurSelect = SelectType.Quotient;

		//initial display
		if(reduceNumberText) reduceNumberText.gameObject.SetActive(false);

		if(dividendNumberText) dividendNumberText.text = mDividendNumber.ToString();

		RefreshSelectDisplay();
		RefreshOutputNumberDisplay();

		QuotientInit();

		//mistake
		mMistakeCount = 0;
		if(mistakeCounter) mistakeCounter.Init(0, GameData.instance.playErrorCount);

		//setup signals
		if(signalListenProceed) signalListenProceed.callback += OnSignalProceed;
		if(signalListenNumberChanged) signalListenNumberChanged.callback += OnSignalNumberChanged;
		if(signalListenPrev) signalListenPrev.callback += OnSignalNext;
		if(signalListenNext) signalListenNext.callback += OnSignalNext;
	}

	void OnSignalProceed(float val) {
		var iVal = Mathf.RoundToInt(val);

		switch(mCurSelect) {
			case SelectType.Output:
				if(mOutputNumber != iVal) {
					mOutputNumber = Mathf.RoundToInt(val);
					RefreshQuotientNumberDisplay();
				}

				StartCoroutine(DoEvaluate());
				break;
		}
	}

	void OnSignalNumberChanged(float val) {
		var iVal = Mathf.RoundToInt(val);

		switch(mCurSelect) {
			case SelectType.Output:
				if(mOutputNumber != iVal) {
					mOutputNumber = iVal;
					RefreshOutputNumberDisplay();
				}
				break;
		}
	}

	void OnSignalNext() {
		switch(mCurSelect) {
			case SelectType.Quotient:
				Select(SelectType.Output);
				break;

			case SelectType.Output:
				Select(SelectType.Quotient);
				break;
		}
	}

	void Awake() {
		mReduceMoveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(reduceMoveEase);
	}

	private void QuotientRefreshNumber() {
		mQuotientNumber = mQuotientDigitNumber;

		for(int i = 1; i < mQuotientDigitCount; i++)
			mQuotientNumber *= 10;
	}

	private void QuotientRefreshDigitNumberDisplay() {
		quotientDigitTexts[0].text = mQuotientDigitNumber.ToString();
	}

	private void QuotientRefreshDigitCountDisplay() {
		for(int i = 0; i < mQuotientDigitCount; i++)
			quotientDigitTexts[i].gameObject.SetActive(true);

		for(int i = mQuotientDigitCount; i < quotientDigitTexts.Length; i++)
			quotientDigitTexts[i].gameObject.SetActive(false);
	}

	private void QuotientInit() {
		mQuotientNumber = 1;
		mQuotientDigitNumber = 1;
		mQuotientDigitCount = 1;
				
		quotientDigitTexts[0].gameObject.SetActive(true);
		quotientDigitTexts[0].text = "1";

		var digitCheck = mDivisorNumber;

		mQuotientDigitMax = 1;

		for(int i = 1; i < quotientDigitTexts.Length; i++) {
			if(digitCheck * 10 <= mDividendNumber) {
				mQuotientDigitMax++;
				digitCheck *= 10;
			}

			quotientDigitTexts[i].text = "0";
			quotientDigitTexts[i].gameObject.SetActive(false);
		}

		quotientTensButtonGO.SetActive(mQuotientDigitMax > 1);

		if(quotientTakeEnter != -1)
			quotientAnimator.ResetTake(quotientTakeEnter);
	}

	private void Select(SelectType toSelect) {
		if(mCurSelect != toSelect) {
			mCurSelect = toSelect;

			var modalNumpad = M8.ModalManager.main.GetBehaviour<ModalCalculator>(GameData.instance.modalNumpad);
			if(!modalNumpad)
				return;
						
			switch(mCurSelect) {
				case SelectType.Quotient:
					M8.ModalManager.main.CloseUpTo(GameData.instance.modalNumpad, true);

					if(quotientTakeEnter != -1)
						quotientAnimator.Play(quotientTakeEnter);

					if(blobQuotientWidget) blobQuotientWidget.Pulse();
					break;

				case SelectType.Output:
					if(quotientTakeExit != -1)
						quotientAnimator.Play(quotientTakeExit);

					mOpText.Clear();
					mOpText.Append(mDivisorNumber);
					mOpText.Append(" x ");
					mOpText.Append(mQuotientNumber);
					mOpText.Append(" =");

					mNumpadParms[ModalCalculator.parmInitValue] = mOutputNumber;
					mNumpadParms[ModalCalculatorParmExt.operationText] = mOpText.ToString();
					mNumpadParms[ModalCalculatorParmExt.showPrevNext] = true;

					M8.ModalManager.main.Open(GameData.instance.modalNumpad, mNumpadParms);

					if(blobOutputWidget) blobOutputWidget.Pulse();
					break;
			}

			RefreshSelectDisplay();
		}
	}

	IEnumerator DoEvaluate() {
		if(signalInvokeInputActive) signalInvokeInputActive.Invoke(false);

		Select(SelectType.None);

		var actionResult = new BlobActionResult { type = BlobActionResult.Type.None, blobDividend = mBlobDividend };

		//check to see if operation is valid
		var isOpSuccess = mDivisorNumber * mQuotientNumber == mOutputNumber;

		if(isOpSuccess) {
			//sfx
			if(!string.IsNullOrEmpty(sfxCorrect))
				M8.SoundPlaylist.instance.Play(sfxCorrect, false);

			if(blobDivisorWidget) blobDivisorWidget.Correct();

			yield return mWaitInterval;

			if(blobQuotientWidget) blobQuotientWidget.Correct();

			yield return mWaitInterval;

			if(blobOutputWidget) blobOutputWidget.Correct();
		}
		else {
			//sfx
			if(!string.IsNullOrEmpty(sfxError))
				M8.SoundPlaylist.instance.Play(sfxError, false);

			if(blobDivisorWidget) blobDivisorWidget.Error();

			yield return mWaitInterval;

			if(blobQuotientWidget) blobQuotientWidget.Error();

			yield return mWaitInterval;

			if(blobOutputWidget) blobOutputWidget.Error();
		}

		//wait for blobs
		while((blobDivisorWidget && blobDivisorWidget.isBusy) || (blobQuotientWidget && blobQuotientWidget.isBusy) || (blobOutputWidget && blobOutputWidget.isBusy))
			yield return null;

		if(isOpSuccess) {
			if(mOutputNumber > 0) {
				var newDividendNumber = mDividendNumber - mOutputNumber;

				//move output number to dividend
				if(reduceNumberText) {
					if(!string.IsNullOrEmpty(sfxNumberJump))
						M8.SoundPlaylist.instance.Play(sfxNumberJump, false);

					reduceNumberText.text = (-mOutputNumber).ToString();
					reduceNumberText.gameObject.SetActive(true);

					var startPos = blobOutputWidget ? blobOutputWidget.position : Vector2.zero; //fail-safe
					var endPos = mBlobDividendWidget ? mBlobDividendWidget.position : Vector2.zero; //fail-safe
					var midPos = new Vector2(Mathf.Lerp(startPos.x, endPos.x, 0.5f), startPos.y + reduceNumberMoveHeight);

					reduceNumberText.transform.position = startPos;

					var curTime = 0f;
					while(curTime < reduceNumberMoveDelay) {
						yield return null;

						curTime += Time.deltaTime;

						var t = mReduceMoveEaseFunc(curTime, reduceNumberMoveDelay, 0f, 0f);

						reduceNumberText.transform.position = M8.MathUtil.Bezier(startPos, midPos, endPos, t);
					}

					reduceNumberText.gameObject.SetActive(false);
				}

				//reduce dividend number
				if(dividendNumberText) {
					float fStartVal = mDividendNumber, fEndVal = newDividendNumber;

					var curTime = 0f;
					while(curTime < reduceNumberCountDelay) {
						yield return null;

						curTime += Time.deltaTime;

						var t = Mathf.Clamp01(curTime / reduceNumberCountDelay);

						dividendNumberText.text = Mathf.RoundToInt(Mathf.Lerp(fStartVal, fEndVal, t)).ToString();
					}
				}

				yield return mWaitOutput;

				if(newDividendNumber >= 0) { //success
					if(!string.IsNullOrEmpty(sfxCorrect))
						M8.SoundPlaylist.instance.Play(sfxCorrect, false);

					if(mBlobDividendWidget) mBlobDividendWidget.Correct();

					//wait for blob
					while((mBlobDividendWidget && mBlobDividendWidget.isBusy))
						yield return null;

					actionResult.type = BlobActionResult.Type.Success;					
					actionResult.newValue = newDividendNumber;
					actionResult.splitValue = mQuotientNumber;
				}
				else { //output is greater than dividend
					if(!string.IsNullOrEmpty(sfxError))
						M8.SoundPlaylist.instance.Play(sfxError, false);

					if(mBlobDividendWidget) mBlobDividendWidget.Error();

					//wait for blob
					while((mBlobDividendWidget && mBlobDividendWidget.isBusy))
						yield return null;

					mMistakeCount++;
					if(mistakeCounter) {
						mistakeCounter.UpdateMistakeCount(mMistakeCount, GameData.instance.playErrorCount);
						while(mistakeCounter.isBusy)
							yield return null;
					}

					//ran out of tries?
					if(mMistakeCount == GameData.instance.playErrorCount)
						actionResult.type = BlobActionResult.Type.Fail;
					else {
						//revert dividend number
						if(dividendNumberText) dividendNumberText.text = mDividendNumber.ToString();
					}
				}
			}
			else //no result
				actionResult.type = BlobActionResult.Type.Cancel;
		}
		else { //operation failed
			mMistakeCount++;
			if(mistakeCounter) {
				mistakeCounter.UpdateMistakeCount(mMistakeCount, GameData.instance.playErrorCount);
				while(mistakeCounter.isBusy)
					yield return null;
			}

			//ran out of tries?
			if(mMistakeCount == GameData.instance.playErrorCount)
				actionResult.type = BlobActionResult.Type.Fail;
		}

		//finish
		if(signalInvokeInputActive) signalInvokeInputActive.Invoke(true);

		if(actionResult.type != BlobActionResult.Type.None) {
			if(signalInvokeResult)
				signalInvokeResult.Invoke(actionResult);

			Close();
		}
		else {
			mOutputNumber = 0;
			RefreshOutputNumberDisplay();

			Select(isOpSuccess ? SelectType.Quotient : SelectType.Output);
		}
	}

	private void RefreshSelectDisplay() {
		if(quotientNumberHighlightGO) quotientNumberHighlightGO.SetActive(mCurSelect == SelectType.Quotient);
		if(outputNumberHighlightGO) outputNumberHighlightGO.SetActive(mCurSelect == SelectType.Output);
	}

	private void RefreshQuotientNumberDisplay() {
		//if(quotientNumberText) quotientNumberText.text = mQuotientNumber.ToString();
	}

	private void RefreshOutputNumberDisplay() {
		if(outputNumberText) outputNumberText.text = mOutputNumber.ToString();
	}
}
