using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class PlayController : GameModeController<PlayController> {

	[Header("Controls")]
	public BoardController boardControl;
	public BlobConnectController connectControl;

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


	}
}
