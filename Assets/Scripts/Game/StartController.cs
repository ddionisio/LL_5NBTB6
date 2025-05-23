using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LoLExt;

public class StartController : GameModeController<StartController> {
	[Header("Screen")]
	public GameObject loadingGO;
	public GameObject readyGO;

	[Header("Play")]
	public AnimatorEnterExit readyAnim;
	public Button newButton;
	public Button continueButton;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		if(loadingGO) loadingGO.SetActive(true);
		if(readyGO) readyGO.SetActive(false);

		//Setup Play
		if(newButton) newButton.onClick.AddListener(OnPlayNew);
		if(continueButton) continueButton.onClick.AddListener(OnPlayContinue);
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		//setup continue
		if(continueButton)
			continueButton.gameObject.SetActive(LoLManager.instance.curProgress > 0);

		//wait a bit
		yield return new WaitForSeconds(0.3f);

		if(loadingGO) loadingGO.SetActive(false);

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, true, true);

		if(readyGO) readyGO.SetActive(true);

		//enter animation
		if(readyAnim)
			readyAnim.PlayEnter();
	}

	IEnumerator DoIntro() {
		if(readyAnim)
			yield return readyAnim.PlayExitWait();

		GameData.instance.NewGame();
	}

	IEnumerator DoProceed() {
		if(readyAnim)
			yield return readyAnim.PlayExitWait();

		GameData.instance.LoadToCurrent();
	}

	void OnPlayNew() {
		StartCoroutine(DoIntro());
	}

	void OnPlayContinue() {
		if(LoLManager.instance.curProgress <= 0)
			StartCoroutine(DoIntro());
		else
			StartCoroutine(DoProceed());
	}
}
