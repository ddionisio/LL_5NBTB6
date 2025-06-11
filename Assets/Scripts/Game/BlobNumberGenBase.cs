using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BlobNumberGenParam {
	public bool divisorLock;
}

public abstract class BlobNumberGenBase : MonoBehaviour {
	public int rounds = 1;
	public int quotientResultCount = 1;
	public int splitUnlockCount = 0; //how many times to split before divisors are unlocked

	public int opCount { get { return mOps.Count; } }

	public int opSolvedCount {
		get {
			var count = 0;

			for(int i = 0; i < mOpsIsSolved.Count; i++) {
				if(mOpsIsSolved[i])
					count++;
			}

			return count;
		}
	}

	public abstract void InitBlobPoolTypes(M8.PoolController blobPool);

	public abstract BlobSpawnInfo[] GenerateSpawnInfos(BlobNumberGenParam parms);

	private const int opCapacity = 4;
	private M8.CacheList<Operation> mOps = new M8.CacheList<Operation>(opCapacity);
	private M8.CacheList<bool> mOpsIsSolved = new M8.CacheList<bool>(opCapacity);

	public Operation GetOperation(int index) {
		return index >= 0 && index < mOps.Count ? mOps[index] : new Operation { op = OperatorType.None };
	}

	public bool IsOperationSolved(int index) {
		return index >= 0 && index < mOpsIsSolved.Count && mOpsIsSolved[index];
	}

	public void SetOperationSolved(int index, bool isSolved) {
		if(index >= 0 && index < mOpsIsSolved.Count)
			mOpsIsSolved[index] = isSolved;
	}

	protected void ClearOps() {
		mOps.Clear();
		mOpsIsSolved.Clear();
	}

	protected void AddOp(int left, int right, OperatorType type) {
		mOps.Add(new Operation { operand1 = left, operand2 = right, op = type });
		mOpsIsSolved.Add(false);
	}
}
