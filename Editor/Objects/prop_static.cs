using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Datamodel;
using DM = Datamodel.Datamodel;

namespace VMAP {
    public class prop_static : MapObject
    {
        public prop_static (Element element, VMAPImportContext context) : base(element, context) { }

        public override void Import () {
            obj.name = "prop_static " + element.ID.ToString();

            string sourcePath = (string)entityData["model"];
            string assetPath = context.propSearchPath + sourcePath.Replace("models/", null).Replace(".vmdl", null) + ".fbx";

            GameObject prop = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prop == null) {
                Debug.LogError("Tried to import unknown prop: " + assetPath);
                return;
            }

            GameObject newProp = GameObject.Instantiate(prop);
            newProp.transform.SetParent(obj.transform);
            newProp.transform.localPosition = Vector3.zero;
            newProp.transform.localRotation = Quaternion.identity;
            newProp.transform.localScale = Vector3.one;

            //Debug.Log("i found a prop_static :)");
        }
    }
}
