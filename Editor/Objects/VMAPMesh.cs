using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;
using Datamodel;
using DM = Datamodel.Datamodel;
using VMAP;

namespace VMAP {
    //represents a CMapMesh
    public class CMapMesh : MapObject {
        public CMapMesh (Element element, VMAPImportContext context) : base(element, context) { }

        public override void Import () {
            obj.name = "mesh " + element.ID.ToString();

            Element meshData = (Element)element["meshData"];
            string[] materialStrings;

            List<MeshConcaveTest.DebugDisplay> errors;

            Mesh mesh = Process(meshData, context, out materialStrings, out errors);

            context.assetContext.AddObjectToAsset(meshData.ID.ToString(), mesh);

            //process material list
            Material[] materials = new Material[materialStrings.Length];
            for (int i = 0; i < materials.Length; i++) {
                string mat = context.materialSearchPath + materialStrings[i].Replace("materials/", null).Replace(".vmat", null) + ".mat";
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>(mat);
            }

            MeshFilter f = obj.AddComponent<MeshFilter>();
            f.sharedMesh = mesh;
            MeshRenderer r = obj.AddComponent<MeshRenderer>();
            r.materials = materials;
            MeshCollider c = obj.AddComponent<MeshCollider>();
            c.sharedMesh = mesh;

            //debug code that displays edge loops for faces that failed to build.
            /*MeshConcaveTest d = obj.AddComponent<MeshConcaveTest>();
            d.debugList = errors;
            Debug.Log(errors.Count);*/
        }

        public class MeshData {
            public int[] vertexEdgeIndices;
            public int[] vertexDataIndices;
            public int[] edgeVertexIndices;
            public int[] edgeOppositeIndices;
            public int[] edgeNextIndices;
            public int[] edgeFaceIndices;
            public int[] edgeDataIndices;
            public int[] edgeVertexDataIndices;
            public int[] faceEdgeIndices;
            public int[] faceDataIndices;
            public string[] materials;

            public Vector3[] position;
            public Vector2[] texcoord;
            public Vector3[] normal;
            public Vector4[] tangent;
            public int[] materialIndex;

            public MeshData (Element element) {
                //helper methods
                

                //get the edge associated with a vert. (unknown use)
                vertexEdgeIndices = VMAP.Util.ConvertIntArray((Array<int>)element["vertexEdgeIndices"]);
                //index list of verts
                vertexDataIndices = VMAP.Util.ConvertIntArray((Array<int>)element["vertexDataIndices"]);
                //edgeVertex tells us which two verts an edge is between.
                edgeVertexIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeVertexIndices"]);
                //unknown use
                edgeOppositeIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeOppositeIndices"]);
                //edgeNext tells us that, if we start at vertex edgeVertexIndices[i], the next vertex in that face is edgeVertexIndices[edgeNextIndices[i]].
                edgeNextIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeNextIndices"]);
                //edgeFace tells us which two faces an edge is between. Always corresponds by index to a loop of edges (i.e. when using edgeNextIndices to cycle all verts in a face.)
                edgeFaceIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeFaceIndices"]);
                //get edge associated with edge array index
                edgeDataIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeDataIndices"]);
                //unknown use
                edgeVertexDataIndices = VMAP.Util.ConvertIntArray((Array<int>)element["edgeVertexDataIndices"]);
                //get an edge array index based on a face index.
                faceEdgeIndices = VMAP.Util.ConvertIntArray((Array<int>)element["faceEdgeIndices"]);
                //list of all faces. we just grab this to know how many faces to expect.
                faceDataIndices = VMAP.Util.ConvertIntArray((Array<int>)element["faceDataIndices"]);
                //list of all materials.
                materials = VMAP.Util.ConvertStringArray((Array<string>)element["materials"]);

                //grab data stream elements
                Element vertexData = (Element)element["vertexData"];
                Element faceVertexData = (Element)element["faceVertexData"];
                Element faceData = (Element)element["faceData"];

                //loop through data streams to grab additional info
                foreach (Element datastream in (Array<Element>)vertexData["streams"]) {
                    if (datastream.Name.Equals("position:0"))
                        position = VMAP.Util.ConvertV3Array((Array<System.Numerics.Vector3>)datastream["data"]);
                }

                foreach (Element datastream in (Array<Element>)faceVertexData["streams"]) {
                    if (datastream.Name.Equals("texcoord:0"))
                        texcoord = VMAP.Util.ConvertV2Array((Array<System.Numerics.Vector2>)datastream["data"]);
                    if (datastream.Name.Equals("normal:0"))
                        normal = VMAP.Util.ConvertV3Array((Array<System.Numerics.Vector3>)datastream["data"]);
                    if (datastream.Name.Equals("tangent:0"))
                        tangent = VMAP.Util.ConvertV4Array((Array<System.Numerics.Vector4>)datastream["data"]);
                }

                foreach (Element datastream in (Array<Element>)faceData["streams"]) {
                    if (datastream.Name.Equals("materialindex:0"))
                        materialIndex = VMAP.Util.ConvertIntArray((Array<int>)datastream["data"]);
                }
            }
        }

