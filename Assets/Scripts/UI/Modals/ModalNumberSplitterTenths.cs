using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalNumberSplitterTenths : M8.ModalController, M8.IModalPush, M8.IModalPop {
	public const string parmBlobDividend = "blobDividend";
	public const string parmBlobDivisor = "blobDivisor";

	[Header("HP")]
	public MistakeCounterWidget mistakeCounter;

	[Header("Blobs")]
	public BlobWidget blobLeftDividend;
	public BlobWidget blobLeftDivisor;
	public BlobWidget blobRightDividend;
	public BlobWidget blobRightDivisor;

	[Header("Connectors")]
	public M8.UI.Graphics.ColorGroup connectorLeftLeftColorGroup;
	public M8.UI.Graphics.ColorGroup connectorLeftRightColorGroup;
	public M8.UI.Graphics.ColorGroup connectorRightLeftColorGroup;
	public M8.UI.Graphics.ColorGroup connectorRightRightColorGroup;

	[Header("Numbers")]
	public DigitGroupWidget dividendDigitGroupLeft;
	public DigitGroupWidget dividendDigitGroupRight;
	public TMP_Text divisorLeftText;
	public TMP_Text divisorRightText;

	[Header("Digit Jump")]
	public TMP_Text digitJumpText;
	public float digitJumpHeight;
	public DG.Tweening.Ease digitJumpEase = DG.Tweening.Ease.InOutSine;
	public float digitJumpDelay = 0.3f;

	[Header("Split Display")]
	public GameObject splitIndicatorLeftGO;
	public GameObject splitIndicatorRightGO;

	[Header("Signal Invoke")]
	public SignalBlobActionResult signalInvokeResult;

	private Blob mBlobDividend;
	private Blob mBlobDivisor;

	private DG.Tweening.EaseFunction mDigitJumpEaseFunc;

	private int mMistakeCount;

	private WaitForSeconds mWaitPulse = new WaitForSeconds(0.3f);
	private WaitForSeconds mWaitBlob = new WaitForSeconds(0.5f);

	private Coroutine mRout;

	public void Split() {
		if(mRout != null) return;

		//if either dividend is 0, just close
		int dividendLeftNumber = dividendDigitGroupLeft ? dividendDigitGroupLeft.number : 0;
		int dividendRightNumber = dividendDigitGroupRight ? dividendDigitGroupRight.number : 0;

		if(dividendLeftNumber == 0 || dividendRightNumber == 0) {
			if(signalInvokeResult)
				signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Cancel });

			Close();
		}
		else
			mRout = StartCoroutine(DoSplitEvaluate());
	}

	public void Cancel() {
		if(mRout != null) return;

		if(signalInvokeResult)
			signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Cancel });

		Close();
	}

	void M8.IModalPop.Pop() {
		mBlobDividend = null;
		mBlobDivisor = null;

		if(mRout != null) {
			StopCoroutine(mRout);
			mRout = null;
		}
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		if(parms != null) {
			if(parms.ContainsKey(parmBlobDividend))
				mBlobDividend = parms.GetValue<Blob>(parmBlobDividend);

			if(parms.ContainsKey(parmBlobDivisor))
				mBlobDivisor = parms.GetValue<Blob>(parmBlobDivisor);
		}

		if(mBlobDividend) {
			if(blobLeftDividend) blobLeftDividend.color = mBlobDividend.color;
			if(blobRightDividend) blobRightDividend.color = mBlobDividend.color;

			if(connectorLeftLeftColorGroup) connectorLeftLeftColorGroup.ApplyColor(mBlobDividend.color);
			if(connectorRightLeftColorGroup) connectorRightLeftColorGroup.ApplyColor(mBlobDividend.color);

			if(dividendDigitGroupLeft) {
				var num = mBlobDividend.number;
				var numCount = WholeNumber.DigitCount(num);

				dividendDigitGroupLeft.SetDigitsFixedCount(num, numCount);

				if(dividendDigitGroupRight)
					dividendDigitGroupRight.SetDigitsZero(numCount);

				//attach digit groups to blobs
				if(blobLeftDividend) blobLeftDividend.SetToAnchor(dividendDigitGroupLeft.rectTransform, false);
				if(blobRightDividend) blobRightDividend.SetToAnchor(dividendDigitGroupRight.rectTransform, false);
			}
		}

		if(mBlobDivisor) {
			if(blobLeftDivisor) blobLeftDivisor.color = mBlobDivisor.color;
			if(blobRightDivisor) blobRightDivisor.color = mBlobDivisor.color;

			if(connectorLeftRightColorGroup) connectorLeftRightColorGroup.ApplyColor(mBlobDivisor.color);
			if(connectorRightRightColorGroup) connectorRightRightColorGroup.ApplyColor(mBlobDivisor.color);

			if(divisorLeftText) divisorLeftText.text = mBlobDivisor.number.ToString();
			if(divisorRightText) divisorRightText.text = mBlobDivisor.number.ToString();

			//attach number texts to blobs
			if(blobLeftDivisor) blobLeftDivisor.SetToAnchor(divisorLeftText.rectTransform, true);
			if(blobRightDivisor) blobRightDivisor.SetToAnchor(divisorRightText.rectTransform, true);
		}
				
		//initialize displays
		if(digitJumpText) digitJumpText.gameObject.SetActive(false);
		if(splitIndicatorLeftGO) splitIndicatorLeftGO.SetActive(false);
		if(splitIndicatorRightGO) splitIndicatorRightGO.SetActive(false);

		mMistakeCount = 0;

		if(mistakeCounter) mistakeCounter.Init(mMistakeCount, GameData.instance.playErrorCount);
	}

	void Awake() {
		mDigitJumpEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(digitJumpEase);

		if(dividendDigitGroupLeft) {
			dividendDigitGroupLeft.Init();
			dividendDigitGroupLeft.clickCallback += OnDigitLeftNumberClick;
			dividendDigitGroupLeft.hoverCallback += OnDigitLeftNumberHover;
		}

		if(dividendDigitGroupRight) {
			dividendDigitGroupRight.Init();
			dividendDigitGroupRight.clickCallback += OnDigitRightNumberClick;
			dividendDigitGroupRight.hoverCallback += OnDigitRightNumberHover;
		}
	}

	void OnDigitLeftNumberClick(int index) {
		if(mRout != null) return;

		var digitNum = dividendDigitGroupLeft.GetDigitNumber(index);
		if(digitNum > 0)
			mRout = StartCoroutine(DoSwap(dividendDigitGroupLeft, dividendDigitGroupRight, index));
	}

	void OnDigitLeftNumberHover(int index, bool isHover) {
		if(isHover && mRout != null) return;

		var digitNum = dividendDigitGroupLeft.GetDigitNumber(index);

		if(isHover) {
			if(splitIndicatorLeftGO) splitIndicatorLeftGO.SetActive(digitNum > 0);
			if(splitIndicatorRightGO) splitIndicatorRightGO.SetActive(false);

			if(dividendDigitGroupRight) {
				var digitWidget = dividendDigitGroupRight.GetDigitWidget(index);
				if(digitWidget) digitWidget.isHighlight = digitNum > 0;
			}
		}
		else {
			if(splitIndicatorLeftGO) splitIndicatorLeftGO.SetActive(false);

			if(dividendDigitGroupRight) {
				var digitWidget = dividendDigitGroupRight.GetDigitWidget(index);
				if(digitWidget) digitWidget.isHighlight = false;
			}
		}
	}

	void OnDigitRightNumberClick(int index) {
		if(mRout != null) return;

		var digitNum = dividendDigitGroupRight.GetDigitNumber(index);
		if(digitNum > 0)
			mRout = StartCoroutine(DoSwap(dividendDigitGroupRight, dividendDigitGroupLeft, index));
	}

	void OnDigitRightNumberHover(int index, bool isHover) {
		if(isHover && mRout != null) return;

		var digitNum = dividendDigitGroupRight.GetDigitNumber(index);

		if(isHover) {
			if(splitIndicatorLeftGO) splitIndicatorLeftGO.SetActive(false);
			if(splitIndicatorRightGO) splitIndicatorRightGO.SetActive(digitNum > 0);

			if(dividendDigitGroupLeft) {
				var digitWidget = dividendDigitGroupLeft.GetDigitWidget(index);
				if(digitWidget) digitWidget.isHighlight = digitNum > 0;
			}
		}
		else {
			if(splitIndicatorRightGO) splitIndicatorRightGO.SetActive(false);

			if(dividendDigitGroupLeft) {
				var digitWidget = dividendDigitGroupLeft.GetDigitWidget(index);
				if(digitWidget) digitWidget.isHighlight = false;
			}
		}
	}

	IEnumerator DoSwap(DigitGroupWidget fromDigitGroup, DigitGroupWidget toDigitGroup, int digitIndex) {
		var digitWidgetFrom = fromDigitGroup.GetDigitWidget(digitIndex);
		var digitWidgetTo = toDigitGroup.GetDigitWidget(digitIndex);

		if(splitIndicatorLeftGO) splitIndicatorLeftGO.SetActive(false);
		if(splitIndicatorRightGO) splitIndicatorRightGO.SetActive(false);

		digitWidgetFrom.isHighlight = false;
		digitWidgetTo.isHighlight = false;

		var fromVal = fromDigitGroup.number;
		var toVal = toDigitGroup.number;
		var tenthPlaceVal = WholeNumber.TenExponent(digitIndex);

		var divisorVal = mBlobDivisor ? mBlobDivisor.number : 1;

		var fromDigitVal = digitWidgetFrom.number;
		var toDigitVal = digitWidgetTo.number;
				
		//determine delta
		var deltaDigitVal = 1;

		var isDivisible = false;

		//try to split by having both divisible
		for(; deltaDigitVal <= fromDigitVal; deltaDigitVal++) {
			var delta = tenthPlaceVal * deltaDigitVal;

			if((fromVal - delta) % divisorVal == 0 && (toVal + delta) % divisorVal == 0) {
				isDivisible = true;
				break;
			}
		}

		//at a minimum, split value should be greater than the divisor
		if(!isDivisible) {
			deltaDigitVal = 1;
			for(; deltaDigitVal <= fromDigitVal; deltaDigitVal++) {
				var delta = tenthPlaceVal * deltaDigitVal;

				if((toVal + delta) >= divisorVal)
					break;
			}
		}

		fromDigitVal -= deltaDigitVal;
		toDigitVal += deltaDigitVal;

		fromDigitGroup.SetDigitNumber(digitIndex, fromDigitVal);

		if(fromDigitGroup == dividendDigitGroupLeft && blobLeftDividend) blobLeftDividend.Pulse();
		else if(fromDigitGroup == dividendDigitGroupRight && blobRightDividend) blobRightDividend.Pulse();

		digitWidgetFrom.PlayPulse();

		//do swap motion
		if(digitJumpText) {
			digitJumpText.gameObject.SetActive(true);

			digitJumpText.text = deltaDigitVal.ToString();

			var trans = digitJumpText.transform;

			var startPos = digitWidgetFrom.position;
			var endPos = digitWidgetTo.position;

			var midPos = Vector2.Lerp(startPos, endPos, 0.5f);
			midPos.y += digitJumpHeight;

			trans.position = startPos;

			var curTime = 0f;
			while(curTime < digitJumpDelay) {
				yield return null;

				curTime += Time.deltaTime;

				var t = mDigitJumpEaseFunc(curTime, digitJumpDelay, 0f, 0f);

				trans.position = M8.MathUtil.Bezier(startPos, midPos, endPos, t);
			}

			digitJumpText.gameObject.SetActive(false);
		}

		toDigitGroup.SetDigitNumber(digitIndex, toDigitVal);

		if(toDigitGroup == dividendDigitGroupLeft && blobLeftDividend) blobLeftDividend.Pulse();
		else if(toDigitGroup == dividendDigitGroupRight && blobRightDividend) blobRightDividend.Pulse();

		digitWidgetTo.PlayPulse();

		yield return mWaitPulse;

		mRout = null;
	}

	IEnumerator DoSplitEvaluate() {
		var dividendLeftNumber = dividendDigitGroupLeft ? dividendDigitGroupLeft.number : 0;
		var dividendRightNumber = dividendDigitGroupRight ? dividendDigitGroupRight.number : 0;

		var divisorNumber = mBlobDivisor ? mBlobDivisor.number : 1; //fail-safe

		var isLeftValid = dividendLeftNumber % divisorNumber == 0;
		var isRightValid = dividendRightNumber % divisorNumber == 0;

		if(blobLeftDividend) {
			if(isLeftValid)
				blobLeftDividend.Correct();
			else
				blobLeftDividend.Error();
		}

		if(blobLeftDivisor) {
			if(isLeftValid)
				blobLeftDivisor.Correct();
			else
				blobLeftDivisor.Error();
		}

		yield return mWaitBlob;

		if(blobRightDividend) {
			if(isRightValid)
				blobRightDividend.Correct();
			else
				blobRightDividend.Error();
		}

		if(blobRightDivisor) {
			if(isRightValid)
				blobRightDivisor.Correct();
			else
				blobRightDivisor.Error();
		}

		yield return mWaitBlob;

		if(isLeftValid && isRightValid) { //success
			if(signalInvokeResult)
				signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Success, blobDividend = mBlobDividend, blobDivisor = mBlobDivisor, newValue = dividendLeftNumber, splitValue = dividendRightNumber });

			mRout = null;

			Close();
		}
		else {
			var errorCount = GameData.instance.playErrorCount;

			mMistakeCount++;

			mistakeCounter.UpdateMistakeCount(mMistakeCount, errorCount);

			while(mistakeCounter.isBusy)
				yield return null;

			mRout = null;

			if(mMistakeCount == errorCount) { //fail
				if(signalInvokeResult)
					signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Fail });

				Close();
			}
		}
	}
}