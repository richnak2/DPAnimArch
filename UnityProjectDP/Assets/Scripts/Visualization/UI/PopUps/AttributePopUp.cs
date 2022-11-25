﻿using UnityEngine;
using TMPro;
using UnityEngine.UI;
using AnimArch.Visualization.Diagrams;

namespace AnimArch.Visualization.UI
{
    public class AttributePopUp : AbstractPopUp
    {
        public TMP_Dropdown dropdown;
        private TMP_Text _atrTxt;
        public Toggle isArray;

        public void ActivateCreation(TMP_Text classTxt, TMP_Text atrTxt)
        {   
            ActivateCreation(classTxt);
            _atrTxt = atrTxt;
        }

        public override void Confirmation()
        {
            if (inp.text == "")
            {
                Deactivate();
                return;
            }
            var atr = new Attribute
            {
                Name = inp.text,
                Type = dropdown.options[dropdown.value].text
            };
            if (DiagramPool.Instance.ClassDiagram.AddAtr(ClassTxt.text, atr))
            {
                _atrTxt.text += atr.Name + (isArray ? "[]: " : ": ") + atr.Type + "\n";
            }

            Deactivate();
        }

        public override void Deactivate()
        {   
            base.Deactivate();
            isArray.isOn = false;
        }
    }
}