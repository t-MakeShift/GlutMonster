﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSceneSetter : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GameObject.Find ("Player").GetComponent<Player> ().BattleScene ();
		UnityEngine.SceneManagement.SceneManager.SetActiveScene (UnityEngine.SceneManagement.SceneManager.GetSceneByName("Battle"));
		GridMgr.getInstance.ChgGridInfo ();

		if (GameMgr.getInstance.m_bAssembleOnly)
			StartCoroutine (BattleSceneMgr.getInstance.NightTurn ());
		else {
			StartCoroutine(BattleSceneMgr.getInstance.DayTurn());
		}

		HealthBarSet ();
		InitLight ();
	}

	public void ToWorldMap()
	{
		StartCoroutine (ToWorldMap_Coroutine ());
	}

	IEnumerator ToWorldMap_Coroutine()
	{
		//Clean Morgue
		Transform morgueTrans = GameObject.Find ("Morgue").transform;
		for (int i = 0; i < morgueTrans.childCount; ++i) {
			if(morgueTrans.GetChild(i).gameObject.name != "Poop")
			{
				ObjectFactory.getInstance.Create_Poop();
				yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
			}
		}


		morgueTrans.gameObject.BroadcastMessage("DestroyThis", SendMessageOptions.DontRequireReceiver);
		GameObject.Find("Morgue").GetComponent<Morgue>().ClearMorgue ();
		Transform WorldTrans = GameObject.Find ("World").transform;
		for (int i = 0; i < WorldTrans.childCount; ++i) {
			WorldTrans.GetChild (i).gameObject.SetActive (true);
		}
		UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("Battle");
	}

	void HealthBarSet()
	{
		Transform PlayerTrans = GameObject.Find ("Player").transform;
		ObjectFactory objFac = ObjectFactory.getInstance;

		for (int i = 0; i < PlayerTrans.childCount; ++i) {
			if (PlayerTrans.GetChild (i).GetComponent<Part> ().m_objHealthBar == null) {
				objFac.Create_HealthBar (PlayerTrans.GetChild (i).gameObject);
			}
		}
	}

	void InitLight()
	{
		Light m_sunLight = GameObject.Find ("Sun").GetComponent<Light> ();

		switch ((int)TimeMgr.getInstance.m_fHour) {
		case 0:
			m_sunLight.color = Color.white;
			m_sunLight.intensity = 0f;
			break;

		case 6:
			m_sunLight.color = Color.white;
			m_sunLight.intensity = 0.75f;
			break;

		case 12:
			m_sunLight.color = Color.white;
			m_sunLight.intensity = 1.2f;
			break;

		case 18:
			m_sunLight.color = new Color (255 / 255f, 168 / 255f, 0 / 255f);
			m_sunLight.intensity = 1f;
			break;
		}
	}
}
