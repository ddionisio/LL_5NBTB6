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

	public BlobData blobDividend;
	public BlobData blobDivisor;

	public RoundData[] rounds;

	private int mRoundInd;

	private BlobSpawnInfo[] mSpawnInfos;

	public override void InitBlobPoolTypes(M8.PoolController blobPool) {
		blobDividend.InitPool(blobPool);
		blobDivisor.InitPool(blobPool);
	}

	public override BlobSpawnInfo[] GenerateSpawnInfos(int round) {
		var gameDat = GameData.instance;

		//get next pair
		var roundClamped = Mathf.Clamp(round, 0, rounds.Length - 1);
		if(mRoundInd != roundClamped) {
			mRoundInd = roundClamped;

			rounds[mRoundInd].Init();
		}
				
		var pair = rounds[mRoundInd].GetPair();

		mSpawnInfos[0] = new BlobSpawnInfo { data = blobDividend, number = pair.dividend };
		mSpawnInfos[1] = new BlobSpawnInfo { data = blobDivisor, number = pair.divisor };

		return mSpawnInfos;
	}

	public override bool CanSplit(Blob blob, int divisor) {
		return false;
	}

	public override void Split(Blob blob, int divisor) {

	}

	void Awake() {
		mRoundInd = -1;
		mSpawnInfos = new BlobSpawnInfo[2];		
	}
}
