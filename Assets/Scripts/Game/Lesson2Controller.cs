using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class Lesson2Controller : GameModeController<Lesson2Controller> {
	[Header("Displays")]
	public AnimatorEnterExit blobsDisplay;
	public AnimatorEnterExit areaModelDisplay;
	public AnimatorEnterExit areaModelDistributeDisplay;

	[Header("Dialogs")]
	public ModalDialogFlowIncremental dialogIntro;
	public ModalDialogFlowIncremental dialogAreaModel;
	public ModalDialogFlowIncremental dialogAreaModelDistribute;
	public ModalDialogFlowIncremental dialogEnd;

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		blobsDisplay.rootGO.SetActive(false);
		areaModelDisplay.rootGO.SetActive(false);
		areaModelDistributeDisplay.rootGO.SetActive(false);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		//intro
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

		//distribute explain
		areaModelDistributeDisplay.rootGO.SetActive(true);
		yield return areaModelDistributeDisplay.PlayEnterWait();

		yield return dialogAreaModelDistribute.Play();

		yield return dialogEnd.Play();

		yield return areaModelDistributeDisplay.PlayExitWait();
		areaModelDistributeDisplay.rootGO.SetActive(false);

		GameData.instance.ProgressNext();
	}
}
