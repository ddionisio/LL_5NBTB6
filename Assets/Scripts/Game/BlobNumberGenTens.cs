using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobNumberGenTens : BlobNumberGenBase {
	[Header("Config")]
	public M8.RangeInt[] tensMultRanges; //each represent tens-place, [0 = 1x, 1 = 10x, 2 = 100x, ...]
	public int[] divisors;
	public int poolCount = 5;

	[Header("Blob Templates")]
	public BlobData blobDividend;
	public BlobData blobDivisor;

	private M8.CacheList<BlobSpawnInfo> mSpawnInfoPool;

	private BlobSpawnInfo[] mSpawnInfoPair = new BlobSpawnInfo[2];

	public override void InitBlobPoolTypes(M8.PoolController blobPool) {
		blobDividend.InitPool(blobPool);
		blobDivisor.InitPool(blobPool);
	}

	public override BlobSpawnInfo[] GenerateSpawnInfos(BlobNumberGenParam parms) {
		if(mSpawnInfoPool.Count < 2)
			GeneratePool();

		mSpawnInfoPair[0] = mSpawnInfoPool.RemoveLast();

		var divisorInf = mSpawnInfoPool.RemoveLast();
		divisorInf.locked = parms.divisorLock;

		mSpawnInfoPair[1] = divisorInf;

		ClearOps();

		AddOp(mSpawnInfoPair[0].number, mSpawnInfoPair[1].number, OperatorType.Divide);

		return mSpawnInfoPair;
	}

	void Awake() {
		GeneratePool();
	}

	private void GeneratePool() {
		if(mSpawnInfoPool == null)
			mSpawnInfoPool = new M8.CacheList<BlobSpawnInfo>(poolCount * 2); //for pairs

		M8.ArrayUtil.Shuffle(divisors);

		int curDivisorInd = 0;

		for(int i = 0; i < poolCount; i++) {
			var divisor = divisors[curDivisorInd];

			var dividend = 0;
			var tens = 1;

			for(int j = 0; j < tensMultRanges.Length; j++) {
				var mult = tensMultRanges[j].random;

				dividend += mult * tens * divisor;

				tens *= 10;
			}

			curDivisorInd++;
			if(curDivisorInd == divisors.Length)
				curDivisorInd = 0;

			mSpawnInfoPool.Add(new BlobSpawnInfo(blobDivisor, divisor, divisor, 0));
			mSpawnInfoPool.Add(new BlobSpawnInfo(blobDividend, dividend, divisor, blobDividend.splitCount));
		}
	}
}