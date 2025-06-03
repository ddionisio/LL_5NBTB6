using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

using LoLExt;

public class EndController : GameModeController<EndController> {
	[Header("Score")]
	public GameObject scoreRootGO;
	public TMP_Text errorText;
	public TMP_Text scoreText;
	public RankWidget rankWidget;

	[Header("End")]
	public GameObject endGO;

	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeEnd = -1;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		if(scoreRootGO) scoreRootGO.SetActive(false);

		if(endGO) endGO.SetActive(false);

		if(takeEnd != -1)
			animator.ResetTake(takeEnd);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		int totalAttack, totalError, totalScore;
		float effectiveScale, scoreScale;

		GameData.instance.GetTotalLevelStats(out totalAttack, out totalError, out effectiveScale, out totalScore, out scoreScale);

		if(errorText) errorText.text = totalError.ToString();
		if(scoreText) scoreText.text = totalScore.ToString();

		if(rankWidget) rankWidget.Apply(GameData.instance.GetRankIndex(scoreScale));

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, false, false);

		//var t = Time.time;

		if(takeEnd != -1)
			yield return animator.PlayWait(takeEnd);

		if(endGO) endGO.SetActive(true);

		var lolMgr = LoLManager.instance;

		while(lolMgr.isSpeakQueueActive)
			yield return null;

		if(scoreRootGO) scoreRootGO.SetActive(true);

		yield return new WaitForSeconds(8f);

		lolMgr.Complete();

		//Debug.Log("time took: " + (Time.time - t).ToString());
	}
}