        //turn a CDmePolygonMesh element into a Unity Mesh.
        public static Mesh Process (Element element, VMAPImportContext context, out string[] newMaterials, out List<MeshConcaveTest.DebugDisplay> errors) {
            //if any mesh faces fail to build, put the info in this list.
            errors = new List<MeshConcaveTest.DebugDisplay>();

            Mesh m = new Mesh();

            MeshData md = new MeshData(element);

            //we have finally harvested all of our data. now build the mesh

            List<Vector3> newVerts = new List<Vector3>();
            List<Vector2> newUV = new List<Vector2>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector4> newTangents = new List<Vector4>();

            //we need one submesh for every material in the mesh. write triangles to the appropriate newTris[i] based on materialIndex.
            m.subMeshCount = md.materials.Length;
            List<int>[] newTris = new List<int>[m.subMeshCount];
            for (int i = 0; i < m.subMeshCount; i++) { newTris[i] = new List<int>(); }

            //run this loop on every face.
            foreach (int face in md.faceDataIndices) {
                //list of all verts in this face. this is unity index, not DMX index.
                List<int> vertsInFace = new List<int>();
                //number of verts in face.
                int vertCount = 0;
                foreach (int f in md.edgeFaceIndices) { if (f == face) vertCount++; }
                //index of edgevert array to start on. this changes every loop as we circle around the verts of the face.
                int vertLoopIndex = md.faceEdgeIndices[face];

                //submesh to write to. each submesh is one material, so write to newTris[] based on materialIndex.
                int subMesh = md.materialIndex[face];

                for (int i = 0; i < vertCount; i++) {
                    int vertIndex = md.edgeVertexIndices[vertLoopIndex];
                    int edgeIndex = md.edgeVertexDataIndices[vertLoopIndex];

                    Vector3 p = new Vector3(-md.position[vertIndex].y, md.position[vertIndex].z, md.position[vertIndex].x) * context.scale;
                    //Vector3 p = new Vector3(position[dataIndex].X, position[dataIndex].Y, position[index].Z) * context.scale;
                    Vector2 uv = new Vector2(md.texcoord[edgeIndex].x, md.texcoord[edgeIndex].y);
                    Vector3 n = new Vector3(-md.normal[edgeIndex].y, md.normal[edgeIndex].z, md.normal[edgeIndex].x);
                    //Vector4 t = new Vector4(tangent[index].W, tangent[index].X, tangent[index].Y, tangent[index].Z);

                    newVerts.Add(p);
                    newUV.Add(uv);
                    newNormals.Add(n);
                    //newTangents.Add(t);

                    vertsInFace.Add(newVerts.Count - 1);

                    vertLoopIndex = md.edgeNextIndices[vertLoopIndex];
                }

                //we now have all the verts in the face. turn that into triangles.

                bool concave = true;

                if (!concave) {
                    for (int i = 0; i < vertsInFace.Count - 2; i++) {
                        newTris[subMesh].Add(vertsInFace[0]);
                        newTris[subMesh].Add(vertsInFace[i + 2]);
                        newTris[subMesh].Add(vertsInFace[i + 1]);
                    }
                } else {
                    

                    //TO GET 2D POLYGON:
                    //find best-fit plane of all points
                    //project all points onto the plane to flatten it
                    //take any vector on the plane and call this the "x axis"
                    //take the cross product of the normal and the "x axis" to get the "y axis"
                    //project both of these axes for each point and measure the magnitude to generate new x,y coordinates for each vert

                    //get list of verts as points in 3d space, not as indexes.
                    List<Vector3> v3D = new List<Vector3>();
                    foreach(int i in vertsInFace) {
                        v3D.Add(newVerts[i]);
                    }

                    Plane bestfit = BestFitPlane(v3D.ToArray());
                    v3D = FlattenToPlane(v3D.ToArray(), bestfit);

                    //TO MAKE TRIANGLES:
                    // LOOP:
                    //  1) Pick point "x"
                    //  2) Grab points at x-1 and x+1 to form a triangle
                    //  3) VERIFY:
                    //   A) Interior of triangle contains no points from list
                    //   B) Triangle is inside polygon (Verify that point "x" is a convex angle rather than concave)
                    //  4) If triangle passes tests, add the triangle to the mesh, and remove "x" from the list.
                    // END LOOP when list of points has fewer than 3 points, indicating all triangles have been made.

                    //TODO: Rework v to be a list of structs that track their index so we can track things properly as v gets culled during polygon creation

                    Vector3 ax, ay;
                    List<Vector2> v = ReorientTo2D(v3D.ToArray(), bestfit, out ax, out ay);                    
                    int size = v.Count;
                    List<int> indexList = new List<int>();
                    for (int i = 0; i < size; i++) { indexList.Add(i); }

                    MeshConcaveTest.DebugDisplay error = new MeshConcaveTest.DebugDisplay(bestfit.normal, bestfit.distance, v3D, new List<Vector2>(v.ToArray()), ax, ay);

                    //thanks for triangle detection
                    //https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle

                    bool IsConcave (Vector2 prev, Vector2 current, Vector2 next, Winding w) {
                        float angle = PointAngle(prev, current, next);
                        return (w == Winding.CW) ? angle <= 0 : angle >= 0;   //treat an angle of 0 to be concave, we want to skip it like any other concave angle
                    }

                    float PointAngle (Vector2 prev, Vector2 current, Vector2 next) {
                        Vector2 first = current - prev;
                        Vector2 second = next - current;
                        return Vector3.SignedAngle(first.normalized, second.normalized, Vector3.forward);
                    }

                    float sign (Vector2 p1, Vector2 p2, Vector2 p3) {
                        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
                    }

                    //when we find the best-fit plane, the normal direction might be flipped from what we want, and as such the ReorientTo2D process might switch the CW/CWW order of verts.
                    //we can figure that out by taking a total of all angles in the shape and seeing if its +360 or -360.
                    //i actually have no idea if +360 is CW or CCW. ultimately doesn't matter, the flag just exists to flip the IsConcave function. figure this out later if we need the winding for something, idk
                    Winding winding;
                    float totalAngle = 0f;
                    for (int i = 0; i < v.Count; i++) {
                        int prev = (i == 0) ? v.Count - 1 : i - 1;
                        int next = (i + 1) % v.Count;
                        totalAngle += PointAngle(v[prev], v[i], v[next]);
                    }
                    winding = (totalAngle > 0) ? Winding.CW : Winding.CCW;

                    int safeguard = 9999;
                    int index = 0;

                    //loop through verts
                    while (safeguard > 0 && v.Count > 2) {
                        size = v.Count; //recalc size every time, just in case.
                        index = (index + 1) % size;
                        safeguard--;

                        //get indices of previous and next verts
                        int prev = (index == 0) ? size - 1 : index - 1;
                        int next = (index + 1) % size;

                        //skip concave angles
                        if (IsConcave(v[prev], v[index], v[next], winding)) continue;

                        //check if triangle is clear
                        bool triangleClear = true;
                        for (int i = 0; i < size; i++) {
                            if (i == index || i == prev || i == next) continue;

                            float d1, d2, d3;

                            d1 = sign(v[i], v[prev],  v[index]);
                            d2 = sign(v[i], v[index], v[next]);
                            d3 = sign(v[i], v[next],  v[prev]);

                            bool has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                            bool has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

                            if (!(has_neg && has_pos)) triangleClear = false;
                        }

                        //if we pass all checks, make triangle and remove current point from list
                        if (triangleClear) {
                            newTris[subMesh].Add(vertsInFace[indexList[prev]]);
                            newTris[subMesh].Add(vertsInFace[indexList[next]]);
                            newTris[subMesh].Add(vertsInFace[indexList[index]]);

                            v.RemoveAt(index);
                            indexList.RemoveAt(index);
                        }
                    }

                    if (safeguard <= 0) {
                        errors.Add(error);
                    }
                }
            }

            m.SetVertices(newVerts);
            m.SetUVs(0, newUV);
            m.SetNormals(newNormals);
            //m.SetTangents(newTangents);
            //m.RecalculateNormals();
            m.RecalculateTangents();
            for (int i = 0; i < newTris.Length; i++) { m.SetTriangles(newTris[i], i); }

            newMaterials = new string[md.materials.Length];
            for (int i = 0; i < md.materials.Length; i++) { newMaterials[i] = md.materials[i]; }

            return m;
        }

