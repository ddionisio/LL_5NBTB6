using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class EndController : GameModeController<EndController> {
	[Header("Animation")]
	public M8.Animator.Animate animator;
	[M8.Animator.TakeSelector]
	public int takeEnd = -1;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		if(takeEnd != -1)
			animator.ResetTake(takeEnd);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, false, false);

		var t = Time.time;

		if(takeEnd != -1)
			yield return animator.PlayWait(takeEnd);

		var lolMgr = LoLManager.instance;

		while(lolMgr.isSpeakQueueActive)
			yield return null;

		lolMgr.Complete();

		//Debug.Log("time took: " + (Time.time - t).ToString());
	}
}
