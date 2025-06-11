using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobNumberGenPairRandomRange : BlobNumberGenBase {
	[Header("Config")]
	public M8.RangeInt multRange;
	public int[] divisors;

	[Header("Blob Templates")]
	public BlobData blobDividend;
	public BlobData blobDivisor;

	private BlobSpawnInfo[] mSpawnInfoPair = new BlobSpawnInfo[2];

	private int mDivisorInd = 0;

	public override void InitBlobPoolTypes(M8.PoolController blobPool) {
		blobDividend.InitPool(blobPool);
		blobDivisor.InitPool(blobPool);
	}

	public override BlobSpawnInfo[] GenerateSpawnInfos(BlobNumberGenParam parms) {
		var divisor = divisors[mDivisorInd];

		mDivisorInd++;
		if(mDivisorInd == divisors.Length) {
			M8.ArrayUtil.Shuffle(divisors);
			mDivisorInd = 0;
		}

		var dividend = multRange.random * divisor;

		mSpawnInfoPair[0] = new BlobSpawnInfo(blobDividend, dividend, divisor, blobDividend.splitCount);
		mSpawnInfoPair[1] = new BlobSpawnInfo(blobDivisor, divisor, divisor, 0, parms.divisorLock);

		ClearOps();

		AddOp(mSpawnInfoPair[0].number, mSpawnInfoPair[1].number, OperatorType.Divide);

		return mSpawnInfoPair;
	}

	void Awake() {
		M8.ArrayUtil.Shuffle(divisors);
	}
}
