using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace ChargeableJailbird.Behaviours
{
    public class ChargeableJailbird : GrabbableObject
    {
        public int shovelHitForce = 3;

        public bool reelingUp;

        public bool isHoldingButton;

        private RaycastHit rayHit;

        private Coroutine reelingUpCoroutine;

        private RaycastHit[] objectsHitByShovel;

        private List<RaycastHit> objectsHitByShovelList = new List<RaycastHit>();

        public AudioClip reelUp;

        public AudioClip swing;

        public AudioClip[] hitSFX;

        public AudioSource jailbirdAudio;

        private PlayerControllerB previousPlayerHeldBy;

        private int shovelMask = 1084754248;

        private GameObject blueGlow;

        public override void Start()
        {
            base.Start();
            blueGlow = transform.Find("jailbird/Mesh_Jailbird/blue_glow").gameObject;
            insertedBattery.charge = 1;
            Plugin.Logger.LogMessage("new jailbird created");
        }
        public override void GrabItem()
        {
            base.GrabItem();
            if (insertedBattery.charge == 0.5)
            {
                insertedBattery.charge = 0.001f;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (playerHeldBy == null)
            {
                return;
            }
            isHoldingButton = buttonDown;
            if (!reelingUp && buttonDown)
            {
                reelingUp = true;
                previousPlayerHeldBy = playerHeldBy;
                if (reelingUpCoroutine != null)
                {
                    StopCoroutine(reelingUpCoroutine);
                }
                reelingUpCoroutine = StartCoroutine(reelUpShovel());
            }
        }

        private IEnumerator reelUpShovel()
        {
            playerHeldBy.activatingItem = true;
            playerHeldBy.twoHanded = true;
            playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
            playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
            jailbirdAudio.PlayOneShot(reelUp);
            ReelUpSFXServerRpc();
            yield return new WaitForSeconds(0.35f);
            yield return new WaitUntil(() => !isHoldingButton || !isHeld);
            SwingShovel(!isHeld);
            yield return new WaitForSeconds(0.13f);
            yield return new WaitForEndOfFrame();
            HitShovel(!isHeld);
            yield return new WaitForSeconds(0.3f);
            reelingUp = false;
            reelingUpCoroutine = null;
        }

        public override void DiscardItem()
        {
            if (playerHeldBy != null)
            {
                playerHeldBy.activatingItem = false;
            }
            if (insertedBattery.charge < 0.5f)
            {
                insertedBattery.charge = 0.5f;
            }
            base.DiscardItem();
        }

        public void SwingShovel(bool cancel = false)
        {
            previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
            if (!cancel)
            {
                jailbirdAudio.PlayOneShot(swing);
                previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
            }
        }

        public void HitShovel(bool cancel = false)
        {
            if (previousPlayerHeldBy == null)
            {
                Debug.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
                return;
            }
            previousPlayerHeldBy.activatingItem = false;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            int num = -1;
            if (!cancel)
            {
                previousPlayerHeldBy.twoHanded = false;
                objectsHitByShovel = Physics.SphereCastAll(previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 0.8f, previousPlayerHeldBy.gameplayCamera.transform.forward, 1.5f, shovelMask, QueryTriggerInteraction.Collide);
                objectsHitByShovelList = objectsHitByShovel.OrderBy((RaycastHit x) => x.distance).ToList();
                List<EnemyAI> enemiesHitList = new List<EnemyAI>();
                for (int i = 0; i < objectsHitByShovelList.Count; i++)
                {
                    if (objectsHitByShovelList[i].transform.gameObject.layer == 8 || objectsHitByShovelList[i].transform.gameObject.layer == 11)
                    {
                        if (objectsHitByShovelList[i].collider.isTrigger)
                        {
                            continue;
                        }
                        flag = true;
                        string text = objectsHitByShovelList[i].collider.gameObject.tag;
                        for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                        {
                            if (StartOfRound.Instance.footstepSurfaces[j].surfaceTag == text)
                            {
                                num = j;
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (!objectsHitByShovelList[i].transform.TryGetComponent<IHittable>(out var component) || objectsHitByShovelList[i].transform == previousPlayerHeldBy.transform || (!(objectsHitByShovelList[i].point == Vector3.zero) && Physics.Linecast(previousPlayerHeldBy.gameplayCamera.transform.position, objectsHitByShovelList[i].point, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
                        {
                            continue;
                        }
                        flag = true;
                        Vector3 forward = previousPlayerHeldBy.gameplayCamera.transform.forward;
                        try
                        {
                            EnemyAICollisionDetect component2 = objectsHitByShovelList[i].transform.GetComponent<EnemyAICollisionDetect>();
                            if (component2 != null)
                            {
                                if (!(component2.mainScript == null) && !enemiesHitList.Contains(component2.mainScript))
                                {
                                    goto IL_02ff;
                                }
                                continue;
                            }
                            if (!(objectsHitByShovelList[i].transform.GetComponent<PlayerControllerB>() != null))
                            {
                                goto IL_02ff;
                            }
                            if (!flag3)
                            {
                                flag3 = true;
                                goto IL_02ff;
                            }
                            goto end_IL_0288;
                        IL_02ff:
                            bool flag4 = component.Hit(shovelHitForce, forward, previousPlayerHeldBy, playHitSFX: true, 1);
                            if (flag4 && component2 != null)
                            {
                                enemiesHitList.Add(component2.mainScript);
                                if (shovelHitForce == 3) { StunEnemiesServerRpc(component2.mainScript.NetworkObjectId); }
                                HitEnemyWithShovelServerRpc();
                            }
                            if (!flag2)
                            {
                                flag2 = flag4;
                            }
                        end_IL_0288:;
                        }
                        catch (Exception arg)
                        {
                            Debug.Log($"Exception caught when hitting object with shovel from player #{previousPlayerHeldBy.playerClientId}: {arg}");
                        }
                    }
                }
            }
            if (flag)
            {
                RoundManager.PlayRandomClip(jailbirdAudio, hitSFX);
                UnityEngine.Object.FindObjectOfType<RoundManager>().PlayAudibleNoise(base.transform.position, 17f, 0.8f);
                if (!flag2 && num != -1)
                {
                    jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
                    WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[num].hitSurfaceSFX);
                }
                playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
                HitShovelServerRpc(num);
            }
        }

        private void HitSurfaceWithShovel(int hitSurfaceID)
        {
            try
            {
                jailbirdAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
                WalkieTalkie.TransmitOneShotAudio(jailbirdAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        public void OnJailbirdHit()
        {
            if (shovelHitForce == 3)
            {
                insertedBattery.charge = 0.001f;
                shovelHitForce = 1;
                blueGlow.SetActive(false);
            }
        }
        public override void ChargeBatteries()
        {
            base.ChargeBatteries();
            if (insertedBattery.charge == 1)
            {
                shovelHitForce = 3;
                blueGlow.SetActive(true);
            }
        }
        public void TryStunEnemy(ulong enemyID)
        {
            RoundManager.Instance.RefreshEnemiesList();
            foreach (EnemyAI spawnedEnemy in RoundManager.Instance.SpawnedEnemies)
            {
                if (spawnedEnemy.NetworkObjectId == enemyID)
                {
                    Plugin.Logger.LogMessage($"found enemy with id {enemyID}");
                    spawnedEnemy.SetEnemyStunned(true, 4.5f);
                    return;
                }
            }
            Plugin.Logger.LogError($"could not find enemy with id {enemyID}");
        }

        #region jailbirdHitRPCs
        [ServerRpc]
        public void HitEnemyWithShovelServerRpc()
        {
            HitEnemyWithShovelClientRpc();
        }

        [ClientRpc]
        public void HitEnemyWithShovelClientRpc()
        {
            OnJailbirdHit();
        }
        #endregion
        #region surfaceHitRPCs
        [ServerRpc]
        public void HitShovelServerRpc(int hitSurfaceID)
        {
            HitShovelClientRpc(hitSurfaceID);
        }

        [ClientRpc]
        public void HitShovelClientRpc(int hitSurfaceID)
        {
            HitSurfaceWithShovel(hitSurfaceID);
        }
        #endregion
        #region reelupSFXRPCs
        [ServerRpc]
        public void ReelUpSFXServerRpc()
        {
            ReelUpSFXClientRpc();
        }

        [ClientRpc]
        public void ReelUpSFXClientRpc()
        {
            jailbirdAudio.PlayOneShot(reelUp);
        }
        #endregion
        #region stunenemiesRPCs
        [ServerRpc]
        public void StunEnemiesServerRpc(ulong enemyID)
        {
            StunEnemiesClientRpc(enemyID);
        }
        [ClientRpc]
        public void StunEnemiesClientRpc(ulong enemyID)
        {
            TryStunEnemy(enemyID);
        }
        #endregion
    }
}
