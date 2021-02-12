using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using Datamodel;
using DM = Datamodel.Datamodel;
using VMAP;

namespace VMAP
{
    //represents a CMapGroup
    public class CMapGroup : MapObject
    {
        public CMapGroup (Element element, VMAPImportContext context) : base(element, context) { }

        public override void Import () {
            obj.name = "group " + element.ID.ToString();
        }
    }
}