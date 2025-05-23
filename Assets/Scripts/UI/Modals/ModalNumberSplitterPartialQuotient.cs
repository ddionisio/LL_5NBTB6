using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalNumberSplitterPartialQuotient : M8.ModalController, M8.IModalPush, M8.IModalPop {
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

	public RectTransform quotientNumberRoot;
	public TMP_Text quotientNumberText;
	public GameObject quotientNumberHighlightGO;

	public RectTransform outputNumberRoot;
	public TMP_Text outputNumberText;
	public GameObject outputNumberHighlightGO;

	public TMP_Text reduceNumberText;

	[Header("Reduce Number Config")]
	public float reduceNumberMoveDelay = 0.3f;
	public DG.Tweening.Ease reduceMoveEase = DG.Tweening.Ease.InOutSine;
	public float reduceNumberMoveHeight;
	public float reduceNumberCountDelay = 0.3f;

	[Header("Signal Invoke")]
	public M8.SignalString signalInvokeSetOpText;
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

	private M8.GenericParams mNumpadParms = new M8.GenericParams();

	private int mMistakeCount;

	private DG.Tweening.EaseFunction mReduceMoveEaseFunc;

	private BlobWidget mBlobDividendWidget;

	private Blob mBlobDividend;
	private Blob mBlobDivisor;

	private SelectType mCurSelect;

	private int mDividendNumber;
	private int mDivisorNumber;
	private int mQuotientNumber;
	private int mOutputNumber;

	private bool mIsQuotientNumberChanged;

	private WaitForSeconds mWaitInterval = new WaitForSeconds(0.2f);
	private WaitForSeconds mWaitOutput = new WaitForSeconds(1f);

	private System.Text.StringBuilder mOpText = new System.Text.StringBuilder();

	void M8.IModalPop.Pop() {
		mBlobDividend = null;
		mBlobDivisor = null;

		if(signalListenProceed) signalListenProceed.callback -= OnSignalProceed;
		if(signalListenNumberChanged) signalListenNumberChanged.callback -= OnSignalNumberChanged;
		if(signalListenPrev) signalListenPrev.callback -= OnSignalNext;
		if(signalListenNext) signalListenNext.callback -= OnSignalNext;
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		mBlobDividend = null;
		mBlobDivisor = null;

		mDividendNumber = 0;
		mDivisorNumber = 0;
		mQuotientNumber = 0;
		mOutputNumber = 0;

		mIsQuotientNumberChanged = false;

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

			mQuotientNumber = 1;
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
		RefreshQuotientNumberDisplay();
		RefreshOutputNumberDisplay();

		//mistake
		mMistakeCount = 0;
		if(mistakeCounter) mistakeCounter.Init(0, GameData.instance.playErrorCount);

		//setup signals
		if(signalListenProceed) signalListenProceed.callback += OnSignalProceed;
		if(signalListenNumberChanged) signalListenNumberChanged.callback += OnSignalNumberChanged;
		if(signalListenPrev) signalListenPrev.callback += OnSignalNext;
		if(signalListenNext) signalListenNext.callback += OnSignalNext;

		//setup and open numpad
		mNumpadParms[ModalCalculator.parmInitValue] = 1;
		mNumpadParms[ModalCalculator.parmMaxDigit] = quotientDigitLimit;
		mNumpadParms[ModalCalculatorParmExt.operationText] = "";
		mNumpadParms[ModalCalculatorParmExt.showPrevNext] = true;

		M8.ModalManager.main.Open(GameData.instance.modalNumpad, mNumpadParms);
	}

	void OnSignalProceed(float val) {
		var iVal = Mathf.RoundToInt(val);

		switch(mCurSelect) {
			case SelectType.Quotient:
				if(mQuotientNumber != iVal) {
					mQuotientNumber = iVal;
					mIsQuotientNumberChanged = true;
					RefreshQuotientNumberDisplay();
				}

				Select(SelectType.Output);
				break;

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
			case SelectType.Quotient:
				if(mQuotientNumber != iVal) {
					mQuotientNumber = iVal;
					mIsQuotientNumberChanged = true;
					RefreshQuotientNumberDisplay();
				}
				break;

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

	private void Select(SelectType toSelect) {
		if(mCurSelect != toSelect) {
			mCurSelect = toSelect;

			var modalNumpad = M8.ModalManager.main.GetBehaviour<ModalCalculator>(GameData.instance.modalNumpad);
			if(!modalNumpad)
				return;

			mOpText.Clear();

			switch(mCurSelect) {
				case SelectType.Quotient:
					modalNumpad.SetMaxDigit(quotientDigitLimit, mQuotientNumber);

					mOpText.Append(mDivisorNumber);
					mOpText.Append(" x ?");

					if(blobQuotientWidget) blobQuotientWidget.Pulse();
					break;

				case SelectType.Output:
					if(mIsQuotientNumberChanged) {
						modalNumpad.SetMaxDigit(modalNumpad.defaultMaxDigits, 0);

						mIsQuotientNumberChanged = false;
					}
					else
						modalNumpad.SetMaxDigit(modalNumpad.defaultMaxDigits, mOutputNumber);
					
					mOpText.Append(mDivisorNumber);
					mOpText.Append(" x ");
					mOpText.Append(mQuotientNumber);
					mOpText.Append(" =");

					if(blobOutputWidget) blobOutputWidget.Pulse();
					break;
			}

			if(signalInvokeSetOpText) signalInvokeSetOpText.Invoke(mOpText.ToString());

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

			if(blobDivisorWidget) blobDivisorWidget.Correct();

			yield return mWaitInterval;

			if(blobQuotientWidget) blobQuotientWidget.Correct();

			yield return mWaitInterval;

			if(blobOutputWidget) blobOutputWidget.Correct();
		}
		else {
			//sfx

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
					if(mBlobDividendWidget) mBlobDividendWidget.Correct();

					//wait for blob
					while((mBlobDividendWidget && mBlobDividendWidget.isBusy))
						yield return null;

					actionResult.type = BlobActionResult.Type.Success;					
					actionResult.newValue = newDividendNumber;
					actionResult.splitValue = mQuotientNumber;
				}
				else { //output is greater than dividend
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
		if(quotientNumberText) quotientNumberText.text = mQuotientNumber.ToString();
	}

	private void RefreshOutputNumberDisplay() {
		if(outputNumberText) outputNumberText.text = mOutputNumber.ToString();
	}
}
