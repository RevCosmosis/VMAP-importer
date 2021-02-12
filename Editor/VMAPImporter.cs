using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using Datamodel;
using DM = Datamodel.Datamodel;
using VMAP;

[ScriptedImporter(1, "vmap")]
public class VMAPImporter : ScriptedImporter
{
    public bool importMeshes = true;
    public bool importEntities = true;

    public float scaleFromInches = 128;
    public float scaleToMeters = 3;

    public string propSearchPath = "Assets/Props/";
    public string materialSearchPath = "Assets/Materials/";

    public override void OnImportAsset (AssetImportContext ctx) {
        VMAPImportContext VMAPctx = new VMAPImportContext(ctx, scaleToMeters / scaleFromInches, propSearchPath, materialSearchPath);

        //TESTING ONLY LOL
        //if (!ctx.assetPath.Contains("importtest")) return;

        FileStream filestream = System.IO.File.OpenRead(ctx.assetPath);
        DM dm = DM.Load(filestream);



        //Element world = (Element)dm.Root["world"];
        //Array<Element> objects = (Array<Element>)world["children"];
        DM.ElementList objects = dm.AllElements;
        List<MapObject> mapObjects = new List<MapObject>();

        //this loop runs on every entity and mesh in the file. implement your import code here!
        foreach (Element element in objects) {
            switch (element.ClassName) {
                case "CMapEntity":
                    if (!importEntities) continue;

                    AttributeList properties = (AttributeList)element["entity_properties"];
                    string classname = (string)properties["classname"];

                    //ADD NEW ENTITY IMPORT CLASSES HERE/////////////////////////////////////////////////////////////////////////////////////////////
                    switch (classname) {
                        case "prop_static":
                            mapObjects.Add(new prop_static(element, VMAPctx));
                            break;
                        //case "light_omni":
                            //break;
                        //case "light_spot":
                            //break;
                        default:
                            Debug.Log("Encountered entity with no import class: " + classname);
                            break;
                    }
                    break;
                    
                case "CMapMesh":
                    if (!importMeshes) continue;

                    mapObjects.Add(new CMapMesh(element, VMAPctx));
                    break;

                case "CMapGroup":
                    mapObjects.Add(new CMapGroup(element, VMAPctx));
                    break;

                case "CMapInstance":
                    mapObjects.Add(new CMapInstance(element, VMAPctx));
                    break;
            }
        }

        //assemble hierarchy!
        
        //assign children
        foreach (MapObject obj in mapObjects) {
            //ctx.AddObjectToAsset(obj.element.ID.ToString(), obj.obj);

            foreach (System.Guid guid in obj.children) {
                MapObject child = mapObjects.Find(x => x.element.ID.Equals(guid));
                if (child != null) child.obj.transform.SetParent(obj.obj.transform);
            }
        }

        //handle instances
        foreach (MapObject instance in mapObjects) {
            if (!(instance is CMapInstance)) continue;

            System.Guid guid = ((Element)instance.element["target"]).ID;
            MapObject target = mapObjects.Find(x => x.element.ID.Equals(guid));
            if (target == null) continue;

            target.obj.SetActive(false);

            GameObject newObj = Instantiate(target.obj);
            newObj.transform.SetParent(instance.obj.transform);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            newObj.transform.localScale = Vector3.one;
            newObj.SetActive(true);
        }

        GameObject prefab = new GameObject();

        //assign all top-level objects to be children of the prefab
        foreach (MapObject obj in mapObjects) {
            if (obj.obj.transform.parent == null) {
                obj.obj.transform.SetParent(prefab.transform);
            }

            //obj.ResetTransformWorld();
        }

        ctx.AddObjectToAsset("Main Object", prefab);
        ctx.SetMainObject(prefab);

        filestream.Close();
    }
}
