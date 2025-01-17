﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using UnityEngine.UI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;
using MLAPI.NetworkVariable.Collections;

public class PlayerStats : NetworkBehaviour
{
    public Slider healthbar;
    private float maxHp = 100f;
    private NetworkVariableFloat currentHP = new NetworkVariableFloat(100f);
    private float damgeVal = 20f;
    public GameObject deathScreen;
    public bool playerDied = false;
    public GameObject timerObject;
    public Text timeCount;
    public float timer;
    public int waitingTime;
    public GameObject gunObject;
    public GameObject playerHealthBar;
    
    public NetworkVariableInt kills = new NetworkVariableInt(0);
    public NetworkVariableInt deaths = new NetworkVariableInt(0);
    // Update is called once per frame
    void Update()
    {
        GetComponent<CharacterController>().enabled = true;
        GetComponent<CapsuleCollider>().enabled = true;
        GetComponent<MeshRenderer>().enabled = true;
        GetComponent<Mp_BulletSpawner>().enabled = true;
        gunObject.SetActive(true);
        playerHealthBar.SetActive(true);
        timerObject.SetActive(false);
        healthbar.value = currentHP.Value / maxHp;
        if(currentHP.Value < 0)
        {
            if(IsOwner){
                timerObject.SetActive(true);
                GetComponent<CharacterController>().enabled = false;
                GetComponent<CapsuleCollider>().enabled = false;
                GetComponent<MeshRenderer>().enabled = false;
                GetComponent<Mp_BulletSpawner>().enabled = false;
                gunObject.SetActive(false);
                playerHealthBar.SetActive(false);
            }

            else{
                timerObject.SetActive(false);
            }
            
            timer += Time.deltaTime;
            string minutes = ((int) timer/ 60).ToString();
            string seconds = (timer % 60).ToString("f2"); 
            timeCount.text = minutes + "." + seconds;
            if (timer > waitingTime)
            {
                timer = 0;
                Debug.Log("Respawn");
                timerObject.SetActive(false); 
                RespawnPlayerServerRpc();
                ResetPlayerClientRpc();
                if(IsOwner)
                {
                    Debug.Log("You died");
                }
            }
           
            
            
            
            //playerDied = true;
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Bullet") && IsOwner)
        {
            if(collision.gameObject.GetComponent<MP_Bullet>().spawnerPlayerId != NetworkManager.Singleton.LocalClientId)
            {

                if(currentHP.Value - damgeVal < 0)
                {
                    IncreaseKillCountServerRpc(collision.gameObject.GetComponent<MP_Bullet>().spawnerPlayerId);
                }

                TakeDamageServerRpc(damgeVal);
                Destroy(collision.gameObject);
            }
        }
        else if(collision.gameObject.CompareTag("Medkit") && IsOwner)
        {
            HealDamageServerRpc();
        }
    }

    [ServerRpc]
    private void TakeDamageServerRpc(float damage, ServerRpcParams svrParams = default)
    {
        currentHP.Value -= damage;
        if(currentHP.Value < 0 && OwnerClientId == svrParams.Receive.SenderClientId)
        {
            deaths.Value++; 
            Debug.Log("You died");

            //playerDied = true;
        }
    }

    [ServerRpc]
    public void HealDamageServerRpc()
    {
        currentHP.Value += 25f;
        if(currentHP.Value > maxHp)
        {
            currentHP.Value = maxHp;
        }
    }

    [ServerRpc]
    private void RespawnPlayerServerRpc()
    {

        currentHP.Value = maxHp;

    }

    [ClientRpc]
    private void ResetPlayerClientRpc()
    {
        
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        int index = UnityEngine.Random.Range(0, spawnPoints.Length);

        GetComponent<CharacterController>().enabled = false;
        transform.position = spawnPoints[index].transform.position;
        GetComponent<CharacterController>().enabled = true;

    }

    [ServerRpc]

    private void IncreaseKillCountServerRpc(ulong spawnerPlayerId)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject playerObj in players)
        {
            if(playerObj.GetComponent<NetworkObject>().OwnerClientId == spawnerPlayerId)
            {
                playerObj.GetComponent<PlayerStats>().kills.Value++;
            }
        }
    }
}
