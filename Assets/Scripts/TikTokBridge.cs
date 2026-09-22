using UnityEngine;
public class TikTokBridge:MonoBehaviour{
 public void Save(string json){PlayerPrefs.SetString("caravan_save",json);PlayerPrefs.Save();}
 public string Load(){return PlayerPrefs.GetString("caravan_save","");}
 public void RewardedRevive(){Debug.Log("TikTok rewarded ad integration point");}
 public void Interstitial(){Debug.Log("TikTok interstitial integration point");}
 public void Share(){Debug.Log("TikTok share integration point");}
 public void Purchase(string productId){Debug.Log("TikTok IAP integration point: "+productId);}
}