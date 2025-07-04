using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
	public enum LevelMode {
		None,

		Intro, //play going to level
		Play,
		Next //defeat of previous blob
	}

	[Serializable]
	public struct RankData {
		public string grade; //SS, S, A, B, C, D
		public Sprite icon;
		public float scale;
	}

	[Serializable]
	public struct LevelData {
		public M8.SceneAssetPath scenePlay;
		public LevelMode mode;
		public int playIndex; //set to -1 if no state
		public int progressCount; //corresponds to number generator count in PlayController, ensure it matches!

		public bool isLevel { get { return mode == LevelMode.Play && playIndex != -1; } }
	}

	public struct LevelStats {
		public int attackCount;
		public int mistakeCount;
		public float effectiveScale;

		public int scoreMax {
			get {
				return instance.scorePerRound + instance.scoreNoMistake;
			}
		}

		public int score {
			get {
				var ret = Mathf.RoundToInt(effectiveScale * instance.scorePerRound);

				if(mistakeCount == 0)
					ret += instance.scoreNoMistake;

				return ret;
			}
		}

		public float scoreScale {
			get { return Mathf.Clamp01((float)score / scoreMax); }
		}

		public int rankIndex {
			get {
				var _ranks = instance.ranks;
				var _scale = scoreScale;

				for(int i = 0; i < _ranks.Length; i++) {
					if(_scale >= _ranks[i].scale)
						return i;
				}

				return _ranks.Length - 1;
			}
		}
	}

	public struct AttackState {
		public float[] scales; //number of rounds played with attack scale value
		public int mistakeCount; //number of failing out of actions
		public int roundCount; //max rounds for this one state

		public int attackCount { get { return scales != null ?  scales.Length : 0; } }

		public float effectiveScale {
			get {
				if(scales == null)
					return 0f;

				var scaleSum = 0f;

				for(int i = 0; i < scales.Length; i++)
					scaleSum += scales[i];

				for(int i = scales.Length; i < roundCount; i++)
					scaleSum += 1f;

				return scaleSum / roundCount;
			}
		}

		public static AttackState Load(M8.UserData userData, string prefix) {
			var scaleCount = userData.GetInt(prefix + "asc");

			var scales = new float[scaleCount];

			for(int i = 0; i < scaleCount; i++)
				scales[i] = userData.GetFloat(prefix + "as" + i, scales[i]);

			var mistakeCount = userData.GetInt(prefix + "ec");
			var roundCount = userData.GetInt(prefix + "rc");

			return new AttackState { scales = scales, mistakeCount = mistakeCount, roundCount = roundCount };
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

			userData.SetInt(prefix + "ec", mistakeCount);
			userData.SetInt(prefix + "rc", roundCount);
		}
	}

	public class LevelState {
		public LevelStats stats {
			get {
				var attackCount = 0;
				var mistakeCount = 0;
				var effectiveScaleSum = 0f;

				for(int i = 0; i < mAttackStates.Length; i++) {
					var state = mAttackStates[i];

					attackCount += state.attackCount;
					mistakeCount += state.mistakeCount;
					effectiveScaleSum += state.effectiveScale;
				}

				return new LevelStats { attackCount = attackCount, mistakeCount = mistakeCount, effectiveScale = effectiveScaleSum / mAttackStates.Length };
			}
		}

		private AttackState[] mAttackStates; //length corresponds to progressCount

		public LevelState(M8.UserData userData, string levelName, int defaultStateCount) {
			Load(userData, levelName, defaultStateCount);
		}

		public LevelState(int stateCount) {
			mAttackStates = new AttackState[stateCount];
		}

		public void ApplyState(int stateIndex, float[] attackScales, int mistakeCount, int roundCount) {
			if(mAttackStates == null)
				mAttackStates = new AttackState[stateIndex + 1];
			else if(mAttackStates.Length <= stateIndex)
				Array.Resize(ref mAttackStates, stateIndex + 1);

			mAttackStates[stateIndex] = new AttackState { scales = attackScales, mistakeCount = mistakeCount, roundCount = roundCount };
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

	[Header("Modals")]
	public string modalOpSolver = "blobOperator";
	public string modalBlobSplitTenths = "blobSplitTenths";
	public string modalBlobSplitPartialQuotient = "blobSplitPartialQuotient";
	public string modalNumpad = "numpad";
	public string modalVictory = "victory";

	[Header("Levels")]
	public LevelData[] levels;
	public M8.SceneAssetPath end;

	[Header("Rank Settings")]
	public RankData[] ranks; //highest to lowest

	[Header("Play Config")]
	public int playAttackCapacity = 10;
	public int playAttackEfficiencyCount = 8; //what attack value is considered 100% efficient
	public float playAttackFullThreshold = 0.6f; //at what percentage of attack is considered full
	public int playAttackSplitReduce = 1; //amount to reduce when splitting blob
	public int playAttackErrorReduce = 2; //amount of reduction when player fails an action
	public int playErrorCount = 3; //how many times before failing a computation
	public int playFailRoundCount = 3; //how many times player failed, if reached, reset board with new blobs

	[Header("Blob Spawn Config")]
	public LayerMask blobSpawnCheckMask; //ensure spot is fine to spawn
	public LayerMask blobSpawnCheckSolidMask; //check for 'out of bounds'
	public float blobSpawnDelay = 0.3f;
	public float blobSpawnClearoutForce = 5f;
	public float blobSpawnClearoutDelay = 3f;
	public float blobMergeImpuse = 5f;

	[Header("Score Config")]
	public int scorePerRound = 4500;
	public int scoreNoMistake = 500;

	public int playStateIndex { 
		get {
			if(mLevelInd == -1 || mPlayStateInd == -1)
				GenerateLevelInfo();

			return mPlayStateInd; 
		} 
	}

	public LevelMode levelMode { get { return mLevelInd >= 0 && mLevelInd < levels.Length ? levels[mLevelInd].mode : LevelMode.None; } }

	public int playIndex { get { return mLevelInd >= 0 && mLevelInd < levels.Length ? levels[mLevelInd].playIndex : 0; } }

	private LevelState[] mLevelStates;

	private int mLevelInd = -1;
	private int mPlayStateInd = -1;

	public int GetRankIndex(float scale) {
		var _ranks = instance.ranks;

		for(int i = 0; i < _ranks.Length; i++) {
			if(scale >= _ranks[i].scale)
				return i;
		}

		return _ranks.Length - 1;
	}

	public AttackState GetAttackState(int stateIndex) {
		if(mLevelInd == -1)
			GenerateLevelInfo();

		if(mLevelInd >= 0 && mLevelInd < levels.Length) {
			var lvl = levels[mLevelInd];
			if(lvl.isLevel)
				return mLevelStates[lvl.playIndex].GetState(stateIndex);
		}

		return new AttackState();
	}

	public LevelStats GetCurrentLevelStats() {
		if(mLevelInd == -1)
			GenerateLevelInfo();

		if(mLevelStates == null)
			LoadStates();

		if(mLevelInd >= 0 && mLevelInd < levels.Length) {
			var lvl = levels[mLevelInd];
			if(lvl.isLevel)
				return mLevelStates[lvl.playIndex].stats;
		}

		return new LevelStats();
	}

	public void GetTotalLevelStats(out int totalAttack, out int totalError, out float totalEffective, out int totalScore, out float totalScoreScale) {
		totalAttack = 0;
		totalError = 0;
		totalEffective = 0f;
		totalScore = 0;
		totalScoreScale = 0f;

		if(mLevelStates != null) {
			var count = 0;

			for(int i = 0; i < mLevelStates.Length; i++) {
				var state = mLevelStates[i];
				if(state != null) {
					var stats = state.stats;

					totalAttack += stats.attackCount;
					totalError += stats.mistakeCount;
					totalEffective += stats.effectiveScale;
					totalScore += stats.score;
					totalScoreScale += stats.scoreScale;

					count++;
				}
			}

			if(count > 0) {
				totalEffective /= count;
				totalScoreScale /= count;
			}
		}
	}

	public int Progress(int stateIndex, float[] attackScales, int mistakeCount, int roundCount) {
		if(mLevelInd == -1)
			GenerateLevelInfo();

		if(mLevelStates == null)
			GenerateLevelStates();

		var playInd = -1;
		if(mLevelInd >= 0 && mLevelInd < levels.Length) {
			var lvl = levels[mLevelInd];
			if(lvl.isLevel)
				playInd = lvl.playIndex;
		}

		if(playInd == -1) //fail-safe
			return mPlayStateInd;
								
		mLevelStates[playInd].ApplyState(stateIndex, attackScales, mistakeCount, roundCount);

		mPlayStateInd = stateIndex + 1;
								
		var userData = LoLManager.instance.userData;
		if(userData) {
			userData.SetInt("lvlInd", mLevelInd);
			userData.SetInt("playInd", mPlayStateInd);

			var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

			mLevelStates[playInd].Save(userData, curScene.name);
		}

		var prog = GetProgressUpToLevel(mLevelInd, false) + mPlayStateInd;

		if(LoLManager.instance.curProgress < prog) {
			Debug.Log("Progress: " + prog);

			LoLManager.instance.ApplyProgress(prog);
		}
		else if(userData)
			userData.Save();

		return mPlayStateInd;
	}

	public void ProgressNext() {
		int curProg = LoLManager.instance.curProgress;
		int nextProg = GetProgressUpToLevel(mLevelInd, true);

		var userData = LoLManager.instance.userData;

		if(mLevelInd == -1) {
			GenerateLevelInfo();

			if(mLevelInd == -1) //not found?
				mLevelInd = 0;
		}

		mLevelInd++;
		mPlayStateInd = 0;

		if(userData) {
			userData.SetInt("lvlInd", mLevelInd);
			userData.SetInt("playInd", mPlayStateInd);
		}

		if(curProg < nextProg) {
			Debug.Log("Progress Next: " + nextProg);

			LoLManager.instance.ApplyProgress(nextProg);
		}
		else if(userData)
			userData.Save();

		if(mLevelInd < levels.Length)
			levels[mLevelInd].scenePlay.Load();
		else
			end.Load();
	}

	public void LoadToCurrent() {
		var userData = LoLManager.instance.userData;
		if(!userData) { //fail-safe
			NewGame();
			return;
		}

		mLevelInd = userData.GetInt("lvlInd");

		LoadStates();

		if(mLevelInd < levels.Length)
			levels[mLevelInd].scenePlay.Load();
		else
			end.Load();
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

		levels[mLevelInd].scenePlay.Load();
	}

	protected override void OnInstanceInit() {
		mLevelStates = null;
		mLevelInd = -1;
		mPlayStateInd = 0;
	}

	private void LoadStates() {
		var userData = LoLManager.instance.userData;
		if(!userData)
			return;

		mPlayStateInd = userData.GetInt("playInd");

		var stateCount = GenerateLevelStateCount();

		mLevelStates = new LevelState[stateCount];

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];
			if(lvl.isLevel)
				mLevelStates[lvl.playIndex] = new LevelState(userData, lvl.scenePlay.name, lvl.progressCount);
		}
	}

	private int GetProgressUpToLevel(int levelIndex, bool inclusive) {
		var prog = 0;

		var count = inclusive ? levelIndex + 1 : levelIndex;
		if(count >= levels.Length)
			count = levels.Length;

		for(int i = 0; i < count; i++)
			prog += levels[i].progressCount;

		return prog;
	}

	private void GenerateLevelInfo() {
		var curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

		mLevelInd = -1;

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];

			if(lvl.scenePlay == curScene) {
				mLevelInd = i;
				break;
			}
		}

		mPlayStateInd = 0;
	}

	private int GenerateLevelStateCount() {
		if(mLevelStates != null)
			return mLevelStates.Length;

		var stateCount = 0;

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];
			if(lvl.isLevel && lvl.playIndex >= stateCount)
				stateCount = lvl.playIndex + 1;
		}

		return stateCount;
	}

	private void GenerateLevelStates() {
		GenerateProgressMax();

		var stateCount = GenerateLevelStateCount();
				
		mLevelStates = new LevelState[stateCount];

		for(int i = 0; i < levels.Length; i++) {
			var lvl = levels[i];

			if(lvl.isLevel)
				mLevelStates[lvl.playIndex] = new LevelState(lvl.progressCount);
		}
	}

	private void GenerateProgressMax() {
		var progCount = 0;

		//determine progress count
		for(int i = 0; i < levels.Length; i++)
			progCount += levels[i].progressCount;

		LoLManager.instance.progressMax = progCount;
	}
}
