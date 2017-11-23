﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : Singleton<WorldGenerator> {
	GridMgr grid;
	Transform m_geoTrans;

	// Use this for initialization
	void Start () {
		grid = GridMgr.getInstance;
		m_geoTrans = GameObject.Find ("Geo").transform;
	}

	public IEnumerator GenerateWorldMap()
	{
		LoadingProgress (0.01f, "Instantiate");
		yield return new WaitForSeconds (0.3f);

		int iCityNum = 20;
		int iCastleNum = 10;

		ObjectFactory objFac = ObjectFactory.getInstance;
		List<int> idxList = new List<int> ();

		//지형생성
		for(int i = 0 ; i < grid.m_iXcount * grid.m_iYcount; ++i){
			objFac.Creat_WorldGeo (grid.GetPosOfIdx(i));
			if (i % 100 == 0) {
				float fProgress = (((float)i / ((float)grid.m_iXcount * (float)grid.m_iYcount)) * 0.3f);
				LoadingProgress (fProgress, string.Format ("Create Geometry ({0}/{1})", i, grid.m_iXcount * grid.m_iYcount));
				yield return new WaitForSeconds (0.001f);
			}
		}

		//아이콘 생성
		for (int i = 0; i < grid.m_iXcount * grid.m_iYcount; ++i) {
			// idx -> x, y
			int x = i % grid.m_iXcount;
			int y = i / grid.m_iXcount;

			//x normalize 클수록 중심으로부터 멀다 
			if (x > grid.m_iXcount / 2)
				x -= grid.m_iXcount/2;
			else
				x = grid.m_iXcount/2 - x;

			//y normalize 클수록 중심으로부터 멀다 
			if (y > grid.m_iYcount / 2)
				y -= grid.m_iYcount/2;
			else
				y = grid.m_iYcount/2 - y;

			float fChance = 1 - (((float)x + (float)y) / ((float)(grid.m_iXcount / 2f) + (float)(grid.m_iYcount / 2f)));

			Debug.Log (GenerateNormalRandom (0.5f, 0.1f));
			if (GenerateNormalRandom(0.5f, 0.1f) < fChance)
				idxList.Add (i);
		}

		if (!idxList.Contains (grid.m_iXcount * grid.m_iYcount / 2))
			idxList.Add (grid.m_iXcount * grid.m_iYcount / 2);

		for (int i = 0; i < iCastleNum; ++i) {
//			int iRandomIdx = idxList[(int)GenerateNormalRandom(idxList.Count/2f, 10f)];
			int iRandomIdx = idxList[Random.Range (0, idxList.Count)];

			GameObject objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (iRandomIdx), (int)WORLDICON_TYPE.CASTLE);
			m_geoTrans.GetChild (iRandomIdx).GetComponent<WorldGeo> ().m_worldIcon = objIcon;
			idxList.Remove (iRandomIdx);
		}

		LoadingProgress (0.3f, "Castle Instantiate");
		yield return new WaitForSeconds(0.1f);

		for (int i = 0; i < iCityNum; ++i) {
			int iRandomIdx = idxList[Random.Range (0, idxList.Count)];

			GameObject objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (iRandomIdx), (int)WORLDICON_TYPE.CITY);
			m_geoTrans.GetChild (iRandomIdx).GetComponent<WorldGeo> ().m_worldIcon = objIcon;
			idxList.Remove (iRandomIdx);
		}

		LoadingProgress (0.4f, "City Instantiate");
		yield return new WaitForSeconds(0.1f);

		for (int i = 0; i < idxList.Count; ++i) {
			int iRandom = Random.Range (0, 100);
			GameObject objIcon = null;

			if(iRandom < 33)
				objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (idxList[i]), (int)WORLDICON_TYPE.FARM);
			else if(iRandom < 66)
				objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (idxList[i]), (int)WORLDICON_TYPE.RANCH);
			else if(iRandom < 88)
				objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (idxList[i]), (int)WORLDICON_TYPE.VILLAGE);
			else
				objIcon = objFac.Create_WorldIcon (grid.GetPosOfIdx (idxList[i]), (int)WORLDICON_TYPE.EMPTY);

			m_geoTrans.GetChild (idxList[i]).GetComponent<WorldGeo> ().m_worldIcon = objIcon;
		}

		LoadingProgress (0.5f, "Icons Instantiate");
		yield return new WaitForSeconds(0.1f);

//		GameObject.Find ("WorldIcons").BroadcastMessage ("CheckAroundAmIAlone");

		Transform icons = GameObject.Find ("WorldIcons").transform;
		for (int i = 0; i < icons.childCount; ++i) {
			icons.GetChild (i).SendMessage ("CheckAroundAmIAlone");
			if (i % 100 == 0) {
				float fProgress = 0.5f + ((float)i / (float)icons.childCount * 0.3f);
				LoadingProgress (fProgress, string.Format ("Check Unreachable Islands ({0}/{1})", i, icons.childCount));
				yield return new WaitForSeconds (0.001f);
			}
		}

		LoadingProgress (0.8f, "Destorying Unreachable Islands");
		icons.BroadcastMessage("DestroyIfIsland");
		yield return new WaitForSeconds (0.5f);

		FloodFill (0,0,WORLD_GEO.WATER);
		FloodFill (grid.m_iXcount-1,0,WORLD_GEO.WATER);
		FloodFill (0,grid.m_iYcount-1,WORLD_GEO.WATER);
		FloodFill (grid.m_iXcount-1,grid.m_iYcount-1,WORLD_GEO.WATER);
		LoadingProgress (0.9f, "Pumping Ocean");
		yield return new WaitForSeconds (0.5f);


		LoadingProgress (1f , "Done");
		yield return new WaitForSeconds (1f);
		GameObject.Find ("LoadingBar").GetComponent<UIPanel> ().alpha = 0f;
		
	}

	float GenerateNormalRandom(float mean, float stdDev) //평균, 표준편차
	{
		float rand1 = Random.Range(0.0f, 1.0f);
		float rand2 = Random.Range(0.0f, 1.0f);

		float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Sin(2.0f * Mathf.PI * rand2); //random normal(0,1)
		float randNormal = mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)

		return randNormal;
	}

	public void LoadingProgress(float fProgress, string strLabel){
		GameObject.Find ("progress").GetComponent<UISlider> ().value = fProgress;
		GameObject.Find ("ProgressLabel").GetComponent<UILabel> ().text = strLabel;
	}
		
	void FloodFill(int iX, int iY, WORLD_GEO geoTarget)
	{
		if ((iX < 0) || (iX >= grid.m_iXcount))
			return;
		if ((iY < 0) || (iY >= grid.m_iYcount))
			return;

		if (m_geoTrans.GetChild (iY * grid.m_iXcount + iX).GetComponent<WorldGeo> ().m_worldIcon != null)
			return;

		if (!m_geoTrans.GetChild (iY * grid.m_iXcount + iX).GetComponent<WorldGeo> ().m_geoStatus.Equals (geoTarget)) {
			m_geoTrans.GetChild (iY * grid.m_iXcount + iX).GetComponent<WorldGeo> ().m_geoStatus = geoTarget;
			m_geoTrans.GetChild (iY * grid.m_iXcount + iX).GetComponent<SpriteRenderer>().sprite = ObjectFactory.getInstance.m_sheet_worldGeo[(int)geoTarget];

			FloodFill (iX+1, iY, geoTarget);
			FloodFill (iX, iY+1, geoTarget);
			FloodFill (iX-1, iY, geoTarget);
			FloodFill (iX, iY-1, geoTarget);
		}
	}
}
