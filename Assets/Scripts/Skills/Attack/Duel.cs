﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  攻击技能-决斗
///  被攻击的坦克只能攻击我方坦克，直到一方被摧毁
/// </summary>
public class Duel : MonoBehaviour
{
    public int Effected = 0;//已触发次数

    //技能预制体
    public GameObject SkillPrefab = null;
    //实例化体
    public GameObject SkillEffect = null;

    //攻击方
    public GameObject FromTank = null;
    //被攻击方
    public GameObject TargetTank = null;


    private void Awake()
    {
        //挂载后的默认状态
        SkillPrefab = CommonHelper.GetPrefabs("skill", "Attack/决斗");

    }
    /// <summary>
    /// 攻击特效
    /// </summary>
    /// <returns></returns>
    public bool EffectAttack()
    {
        GameObject effect = GameObject.Instantiate(SkillPrefab, TargetTank.transform.parent, false);
        effect.SetActive(true);
        GameObject child = effect.transform.Find("fx_magic_lightning_falling_gold").gameObject;
        child.SetActive(true);
        child.transform.GetComponent<ParticleSystem>().Play();

        StartCoroutine(CommonHelper.DelayToInvokeDo(() => { DestroyImmediate(effect); }, 2f));

        Effected++;

        return true;
    }
}