        enum Winding {
            CW,
            CCW
        }

        public static Plane BestFitPlane (Vector3[] verts) {
            //Find best-fit plane for all verts in face
            //thanks: https://stackoverflow.com/questions/29356594/fitting-a-plane-to-a-set-of-points-using-singular-value-decomposition

            Vector3 centroid = Vector3.zero;
            foreach (Vector3 v in verts) { centroid += v; }
            centroid /= verts.Length;

            int size = verts.Length;

            double[,] dataMat = new double[3, size];
            double[] w = new double[3];
            double[,] u = new double[3, 3];
            double[,] vt = new double[size, size];

            for (int i = 0; i < size; i++) {
                dataMat[0, i] = verts[i].x - centroid.x;
                dataMat[1, i] = verts[i].y - centroid.y;
                dataMat[2, i] = verts[i].z - centroid.z;
            }

            bool a = alglib.svd.rmatrixsvd(dataMat, 3, size, 1, 1, 2, ref w, ref u, ref vt, null);

            Vector3 planeNormal = new Vector3((float)u[0, 2], (float)u[1, 2], (float)u[2, 2]);
            Plane plane = new Plane(planeNormal, centroid);
            //Debug.Log(plane);
            return plane;
        }

        public static List<Vector3> FlattenToPlane (Vector3[] verts, Plane plane) {
            for (int i = 0; i < verts.Length; i++) {
                verts[i] = plane.ClosestPointOnPlane(verts[i]);
            }

            return new List<Vector3>(verts);
        }

