﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour {

	public float m_fHealth; // Health
	public float m_fCurHealth;

	public bool m_bAttackAvailable; //Can hit enemy?
	public float m_fAttackDmg; //Damage
//	public bool m_bFriendly; //아군?
//	public Color m_colorLine; //공격 이펙트 줄 색
	public bool m_bDestroied;
	public bool m_bEdgePart;
	public int m_iGridIdx;

	public int m_iMove; //턴 당 몇번 움직일 수 있는지

	public DIRECTION m_headingDirection;

	public string m_strNameKey; // TODO : Localize
	public string m_strExplainKey;

	public float m_fOriginEmissionRate;

	void Start()
	{
		m_fOriginEmissionRate = GetComponent<SpriteParticleEmitter.DynamicEmitter> ().EmissionRate;
		m_fCurHealth = m_fHealth;

		StartCoroutine (Heal ());
	}

	public void SetDirection()
	{
		switch (m_headingDirection) {
		case DIRECTION.UP:
			transform.localRotation = Quaternion.AngleAxis(180f, Vector3.forward);
			break;
			
		case DIRECTION.LEFT:
			transform.localRotation = Quaternion.AngleAxis(270f, Vector3.forward);
			break;
			
		case DIRECTION.RIGHT:
			transform.localRotation = Quaternion.AngleAxis(90f, Vector3.forward);
			break;
		}
	}

	void OnDestroy()
	{
		if (transform.parent.name.Equals ("Player")) {

		}
	}

	public Coroutine AssembleRoutine;

	public void StopAssemble()
	{
		bStopAssemble = true;
	}

	public IEnumerator OnField()
	{
		Vector2 mousePosition = Vector2.zero;
		BoxCollider2D collider2D = GetComponent<BoxCollider2D> ();

		mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		
		yield return new WaitForSeconds(1f);
		
		Vector3 corePos = GameObject.Find("Core").transform.position;
		iTween.MoveTo(gameObject, iTween.Hash("x", corePos.x, "y", corePos.y, "time" , 0.5f, "easetype", "easeInSine"));
		
		yield return new WaitForSeconds(0.55f);
		
		BattleSceneMgr.getInstance.m_iMeat += 1;
		
		Destroy(gameObject);
		
		yield return null;
	}

	bool bStopAssemble = false;
	GameObject m_StickedPart = null;
	public IEnumerator Assemble()
	{
		if (gameObject.name.Equals ("Core"))
			yield break;

		Vector2 mousePosition = Vector2.zero;
		BoxCollider2D collider2D = GetComponent<BoxCollider2D> ();
		BoxCollider2D morgueCollider2D = GameObject.Find ("Morgue").GetComponent<BoxCollider2D> ();

		bool bWasPoop = false;
		BoxCollider2D PoopColldier2D = GameObject.Find ("Poop").GetComponent<BoxCollider2D> ();
		Vector3 OriginPos = transform.position;
		
		Core core = GameObject.Find ("Core").GetComponent<Core> ();
		core.CalculateStickableSeat (false);
		bool bFollowCursor = false;
		bool bParentWasCore = false;
		int iBeforeSeatIdx = -1;
		bStopAssemble = false;
//		int[] morgueIdxArr = Morgue.getInstance.m_iMorgueIdxArr;
		int curGridIdx = 0;

		GameObject PartBorder = GameObject.Find ("PartBorder").gameObject;
		
		GridMgr grid = GridMgr.getInstance;
		DIRECTION m_BeforeheadingDirection = DIRECTION.EVERYWHERE;
		BattleSceneMgr battleSceneMgr = BattleSceneMgr.getInstance;

		do{
			if(!battleSceneMgr.m_mouseState.Equals(MOUSE_STATE.NORMAL))
			{
				yield return null;
				continue;
			}

			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if(Input.GetMouseButtonDown(0) && collider2D.OverlapPoint(mousePosition))
			{
				OriginPos = transform.position;
				bFollowCursor = true;

				Morgue.getInstance.SelectPart(this);
				PartBorder.GetComponent<SpriteRenderer>().enabled = false;
				transform.localScale = new Vector3(1f, 1f, 1f);

				if(transform.parent.name.Equals("Player"))
				{
					transform.parent = GameObject.Find("Temp").transform;
					iBeforeSeatIdx = grid.GetGridIdx(transform.position);
					bParentWasCore = true;
				}

				if(bParentWasCore)
				{
					m_BeforeheadingDirection = m_headingDirection;
					GetComponent<SpriteSheet>().CheckAround(false, iBeforeSeatIdx);
				}

				GetComponent<SpriteRenderer>().sortingLayerName = "FrontObject";
				GetComponent<ParticleSystemRenderer>().sortingLayerName = "FrontObject_Particle";

				GetComponent<SpriteParticleEmitter.DynamicEmitter>().enabled = false;
				GetComponent<SpriteRenderer>().color = Color.white;
				
				core.CalculateStickableSeat (true);
			}
			
			if(bFollowCursor && Input.GetMouseButton(0)) //클릭시 따라다니게
			{
				transform.position = mousePosition;
				curGridIdx = grid.GetGridIdx(transform.position);

				for(int i = 0 ; i < core.m_StickAvailableSeat.Count; ++i)
				{
					if(core.m_StickAvailableSeat[i].Equals(curGridIdx)) //드래그 중 붙을 수 있는 지역안에 들어옴
					{
						transform.position = grid.GetPosOfIdx(core.m_StickAvailableSeat[i]);

						//붙는방향으로 파츠 회전하도록
						Transform PlayerTrans = GameObject.Find("Player").transform;
						GameObject ClosestPart = null;

						for(int j=0 ; j < PlayerTrans.childCount; ++j)
						{
							GameObject target;
							target = PlayerTrans.GetChild(j).gameObject;

							if(ClosestPart == null)
								ClosestPart = target;

							if(!target.GetComponent<Part>().m_bEdgePart && Vector3.Distance(mousePosition , ClosestPart.transform.position) > Vector3.Distance(mousePosition, target.transform.position))
							{
								ClosestPart = target;
							}
						}

						int iIdx = grid.GetGridIdx(transform.position);
						int iTargetIdx = grid.GetGridIdx(ClosestPart.transform.position);

						if(iIdx + 1 == iTargetIdx){
							m_headingDirection = DIRECTION.LEFT;
							if(m_bEdgePart && gameObject.name != "Head")
								iTween.RotateTo(gameObject, iTween.Hash ("z", 270f, "time", 0.2f));
							else
								iTween.RotateTo(gameObject, iTween.Hash ("z", 90f, "time", 0.2f));
						}else if(iIdx - 1 == iTargetIdx){
							m_headingDirection = DIRECTION.RIGHT;
							if(m_bEdgePart && gameObject.name != "Head")
								iTween.RotateTo(gameObject, iTween.Hash ("z", 90f, "time", 0.2f));
							else
								iTween.RotateTo(gameObject, iTween.Hash ("z", 270f, "time", 0.2f));
						}else if(iIdx - grid.m_iXcount == iTargetIdx){
							m_headingDirection = DIRECTION.DOWN;
							if(m_bEdgePart && gameObject.name != "Head")
								iTween.RotateTo(gameObject, iTween.Hash ("z", 0f, "time", 0.2f));
							else
								iTween.RotateTo(gameObject, iTween.Hash ("z", 180f, "time", 0.2f));
						}else if(iIdx + grid.m_iXcount == iTargetIdx){
							m_headingDirection = DIRECTION.UP;
							if(m_bEdgePart && gameObject.name != "Head")
								iTween.RotateTo(gameObject, iTween.Hash ("z", 180f, "time", 0.2f));
							else
								iTween.RotateTo(gameObject, iTween.Hash ("z", 0f, "time", 0.2f));
						}
					}
				}

				if(morgueCollider2D.OverlapPoint(mousePosition)) // get in morgue
				{
					if(Morgue.getInstance.GetIdxFromPos(mousePosition) != -1 && !Morgue.getInstance.m_bBodyArr[Morgue.getInstance.GetIdxFromPos(mousePosition)])
					{
						transform.position = Morgue.getInstance.GetIdxPos(Morgue.getInstance.GetIdxFromPos(mousePosition));
						iTween.RotateTo(gameObject, iTween.Hash ("z", 0f, "time", 0.2f));
					}
				}
				 
				if(PoopColldier2D.OverlapPoint(mousePosition)) // get in poop
				{
					GetComponent<SpriteRenderer>().color = new Color(1,0,0,0.5f);
					bWasPoop = true;
				}else{
					if(bWasPoop)
					{
						GetComponent<SpriteRenderer>().color = new Color(1,1,1,1f);
						bWasPoop = false;
					}
				}

//				for(int i = 0; i < morgueIdxArr.Length; ++i)
//				{
//					if(morgueIdxArr[i].Equals(curGridIdx) && (!Morgue.getInstance.m_bBodyArr[i] || curGridIdx.Equals(grid.GetGridIdx(OriginPos)))){ //드래그중 비어있는 시체창고 안에 들어옴
//						transform.position = grid.GetPosOfIdx(morgueIdxArr[i]);
//						iTween.RotateTo(gameObject, iTween.Hash ("z", 0f, "time", 0.2f));
//					}
//				}

				if(m_objAleart != null)
					Destroy(m_objAleart);
			}
			
			if(bFollowCursor && Input.GetMouseButtonUp(0))//클릭 뗏을때
			{
				bool bToOrigin = true;

				iTween.Stop(gameObject);

				for(int i = 0 ; i < core.m_StickAvailableSeat.Count; ++i)
				{
					if(core.m_StickAvailableSeat[i].Equals(curGridIdx)) // Stick!!!!!
					{
						transform.position = grid.GetPosOfIdx(core.m_StickAvailableSeat[i]);
						bToOrigin = false;
						transform.parent = GameObject.Find("Player").transform;
						m_iGridIdx = core.m_StickAvailableSeat[i];
						StartCoroutine(Debug_AStarLine());

						if(!m_bEdgePart)
							transform.localRotation = Quaternion.AngleAxis(0, Vector3.forward);

						if(GetComponent<FSM_Freindly>() == null)
							gameObject.AddComponent<FSM_Freindly>();

						if(bParentWasCore)
						{
							m_iGridIdx = core.m_StickAvailableSeat[i];

							if(!m_bEdgePart)
								m_headingDirection = DIRECTION.EVERYWHERE;

							GetComponent<SpriteSheet>().CheckAround(false, iBeforeSeatIdx);
							iBeforeSeatIdx = -1;
							bParentWasCore = false;

							transform.parent.BroadcastMessage("AmI_InCoreSide");
						}else{
							Morgue.getInstance.RemoveBody(OriginPos);
						}

						GetComponent<SpriteSheet>().CheckAround(false);
						GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
						GetComponent<ParticleSystemRenderer>().sortingLayerName = "Object_Particle";

						OriginPos = transform.position;

						GetComponent<SpriteRenderer>().color = Color.red;
						GetComponent<SpriteParticleEmitter.DynamicEmitter>().enabled = true;
					}
				}

				if(morgueCollider2D.OverlapPoint(mousePosition)) // get in morgue
				{
					if(Morgue.getInstance.GetIdxFromPos(mousePosition) != -1 && !Morgue.getInstance.m_bBodyArr[Morgue.getInstance.GetIdxFromPos(mousePosition)])
					{
						transform.position = Morgue.getInstance.GetIdxPos(Morgue.getInstance.GetIdxFromPos(mousePosition));
						bToOrigin = false;
						transform.localRotation = Quaternion.AngleAxis(0, Vector3.forward);

						if(!bParentWasCore)
							Morgue.getInstance.RemoveBody(OriginPos);
						else{
							GetComponent<SpriteSheet>().CheckAround(false, iBeforeSeatIdx);
							iBeforeSeatIdx = -1;
							bParentWasCore = false;

							GameObject.Find("Player").BroadcastMessage("AmI_InCoreSide");
						}

						Morgue.getInstance.AddBody(true, gameObject, curGridIdx);

						GetComponent<SpriteRenderer>().sortingLayerName = "DeadBodies";
						GetComponent<ParticleSystemRenderer>().sortingLayerName = "DeadBodies_Particle";



						OriginPos = transform.position;
						transform.parent = GameObject.Find("Morgue").transform;
					}
				}

				if(PoopColldier2D.OverlapPoint(mousePosition)) // get in poop
				{
					Morgue.getInstance.DeselectPart();
					Morgue.getInstance.RemoveBody(OriginPos);
					core.CalculateStickableSeat (false);
					Destroy(gameObject);
					ObjectFactory.getInstance.Create_Poop();
				}

//				for(int i = 0; i < morgueIdxArr.Length; ++i) // get in morgue
//				{
//					if(morgueIdxArr[i].Equals(curGridIdx) && (!Morgue.getInstance.m_bBodyArr[i] || curGridIdx.Equals(grid.GetGridIdx(OriginPos)))){ 
//						transform.position = grid.GetPosOfIdx(morgueIdxArr[i]);
//						bToOrigin = false;
//						transform.localRotation = Quaternion.AngleAxis(0, Vector3.forward);
//
//						if(!bParentWasCore)
//							Morgue.getInstance.RemoveBody(grid.GetGridIdx(OriginPos));
//						else{
//							GetComponent<SpriteSheet>().CheckAround(false, iBeforeSeatIdx);
//							iBeforeSeatIdx = -1;
//							bParentWasCore = false;
//
//							GameObject.Find("Core").BroadcastMessage("AmI_InCoreSide");
//						}
//
//						Morgue.getInstance.AddBody(true, gameObject, curGridIdx);
//
//						GetComponent<SpriteRenderer>().sortingLayerName = "DeadBodies";
//						GetComponent<ParticleSystemRenderer>().sortingLayerName = "DeadBodies_Particle";
//
//
//
//						OriginPos = transform.position;
//						transform.parent = GameObject.Find("Morgue").transform;
//					}
//				}
				
				if(bToOrigin)
				{
					iTween.MoveTo (gameObject, iTween.Hash ("x", OriginPos.x, "y", OriginPos.y, "islocal", false, "time", 0.05f, "easetype", "easeInSine"));
					if(!bParentWasCore) iTween.RotateTo(gameObject, iTween.Hash ("z", 0f, "time", 0.1f));
					yield return new WaitForSeconds(0.12f);

					if(bParentWasCore)
					{
						transform.parent = GameObject.Find("Player").transform;

						transform.parent.BroadcastMessage("AmI_InCoreSide");
						GetComponent<SpriteRenderer>().sortingLayerName = "Objects";
						GetComponent<ParticleSystemRenderer>().sortingLayerName = "Objects_Particle";
					}else
						GetComponent<SpriteRenderer>().sortingLayerName = "DeadBodies";
					GetComponent<ParticleSystemRenderer>().sortingLayerName = "DeadBodies_Particle";

					m_headingDirection = m_BeforeheadingDirection;
					GetComponent<SpriteSheet>().CheckAround(false);
				}
				
				core.CalculateStickableSeat (false);
				
				bFollowCursor = false;

				PartBorder.GetComponent<SpriteRenderer>().enabled = true;
				PartBorder.transform.position = OriginPos;
			}
			
			yield return null;
		}while(!bStopAssemble);
	}

	IEnumerator Debug_AStarLine()
	{
		Vector2 mousePosition = Vector2.zero;
		BoxCollider2D collider2D = GetComponent<BoxCollider2D> ();
		int coreIdx = GridMgr.getInstance.GetGridIdx (GameObject.Find ("Core").transform.position);
		int partIdx = GridMgr.getInstance.GetGridIdx (transform.position);

		do {
			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			if(collider2D.OverlapPoint(mousePosition))
			{
				if(AStar.getInstance.AStarStart_CoreFind(partIdx, coreIdx, true))
				{
					yield return new WaitForSeconds(0.08f);
					continue;
				}
			}

			yield return null;

		} while(transform.parent.name.Equals("Player"));
	}

	public void HealCheck()
	{
		StartCoroutine (Heal ());
	}

	protected IEnumerator Heal()
	{
		Vector3 mousePosition;
		BoxCollider2D collider2D = GetComponent<BoxCollider2D> ();
		BattleSceneMgr battleSceneMgr = BattleSceneMgr.getInstance;
		bool bHighlighted = false;

		do{
			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			if(!bHighlighted && collider2D.OverlapPoint(mousePosition))
			{
				bHighlighted = true;
				iTween.ScaleTo(gameObject, iTween.Hash("x", 1.3f, "y", 1.3f, "time" , 0.1f, "easetype", "easeInSine"));
			}else if(bHighlighted && !collider2D.OverlapPoint(mousePosition)){
				bHighlighted = false;
				iTween.ScaleTo(gameObject, iTween.Hash("x", 1f, "y", 1f, "time" , 0.1f, "easetype", "easeInSine"));
			}

			if(Input.GetMouseButtonDown(0) && collider2D.OverlapPoint(mousePosition))
			{
				if(m_fCurHealth < m_fHealth && battleSceneMgr.m_iMeat > 0)
				{
					battleSceneMgr.m_iMeat -= 1;
					m_fCurHealth += 1;
					AdjustEmissionRate();
				}
			}

			yield return null;

		}while(battleSceneMgr.m_mouseState == MOUSE_STATE.HEAL);

		if (bHighlighted) {
			bHighlighted = false;
			iTween.ScaleTo(gameObject, iTween.Hash("x", 1f, "y", 1f, "time" , 0.1f, "easetype", "easeInSine"));
		}
	}

	IEnumerator ResetRotation()
	{
		yield return null;
		yield return null;
		yield return null;

		transform.localRotation = Quaternion.AngleAxis (0f, Vector3.forward);
	}

	public void AdjustEmissionRate()
	{
		if (transform.parent.GetComponent<FSM_Enemy> () != null) { // Enemy
			Unit unit = transform.parent.GetComponent<Unit>();
			if(unit.m_fCurHealth == unit.m_fHealth)
				GetComponent<SpriteParticleEmitter.DynamicEmitter>().EmissionRate = m_fOriginEmissionRate;
			else
				GetComponent<SpriteParticleEmitter.DynamicEmitter>().EmissionRate = (int)((unit.m_fCurHealth / unit.m_fHealth) * m_fOriginEmissionRate / 3f);
		}else
			GetComponent<SpriteParticleEmitter.DynamicEmitter>().EmissionRate = (m_fCurHealth / m_fHealth) * m_fOriginEmissionRate;
	}

	public GameObject m_objAleart;
	void AmI_InCoreSide()
	{
		int iStart = GridMgr.getInstance.GetGridIdx (transform.position);
		int iEnd = GridMgr.getInstance.GetGridIdx (GameObject.Find("Core").transform.position);

		///전부 닫혔는지 체크, 닫혔으면 느낌표
//		bool bAllClosed = true;
//		bool[] m_bOpenedDir = GetComponent<SpriteSheet> ().m_bOpenedDir;
//		
//		for(int i = 0; i < 4; ++i)
//		{
//			if(m_bOpenedDir[i])
//			{
//				bAllClosed = false;
//				break;
//			}
//		}

		if (!AStar.getInstance.AStarStart_CoreFind (iStart, iEnd)) {
			if(m_objAleart == null)
				m_objAleart = ObjectFactory.getInstance.Create_Aleart (iStart);
			else
				m_objAleart.transform.position = transform.position;
		} else {
			if(m_objAleart != null)
				Destroy(m_objAleart);
		}

	}

	void DestroyPart_WhenPathBreaked()
	{
		int iStart = GridMgr.getInstance.GetGridIdx (transform.position);
		int iEnd = GridMgr.getInstance.GetGridIdx (GameObject.Find("Core").transform.position);

		if (!AStar.getInstance.AStarStart_CoreFind (iStart, iEnd)) {//Breaked Path -> Disabled

			GetComponent<FSM_Freindly>().m_AiState = AI_STATE.DISABLED;
			GetComponent<SpriteParticleEmitter.DynamicEmitter>().enabled = false;
			GetComponent<SpriteRenderer>().color = Color.gray;

		} else {//CoreSide


		}
	}

	public void DestroyThis()
	{
		Destroy (gameObject);
	}

	public void PartDestroyed()
	{
		m_bDestroied = true;

		Transform FieldTrans = GameObject.Find ("Field").transform;

		Destroy (GetComponent<FSM_Freindly> ());
		transform.localRotation = Quaternion.AngleAxis (0, Vector3.forward);

		GetComponent<SpriteRenderer>().color = Color.white;

		GetComponent<Rigidbody2D>().AddTorque(Random.Range(0f, 30f));
		GetComponent<SpriteRenderer>().color = new Color(160/255f,160/255f,160/255f);
		
		StartCoroutine(ChangeParentToField(gameObject));
	}

	IEnumerator ChangeParentToField(GameObject target)
	{
		yield return null;
		target.transform.parent = GameObject.Find ("Field").transform;
		BattleSceneMgr.getInstance.OnField(target);

		GameObject.Find ("Player").BroadcastMessage ("DestroyPart_WhenPathBreaked");
		GetComponent<SpriteSheet>().CheckAround(false);
		GetComponent<SpriteRenderer>().sprite = ObjectFactory.getInstance.m_sprite_meat;
	}

//	void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
//	{
//		GameObject myLine = new GameObject();
//		myLine.transform.position = start;
//		myLine.AddComponent<LineRenderer>();
//		LineRenderer lr = myLine.GetComponent<LineRenderer>();
//		lr.material = new Material(Shader.Find("Sprites/Default"));
//		lr.SetColors(color, color);
//		lr.SetWidth(0.05f, 0.05f);
//		lr.SetPosition(0, start);
//		lr.SetPosition(1, end);
//		lr.sortingLayerName = "Objects";
//		GameObject.Destroy(myLine, duration);
//	}
}
