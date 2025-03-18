using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobNumberGenTenthsDivisible : BlobNumberGenBase {
	[System.Serializable]
	public struct NumberPair {
		public int dividend;
		public int divisor;
	}

	[System.Serializable]
	public class RoundData {
		public NumberPair[] roundPairs;

		private int mPairInd;

		public NumberPair GetPair() {
			var ret = roundPairs[mPairInd];

			mPairInd++;
			if(mPairInd == roundPairs.Length)
				mPairInd = 0;

			return ret;
		}

		public void Init() {
			mPairInd = 0;

			M8.ArrayUtil.Shuffle(roundPairs);
		}
	}

	public NumberPair firstPair;
	public bool firstPairEnabled;

	public RoundData[] rounds;

	private int mRoundInd;

	private BlobSpawnInfo[] mSpawnInfos;

	public override BlobSpawnInfo[] GenerateSpawnInfos(int round) {
		//get next pair
		var roundClamped = Mathf.Clamp(round, 0, rounds.Length - 1);
		if(mRoundInd != roundClamped) {
			mRoundInd = roundClamped;

			rounds[mRoundInd].Init();
		}

		var gameDat = GameData.instance;

		var pair = rounds[mRoundInd].GetPair();

		mSpawnInfos[0] = new BlobSpawnInfo { data = gameDat.blobDividend, number = pair.dividend };
		mSpawnInfos[1] = new BlobSpawnInfo { data = gameDat.blobDivisor, number = pair.divisor };

		return mSpawnInfos;
	}

	void Awake() {
		mRoundInd = -1;
		mSpawnInfos = new BlobSpawnInfo[2];		
	}
}
