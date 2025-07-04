using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using LoLExt;

public class Lesson1Controller : GameModeController<Lesson1Controller> {
	[Header("Displays")]
	public AnimatorEnterExit blobsDisplay;
	public AnimatorEnterExit placeValueDisplay;
	public AnimatorEnterExit placeValueDistributeDisplay;

	[Header("Distribute")]
	public GameObject distributeRootGO;

	public GameObject distributeDragGO;
	public GameObject distributeDragInstructGO;
	public RectTransform distributeDragBeginRoot;
	public RectTransform distributeDragEndRoot;

	public M8.Animator.Animate distributeAnim;
	[M8.Animator.TakeSelector(animatorField = "distributeAnim")]
	public int distributeTakeDrag = -1;
	[M8.Animator.TakeSelector(animatorField = "distributeAnim")]
	public int distributeTakeDragEnd = -1;

	[Header("Tens Place")]
	public GameObject tensRootGO;

	public RectTransform tensClickRoot;
	public Transform tensSelectClickAnchorRoot;
	public Transform tensSelectClickIndicatorRoot;

	public RectTransform tensNumberAreaFirstRoot;
	public RectTransform tensNumberAreaSecondRoot;
	public RectTransform tensDivRightRoot;
	public RectTransform tensDivLeftRoot;
	public RectTransform tensPlusRoot;

	public M8.Animator.Animate tensAnim;
	[M8.Animator.TakeSelector(animatorField = "tensAnim")]
	public int tensTakeMoveNumberFirst = -1;
	[M8.Animator.TakeSelector(animatorField = "tensAnim")]
	public int tensTakeMoveNumberSecond = -1;
	[M8.Animator.TakeSelector(animatorField = "tensAnim")]
	public int tensTakeDivRight = -1;
	[M8.Animator.TakeSelector(animatorField = "tensAnim")]
	public int tensTakeDivLeft = -1;
	[M8.Animator.TakeSelector(animatorField = "tensAnim")]
	public int tensTakeAddOp = -1;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogPlaceValue;
	public ModalDialogFlowIncremental dialogPlaceValueDistribute;
	public ModalDialogFlowIncremental dialogSwapFirst;
	public ModalDialogFlowIncremental dialogSwapSecond;
	public ModalDialogFlowIncremental dialogSwapComplete;
	public ModalDialogFlowIncremental dialogDivisionsSolved;
	public ModalDialogFlowIncremental dialogAddSolved;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxTaskComplete;

	private bool mIsDragging;
	private PointerEventData mDragPointer;
	private bool mIsDragComplete;

	private bool mIsNumberClickDone;

	public void NumberClick() {
		mIsNumberClickDone = true;
	}

	public void DistributeDragBegin(PointerEventData eventData) {
		if(mIsDragComplete) //prevent input after done
			return;
				
		mIsDragging = true;
		mDragPointer = eventData;

		distributeDragInstructGO.SetActive(false);
	}

	public void DistributeDrag(PointerEventData eventData) {
		if(!mIsDragging)
			return;

		mDragPointer = eventData;
	}

	public void DistributeDragEnd(PointerEventData eventData) {
		if(!mIsDragging)
			return;
				
		mIsDragging = false;
		mDragPointer = null;

		distributeDragInstructGO.SetActive(true);

		distributeAnim.Resume();
		if(!distributeAnim.isReversed)
			distributeAnim.Reverse();
	}

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		blobsDisplay.rootGO.SetActive(false);
		placeValueDisplay.rootGO.SetActive(false);
		placeValueDistributeDisplay.rootGO.SetActive(false);

		distributeRootGO.SetActive(false);
		distributeDragGO.SetActive(false);
		distributeDragInstructGO.SetActive(false);

