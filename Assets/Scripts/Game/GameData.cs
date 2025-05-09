using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
	public enum LevelMode {
		None,

		Intro,
		Play,
		End
	}

	[Serializable]
	public struct RankData {
		public string grade; //SS, S, A, B, C, D
		public Sprite icon;
		public float scale;
	}

	[Serializable]
	public struct LevelData {
		public M8.SceneAssetPath sceneInterlude;
		public M8.SceneAssetPath scenePlay;
		public int progressCount; //corresponds to number of rounds in PlayController, ensure it matches!
	}

	public struct AttackState {
		public float[] scales; //number of rounds played with attack scale value
		public int roundCount; //max rounds for this one state

		public static AttackState Load(M8.UserData userData, string prefix) {
			var scaleCount = userData.GetInt(prefix + "asc");

			var scales = new float[scaleCount];

			for(int i = 0; i < scaleCount; i++)
				scales[i] = userData.GetFloat(prefix + "as" + i, scales[i]);

			var roundCount = userData.GetInt(prefix + "rc");

			return new AttackState { scales = scales, roundCount = roundCount };
		}

		public void Save(M8.UserData userData, string prefix) {
			if(scales != null) {
				userData.SetInt(prefix + "asc", scales.Length);

				for(int i = 0; i < scales.Length; i++)
					userData.SetFloat(prefix + "as" + i, scales[i]);
			}
			else {
				userData.SetInt(prefix + "asc", 0);
			}

			userData.SetInt(prefix + "rc", roundCount);
		}
	}

	public class LevelState {
		private AttackState[] mAttackStates; //length corresponds to progressCount

		public LevelState(M8.UserData userData, string levelName, int defaultStateCount) {
			Load(userData, levelName, defaultStateCount);
		}

		public LevelState(int stateCount) {
			mAttackStates = new AttackState[stateCount];
		}

		public void ApplyState(int stateIndex, float[] attackScales, int roundCount) {
			if(mAttackStates == null)
				mAttackStates = new AttackState[stateIndex + 1];
			else if(mAttackStates.Length <= stateIndex)
				Array.Resize(ref mAttackStates, stateIndex + 1);

			mAttackStates[stateIndex] = new AttackState { scales = attackScales, roundCount = roundCount };
		}

		public AttackState GetState(int stateIndex) {
			if(mAttackStates != null && stateIndex < mAttackStates.Length)
				return mAttackStates[stateIndex];

			return new AttackState();
		}

		public void Save(M8.UserData userData, string levelName) {
			if(mAttackStates != null) {
				var prefix = levelName + "_";

				userData.SetInt(prefix + "ac", mAttackStates.Length);

				for(int i = 0; i < mAttackStates.Length; i++)
					mAttackStates[i].Save(userData, prefix);
			}
			else
				userData.SetInt(levelName + "ac", 0);
		}

		public void Load(M8.UserData userData, string levelName, int defaultStateCount) {
			var prefix = levelName + "_";

			var stateCount = userData.GetInt(prefix + "ac", defaultStateCount);

			mAttackStates = new AttackState[stateCount];

			for(int i = 0; i < stateCount; i++)
				mAttackStates[i] = AttackState.Load(userData, prefix);
		}
	}

	[Header("Levels")]
	public LevelData[] levels;
	public M8.SceneAssetPath end;

	[Header("Rank Settings")]
	public RankData[] ranks; //highest to lowest

	[Header("Play Config")]
	public int playAttackCapacity = 10;
	public float playAttackFullThreshold = 0.6f; //at what percentage of attack is considered full

	[Header("Blob Spawn Config")]
	public LayerMask blobSpawnCheckMask; //ensure spot is fine to spawn
	public float blobSpawnDelay = 0.3f;
	public float blobSpawnClearoutForce = 5f;
	public float blobSpawnClearoutDelay = 3f;
	public float blobMergeImpuse = 5f;

	public int playStateIndex { get { return mPlayStateInd; } }

	private LevelState[] mLevelStates;

	private int mLevelInd = -1;
	private int mPlayStateInd = -1;
	private LevelMode mLevelMode = LevelMode.None;

	public AttackState GetAttackState(int stateIndex) {
		if(mLevelInd == -1)
			GenerateLevelInfo();

		if(mLevelInd != -1)
			return mLevelStates[mLevelInd].GetState(stateIndex);

		return new AttackState();
	}

	public void SaveCurrentLevelPlayState(int stateIndex, float[] attackScales, int roundCount) {
		if(mLevelInd == -1)
			GenerateLevelInfo();

		if(mLevelStates == null)
			GenerateLevelStates();

		mPlayStateInd = stateIndex;
				
		mLevelStates[mLevelInd].ApplyState(stateIndex, attackScales, roundCount);

		var prog = GetProgressUpToLevel(mLevelInd, false);

		prog += mPlayStateInd;
				
		var userData = LoLManager.instance.userData;
		if(userData) {
			userData.SetInt("lvlInd", mLevelInd);
			userData.SetInt("playInd", mPlayStateInd);
			userData.SetInt("lvlMode", (int)mLevelMode);

			var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

			mLevelStates[mLevelInd].Save(userData, curScene.name);
		}

		LoLManager.instance.ApplyProgress(prog);
	}

	public void ProgressNext() {
		int curProg = LoLManager.instance.curProgress, nextProg = LoLManager.instance.curProgress;
				
		var toScene = end;

		var userData = LoLManager.instance.userData;

		if(mLevelInd == -1) {
			GenerateLevelInfo();

			if(mLevelInd == -1) //not found?
				mLevelInd = 0;
		}

		bool isNextLevel = false;
		var curLvl = levels[mLevelInd];

		switch(mLevelMode) {
			case LevelMode.Intro:
				nextProg = GetProgressUpToLevel(mLevelInd, true);

				if(curLvl.scenePlay.isValid) { //enter play
					toScene = curLvl.scenePlay;
					mLevelMode = LevelMode.Play;
				}
				else
					isNextLevel = true;
				break;

			case LevelMode.Play:
				nextProg = GetProgressUpToLevel(mLevelInd, true);

				if(curLvl.sceneInterlude.isValid) { //show level end
					toScene = curLvl.sceneInterlude;
					mLevelMode = LevelMode.End;
				}
				else
					isNextLevel = true;
				break;

			default: //go next level
				isNextLevel = true;
				break;
		}

		if(isNextLevel) {
			mLevelInd++;
			if(mLevelInd >= 0 && mLevelInd < levels.Length) {
				nextProg = GetProgressUpToLevel(mLevelInd, false);

				if(levels[mLevelInd].sceneInterlude.isValid) {
					toScene = levels[mLevelInd].sceneInterlude;
					mLevelMode = LevelMode.Intro;
				}
				else if(levels[mLevelInd].scenePlay.isValid) {
					toScene = levels[mLevelInd].scenePlay;
					mLevelMode = LevelMode.Play;
				}
				else { //fail-safe, get next valid level
					for(; mLevelInd < levels.Length; mLevelInd++) {
						if(levels[mLevelInd].sceneInterlude.isValid) {
							toScene = levels[mLevelInd].sceneInterlude;
							mLevelMode = LevelMode.Intro;
							break;
						}
						else if(levels[mLevelInd].sceneInterlude.isValid) {
							toScene = levels[mLevelInd].scenePlay;
							mLevelMode = LevelMode.Play;
							break;
						}
					}
				}
			}
			else {
				nextProg = LoLManager.instance.progressMax;

				mLevelMode = LevelMode.None;
			}
		}

		mPlayStateInd = 0;

		if(userData) {
			userData.SetInt("lvlInd", mLevelInd);
			userData.SetInt("playInd", mPlayStateInd);
			userData.SetInt("lvlMode", (int)mLevelMode);
		}

		if(curProg != nextProg)
			LoLManager.instance.ApplyProgress(nextProg);
		else if(userData)
			userData.Save();

		toScene.Load();
	}

	public void LoadToCurrent() {
		var userData = LoLManager.instance.userData;

		mLevelInd = userData.GetInt("lvlInd");
		
		mLevelMode = (LevelMode)userData.GetInt("lvlMode");

		if(mLevelMode == LevelMode.Play)
			mPlayStateInd = userData.GetInt("playInd");
		else
			mPlayStateInd = 0;

		mLevelStates = new LevelState[levels.Length];

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];
			if(lvl.scenePlay.isValid)
				mLevelStates[i] = new LevelState(userData, lvl.scenePlay.name, lvl.progressCount);
			else
				mLevelStates[i] = new LevelState(0);
		}

		var curLvl = levels[mLevelInd];

		switch(mLevelMode) {
			case LevelMode.Intro:
			case LevelMode.End:
				if(curLvl.sceneInterlude.isValid)
					curLvl.sceneInterlude.Load();
				else //fail-safe
					ProgressNext();
				break;

			case LevelMode.Play:
				if(curLvl.scenePlay.isValid)
					curLvl.scenePlay.Load();
				else //fail-safe
					ProgressNext();
				break;

			default: //shouldn't be None, just move to next progress
				ProgressNext();
				break;
		}
	}

	public void NewGame() {
		//reset states
		mLevelInd = 0;		
		mPlayStateInd = 0;

		GenerateLevelStates();

		//clear out progress and data
		if(LoLManager.instance.curProgress > 0) {			
			if(LoLManager.instance.userData)
				LoLManager.instance.userData.Delete();

			LoLManager.instance.ApplyProgress(0);
		}

		var curLvl = levels[mLevelInd];

		if(curLvl.sceneInterlude.isValid) { //should be set to intro scene if available, this should be valid!
			mLevelMode = LevelMode.Intro;
			curLvl.sceneInterlude.Load();
		}
		else if(curLvl.scenePlay.isValid) { //fail-safe
			mLevelMode = LevelMode.Play;
			curLvl.scenePlay.Load();
		}
		else
			Debug.LogWarning("First level has no valid scene!");
	}

	protected override void OnInstanceInit() {
		var progCount = 0;

		//determine progress count
		for(int i = 0; i < levels.Length; i++)
			progCount += levels[i].progressCount;

		LoLManager.instance.progressMax = progCount;
	}

	private int GetProgressUpToLevel(int levelIndex, bool inclusive) {
		var prog = 0;

		if(inclusive) {
			for(int i = 0; i <= levelIndex; i++)
				prog += levels[i].progressCount;
		}
		else {
			for(int i = 0; i < levelIndex; i++)
				prog += levels[i].progressCount;
		}

			return prog;
	}

	private void GenerateLevelInfo() {
		var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

		mLevelInd = -1;

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];

			if(lvl.sceneInterlude == curScene) {
				mLevelInd = i;
				mLevelMode = LevelMode.Intro;				
				break;
			}
			else if(lvl.scenePlay == curScene) {
				mLevelInd = i;
				mLevelMode = LevelMode.Play;
				break;
			}
		}

		mPlayStateInd = 0;
	}

	private void GenerateLevelStates() {
		mLevelStates = new LevelState[levels.Length];

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];

			if(lvl.scenePlay.isValid)
				mLevelStates[i] = new LevelState(lvl.progressCount);
			else
				mLevelStates[i] = new LevelState(0);
		}
	}
}
