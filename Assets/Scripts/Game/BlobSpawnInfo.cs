using UnityEngine;

public struct BlobSpawnInfo {
	public BlobData data;
	public int number;
	public int divisor;

	public string nameOverride;

	public Blob.State spawnToState;

	public Vector2 spawnPointOverride;
	public bool isSpawnPointOverride;

	public BlobSpawnInfo(BlobData data, Blob.State toState, int number, int divisor) {
		this.data = data;
		this.spawnToState = toState;
		this.number = number;
		this.divisor = divisor;

		nameOverride = "";

		spawnPointOverride = Vector2.zero;
		isSpawnPointOverride = false;
	}

	public BlobSpawnInfo(BlobData data, int number, int divisor, Vector2 position) {
		this.data = data;
		this.spawnToState = Blob.State.Normal;
		this.number = number;
		this.divisor = divisor;

		nameOverride = "";

		spawnPointOverride = position;
		isSpawnPointOverride = true;
	}
}
