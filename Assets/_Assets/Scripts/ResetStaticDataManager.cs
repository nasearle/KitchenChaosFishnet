using System;
using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour {
    private void Awake() {
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
        PlateKitchenObject.ResetStaticData();
        Player.ResetStaticData();
    }
}