        public static List<Vector2> ReorientTo2D (Vector3[] verts, Plane plane) { Vector3 x, y; return ReorientTo2D(verts, plane, out x, out y); }

        public static List<Vector2> ReorientTo2D (Vector3[] verts, Plane plane, out Vector3 axisX, out Vector3 axisY) {
            //our method:
            //take any vector on the plane and call this the "x axis"
            //take the cross product of the normal and the "x axis" to get the "y axis"
            //project both of these axes for each point and measure the magnitude to generate new x,y coordinates for each vert

            //define axes
            axisX = (verts[0] - verts[1]).normalized;    //this is COMPLETELY arbitrary and could be any vector coplanar to the plane.
            axisY = Vector3.Cross(axisX, plane.normal).normalized;

            //project to get x,y coords
            List<Vector2> output = new List<Vector2>();
            foreach (Vector3 v in verts) {
                Vector3 xv = Vector3.Project(v, axisX);
                Vector3 yv = Vector3.Project(v, axisY);

                //if the vector only got longer, use magnitude. if it flipped the opposite direction, use -magnitude.
                float x = (Vector3.Distance(axisX, xv.normalized) > Vector3.Distance(-axisX, xv.normalized)) ? xv.magnitude : -xv.magnitude;
                float y = (Vector3.Distance(axisY, yv.normalized) > Vector3.Distance(-axisY, yv.normalized)) ? yv.magnitude : -yv.magnitude;

                output.Add(new Vector2(x, y));
            }

            if (verts.Length != output.Count) Debug.LogError("Mismatch of input and output vert counts when reorienting face to 2D.");

            return output;
        }
    }
}
