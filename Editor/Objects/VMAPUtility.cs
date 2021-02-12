using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using Datamodel;
using DM = Datamodel.Datamodel;

namespace VMAP
{
    public struct VMAPImportContext
    {
        public AssetImportContext assetContext;

        public float scale;
        public string propSearchPath;
        public string materialSearchPath;

        public VMAPImportContext (AssetImportContext assetContext, float scale, string propSearchPath, string materialSearchPath) {
            this.assetContext = assetContext;
            this.scale = scale;
            this.propSearchPath = propSearchPath;
            this.materialSearchPath = materialSearchPath;
        }
    }

    public class MapObject
    {
        //the Datamodel Element that represents this entity.
        public Element element;
        protected VMAPImportContext context;

        public GameObject obj;
        public AttributeList entityData;
        public List<System.Guid> children = new List<System.Guid>();

        public MapObject (Element element, VMAPImportContext context) {
            this.element = element;
            this.context = context;
            InitObj();
        }

        private void InitObj() {
            //init gameobject
            obj = new GameObject();

            //transform
            ResetTransformWorld();

            //init entity data if applicable.
            if (element.ContainsKey("entity_properties"))
                 entityData = (AttributeList)element["entity_properties"];
            else entityData = null;

            //set children
            foreach (Element e in (Array<Element>)element["children"]) {
                children.Add(e.ID);
            }

            //run import method, implemented by classes that inherit this class
            Import();
        }

        public void ResetTransformWorld() {
            System.Numerics.Vector3 origin = (System.Numerics.Vector3)element["origin"];
            System.Numerics.Vector3 angles = (System.Numerics.Vector3)element["angles"];
            System.Numerics.Vector3 scales = (System.Numerics.Vector3)element["scales"];

            obj.transform.position = new Vector3(-origin.Y, origin.Z, origin.X);
            obj.transform.eulerAngles = new Vector3(angles.X, -angles.Y, -angles.Z);
            obj.transform.localScale = new Vector3(scales.Y, scales.Z, scales.X);

            obj.transform.position *= context.scale;
        }

        public void ResetTransformLocal () {
            System.Numerics.Vector3 origin = (System.Numerics.Vector3)element["origin"];
            System.Numerics.Vector3 angles = (System.Numerics.Vector3)element["angles"];
            System.Numerics.Vector3 scales = (System.Numerics.Vector3)element["scales"];

            obj.transform.localPosition = new Vector3(-origin.Y, origin.Z, origin.X);
            obj.transform.localEulerAngles = new Vector3(angles.X, -angles.Y, -angles.Z);
            obj.transform.localScale = new Vector3(scales.Y, scales.Z, scales.X);

            obj.transform.localPosition *= context.scale;
        }

        public virtual void Import() { }
    }

    
}
