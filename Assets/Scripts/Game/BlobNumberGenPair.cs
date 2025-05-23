using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobNumberGenPair : BlobNumberGenBase {
	[System.Serializable]
	public struct NumberPair {
		public int dividend;
		public int divisor;
	}

	public BlobData blobDividend;
	public BlobData blobDivisor;

	public NumberPair[] numberPairs;

	private int mInd;

	private BlobSpawnInfo[] mSpawnInfos;

	public override void InitBlobPoolTypes(M8.PoolController blobPool) {
		blobDividend.InitPool(blobPool);
		blobDivisor.InitPool(blobPool);
	}

	public override BlobSpawnInfo[] GenerateSpawnInfos() {
		var gameDat = GameData.instance;
		
		var pair = numberPairs[mInd];

		mInd++;
		if(mInd == numberPairs.Length) {
			M8.ArrayUtil.Shuffle(numberPairs);
			mInd = 0;
		}

		mSpawnInfos[0] = new BlobSpawnInfo(blobDividend, pair.dividend, pair.divisor, blobDividend.splitCount);
		mSpawnInfos[1] = new BlobSpawnInfo(blobDivisor, pair.divisor, pair.divisor, 0);

		ClearOps();

		AddOp(pair.dividend, pair.divisor, OperatorType.Divide);

		return mSpawnInfos;
	}

	void Awake() {
		mSpawnInfos = new BlobSpawnInfo[2];
				
		M8.ArrayUtil.Shuffle(numberPairs);
		mInd = 0;
	}
}