		tensRootGO.SetActive(false);
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, true, false);

		//intro
		blobsDisplay.rootGO.SetActive(true);
		yield return blobsDisplay.PlayEnterWait();

		yield return dialogIntro.Play();

		yield return blobsDisplay.PlayExitWait();
		blobsDisplay.rootGO.SetActive(false);

		//place value explain
		placeValueDisplay.rootGO.SetActive(true);
		yield return placeValueDisplay.PlayEnterWait();

		yield return dialogPlaceValue.Play();

		yield return placeValueDisplay.PlayExitWait();
		placeValueDisplay.rootGO.SetActive(false);

		//distribute

		distributeRootGO.SetActive(true);

		placeValueDistributeDisplay.rootGO.SetActive(true);
		yield return placeValueDistributeDisplay.PlayEnterWait();

		distributeDragInstructGO.SetActive(true);

		//dialog distribute
		yield return dialogPlaceValueDistribute.Play();

		distributeDragGO.SetActive(true);

		var takeDragDuration = distributeAnim.GetTakeTotalTime(distributeTakeDrag);

		//wait for dragging to complete
		Vector2 dragBegin = distributeDragBeginRoot.position;
		Vector2 dragEnd = distributeDragEndRoot.position;

		mIsDragComplete = false;
		while(!mIsDragComplete) {
			if(mDragPointer != null) {
				Vector2 pos = mDragPointer.position;

				var t = (pos.x - dragBegin.x) / (dragEnd.x - dragBegin.x);
				if(t < 0f) t = 0f;
				else if(t > 1f) t = 1f;

				var time = t * takeDragDuration;

				distributeAnim.Goto(distributeTakeDrag, time);

				mIsDragComplete = t == 1f;
			}

			yield return null;
		}

		mIsDragging = false;
		mDragPointer = null;

		distributeAnim.Goto(distributeTakeDrag, takeDragDuration);

		if(!string.IsNullOrEmpty(sfxTaskComplete))
			M8.SoundPlaylist.instance.Play(sfxTaskComplete, false);

		yield return distributeAnim.PlayWait(distributeTakeDragEnd);

		distributeRootGO.SetActive(false);

		tensRootGO.SetActive(true);

		//show input first number
		tensClickRoot.gameObject.SetActive(true);

		tensClickRoot.SetParent(tensNumberAreaFirstRoot, false);
		tensClickRoot.anchorMin = Vector2.zero;
		tensClickRoot.anchorMax = Vector2.one;
		tensClickRoot.anchoredPosition = Vector2.zero;
		tensClickRoot.sizeDelta = Vector2.zero;

		tensSelectClickIndicatorRoot.gameObject.SetActive(true);
		tensSelectClickIndicatorRoot.position = tensSelectClickAnchorRoot.position;

		//dialog instruct
		yield return dialogSwapFirst.Play();

		//wait for click
		mIsNumberClickDone = false;
		while(!mIsNumberClickDone)
			yield return null;

		//hide input
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);

		//play anim
		yield return tensAnim.PlayWait(tensTakeMoveNumberFirst);

		//show input second number
		tensClickRoot.gameObject.SetActive(true);

		tensClickRoot.SetParent(tensNumberAreaSecondRoot, false);
		tensClickRoot.anchorMin = Vector2.zero;
		tensClickRoot.anchorMax = Vector2.one;
		tensClickRoot.anchoredPosition = Vector2.zero;
		tensClickRoot.sizeDelta = Vector2.zero;

		tensSelectClickIndicatorRoot.gameObject.SetActive(true);
		tensSelectClickIndicatorRoot.position = tensSelectClickAnchorRoot.position;

		//dialog
		yield return dialogSwapSecond.Play();

		//wait for click
		mIsNumberClickDone = false;
		while(!mIsNumberClickDone)
			yield return null;

		//hide input
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);

		//play anim
		yield return tensAnim.PlayWait(tensTakeMoveNumberSecond);

		//dialog
		yield return dialogSwapComplete.Play();

		//show input div right
		tensClickRoot.gameObject.SetActive(true);

		tensClickRoot.SetParent(tensDivRightRoot, false);
		tensClickRoot.anchorMin = Vector2.zero;
		tensClickRoot.anchorMax = Vector2.one;
		tensClickRoot.anchoredPosition = Vector2.zero;
		tensClickRoot.sizeDelta = Vector2.zero;

		tensSelectClickIndicatorRoot.gameObject.SetActive(true);
		tensSelectClickIndicatorRoot.position = tensSelectClickAnchorRoot.position;

		//wait for click
		mIsNumberClickDone = false;
		while(!mIsNumberClickDone)
			yield return null;

		//hide input
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);

		//play anim
		yield return tensAnim.PlayWait(tensTakeDivRight);

		//show input div left
		tensClickRoot.gameObject.SetActive(true);

		tensClickRoot.SetParent(tensDivLeftRoot, false);
		tensClickRoot.anchorMin = Vector2.zero;
		tensClickRoot.anchorMax = Vector2.one;
		tensClickRoot.anchoredPosition = Vector2.zero;
		tensClickRoot.sizeDelta = Vector2.zero;

		tensSelectClickIndicatorRoot.gameObject.SetActive(true);
		tensSelectClickIndicatorRoot.position = tensSelectClickAnchorRoot.position;

		//wait for click
		mIsNumberClickDone = false;
		while(!mIsNumberClickDone)
			yield return null;

		//hide input
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);

		//play anim
		yield return tensAnim.PlayWait(tensTakeDivLeft);

		//dialog
		yield return dialogDivisionsSolved.Play();

		//show input add
		tensClickRoot.gameObject.SetActive(true);

		tensClickRoot.SetParent(tensPlusRoot, false);
		tensClickRoot.anchorMin = Vector2.zero;
		tensClickRoot.anchorMax = Vector2.one;
		tensClickRoot.anchoredPosition = Vector2.zero;
		tensClickRoot.sizeDelta = Vector2.zero;

		tensSelectClickIndicatorRoot.gameObject.SetActive(true);
		tensSelectClickIndicatorRoot.position = tensSelectClickAnchorRoot.position;

		//wait for click
		mIsNumberClickDone = false;
		while(!mIsNumberClickDone)
			yield return null;

		//hide input
		tensClickRoot.gameObject.SetActive(false);
		tensSelectClickIndicatorRoot.gameObject.SetActive(false);

		if(!string.IsNullOrEmpty(sfxTaskComplete))
			M8.SoundPlaylist.instance.Play(sfxTaskComplete, false);

		//play anim
		yield return tensAnim.PlayWait(tensTakeAddOp);

		//dialog
		yield return dialogAddSolved.Play();

		yield return placeValueDistributeDisplay.PlayExitWait();
		placeValueDistributeDisplay.rootGO.SetActive(false);

		GameData.instance.ProgressNext();
	}
}
