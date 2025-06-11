using UnityEngine;

public struct BlobSpawnInfo {
	public BlobData data;
	public int number;
	public int divisor;
	public int splitCount;

	public string nameOverride;

	public Blob.State spawnToState;

	public Vector2 spawnPointOverride;
	public bool isSpawnPointOverride;
	public bool locked;

	public BlobSpawnInfo(BlobData data, int number, int divisor, int splitCount) {
		this.data = data;
		this.spawnToState = Blob.State.Normal;
		this.number = number;
		this.divisor = divisor;
		this.splitCount = splitCount;
		this.locked = false;

		nameOverride = "";

		spawnPointOverride = Vector2.zero;
		isSpawnPointOverride = false;
	}

	public BlobSpawnInfo(BlobData data, int number, int divisor, int splitCount, bool locked) {
		this.data = data;
		this.spawnToState = Blob.State.Normal;
		this.number = number;
		this.divisor = divisor;
		this.splitCount = splitCount;
		this.locked = locked;

		nameOverride = "";

		spawnPointOverride = Vector2.zero;
		isSpawnPointOverride = false;
	}

	public BlobSpawnInfo(BlobData data, Blob.State toState, Vector2 position, int number, int divisor) {
		this.data = data;
		this.spawnToState = toState;
		this.number = number;
		this.divisor = divisor;
		this.splitCount = 0;
		this.locked = false;

		nameOverride = "";

		spawnPointOverride = position;
		isSpawnPointOverride = true;
	}

	public BlobSpawnInfo(BlobData data, Vector2 position, int number, int divisor, int splitCount) {
		this.data = data;
		this.spawnToState = Blob.State.Normal;
		this.number = number;
		this.divisor = divisor;
		this.splitCount = splitCount;
		this.locked = false;

		nameOverride = "";

		spawnPointOverride = position;
		isSpawnPointOverride = true;
	}

	public BlobSpawnInfo(BlobData data, Vector2 position, int number, int divisor, bool locked) {
		this.data = data;
		this.spawnToState = Blob.State.Normal;
		this.number = number;
		this.divisor = divisor;
		this.splitCount = 0;
		this.locked = locked;

		nameOverride = "";

		spawnPointOverride = position;
		isSpawnPointOverride = true;
	}
}
