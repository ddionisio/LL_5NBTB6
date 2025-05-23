using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalVictory : M8.ModalController, M8.IModalPush, M8.IModalPop {
	[Header("Config")]
	public float exitWaitDelay;

	[Header("Panels")]
	public LoLExt.AnimatorEnterExit attackPanel;
	public LoLExt.AnimatorEnterExit errorPanel;
	public LoLExt.AnimatorEnterExit efficientPanel;
	public LoLExt.AnimatorEnterExit scorePanel;

	[Header("Counters")]
	public M8.TextMeshPro.TextMeshProCounter attackCounter;
	public M8.TextMeshPro.TextMeshProCounter errorCounter;
	public M8.TextMeshPro.TextMeshProCounter efficientCounter;
	public M8.TextMeshPro.TextMeshProCounter scoreCounter;

	[Header("Rank")]
	public RankWidget rankWidget;

	private WaitForSeconds mExitWait;

	private GameData.LevelStats mLevelStats;

	void M8.IModalPop.Pop() {
	}

	void M8.IModalPush.Push(M8.GenericParams parms) {
		mLevelStats = GameData.instance.GetCurrentLevelStats();

		attackCounter.SetCountImmediate(0);
		errorCounter.SetCountImmediate(0);
		efficientCounter.SetCountImmediate(0);
		scoreCounter.SetCountImmediate(0);

		attackPanel.rootGO.SetActive(false);
		errorPanel.rootGO.SetActive(false);
		efficientPanel.rootGO.SetActive(false);
		scorePanel.rootGO.SetActive(false);

		rankWidget.gameObject.SetActive(false);

		StartCoroutine(DoProceed());
	}

	void Awake() {
		mExitWait = new WaitForSeconds(exitWaitDelay);
	}

	IEnumerator DoProceed() {
		do {
			yield return null;
		} while(M8.ModalManager.main.isBusy);

		//attack
		attackPanel.rootGO.SetActive(true);
		yield return attackPanel.PlayEnterWait();

		attackCounter.count = mLevelStats.attackCount;

		//error
		errorPanel.rootGO.SetActive(true);
		yield return errorPanel.PlayEnterWait();

		errorCounter.count = mLevelStats.mistakeCount;

		//efficient
		efficientPanel.rootGO.SetActive(true);
		yield return efficientPanel.PlayEnterWait();

		efficientCounter.count = Mathf.RoundToInt(mLevelStats.effectiveScale * 100.0f);

		//score
		scorePanel.rootGO.SetActive(true);
		yield return scorePanel.PlayEnterWait();

		scoreCounter.count = mLevelStats.score;

		while(scoreCounter.isPlaying)
			yield return null;

		rankWidget.Apply(mLevelStats.rankIndex);
		rankWidget.gameObject.SetActive(true);

		yield return mExitWait;

		Close();
	}
}
