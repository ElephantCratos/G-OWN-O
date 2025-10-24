using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class ShipSystemController : MonoBehaviour {

        [Header("Tracked Parameters")]
        public ShipSystemParameter[] Parameters;

        public bool AllStable => CheckAllStable();

        public void Update() {
            if (AllStable) {
                Debug.Log("✅ Ship systems stable!");
                // Здесь можешь вызвать, например, анимацию "полет стабилизирован"
            }
        }

        bool CheckAllStable() {
            foreach (var param in Parameters) {
                if (param == null || !param.InNormalRange) {
                    return false;
                }
            }
            return true;
        }
    }
}
