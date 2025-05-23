using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalOperationSolver : M8.ModalController, M8.IModalPush, M8.IModalPop {
	public const string parmBlobConnectGroup = "grp"; //BlobConnectController.Group

	[Header("HP")]
	public MistakeCounterWidget mistakeCounter;

	[Header("Blobs")]
	public BlobWidgetTemplateMatch blobTemplateLeft;
	public BlobWidgetTemplateMatch blobTemplateRight;
	public BlobWidgetTemplateMatch blobTemplateEq;

	[Header("Connectors")]
	public GameObject connectorLeftGO;
	public GameObject connectorRightGO;
	public GameObject connectorEqLeftGO;
	public GameObject connectorEqRightGO;

	[Header("Ops")]
	public OpWidget opWidget;
	public OpWidget opEqualWidget;

	[Header("Numbers")]
	public TMP_Text numberLeft;
	public TMP_Text numberRight;
	public TMP_Text numberEq;
	public GameObject numberEqHighlightGO;

	[Header("Signal Invoke")]
	public M8.SignalBoolean signalInvokeInputActive;
	public M8.SignalFloat signalInvokeValueChange;
	public SignalBlobActionResult signalInvokeResult;
	public M8.Signal signalInvokeCorrect;
	public M8.Signal signalInvokeError;

	[Header("Signal Listen")]
	public M8.SignalFloat signalListenProceed;

	private BlobConnectController.Group mBlobConnectGroup;

	private BlobWidget mBlobLeftWidget;
	private BlobWidget mBlobRightWidget;
	private BlobWidget mBlobEqWidget;

	private string mNumberEqUnknownStr;

	private Operation mOp;

	private int mMistakeCount;

	private M8.GenericParams mNumpadParms = new M8.GenericParams();

	private M8.UI.Graphics.ColorGroup mConnectorLeftClrGrp;
	private M8.UI.Layouts.LayoutAnchorFromTo mConnectorLeftAnchor;

	private M8.UI.Graphics.ColorGroup mConnectorRightClrGrp;
	private M8.UI.Layouts.LayoutAnchorFromTo mConnectorRightAnchor;

	private M8.UI.Graphics.ColorGroup mConnectorEqLeftClrGrp;
	private M8.UI.Layouts.LayoutAnchorFromTo mConnectorEqLeftAnchor;

	private M8.UI.Graphics.ColorGroup mConnectorEqRightClrGrp;
	private M8.UI.Layouts.LayoutAnchorFromTo mConnectorEqRightAnchor;

	private WaitForSeconds mWaitInterval = new WaitForSeconds(0.2f);

	void M8.IModalPop.Pop() {
		mBlobLeftWidget = null;
		mBlobRightWidget = null;
		mBlobEqWidget = null;

		if(signalListenProceed) signalListenProceed.callback -= OnSignalProceed;
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		mBlobConnectGroup = null;

		Blob blobLeft = null;
		Blob blobRight = null;

		mOp = new Operation();

		if(parms != null) {
			if(parms.ContainsKey(parmBlobConnectGroup))
				mBlobConnectGroup = parms.GetValue<BlobConnectController.Group>(parmBlobConnectGroup);

			/*if(parms.ContainsKey(parmBlobLeft))
				blobLeft = parms.GetValue<Blob>(parmBlobLeft);

			if(parms.ContainsKey(parmBlobRight))
				blobRight = parms.GetValue<Blob>(parmBlobRight);

			if(parms.ContainsKey(parmOpType))
				mOp.op = parms.GetValue<OperatorType>(parmOpType);*/
		}

		if(mBlobConnectGroup != null) {
			blobLeft = mBlobConnectGroup.blobOpLeft;
			blobRight = mBlobConnectGroup.blobOpRight;

			if(mBlobConnectGroup.connectOp)
				mOp.op = mBlobConnectGroup.connectOp.op;
		}

		//setup blob left
		if(blobTemplateLeft && blobLeft) {
			blobTemplateLeft.ApplyMatchingWidget(blobLeft.data);

			mBlobLeftWidget = blobTemplateLeft.blobWidget;
			if(mBlobLeftWidget) {
				mBlobLeftWidget.color = blobLeft.color;

				if(numberLeft) {
					mBlobLeftWidget.SetToAnchor(numberLeft.rectTransform, true);
					numberLeft.text = blobLeft.number.ToString();
				}
			}

			mOp.operand1 = blobLeft.number;
		}

		//setup blob right
		if(blobTemplateRight && blobRight) {
			blobTemplateRight.ApplyMatchingWidget(blobRight.data);

			mBlobRightWidget = blobTemplateRight.blobWidget;
			if(mBlobRightWidget) {
				mBlobRightWidget.color = blobRight.color;

				if(numberRight) {
					mBlobRightWidget.SetToAnchor(numberRight.rectTransform, true);
					numberRight.text = blobRight.number.ToString();
				}
			}

			mOp.operand2 = blobRight.number;
		}

		//setup blob equal
		mNumberEqUnknownStr = WholeNumber.RepeatingChar(mOp.equal, '?');

		if(blobTemplateEq) {
			var blobMergeDat = BlobData.GetMergeData(blobLeft, blobRight, mOp.equal);
			if(blobMergeDat) {
				blobTemplateEq.ApplyMatchingWidget(blobMergeDat);

				mBlobEqWidget = blobTemplateEq.blobWidget;
				if(mBlobEqWidget) {
					mBlobEqWidget.color = blobMergeDat.color;

					if(numberEq)
						mBlobEqWidget.SetToAnchor(numberEq.rectTransform, true);
				}
			}
		}

		//connectors and stuff
		if(opWidget)
			opWidget.operatorType = mOp.op;

		if(mBlobLeftWidget)
			SetupConnector(connectorLeftGO, mBlobLeftWidget.color, mBlobLeftWidget.root, ref mConnectorLeftClrGrp, ref mConnectorLeftAnchor);

		if(mBlobRightWidget) {
			SetupConnector(connectorRightGO, mBlobRightWidget.color, mBlobRightWidget.root, ref mConnectorRightClrGrp, ref mConnectorRightAnchor);

			if(mBlobEqWidget) {
				SetupConnector(connectorEqLeftGO, mBlobRightWidget.color, mBlobRightWidget.root, ref mConnectorEqLeftClrGrp, ref mConnectorEqLeftAnchor);
				SetupConnector(connectorEqRightGO, mBlobEqWidget.color, mBlobEqWidget.root, ref mConnectorEqRightClrGrp, ref mConnectorEqRightAnchor);
			}
		}

		SetEqualUnknown();

		//mistake display
		mMistakeCount = 0;

		if(mistakeCounter)
			mistakeCounter.Init(0, GameData.instance.playErrorCount);

		//setup signals
		if(signalListenProceed) signalListenProceed.callback += OnSignalProceed;

		//setup and open numpad
		mNumpadParms[ModalCalculatorParmExt.operationText] = mOp.GetUnsolvedString();
		mNumpadParms[ModalCalculatorParmExt.showPrevNext] = false;

		M8.ModalManager.main.Open(GameData.instance.modalNumpad, mNumpadParms);
	}

	void Update() {
		//place op widgets midpoint of blobs
		if(mBlobLeftWidget && mBlobRightWidget)
			opWidget.position = BlobWidget.GetMidpoint(mBlobLeftWidget, mBlobRightWidget);

		if(mBlobRightWidget && mBlobEqWidget)
			opEqualWidget.position = BlobWidget.GetMidpoint(mBlobRightWidget, mBlobEqWidget);
	}

	void OnSignalProceed(float val) {
		var iVal = Mathf.FloorToInt(val);

		SetEqualNumber(iVal);

		//check if correct
		if(mOp.equal == iVal)
			StartCoroutine(DoCorrect(iVal));
		else
			StartCoroutine(DoError(iVal));
	}

	IEnumerator DoCorrect(int val) {
		if(signalInvokeInputActive) signalInvokeInputActive.Invoke(false);

		if(signalInvokeCorrect) signalInvokeCorrect.Invoke();

		//animate
		if(mBlobLeftWidget) mBlobLeftWidget.Correct();

		yield return mWaitInterval;

		if(mBlobRightWidget) mBlobRightWidget.Correct();

		yield return mWaitInterval;

		if(mBlobEqWidget) mBlobEqWidget.Correct();

		while((mBlobLeftWidget && mBlobLeftWidget.isBusy) || (mBlobRightWidget && mBlobRightWidget.isBusy) || (mBlobEqWidget && mBlobEqWidget.isBusy))
			yield return null;

		if(signalInvokeResult)
			signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Success, group = mBlobConnectGroup, newValue = val });

		Close();
	}

	IEnumerator DoError(int val) {
		if(signalInvokeInputActive) signalInvokeInputActive.Invoke(false);

		if(signalInvokeError) signalInvokeError.Invoke();

		var errorMaxCount = GameData.instance.playErrorCount;

		mMistakeCount++;

		if(mistakeCounter) mistakeCounter.UpdateMistakeCount(mMistakeCount, errorMaxCount);

		//animate
		if(mBlobLeftWidget) mBlobLeftWidget.Error();

		yield return mWaitInterval;

		if(mBlobRightWidget) mBlobRightWidget.Error();

		yield return mWaitInterval;

		if(mBlobEqWidget) mBlobEqWidget.Error();

		while((mBlobLeftWidget && mBlobLeftWidget.isBusy) || (mBlobRightWidget && mBlobRightWidget.isBusy) || (mBlobEqWidget && mBlobEqWidget.isBusy))
			yield return null;

		//boot out with failure?
		if(mMistakeCount >= errorMaxCount) {
			if(signalInvokeResult)
				signalInvokeResult.Invoke(new BlobActionResult { type = BlobActionResult.Type.Fail, group = mBlobConnectGroup, newValue = val });

			Close();
		}
		else {
			if(signalInvokeValueChange) signalInvokeValueChange.Invoke(0f);
			if(signalInvokeInputActive) signalInvokeInputActive.Invoke(true);

			SetEqualUnknown();
		}
	}

	private void SetupConnector(GameObject connectorGO, Color color, Transform from, ref M8.UI.Graphics.ColorGroup colorGrp, ref M8.UI.Layouts.LayoutAnchorFromTo layout) {
		if(!connectorGO)
			return;

		if(!colorGrp)
			colorGrp = connectorGO.GetComponent<M8.UI.Graphics.ColorGroup>();

		if(!layout)
			layout = connectorGO.GetComponent<M8.UI.Layouts.LayoutAnchorFromTo>();

		if(colorGrp)
			colorGrp.ApplyColor(color);

		if(layout)
			layout.from = from;
	}

	private void SetEqualUnknown() {
		if(numberEq) numberEq.text = mNumberEqUnknownStr;

		if(numberEqHighlightGO) numberEqHighlightGO.SetActive(true);
	}

	private void SetEqualNumber(int num) {
		if(numberEq) numberEq.text = num.ToString();

		if(numberEqHighlightGO) numberEqHighlightGO.SetActive(false);
	}
}
