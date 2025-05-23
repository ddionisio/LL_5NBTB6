using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class Lesson1Controller : GameModeController<Lesson1Controller> {
	[Header("Displays")]
	public AnimatorEnterExit blobsDisplay;
	public AnimatorEnterExit placeValueDisplay;
	public AnimatorEnterExit placeValueDistributeDisplay;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogPlaceValue;
	public ModalDialogFlowIncremental dialogPlaceValueDistribute;
	public ModalDialogFlowIncremental dialogEnd;

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		blobsDisplay.rootGO.SetActive(false);
		placeValueDisplay.rootGO.SetActive(false);
		placeValueDistributeDisplay.rootGO.SetActive(false);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

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

		//distribute explain
		placeValueDistributeDisplay.rootGO.SetActive(true);
		yield return placeValueDistributeDisplay.PlayEnterWait();

		yield return dialogPlaceValueDistribute.Play();

		yield return dialogEnd.Play();

		yield return placeValueDistributeDisplay.PlayExitWait();
		placeValueDistributeDisplay.rootGO.SetActive(false);

		GameData.instance.ProgressNext();
	}
}
