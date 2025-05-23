using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class InterludeController : GameModeController<InterludeController> {
	[System.Serializable]
	public class LevelInfo {
		public InterludePip pip;
		public AnimatorEnterExit blobAnim;
		public M8.ColorPalette palette;
	}

	[System.Serializable]
	public class RotationInfo {
		public Transform transform;
		public float scale;

		public void Rotate(float angle) {
			var rot = transform.eulerAngles;
			
			rot.z = angle * scale;

			transform.eulerAngles = rot;
		}
	}

	[Header("Data")]
	public M8.ColorPalette palette;
	public LevelInfo[] levels;
	public RotationInfo[] rotates;

	[Header("Config")]
	public float rotateDelay = 1f;
	public DG.Tweening.Ease rotateEase;

	public float startDelay = 1f;
	public float nextDelay = 1f;
	public float endDelay = 2f;

	[Header("Music")]
	[M8.MusicPlaylist]
	public string music;

	[Header("Debug")]
	public bool isDebug;
	public GameData.LevelMode debugLevelMode;
	public int debugLevelIndex;

	private DG.Tweening.EaseFunction mRotateEaseFunc;

	private GameData.LevelMode mLevelMode;
	private int mLevelIndex;

	private float mRotAngle;

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		mRotateEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(rotateEase);

		var gameDat = GameData.instance;

		var _isDebug = Application.isEditor ? isDebug : false;
		if(_isDebug) {
			mLevelMode = debugLevelMode;
			mLevelIndex = debugLevelIndex;
		}
		else {
			mLevelMode = gameDat.levelMode;
			mLevelIndex = gameDat.playIndex;
		}

		//initialize state
		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];

			if(mLevelIndex == i) {
				lvl.pip.InitMode(InterludePip.Mode.EmptySelect);

				lvl.blobAnim.rootGO.SetActive(true);
				lvl.blobAnim.PlayEnter();

				//apply palette
				palette.CopyColors(lvl.palette);
			}
			else if(mLevelIndex < i) {
				lvl.pip.InitMode(InterludePip.Mode.Empty);

				lvl.blobAnim.rootGO.SetActive(true);
				lvl.blobAnim.PlayEnter();
			}
			else {
				lvl.pip.InitMode(InterludePip.Mode.Fill);

				lvl.blobAnim.rootGO.SetActive(false);
			}
		}

		//initialize rotation
		mRotAngle = mLevelIndex * 90f;

		ApplyRotate();
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		if(!string.IsNullOrEmpty(music))
			M8.MusicPlaylist.instance.Play(music, false, false);

		if(mLevelMode == GameData.LevelMode.Next) {
			yield return new WaitForSeconds(startDelay);

			var curLvl = mLevelIndex >= 0 && mLevelIndex < levels.Length ? levels[mLevelIndex] : null;
			if(curLvl != null) {
				var curPalette = curLvl.palette;

				//defeat current blob
				yield return curLvl.blobAnim.PlayExitWait();

				curLvl.blobAnim.rootGO.SetActive(false);

				//fill pip
				curLvl.pip.mode = InterludePip.Mode.Fill;
				while(curLvl.pip.isBusy)
					yield return null;

				yield return new WaitForSeconds(nextDelay);

				//move to next level
				int levelNextIndex = mLevelIndex + 1;
				var nextLvl = levelNextIndex >= 0 && levelNextIndex < levels.Length ? levels[levelNextIndex] : null;
				if(nextLvl != null) {
					var nextPalette = nextLvl.palette;

					float sRotAngle = mRotAngle, eRotAngle = mRotAngle + 90f;

					var curTime = 0f;
					while(curTime < rotateDelay) {
						yield return null;

						curTime += Time.deltaTime;

						var t = mRotateEaseFunc(curTime, rotateDelay, 0f, 0f);

						mRotAngle = Mathf.LerpAngle(sRotAngle, eRotAngle, t);
						ApplyRotate();

						//also apply palette
						palette.SetColorsLerp(curPalette, nextPalette, t);
					}

					//select pip
					nextLvl.pip.mode = InterludePip.Mode.EmptySelect;
				}
			}
		}

		yield return new WaitForSeconds(endDelay);

		GameData.instance.ProgressNext();
	}

	private void ApplyRotate() {
		for(int i = 0; i < rotates.Length; i++)
			rotates[i].Rotate(mRotAngle);
	}
}
