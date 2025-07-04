using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using TMPro;

using LoLExt;

public class Lesson2Controller : GameModeController<Lesson2Controller> {
	[Header("Displays")]
	public AnimatorEnterExit blobsDisplay;
	public AnimatorEnterExit areaModelDisplay;
	public AnimatorEnterExit areaModelDistributeDisplay;

	[Header("Operation Texts")]
	public TMP_Text opText;
	public string opInitialString;
	public string opSolvedString;

	public TMP_Text opDistText;
	public string opDistInitialString;
	public string opDistSolvedString;

	[Header("Split")]
	public GameObject splitDragInteractGO;
	public GameObject splitDragInstructGO;
	public RectTransform splitDragBeginRoot;
	public RectTransform splitDragEndRoot;

	public M8.TextMeshPro.TextMeshProInteger splitAreaWidthNumber;
	public M8.TextMeshPro.TextMeshProInteger splitAreaFirstNumber;
	public M8.TextMeshPro.TextMeshProInteger splitAreaSecondNumber;

	public M8.Animator.Animate splitAnim;
	[M8.Animator.TakeSelector(animatorField = "splitAnim")]
	public int splitTakeDrag = -1;
	[M8.Animator.TakeSelector(animatorField = "splitAnim")]
	public int splitTakeDragEnd = -1;
	[M8.Animator.TakeSelector(animatorField = "splitAnim")]
	public int splitTakeOpSolved = -1;
	[M8.Animator.TakeSelector(animatorField = "splitAnim")]
	public int splitTakeOpDistSolved = -1;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogAreaModel;
	public ModalDialogFlowIncremental dialogAreaModelDragInstruct;
	public ModalDialogFlowIncremental dialogAreaModelDragComplete;
	public ModalDialogFlowIncremental dialogAreaModelAnswer;
	public ModalDialogFlowIncremental dialogEnd;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	[Header("SFX")]
	[M8.SoundPlaylist]
	public string sfxTaskComplete;
	[M8.SoundPlaylist]
	public string sfxTaskDone;

	private bool mIsDragging;
	private PointerEventData mDragPointer;
	private bool mIsDragComplete;

	const int divisor = 12;
	const int dividend = 204;

	public void SplitDragBegin(PointerEventData eventData) {
		if(mIsDragComplete) //prevent input after done
			return;

		mIsDragging = true;
		mDragPointer = eventData;

		splitDragInstructGO.SetActive(false);
	}

	public void SplitDrag(PointerEventData eventData) {
		if(!mIsDragging)
			return;

		mDragPointer = eventData;
	}

	public void SplitDragEnd(PointerEventData eventData) {
		if(!mIsDragging)
			return;

		mIsDragging = false;
		mDragPointer = null;

		splitDragInstructGO.SetActive(true);

		splitAnim.Resume();
		if(!splitAnim.isReversed)
			splitAnim.Reverse();
	}

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		blobsDisplay.rootGO.SetActive(false);
		areaModelDisplay.rootGO.SetActive(false);
		areaModelDistributeDisplay.rootGO.SetActive(false);

		opText.text = opInitialString;
		opDistText.text = opDistInitialString;

		splitAreaSecondNumber.number = dividend;

		splitAnim.ResetTake(splitTakeDrag);

		splitDragInteractGO.SetActive(false);
		splitDragInstructGO.SetActive(false);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, true, false);

		blobsDisplay.rootGO.SetActive(true);
		yield return blobsDisplay.PlayEnterWait();

		yield return dialogIntro.Play();

		yield return blobsDisplay.PlayExitWait();
		blobsDisplay.rootGO.SetActive(false);

		//area model explain
		areaModelDisplay.rootGO.SetActive(true);
		yield return areaModelDisplay.PlayEnterWait();

		yield return dialogAreaModel.Play();

		yield return areaModelDisplay.PlayExitWait();
		areaModelDisplay.rootGO.SetActive(false);

		//split

		areaModelDistributeDisplay.rootGO.SetActive(true);
		yield return areaModelDistributeDisplay.PlayEnterWait();

		splitDragInstructGO.SetActive(true);

		splitDragInteractGO.SetActive(true);

		//dialog instruct
		yield return dialogAreaModelDragInstruct.Play();

		var takeDragDuration = splitAnim.GetTakeTotalTime(splitTakeDrag);

		//wait for dragging to complete
		Vector2 dragBegin = splitDragBeginRoot.position;
		Vector2 dragEnd = splitDragEndRoot.position;

		mIsDragComplete = false;
		while(!mIsDragComplete) {
			if(mDragPointer != null) {
				Vector2 pos = mDragPointer.position;

				var t = (pos.x - dragBegin.x) / (dragEnd.x - dragBegin.x);
				if(t < 0f) t = 0f;
				else if(t > 1f) t = 1f;

				var time = t * takeDragDuration;

				splitAnim.Goto(splitTakeDrag, time);

				mIsDragComplete = t == 1f;
			}

			//update numbers
			var areaNum = splitAreaWidthNumber.number * divisor;

			splitAreaFirstNumber.number = areaNum;
			splitAreaSecondNumber.number = dividend - areaNum;

			yield return null;
		}

		mIsDragging = false;
		mDragPointer = null;

		splitDragInteractGO.SetActive(false);
		splitDragInstructGO.SetActive(false);

		splitAnim.Goto(splitTakeDrag, takeDragDuration);

		if(!string.IsNullOrEmpty(sfxTaskComplete))
			M8.SoundPlaylist.instance.Play(sfxTaskComplete, false);

		//dialog drag complete
		yield return dialogAreaModelDragComplete.Play();

		if(!string.IsNullOrEmpty(sfxTaskComplete))
			M8.SoundPlaylist.instance.Play(sfxTaskComplete, false);

		yield return splitAnim.PlayWait(splitTakeDragEnd);

		yield return new WaitForSeconds(0.5f);

		//update op dist display
		if(!string.IsNullOrEmpty(sfxTaskComplete))
			M8.SoundPlaylist.instance.Play(sfxTaskComplete, false);

		opDistText.text = opDistSolvedString;
		yield return splitAnim.PlayWait(splitTakeOpDistSolved);

		//dialog answer
		yield return dialogAreaModelAnswer.Play();

		//update op display
		if(!string.IsNullOrEmpty(sfxTaskDone))
			M8.SoundPlaylist.instance.Play(sfxTaskDone, false);

		opText.text = opSolvedString;
		yield return splitAnim.PlayWait(splitTakeOpSolved);

		//end dialog
		yield return dialogEnd.Play();

		yield return areaModelDistributeDisplay.PlayExitWait();
		areaModelDistributeDisplay.rootGO.SetActive(false);

		GameData.instance.ProgressNext();
	}
}
