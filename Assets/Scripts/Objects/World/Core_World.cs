﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Com.LuisPedroFonseca.ProCamera2D;

public class Core_World : MonoBehaviour {
	
	public int m_iDestinationIdx;
	public List<int> m_listMoveIdx;

	bool m_bWasMovingBeforeChgedScene = false;

	// Use this for initialization
	void Start()
	{
		m_listMoveIdx = new List<int> ();
		StartCoroutine (Idle ());
	}

	IEnumerator Idle()
	{
		WorldMapManager world = WorldMapManager.getInstance;
		GridMgr grid = GridMgr.getInstance;

		float fMouseTimer = 0f;
		bool bMouseTimerOn = false;
		Vector3 vecMouseClickedPos = Vector3.zero;
		Vector3 mousePosition = Vector3.zero;
		bool bOverviewOn = false;

		do{
			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			if(bMouseTimerOn)
				fMouseTimer += Time.deltaTime;

			if(Input.GetMouseButtonDown(0)){
				fMouseTimer = 0f;
				bMouseTimerOn = true;
				vecMouseClickedPos = mousePosition;
				if(bOverviewOn && UICamera.hoveredObject != GameObject.Find("WorldOverview").gameObject)
				{
					bOverviewOn = false;
					iTween.MoveTo(GameObject.Find("WorldOverview").transform.GetChild(0).gameObject, iTween.Hash("x", 150f, "time", 0.5f,"isLocal", true,  "easetype", "easeInSine"));
				}
			}else if(Input.GetMouseButtonUp(0))
			{
				bMouseTimerOn = false;

				if(Vector3.Distance(vecMouseClickedPos, mousePosition) < 0.025f){
					m_listMoveIdx = AStar.getInstance.AStarStart_World(grid.GetGridIdx(gameObject.transform.position), grid.m_iGridIdx);
					m_iDestinationIdx = grid.m_iGridIdx;
					DrawPath();

					if(m_listMoveIdx.Count != 0) // Select WorldIcon
					{
						GameObject Dest = GameObject.Find("Destination").gameObject;
						Dest.GetComponent<SpriteRenderer>().enabled = true;
						Dest.transform.position = grid.GetPosOfIdx(grid.GetGridIdx(vecMouseClickedPos));

						WorldOverView.getInstance.SelectWorldIcon(GameObject.Find("Geo").transform.GetChild(grid.m_iGridIdx).GetComponent<WorldGeo>());

						bOverviewOn = true;
						iTween.MoveTo(GameObject.Find("WorldOverview").transform.GetChild(0).gameObject, iTween.Hash("x", -150f, "time", 0.5f,"isLocal", true,  "easetype", "easeInSine"));

					}else{
						GameObject Dest = GameObject.Find("Destination").gameObject;
						Dest.GetComponent<SpriteRenderer>().enabled = false;
					}
				}
			}

			yield return null;

		}while(world.m_worldTurnState.Equals(WORLDTURN_STATE.IDLE));

		StartCoroutine (Move());
	}

	public void MoveOrder()
	{
		WorldMapManager.getInstance.m_worldTurnState = WORLDTURN_STATE.MOVE;

		GameObject Dest = GameObject.Find("Destination").gameObject;
		Dest.GetComponent<SpriteRenderer>().enabled = false;

		iTween.MoveTo(GameObject.Find("WorldOverview").transform.GetChild(0).gameObject, iTween.Hash("x", 150f, "time", 0.5f,"isLocal", true,  "easetype", "easeInSine"));

		Transform pathTrans = GameObject.Find ("Path").transform;
		for (int i = 0; i < pathTrans.childCount; ++i) {
			Destroy (pathTrans.GetChild (i).gameObject);
		}
	}

