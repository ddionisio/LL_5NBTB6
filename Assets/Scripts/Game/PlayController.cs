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

	protected override void OnInstanceDeinit() {
		base.OnInstanceDeinit();
	}

	protected override void OnInstanceInit() {
		base.OnInstanceInit();
	}

	protected override IEnumerator Start() {
		yield return base.Start();

		//intro

		//board enter
		yield return boardControl.PlayReady();

		var isComplete = false;
		var isFinal = false;
		var numberGen = numberGenNormal;
		var roundInd = 0;

		while(!isComplete) {
			yield return null;

			var spawnInfos = numberGen.GenerateSpawnInfos(roundInd);

			boardControl.Spawn(spawnInfos);

			//wait for expected final quotient count based on number generator
			while(boardControl.isBlobSpawning || boardControl.blobActiveCount > numberGen.quotientResultCount)
				yield return null;

			//do attack
			yield return boardControl.Attack();

			//determine if we are finish, or there's one last attack needed
			if(isFinal)
				isComplete = true;
			else if(boardControl.hitpoints <= 0) {
				if(numberGenFinal) {
					numberGen = numberGenFinal;
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
}
