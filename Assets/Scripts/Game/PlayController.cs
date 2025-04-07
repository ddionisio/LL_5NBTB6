using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class PlayController : GameModeController<PlayController> {
	[Header("Controls")]
	public BoardController boardControl;
	public BlobConnectController connectControl;

	[Header("Spawn Control")]
	public BlobNumberGenBase numberGenNormal;
	public BlobNumberGenBase numberGenFinal; //if not null, generated after mega blob hp <= 0 for last attack round

	[Header("Music")]
	[M8.MusicPlaylist]
	public string playMusic;

	[Header("Signal Listen")]
	public SignalBlob signalListenBlobClick;
	public SignalBlob signalListenBlobDragBegin;
	public SignalBlob signalListenBlobDragEnd;

	private BlobNumberGenBase mCurNumberGen;

	protected override void OnInstanceDeinit() {


		if(signalListenBlobClick) signalListenBlobClick.callback -= OnSignalBlobClick;
		if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback -= OnSignalBlobDragBegin;
		if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback -= OnSignalBlobDragEnd;

		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();

		var gameDat = GameData.instance;

		//initialize blob pools
		boardControl.InitBlobPool();

		if(numberGenNormal) numberGenNormal.InitBlobPoolTypes(boardControl.blobPool);
		if(numberGenFinal) numberGenFinal.InitBlobPoolTypes(boardControl.blobPool);


		if(signalListenBlobClick) signalListenBlobClick.callback += OnSignalBlobClick;
		if(signalListenBlobDragBegin) signalListenBlobDragBegin.callback += OnSignalBlobDragBegin;
		if(signalListenBlobDragEnd) signalListenBlobDragEnd.callback += OnSignalBlobDragEnd;
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		//intro

		//board enter
		yield return boardControl.PlayReady();

		var isComplete = false;
		var isFinal = false;
		var roundInd = 0;

		mCurNumberGen = numberGenNormal;

		while(!isComplete) {
			yield return null;

			var spawnInfos = mCurNumberGen.GenerateSpawnInfos(roundInd);

			boardControl.Spawn(spawnInfos);

			//wait for expected final quotient count based on number generator
			while(boardControl.isBlobSpawning || boardControl.blobActiveCount > mCurNumberGen.quotientResultCount)
				yield return null;

			//do attack
			yield return boardControl.Attack();

			//determine if we are finish, or there's one last attack needed
			if(isFinal)
				isComplete = true;
			else if(boardControl.hitpoints <= 0) {
				if(numberGenFinal) {
					mCurNumberGen = numberGenFinal;
					isFinal = true;
				}
				else
					isComplete = true;
			}

			roundInd++;
		}

		//board defeat
		yield return boardControl.PlayDefeat();

		//victory
	}

	void OnSignalBlobClick(Blob blob) {
	}

	void OnSignalBlobDragBegin(Blob blob) {
		if(connectControl.isDragDisabled)
			return;

		var blobActives = boardControl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blobActive = blobActives[i];
			if(blobActive == blob)
				continue;

			if(blob.GetConnectOpType(blobActive) != OperatorType.None) {
				blobActive.highlightLock = true;
			}
			else {
				blobActive.inputLocked = true;
			}
		}
	}

	void OnSignalBlobDragEnd(Blob blob) {
		if(connectControl.isDragDisabled)
			return;

		var blobActives = boardControl.blobActives;
		for(int i = 0; i < blobActives.Count; i++) {
			var blobActive = blobActives[i];

			blobActive.highlightLock = false;
			blobActive.inputLocked = false;
		}
	}
}