	IEnumerator Move()
	{
		WorldMapManager world = WorldMapManager.getInstance;
		GridMgr grid = GridMgr.getInstance;

		if (m_listMoveIdx.Count > 0) {
			ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [0], 1f, 1f);
			ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [1], 0f, 0f);
		}

		for(int i = 0; i < m_listMoveIdx.Count; ++i)
		{
			if (GameMgr.getInstance.m_iHunger <= 0) {
				GameMgr.getInstance.m_iHunger = 100;
			}

			Vector3 destPos = grid.GetPosOfIdx(m_listMoveIdx[i]);
			iTween.MoveTo(gameObject, iTween.Hash("x", destPos.x, "y", destPos.y, "time", 1f, "easetype", "easeInSine"));
			GameMgr.getInstance.m_iHunger -= 30;

			yield return new WaitForSeconds(1f);

			if(i != m_listMoveIdx.Count - 1)
				m_bWasMovingBeforeChgedScene = true;
			else
				m_bWasMovingBeforeChgedScene = false;

			if(CheckLocationBreak ())
				yield break;
		}

		ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [0], 0f, 0f);
		ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [1], 1f, 1f);
		GameObject.Find ("PC2DPanTarget").transform.position = gameObject.transform.position;

		world.m_worldTurnState = WORLDTURN_STATE.IDLE;
		StartCoroutine (Idle());
	}

	bool CheckLocationBreak()
	{
		int iCoreIdx = GridMgr.getInstance.GetGridIdx (gameObject.transform.position);
		GameObject objIcon = GameObject.Find ("Geo").transform.GetChild (iCoreIdx).GetComponent<WorldGeo> ().m_worldIcon;

		if (objIcon.GetComponent<WorldIcon> ().m_iconType != 0) {
			WorldMapManager.getInstance.EncountEnemy ();
			return true;
		}

		return false;
	}

	void CameBackFromBattleScene()
	{
		if (m_bWasMovingBeforeChgedScene) {
			m_listMoveIdx = AStar.getInstance.AStarStart_World(GridMgr.getInstance.GetGridIdx(gameObject.transform.position), m_iDestinationIdx);
			StartCoroutine (Move ());
		} else {
			ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [0], 0f, 0f);
			ProCamera2D.Instance.AdjustCameraTargetInfluence (ProCamera2D.Instance.CameraTargets [1], 1f, 1f);
			GameObject.Find ("PC2DPanTarget").transform.position = gameObject.transform.position;

			WorldMapManager.getInstance.m_worldTurnState = WORLDTURN_STATE.IDLE;
			StartCoroutine (Idle ());
		}

		StartCoroutine (AdjustCamOffset ());
	}

	IEnumerator AdjustCamOffset()
	{
		yield return new WaitForSeconds (0.1f);

		ProCamera2D.Instance.OffsetX = 0f;
		ProCamera2D.Instance.OffsetY = 0f;
	}

	void DrawPath()
	{
		GridMgr grid = GridMgr.getInstance;

		Transform pathTrans = GameObject.Find ("Path").transform;
		for (int i = 0; i < pathTrans.childCount; ++i) {
			Destroy (pathTrans.GetChild (i).gameObject);
		}

		for (int i = 0; i < m_listMoveIdx.Count; ++i) {
			GameObject myLine = new GameObject ();
			myLine.transform.position = grid.GetPosOfIdx(m_listMoveIdx[i]);
			myLine.AddComponent<LineRenderer> ();
			LineRenderer lr = myLine.GetComponent<LineRenderer> ();
			lr.material = new Material (Shader.Find ("Sprites/Default"));
			lr.SetColors (new Color (119 / 255f, 43 / 255f, 130 / 255f), new Color (119 / 255f, 43 / 255f, 130 / 255f));
			lr.SetWidth (0.05f, 0.05f);
			if (i == 0) {
				lr.SetPosition (0, GameObject.Find("Core").transform.position);
				lr.SetPosition (1, grid.GetPosOfIdx (m_listMoveIdx [i]));
			} else {
				lr.SetPosition (0, grid.GetPosOfIdx (m_listMoveIdx [i - 1]));
				lr.SetPosition (1, grid.GetPosOfIdx (m_listMoveIdx [i]));
			}
			lr.sortingLayerName = "Default";
			lr.sortingOrder = -1;
			lr.transform.parent = GameObject.Find ("Path").transform;
		}
	}
}