using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class TooltipData : MonoBehaviour
    {
        public string Title = "Название";
        [TextArea(2, 5)]
        public string Description = "Описание объекта";
        public Sprite Icon;
    }
